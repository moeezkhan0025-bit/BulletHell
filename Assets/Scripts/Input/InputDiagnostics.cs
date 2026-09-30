using System;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BulletHell.Input
{
    /// <summary>
    /// Diagnostics for the input readers; it never changes what input does. Handlers raise their events through
    /// <see cref="Raise(MonoBehaviour, string, in InputAction.CallbackContext, Action)"/>, which rethrows anything a subscriber throws after logging
    /// the script, the object's state and the input event being processed. It also remembers the last input event our readers
    /// handled, and in the Editor appends that to any NullReferenceException that comes out of the Input System package's own
    /// update loop (BUGS.md: "Input System NullReferenceException in InputEvent.get_handled") so a repeat comes with something to go on.
    /// </summary>
    public static class InputDiagnostics
    {
        private static string lastHandler = "(none yet)";
        private static string lastAction = "";
        private static int lastFrame = -1;
        private static double lastTime;

        /// <summary>Raises an event from an input callback; a throwing subscriber is logged with context, then the exception continues as before.</summary>
        public static void Raise(MonoBehaviour owner, string handler, in InputAction.CallbackContext context, Action callback)
        {
            Remember(handler, context);
            if (callback == null)
                return;
            try
            {
                callback();
            }
            catch (Exception exception)
            {
                Report(owner, handler, context, exception);
                throw;
            }
        }

        /// <summary>Same for an event that carries one int (the ammo slot).</summary>
        public static void Raise(MonoBehaviour owner, string handler, in InputAction.CallbackContext context, Action<int> callback, int value)
        {
            Remember(handler, context);
            if (callback == null)
                return;
            try
            {
                callback(value);
            }
            catch (Exception exception)
            {
                Report(owner, handler, context, exception);
                throw;
            }
        }

        /// <summary>Logs a failure in a reader's own lifecycle method (OnEnable, OnDisable, OnDestroy). Call from a catch, then rethrow.</summary>
        public static void ReportLifecycle(MonoBehaviour owner, string method, Exception exception) =>
            Report(owner, method, default, exception);

        private static void Remember(string handler, in InputAction.CallbackContext context)
        {
            lastHandler = handler;
            lastAction = ActionName(context);
            lastFrame = Time.frameCount;
            lastTime = context.time;
        }

        private static void Report(MonoBehaviour owner, string handler, in InputAction.CallbackContext context, Exception exception)
        {
            var text = new StringBuilder(384);
            text.Append("[InputDiagnostics] ").Append(owner != null ? owner.GetType().Name : "(destroyed)").Append('.').Append(handler)
                .Append(" threw ").Append(exception.GetType().Name).Append(": ").Append(exception.Message).Append('\n');
            try
            {
                if (owner != null)
                    text.Append("  object='").Append(owner.name).Append("' activeAndEnabled=").Append(owner.isActiveAndEnabled)
                        .Append(" scene='").Append(owner.gameObject.scene.name).Append("'\n");
                text.Append("  frame=").Append(Time.frameCount).Append(" playing=").Append(Application.isPlaying)
                    .Append(" timeScale=").Append(Time.timeScale).Append('\n');
                text.Append("  input: action='").Append(ActionName(context)).Append("' phase=").Append(context.action != null ? context.phase.ToString() : "n/a");
                if (context.action != null && context.control != null)
                    text.Append(" control='").Append(context.control.path).Append("' device='").Append(context.control.device?.displayName).Append('\'');
                text.Append('\n');
            }
            catch (Exception)
            {
                text.Append("  (state could not be read)\n");
            }
            Debug.LogError(text.ToString(), owner);
        }

        private static string ActionName(in InputAction.CallbackContext context)
        {
            try
            {
                return context.action != null ? context.action.name : "";
            }
            catch (Exception)
            {
                return "?";
            }
        }

#if UNITY_EDITOR
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Install()
        {
            Application.logMessageReceived -= OnLog;
            Application.logMessageReceived += OnLog;
        }

        // Only the package's own failure gets the extra line.
        private static void OnLog(string condition, string stackTrace, LogType type)
        {
            if (type != LogType.Exception || stackTrace == null || !stackTrace.Contains("InputEvent.get_handled"))
                return;
            Debug.LogWarning("[InputDiagnostics] Input System exception seen. Last input our readers handled: '" + lastHandler + "' (action '" + lastAction
                             + "') at frame " + lastFrame + ", input time " + lastTime.ToString("F3") + "; now frame " + Time.frameCount
                             + ", editor paused=" + UnityEditor.EditorApplication.isPaused + ", app focused=" + UnityEditorInternal.InternalEditorUtility.isApplicationActive
                             + ". Note what you did just before and whether Play was paused.");
        }
#endif
    }
}
