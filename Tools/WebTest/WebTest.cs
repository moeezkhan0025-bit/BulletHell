// D6 browser test harness (run with: dotnet run Tools/WebTest/WebTest.cs -- <buildFolder> [httpPort] [controlPort]).
//
// A small tool with no dependencies beyond the .NET SDK. It serves a WebGL build folder over http://localhost:<httpPort>/, launches
// Chrome with the DevTools protocol open, and exposes a control API on http://localhost:<controlPort>/ so a script (curl) can drive
// the game like a player: load it and time it, press keys, click, hold a pretend gamepad, read the console, take screenshots, read
// what the page's audio is doing. There is no real controller in a scripted Chrome (the browser only reveals a gamepad after a
// physical button press), so the gamepad is a stand-in object injected in place of navigator.getGamepads().
//
//   /launch?path=index.html&w=1920&h=1080   start Chrome (once) and open the page      /nav?path=index.html?perfstress
//   /eval   (POST the JavaScript as the body)  evaluate in the page, returns JSON       /shot?file=C:/x.png
//   /logs?since=0                               console lines from index `since`       /key?k=ArrowDown&t=press|down|up
//   /click?x=960&y=540                          mouse click                            /pad?axes=0,0,0,0&btn=0,9&dpad=...
//   /audio                                      what the page's audio context is doing /quit
using System.Diagnostics;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

string buildFolder = Path.GetFullPath(args.Length > 0 ? args[0] : ".");
int httpPort = args.Length > 1 ? int.Parse(args[1]) : 8765;
int controlPort = args.Length > 2 ? int.Parse(args[2]) : 8766;
const int DevToolsPort = 9333;

var logs = new List<string>();
var logLock = new object();
var pending = new Dictionary<int, TaskCompletionSource<JsonNode?>>();
int nextId = 0;
ClientWebSocket? socket = null;
Process? chrome = null;
string profileDir = Path.Combine(Path.GetTempPath(), "voxweb_profile_" + Guid.NewGuid().ToString("N")[..8]);

// ---------------------------------------------------------------- static file server for the build

string Mime(string file) => Path.GetExtension(file).ToLowerInvariant() switch
{
    ".html" => "text/html", ".js" => "application/javascript", ".wasm" => "application/wasm", ".json" => "application/json",
    ".css" => "text/css", ".png" => "image/png", ".ico" => "image/x-icon", ".data" => "application/octet-stream",
    ".unityweb" => "application/octet-stream", ".gz" => "application/octet-stream", ".br" => "application/octet-stream",
    _ => "application/octet-stream",
};

var site = new HttpListener();
site.Prefixes.Add($"http://localhost:{httpPort}/");
site.Start();
_ = Task.Run(async () =>
{
    while (true)
    {
        var ctx = await site.GetContextAsync();
        _ = Task.Run(() =>
        {
            try
            {
                string rel = Uri.UnescapeDataString(ctx.Request.Url!.AbsolutePath.TrimStart('/'));
                if (rel.Length == 0) rel = "index.html";
                string file = Path.GetFullPath(Path.Combine(buildFolder, rel));
                if (!file.StartsWith(buildFolder) || !File.Exists(file)) { ctx.Response.StatusCode = 404; ctx.Response.Close(); return; }
                string name = Path.GetFileName(file);
                // Files named *.gz / *.br are what a real host (itch.io) serves with a Content-Encoding header.
                string encoded = name.EndsWith(".gz") ? "gzip" : name.EndsWith(".br") ? "br" : "";
                string typeName = encoded.Length > 0 ? name[..name.LastIndexOf('.')] : name;
                ctx.Response.ContentType = Mime(typeName);
                if (encoded.Length > 0) ctx.Response.AddHeader("Content-Encoding", encoded);
                ctx.Response.AddHeader("Cache-Control", "no-cache");
                var bytes = File.ReadAllBytes(file);
                ctx.Response.ContentLength64 = bytes.Length;
                ctx.Response.OutputStream.Write(bytes, 0, bytes.Length);
                ctx.Response.Close();
            }
            catch { try { ctx.Response.Abort(); } catch { } }
        });
    }
});

// ---------------------------------------------------------------- DevTools protocol

async Task<JsonNode?> Cdp(string method, JsonObject? p = null, int timeoutMs = 30000)
{
    int id = Interlocked.Increment(ref nextId);
    var tcs = new TaskCompletionSource<JsonNode?>();
    lock (pending) pending[id] = tcs;
    var msg = new JsonObject { ["id"] = id, ["method"] = method, ["params"] = p ?? new JsonObject() };
    var data = Encoding.UTF8.GetBytes(msg.ToJsonString());
    await socket!.SendAsync(data, WebSocketMessageType.Text, true, CancellationToken.None);
    var done = await Task.WhenAny(tcs.Task, Task.Delay(timeoutMs));
    if (done != tcs.Task) throw new TimeoutException(method);
    return await tcs.Task;
}

async Task ReadLoop()
{
    var buffer = new byte[1 << 20];
    var sb = new StringBuilder();
    while (socket!.State == WebSocketState.Open)
    {
        WebSocketReceiveResult r;
        sb.Clear();
        do
        {
            r = await socket.ReceiveAsync(buffer, CancellationToken.None);
            if (r.MessageType == WebSocketMessageType.Close) return;
            sb.Append(Encoding.UTF8.GetString(buffer, 0, r.Count));
        } while (!r.EndOfMessage);
        var node = JsonNode.Parse(sb.ToString())!;
        if (node["id"] is JsonNode idNode)
        {
            TaskCompletionSource<JsonNode?>? tcs;
            lock (pending) { pending.Remove(idNode.GetValue<int>(), out tcs); }
            if (node["error"] != null) tcs?.TrySetException(new Exception(node["error"]!.ToJsonString()));
            else tcs?.TrySetResult(node["result"]);
        }
        else if (node["method"]?.GetValue<string>() == "Runtime.consoleAPICalled")
        {
            var parts = node["params"]!["args"]!.AsArray().Select(a => a!["value"]?.ToString() ?? a["description"]?.ToString() ?? "").ToArray();
            lock (logLock) logs.Add($"[{node["params"]!["type"]}] " + string.Join(" ", parts));
        }
        else if (node["method"]?.GetValue<string>() == "Runtime.exceptionThrown")
        {
            lock (logLock) logs.Add("[exception] " + node["params"]!["exceptionDetails"]!["text"] + " " + node["params"]!["exceptionDetails"]!["exception"]?["description"]);
        }
    }
}

// Injected before any page script runs: a stand-in gamepad, a tap on everything the page's audio sends to the speakers, and a load timer.
const string InitScript = @"
(() => {
  window.__pad = { id: 'Xbox 360 Controller (XInput STANDARD GAMEPAD)', index: 0, connected: true, mapping: 'standard',
    axes: [0, 0, 0, 0], timestamp: 0, hapticActuators: [],
    buttons: Array.from({ length: 17 }, () => ({ pressed: false, touched: false, value: 0 })) };
  window.__padEnabled = false;
  const real = navigator.getGamepads ? navigator.getGamepads.bind(navigator) : () => [];
  navigator.getGamepads = () => { if (!window.__padEnabled) return real(); window.__pad.timestamp = performance.now(); return [window.__pad, null, null, null]; };
  window.__setPad = (axes, pressed) => {
    window.__pad.axes = axes;
    window.__pad.buttons.forEach((b, i) => { const on = pressed.includes(i); b.pressed = on; b.touched = on; b.value = on ? 1 : 0; });
    if (!window.__padEnabled) { window.__padEnabled = true; { const ev = new Event('gamepadconnected'); ev.gamepad = window.__pad; window.dispatchEvent(ev); } }
  };
  window.__audio = { contexts: [], analyser: null, peak: 0, samples: 0 };
  const origConnect = AudioNode.prototype.connect;
  AudioNode.prototype.connect = function (dest, ...rest) {
    try {
      const ctx = this.context;
      if (!window.__audio.contexts.includes(ctx)) window.__audio.contexts.push(ctx);
      if (dest instanceof AudioDestinationNode) {
        if (!ctx.__tap) { ctx.__tap = ctx.createAnalyser(); ctx.__tap.fftSize = 2048; }
        origConnect.call(this, ctx.__tap);
      }
    } catch (e) {}
    return origConnect.call(this, dest, ...rest);
  };
  window.__audioLevel = () => {
    const out = { contexts: window.__audio.contexts.length, states: window.__audio.contexts.map(c => c.state), rms: 0 };
    for (const ctx of window.__audio.contexts) {
      if (!ctx.__tap) continue;
      const data = new Float32Array(ctx.__tap.fftSize); ctx.__tap.getFloatTimeDomainData(data);
      let sum = 0; for (const v of data) sum += v * v; out.rms = Math.max(out.rms, Math.sqrt(sum / data.length));
    }
    return out;
  };
  const loadTimer = setInterval(() => {
    const bar = document.querySelector('#unity-loading-bar');
    if (bar && bar.style.display === 'none' && !window.__loadedMs) { window.__loadedMs = performance.now(); clearInterval(loadTimer); }
  }, 25);
  let hooked;
  Object.defineProperty(window, 'createUnityInstance', {
    configurable: true,
    get() { return hooked; },
    set(fn) {
      hooked = function (...a) {
        window.__unityStartMs = performance.now();
        return fn.apply(this, a).then(inst => { window.__unityReadyMs = performance.now(); window.__unity = inst; return inst; });
      };
    },
  });
})();";

async Task Launch(string path, int w, int h)
{
    if (chrome == null)
    {
        string exe = @"C:\Program Files\Google\Chrome\Application\chrome.exe";
        chrome = Process.Start(new ProcessStartInfo(exe,
            $"--remote-debugging-port={DevToolsPort} --user-data-dir=\"{profileDir}\" --no-first-run --no-default-browser-check --disable-backgrounding-occluded-windows --disable-renderer-backgrounding --disable-background-timer-throttling --disable-features=CalculateNativeWinOcclusion --window-size={w},{h + 120} about:blank") { UseShellExecute = false });
        using var http = new HttpClient();
        string? wsUrl = null;
        for (int i = 0; i < 60 && wsUrl == null; i++)
        {
            await Task.Delay(500);
            try
            {
                var list = JsonNode.Parse(await http.GetStringAsync($"http://127.0.0.1:{DevToolsPort}/json"))!.AsArray();
                wsUrl = list.FirstOrDefault(t => t!["type"]!.GetValue<string>() == "page")?["webSocketDebuggerUrl"]?.GetValue<string>();
            }
            catch { }
        }
        if (wsUrl == null) throw new Exception("Chrome did not open its DevTools port");
        socket = new ClientWebSocket();
        await socket.ConnectAsync(new Uri(wsUrl), CancellationToken.None);
        _ = Task.Run(ReadLoop);
        await Cdp("Runtime.enable");
        await Cdp("Page.enable");
        await Cdp("Network.enable");
        await Cdp("Performance.enable");
        await Cdp("Emulation.setFocusEmulationEnabled", new JsonObject { ["enabled"] = true });
        await Cdp("Page.bringToFront");
        await Cdp("Page.addScriptToEvaluateOnNewDocument", new JsonObject { ["source"] = InitScript });
        await Cdp("Emulation.setDeviceMetricsOverride", new JsonObject { ["width"] = w, ["height"] = h, ["deviceScaleFactor"] = 1, ["mobile"] = false });
    }
    await Nav(path);
}

async Task Nav(string path)
{
    lock (logLock) logs.Add($"--- navigate {path}");
    await Cdp("Page.navigate", new JsonObject { ["url"] = $"http://localhost:{httpPort}/{path}" });
}

async Task<string> Eval(string js)
{
    var r = await Cdp("Runtime.evaluate", new JsonObject { ["expression"] = js, ["returnByValue"] = true, ["awaitPromise"] = true });
    if (r?["exceptionDetails"] != null) return "EXCEPTION " + r["exceptionDetails"]!.ToJsonString();
    return r?["result"]?["value"]?.ToJsonString() ?? r?["result"]?["description"]?.ToString() ?? "null";
}

// ---------------------------------------------------------------- control API

var control = new HttpListener();
control.Prefixes.Add($"http://localhost:{controlPort}/");
control.Start();
Console.WriteLine($"WebTest: build {buildFolder} on http://localhost:{httpPort}/ , control on http://localhost:{controlPort}/");

while (true)
{
    var ctx = await control.GetContextAsync();
    string reply;
    try
    {
        var q = ctx.Request.QueryString;
        switch (ctx.Request.Url!.AbsolutePath.Trim('/'))
        {
            case "launch": await Launch(q["path"] ?? "index.html", int.Parse(q["w"] ?? "1920"), int.Parse(q["h"] ?? "1080")); reply = "ok"; break;
            case "nav": await Nav(q["path"] ?? "index.html"); reply = "ok"; break;
            case "eval":
                using (var reader = new StreamReader(ctx.Request.InputStream, Encoding.UTF8)) reply = await Eval(await reader.ReadToEndAsync());
                break;
            case "logs":
                int since = int.Parse(q["since"] ?? "0");
                lock (logLock) reply = string.Join("\n", logs.Skip(since).Select((l, i) => $"{since + i}: {l}")) + $"\n(total {logs.Count})";
                break;
            case "shot":
                var shot = await Cdp("Page.captureScreenshot", new JsonObject { ["format"] = "png" });
                File.WriteAllBytes(q["file"]!, Convert.FromBase64String(shot!["data"]!.GetValue<string>()));
                reply = "saved " + q["file"];
                break;
            case "key":
            {
                string k = q["k"]!; string t = q["t"] ?? "press";
                var info = new JsonObject { ["key"] = k, ["code"] = q["code"] ?? (k.Length == 1 ? "Key" + k.ToUpper() : k), ["windowsVirtualKeyCode"] = int.Parse(q["vk"] ?? "0") };
                if (t is "press" or "down") { var d = (JsonObject)JsonNode.Parse(info.ToJsonString())!; d["type"] = "rawKeyDown"; await Cdp("Input.dispatchKeyEvent", d); }
                if (t == "press") await Task.Delay(int.Parse(q["ms"] ?? "80"));
                if (t is "press" or "up") { var u = (JsonObject)JsonNode.Parse(info.ToJsonString())!; u["type"] = "keyUp"; await Cdp("Input.dispatchKeyEvent", u); }
                reply = "ok"; break;
            }
            case "click":
                foreach (var type in new[] { "mouseMoved", "mousePressed", "mouseReleased" })
                    await Cdp("Input.dispatchMouseEvent", new JsonObject { ["type"] = type, ["x"] = double.Parse(q["x"]!), ["y"] = double.Parse(q["y"]!), ["button"] = "left", ["clickCount"] = 1 });
                reply = "ok"; break;
            case "pad":
                reply = await Eval($"window.__setPad([{q["axes"] ?? "0,0,0,0"}], [{q["btn"] ?? ""}]); 'ok'"); break;
            case "cache":
                await Cdp("Network.setCacheDisabled", new JsonObject { ["cacheDisabled"] = q["off"] == "1" });
                reply = "cache disabled=" + (q["off"] == "1"); break;
            case "throttle":
            {
                double mbps = double.Parse(q["mbps"] ?? "0");
                await Cdp("Network.emulateNetworkConditions", new JsonObject { ["offline"] = false, ["latency"] = double.Parse(q["latency"] ?? "0"),
                    ["downloadThroughput"] = mbps <= 0 ? -1 : mbps * 125000.0, ["uploadThroughput"] = mbps <= 0 ? -1 : mbps * 125000.0 });
                reply = "throttle " + mbps + " Mbit/s"; break;
            }
            case "viewport":
                await Cdp("Emulation.setDeviceMetricsOverride", new JsonObject { ["width"] = int.Parse(q["w"]!), ["height"] = int.Parse(q["h"]!), ["deviceScaleFactor"] = 1, ["mobile"] = false });
                reply = "viewport " + q["w"] + "x" + q["h"]; break;
            case "metrics":
                reply = (await Cdp("Performance.getMetrics"))!.ToJsonString(); break;
            case "audio": reply = await Eval("JSON.stringify(window.__audioLevel ? window.__audioLevel() : null)"); break;
            case "quit":
                reply = "bye";
                try { chrome?.Kill(true); } catch { }
                try { Directory.Delete(profileDir, true); } catch { }
                var bytesQuit = Encoding.UTF8.GetBytes(reply); ctx.Response.OutputStream.Write(bytesQuit); ctx.Response.Close();
                return;
            default: reply = "unknown command"; break;
        }
    }
    catch (Exception e) { reply = "ERROR " + e.Message; }
    var bytes = Encoding.UTF8.GetBytes(reply);
    ctx.Response.ContentLength64 = bytes.Length;
    ctx.Response.OutputStream.Write(bytes);
    ctx.Response.Close();
}
