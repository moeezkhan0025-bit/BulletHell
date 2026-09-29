using System.Collections.Generic;
using UnityEditor;
using UnityEditor.ShaderGraph;
using UnityEngine;

namespace BulletHell.EditorTools.ShaderGen
{
    /// <summary>
    /// The M8.6 shader library as real Shader Graph assets, all "Sprite Unlit" 2D graphs that read the SpriteRenderer's
    /// _MainTex and colour: Sprite_Character (everything a fighter needs at once), plus the single-purpose HitFlash, Dissolve,
    /// Outline, TintPalette, PulseGlow, UVScroll and Wave. Regenerate any time with BulletHell/M8.6/Generate Shaders.
    /// </summary>
    public static class ShaderDefinitions
    {
        public const string Folder = "Assets/Art/Shaders";

        [MenuItem("BulletHell/M8.6/Generate Shaders")]
        public static void GenerateAll()
        {
            var paths = new List<string>
            {
                Character(),
                HitFlash(),
                Dissolve(),
                Outline(),
                TintPalette(),
                PulseGlow(),
                UvScroll(),
                Wave(),
            };
            AssetDatabase.SaveAssets();
            Debug.Log("M8.6 shaders generated:\n" + string.Join("\n", paths));
        }

        // ---- shared pieces ---------------------------------------------------------------------------------

        // vec3 from a vec4 output (drops alpha).
        private static AbstractMaterialNode Rgb(GraphBuilder b, AbstractMaterialNode node, string output)
        {
            AbstractMaterialNode split = b.Node("Channel/Split");
            b.Link(node, output, split, "In");
            AbstractMaterialNode combine = b.Node("Channel/Combine");
            b.Link(split, "R", combine, "R");
            b.Link(split, "G", combine, "G");
            b.Link(split, "B", combine, "B");
            return combine; // output "RGB"
        }

        private static AbstractMaterialNode AlphaOf(GraphBuilder b, AbstractMaterialNode node, string output)
        {
            AbstractMaterialNode split = b.Node("Channel/Split");
            b.Link(node, output, split, "In");
            return split; // output "A"
        }

        private static AbstractMaterialNode Mix(GraphBuilder b, AbstractMaterialNode a, string aOut, AbstractMaterialNode to, string toOut,
                                                AbstractMaterialNode t, string tOut)
        {
            AbstractMaterialNode lerp = b.Node("Math/Interpolation/Lerp");
            b.Link(a, aOut, lerp, "A");
            b.Link(to, toOut, lerp, "B");
            b.Link(t, tOut, lerp, "T");
            return lerp; // "Out"
        }

        // Dissolve mask: 1 where the sprite survives, plus a thin rim band just above the threshold.
        private static void DissolveMask(GraphBuilder b, AbstractMaterialNode amount, AbstractMaterialNode scale, AbstractMaterialNode rimWidth,
                                         out AbstractMaterialNode cut, out AbstractMaterialNode rim)
        {
            AbstractMaterialNode uv = b.Node("Input/Geometry/UV");
            AbstractMaterialNode noise = b.Node("Procedural/Noise/Simple Noise");
            b.Link(uv, "Out", noise, "UV");
            b.Link(scale, "*", noise, "Scale");

            AbstractMaterialNode step = b.Node("Math/Round/Step");
            b.Link(amount, "*", step, "Edge");
            b.Link(noise, "Out", step, "In");

            AbstractMaterialNode upper = b.Node("Math/Basic/Add");
            b.Link(amount, "*", upper, "A");
            b.Link(rimWidth, "*", upper, "B");
            AbstractMaterialNode stepUpper = b.Node("Math/Round/Step");
            b.Link(upper, "Out", stepUpper, "Edge");
            b.Link(noise, "Out", stepUpper, "In");

            AbstractMaterialNode band = b.Node("Math/Basic/Subtract");
            b.Link(step, "Out", band, "A");
            b.Link(stepUpper, "Out", band, "B");
            cut = step;
            rim = band;
        }

        // ---- graphs ----------------------------------------------------------------------------------------

        private static string Character()
        {
            var b = new GraphBuilder();
            AbstractMaterialNode tintColor = b.ColorProperty("Tint Color", "_TintColor", Color.white);
            AbstractMaterialNode tintAmount = b.FloatProperty("Tint Amount", "_TintAmount", 0f);
            AbstractMaterialNode dangerColor = b.ColorProperty("Danger Color", "_DangerColor", new Color(1f, 0.2f, 0.55f));
            AbstractMaterialNode dangerAmount = b.FloatProperty("Danger Amount", "_DangerAmount", 0f);
            AbstractMaterialNode flashColor = b.ColorProperty("Flash Color", "_FlashColor", Color.white);
            AbstractMaterialNode flashAmount = b.FloatProperty("Flash Amount", "_FlashAmount", 0f);
            AbstractMaterialNode dissolveAmount = b.FloatProperty("Dissolve Amount", "_DissolveAmount", 0f);
            AbstractMaterialNode noiseScale = b.FloatProperty("Noise Scale", "_NoiseScale", 30f);
            AbstractMaterialNode rimWidth = b.FloatProperty("Rim Width", "_RimWidth", 0.08f);

            AbstractMaterialNode baseRgb = Rgb(b, b.Tinted, "Out");
            AbstractMaterialNode alpha = AlphaOf(b, b.Tinted, "Out");
            AbstractMaterialNode tintRgb = Rgb(b, tintColor, "*");
            AbstractMaterialNode dangerRgb = Rgb(b, dangerColor, "*");
            AbstractMaterialNode flashRgb = Rgb(b, flashColor, "*");

            AbstractMaterialNode tinted = Mix(b, baseRgb, "RGB", tintRgb, "RGB", tintAmount, "*");
            AbstractMaterialNode danger = Mix(b, tinted, "Out", dangerRgb, "RGB", dangerAmount, "*");

            DissolveMask(b, dissolveAmount, noiseScale, rimWidth, out AbstractMaterialNode cut, out AbstractMaterialNode rim);
            AbstractMaterialNode rimmed = Mix(b, danger, "Out", flashRgb, "RGB", rim, "Out");
            AbstractMaterialNode flashed = Mix(b, rimmed, "Out", flashRgb, "RGB", flashAmount, "*");

            AbstractMaterialNode outAlpha = b.Math("Math/Basic/Multiply", alpha, "A", cut, "Out");
            b.Output(flashed, "Out", outAlpha, "Out");
            return b.Save(Folder + "/Sprite_Character.shadergraph");
        }

        private static string HitFlash()
        {
            var b = new GraphBuilder();
            AbstractMaterialNode flashColor = b.ColorProperty("Flash Color", "_FlashColor", Color.white);
            AbstractMaterialNode flashAmount = b.FloatProperty("Flash Amount", "_FlashAmount", 0f);
            AbstractMaterialNode rgb = Rgb(b, b.Tinted, "Out");
            AbstractMaterialNode alpha = AlphaOf(b, b.Tinted, "Out");
            AbstractMaterialNode flashRgb = Rgb(b, flashColor, "*");
            AbstractMaterialNode mixed = Mix(b, rgb, "RGB", flashRgb, "RGB", flashAmount, "*");
            b.Output(mixed, "Out", alpha, "A");
            return b.Save(Folder + "/Sprite_HitFlash.shadergraph");
        }

        private static string Dissolve()
        {
            var b = new GraphBuilder();
            AbstractMaterialNode amount = b.FloatProperty("Dissolve Amount", "_DissolveAmount", 0f);
            AbstractMaterialNode scale = b.FloatProperty("Noise Scale", "_NoiseScale", 30f);
            AbstractMaterialNode rimWidth = b.FloatProperty("Rim Width", "_RimWidth", 0.08f);
            AbstractMaterialNode edgeColor = b.ColorProperty("Edge Color", "_EdgeColor", new Color(1f, 0.85f, 0.3f), hdr: true);
            AbstractMaterialNode rgb = Rgb(b, b.Tinted, "Out");
            AbstractMaterialNode alpha = AlphaOf(b, b.Tinted, "Out");
            AbstractMaterialNode edgeRgb = Rgb(b, edgeColor, "*");
            DissolveMask(b, amount, scale, rimWidth, out AbstractMaterialNode cut, out AbstractMaterialNode rim);
            AbstractMaterialNode mixed = Mix(b, rgb, "RGB", edgeRgb, "RGB", rim, "Out");
            AbstractMaterialNode outAlpha = b.Math("Math/Basic/Multiply", alpha, "A", cut, "Out");
            b.Output(mixed, "Out", outAlpha, "Out");
            return b.Save(Folder + "/Sprite_Dissolve.shadergraph");
        }

        // Grows a coloured outline around the opaque pixels. Needs transparent padding around the art (ART_SPEC: 4 px) and a
        // Full Rect sprite mesh so the outline isn't clipped. _OutlineWidth is in texels; width 0 = no outline.
        private static string Outline()
        {
            var b = new GraphBuilder();
            AbstractMaterialNode color = b.ColorProperty("Outline Color", "_OutlineColor", Color.white);
            AbstractMaterialNode width = b.FloatProperty("Outline Width", "_OutlineWidth", 0f);

            AbstractMaterialNode size = b.Node("Input/Texture/Texture Size");
            b.Link(b.MainTexture, "*", size, "Texture");

            AbstractMaterialNode uv = b.Node("Input/Geometry/UV");
            AbstractMaterialNode texelW = b.Math("Math/Basic/Multiply", size, "Texel Width", width, "*");
            AbstractMaterialNode texelH = b.Math("Math/Basic/Multiply", size, "Texel Height", width, "*");

            AbstractMaterialNode maxAlpha = null;
            string[] dirs = { "+0", "-0", "0+", "0-", "++", "--", "+-", "-+" };
            foreach (string d in dirs)
            {
                float sx = d[0] == '+' ? 1f : d[0] == '-' ? -1f : 0f;
                float sy = d[1] == '+' ? 1f : d[1] == '-' ? -1f : 0f;
                AbstractMaterialNode offset = b.Node("Channel/Combine");
                b.Link(OffsetAxis(b, texelW, sx), "Out", offset, "R");
                b.Link(OffsetAxis(b, texelH, sy), "Out", offset, "G");
                AbstractMaterialNode shifted = b.Math("Math/Basic/Add", uv, "Out", offset, "RG");
                AbstractMaterialNode sample = b.Node("Input/Texture/Sample Texture 2D");
                b.Link(b.MainTexture, "*", sample, "Texture");
                b.Link(shifted, "Out", sample, "UV");
                b.Link(b.Sampler, "Out", sample, "Sampler");
                maxAlpha = maxAlpha == null
                    ? AlphaOf(b, sample, "RGBA")
                    : b.Math("Math/Range/Maximum", maxAlpha, maxAlpha.name == "Maximum" ? "Out" : "A", AlphaOf(b, sample, "RGBA"), "A");
            }
            return OutlineFinish(b, color, maxAlpha);
        }

        private static AbstractMaterialNode OffsetAxis(GraphBuilder b, AbstractMaterialNode texel, float sign)
        {
            if (sign == 0f)
                return b.Float(0f);
            return b.Math("Math/Basic/Multiply", texel, "Out", b.Float(sign), "Out");
        }

        private static string OutlineFinish(GraphBuilder b, AbstractMaterialNode color, AbstractMaterialNode maxNeighbour)
        {
            AbstractMaterialNode rgb = Rgb(b, b.Tinted, "Out");
            AbstractMaterialNode alpha = AlphaOf(b, b.Tinted, "Out");
            AbstractMaterialNode neighbourAlpha = maxNeighbour;
            string neighbourOut = maxNeighbour.name == "Maximum" ? "Out" : "A";
            AbstractMaterialNode outlineRgb = Rgb(b, color, "*");
            AbstractMaterialNode outlineAlpha = AlphaOf(b, color, "*");

            // Outline appears where a neighbour is opaque but this pixel isn't: ring = neighbour * (1 - alpha).
            AbstractMaterialNode inverse = b.Node("Math/Range/One Minus");
            b.Link(alpha, "A", inverse, "In");
            AbstractMaterialNode ring = b.Math("Math/Basic/Multiply", neighbourAlpha, neighbourOut, inverse, "Out");
            AbstractMaterialNode ringStrength = b.Math("Math/Basic/Multiply", ring, "Out", outlineAlpha, "A");
            AbstractMaterialNode saturated = b.Node("Math/Range/Saturate");
            b.Link(ringStrength, "Out", saturated, "In");

            AbstractMaterialNode mixed = Mix(b, rgb, "RGB", outlineRgb, "RGB", saturated, "Out");
            AbstractMaterialNode finalAlpha = b.Math("Math/Range/Maximum", alpha, "A", saturated, "Out");
            b.Output(mixed, "Out", finalAlpha, "Out");
            return b.Save(Folder + "/Sprite_Outline.shadergraph");
        }

        private static string TintPalette()
        {
            var b = new GraphBuilder();
            AbstractMaterialNode dark = b.ColorProperty("Shadow Color", "_PaletteDark", new Color(0.2f, 0.1f, 0.3f));
            AbstractMaterialNode light = b.ColorProperty("Light Color", "_PaletteLight", new Color(1f, 0.8f, 0.9f));
            AbstractMaterialNode amount = b.FloatProperty("Amount", "_PaletteAmount", 1f);

            AbstractMaterialNode rgb = Rgb(b, b.Tinted, "Out");
            AbstractMaterialNode alpha = AlphaOf(b, b.Tinted, "Out");
            AbstractMaterialNode darkRgb = Rgb(b, dark, "*");
            AbstractMaterialNode lightRgb = Rgb(b, light, "*");

            // Luminance decides where along dark -> light a pixel lands, so shading in the art survives the swap.
            AbstractMaterialNode weights = b.Node("Channel/Combine");
            b.SetFloat(weights, "R", 0.299f);
            b.SetFloat(weights, "G", 0.587f);
            b.SetFloat(weights, "B", 0.114f);
            AbstractMaterialNode luma = b.Math("Math/Vector/Dot Product", rgb, "RGB", weights, "RGB");
            AbstractMaterialNode ramp = Mix(b, darkRgb, "RGB", lightRgb, "RGB", luma, "Out");
            AbstractMaterialNode mixed = Mix(b, rgb, "RGB", ramp, "Out", amount, "*");
            b.Output(mixed, "Out", alpha, "A");
            return b.Save(Folder + "/Sprite_TintPalette.shadergraph");
        }

        private static string PulseGlow()
        {
            var b = new GraphBuilder();
            AbstractMaterialNode glowColor = b.ColorProperty("Glow Color", "_GlowColor", new Color(1f, 0.5f, 0.1f), hdr: true);
            AbstractMaterialNode intensity = b.FloatProperty("Intensity", "_GlowIntensity", 0.6f);
            AbstractMaterialNode speed = b.FloatProperty("Speed", "_GlowSpeed", 6f);

            AbstractMaterialNode rgb = Rgb(b, b.Tinted, "Out");
            AbstractMaterialNode alpha = AlphaOf(b, b.Tinted, "Out");
            AbstractMaterialNode glowRgb = Rgb(b, glowColor, "*");

            AbstractMaterialNode time = b.Node("Input/Basic/Time");
            AbstractMaterialNode phase = b.Math("Math/Basic/Multiply", time, "Time", speed, "*");
            AbstractMaterialNode sine = b.Node("Math/Trigonometry/Sine");
            b.Link(phase, "Out", sine, "In");
            AbstractMaterialNode half = b.Math("Math/Basic/Multiply", sine, "Out", b.Float(0.5f), "Out");
            AbstractMaterialNode pulse = b.Math("Math/Basic/Add", half, "Out", b.Float(0.5f), "Out");
            AbstractMaterialNode strength = b.Math("Math/Basic/Multiply", pulse, "Out", intensity, "*");
            AbstractMaterialNode glow = b.Math("Math/Basic/Multiply", glowRgb, "RGB", strength, "Out");
            AbstractMaterialNode sum = b.Math("Math/Basic/Add", rgb, "RGB", glow, "Out");
            b.Output(sum, "Out", alpha, "A");
            return b.Save(Folder + "/Sprite_PulseGlow.shadergraph");
        }

        // Scrolls the texture's UV over time (laser beams, conveyors). The texture should use Wrap Mode: Repeat.
        private static string UvScroll()
        {
            var b = new GraphBuilder();
            AbstractMaterialNode speed = b.Vector2Property("Scroll Speed", "_ScrollSpeed", new Vector2(1f, 0f));
            AbstractMaterialNode uv = b.Node("Input/Geometry/UV");
            AbstractMaterialNode time = b.Node("Input/Basic/Time");
            AbstractMaterialNode offset = b.Math("Math/Basic/Multiply", speed, "*", time, "Time");
            AbstractMaterialNode moved = b.Math("Math/Basic/Add", uv, "Out", offset, "Out");
            AbstractMaterialNode sample = b.Node("Input/Texture/Sample Texture 2D");
            b.Link(b.MainTexture, "*", sample, "Texture");
            b.Link(moved, "Out", sample, "UV");
            b.Link(b.Sampler, "Out", sample, "Sampler");
            AbstractMaterialNode tinted = b.Math("Math/Basic/Multiply", sample, "RGBA", b.VertexColor, "Out");
            AbstractMaterialNode rgb = Rgb(b, tinted, "Out");
            AbstractMaterialNode alpha = AlphaOf(b, tinted, "Out");
            b.Output(rgb, "RGB", alpha, "A");
            return b.Save(Folder + "/Sprite_UVScroll.shadergraph");
        }

        // Ripples the picture sideways along its height (banners, flags). A UV wobble, so it works on a plain quad.
        private static string Wave()
        {
            var b = new GraphBuilder();
            AbstractMaterialNode amplitude = b.FloatProperty("Amplitude", "_WaveAmplitude", 0.02f);
            AbstractMaterialNode frequency = b.FloatProperty("Frequency", "_WaveFrequency", 12f);
            AbstractMaterialNode speed = b.FloatProperty("Speed", "_WaveSpeed", 3f);

            AbstractMaterialNode uv = b.Node("Input/Geometry/UV");
            AbstractMaterialNode split = b.Node("Channel/Split");
            b.Link(uv, "Out", split, "In");
            AbstractMaterialNode time = b.Node("Input/Basic/Time");
            AbstractMaterialNode along = b.Math("Math/Basic/Multiply", split, "G", frequency, "*");
            AbstractMaterialNode drift = b.Math("Math/Basic/Multiply", time, "Time", speed, "*");
            AbstractMaterialNode phase = b.Math("Math/Basic/Add", along, "Out", drift, "Out");
            AbstractMaterialNode sine = b.Node("Math/Trigonometry/Sine");
            b.Link(phase, "Out", sine, "In");
            AbstractMaterialNode shift = b.Math("Math/Basic/Multiply", sine, "Out", amplitude, "*");
            AbstractMaterialNode newU = b.Math("Math/Basic/Add", split, "R", shift, "Out");
            AbstractMaterialNode combined = b.Node("Channel/Combine");
            b.Link(newU, "Out", combined, "R");
            b.Link(split, "G", combined, "G");

            AbstractMaterialNode sample = b.Node("Input/Texture/Sample Texture 2D");
            b.Link(b.MainTexture, "*", sample, "Texture");
            b.Link(combined, "RG", sample, "UV");
            b.Link(b.Sampler, "Out", sample, "Sampler");
            AbstractMaterialNode tinted = b.Math("Math/Basic/Multiply", sample, "RGBA", b.VertexColor, "Out");
            AbstractMaterialNode rgb = Rgb(b, tinted, "Out");
            AbstractMaterialNode alpha = AlphaOf(b, tinted, "Out");
            b.Output(rgb, "RGB", alpha, "A");
            return b.Save(Folder + "/Sprite_Wave.shadergraph");
        }
    }
}
