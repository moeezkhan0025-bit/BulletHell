using System.IO;
using BulletHell.Core;
using BulletHell.Feedback;
using UnityEditor;
using UnityEngine;

namespace BulletHell.EditorTools
{
    /// <summary>
    /// S1: procedural placeholder effects in the VOX VEGETALLIS palette, so the demo does not wait for hand-drawn effect art. Generates the
    /// masks (glow, shockwave ring, muzzle star, puff, spark, shard) into Assets/Art/Placeholder/Vfx/Vox, rebuilds the nine particle presets
    /// (hit sparks, dust, smoke, steam, debris, coin burst, confetti, muzzle flash, spawn puff) with palette colours, assigns the glow and
    /// shockwave sprites on the FeedbackTuning, and puts a PickupGlow on the coin and ammo pickup prefabs. Safe to run again. The reserved
    /// enemy-bullet hues (violet, magenta) are never used. Replace any of it with painted art by swapping the sprite or prefab.
    /// </summary>
    public static class VoxVfx
    {
        private const string TexDir = "Assets/Art/Placeholder/Vfx/Vox";
        private const string PrefabDir = "Assets/Prefabs/Vfx";
        private const string MatDir = "Assets/Art/Materials";
        private const string TuningPath = "Assets/Data/Feedback/FeedbackTuning.asset";

        // VoxKit tokens (vox_ui_kit_manifest.json)
        public static readonly Color InkSoil = Hex(0x2E1F14);
        public static readonly Color Marble = Hex(0xF4EEDC);
        public static readonly Color MarbleShade = Hex(0xE2D6BC);
        public static readonly Color Gold = Hex(0xE9B63A);
        public static readonly Color GoldDark = Hex(0xA7781A);
        public static readonly Color Tomato = Hex(0xD8443A);
        public static readonly Color Leaf = Hex(0x5E9F3E);
        public static readonly Color Carrot = Hex(0xE57A24);
        public static readonly Color Corn = Hex(0xF4CE4A);
        public static readonly Color Sage = Hex(0xA9C48A);
        public static readonly Color MutedText = Hex(0x6B604E);

        // Smoothstep that also works with the edges the other way round (Mathf.SmoothStep does not).
        private static float Smooth(float edge0, float edge1, float x)
        {
            float t = Mathf.Clamp01((x - edge0) / (edge1 - edge0));
            return t * t * (3f - 2f * t);
        }

        private static Color Hex(int rgb) => new Color(((rgb >> 16) & 255) / 255f, ((rgb >> 8) & 255) / 255f, (rgb & 255) / 255f, 1f);
        private static Color WithAlpha(Color c, float a) => new Color(c.r, c.g, c.b, a);

        [MenuItem("BulletHell/Vox/10 Build VFX Placeholders")]
        public static void Build()
        {
            Directory.CreateDirectory(TexDir);
            Directory.CreateDirectory(PrefabDir);

            Texture2D glow = Mask("glow", 128, 128, (u, v) => Mathf.Pow(Mathf.Clamp01(1f - Mathf.Sqrt(u * u + v * v)), 1.6f));
            Texture2D ring = Mask("shockwave_ring", 256, 256, (u, v) =>
            {
                float d = Mathf.Sqrt(u * u + v * v);
                float outer = Smooth(1f, 0.93f, d);            // crisp outer edge
                float inner = Smooth(0.72f, 0.86f, d);          // soft inner edge
                float halo = Mathf.Clamp01(1f - Mathf.Abs(d - 0.85f) * 5f) * 0.25f;
                return Mathf.Max(outer * inner, halo);
            });
            Texture2D star = Mask("muzzle_star", 128, 128, (u, v) =>
            {
                float a = Mathf.Abs(u), b = Mathf.Abs(v);
                float cross = Mathf.Clamp01(1f - (a * b * 9f + Mathf.Max(a, b) * 0.35f)) * Mathf.Clamp01(1.2f - Mathf.Max(a, b));
                float core = Mathf.Clamp01(1f - Mathf.Sqrt(u * u + v * v) * 2.2f);
                float edge = Mathf.Clamp01((1f - Mathf.Sqrt(u * u + v * v)) * 3f);   // zero at the border, so no square shows
                return Mathf.Max(cross, core) * edge;
            });
            Texture2D puff = Mask("puff", 64, 64, (u, v) =>
            {
                float d = Mathf.Sqrt(u * u + v * v);
                float n = Mathf.PerlinNoise(u * 2.2f + 5f, v * 2.2f + 5f);
                return Mathf.SmoothStep(0f, 1f, (1f - d) * 1.7f) * Mathf.Lerp(0.6f, 1f, n);
            });
            Texture2D spark = Mask("spark", 64, 16, (u, v) => (1f - Mathf.Abs(u)) * (1f - Mathf.Abs(v) * 1.15f));
            Texture2D shard = Mask("shard", 32, 32, (u, v) => (v > -0.9f && Mathf.Abs(u) < (0.9f - v) * 0.5f) ? 1f : 0f);
            Texture2D dot = Mask("dot", 64, 64, (u, v) => Smooth(1f, 0.6f, Mathf.Sqrt(u * u + v * v)));

            var tuning = AssetDatabase.LoadAssetAtPath<FeedbackTuning>(TuningPath);
            var so = new SerializedObject(tuning);

            // name, texture, duration, life, speed, gravity, size, colours, burst, options
            Assign(so, "spark", Preset("Vfx_Spark", spark, 0.25f, 0.05f, 0.32f, 5f, 9f, 0f, 0.06f, 0.14f, Corn, Marble, 6, alignToVelocity: true));
            Assign(so, "dust", Preset("Vfx_Dust", puff, 0.4f, 0.3f, 0.5f, 0.4f, 1.2f, 0f, 0.15f, 0.3f, WithAlpha(MarbleShade, 0.75f), WithAlpha(Sage, 0.5f), 6, growth: 1.8f));
            Assign(so, "smoke", Preset("Vfx_Smoke", puff, 0.9f, 0.6f, 1f, 0.2f, 0.7f, -0.05f, 0.25f, 0.45f, WithAlpha(MutedText, 0.7f), WithAlpha(InkSoil, 0.5f), 6, growth: 2.2f));
            Assign(so, "steam", Preset("Vfx_Steam", glow, 0.8f, 0.5f, 0.9f, 0.5f, 1.1f, -0.15f, 0.15f, 0.3f, WithAlpha(Marble, 0.6f), WithAlpha(MarbleShade, 0.4f), 8, growth: 2f));
            Assign(so, "debris", Preset("Vfx_Debris", shard, 0.6f, 0.35f, 0.7f, 2.5f, 5f, 1.4f, 0.08f, 0.18f, Carrot, Gold, 10, spin: true, palette: new[] { Carrot, Gold, Tomato, Leaf }));
            Assign(so, "coin", Preset("Vfx_Coin", dot, 0.5f, 0.3f, 0.6f, 1.5f, 3f, 1f, 0.08f, 0.16f, Gold, Corn, 6));
            Assign(so, "confetti", Preset("Vfx_Confetti", shard, 1.6f, 1.1f, 1.8f, 3f, 7f, 1f, 0.1f, 0.2f, Tomato, Gold, 40, spin: true, palette: new[] { Tomato, Gold, Leaf, Carrot, Marble }));
            Assign(so, "muzzleFlash", Preset("Vfx_MuzzleFlash", star, 0.09f, 0.07f, 0.09f, 0f, 0.05f, 0f, 0.3f, 0.42f, Corn, Marble, 1, growth: 1.4f, radius: 0.001f));
            Assign(so, "spawnPuff", Preset("Vfx_SpawnPuff", puff, 0.5f, 0.35f, 0.55f, 0.3f, 0.9f, 0f, 0.3f, 0.5f, WithAlpha(MarbleShade, 0.8f), WithAlpha(Sage, 0.6f), 10, growth: 2.4f, radius: 0.25f));
            so.FindProperty("glowSprite").objectReferenceValue = Sprite(glow);
            so.FindProperty("shockwaveSprite").objectReferenceValue = Sprite(ring);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(tuning);

            GlowOnPrefab("Assets/Prefabs/Coin.prefab", WithAlpha(Gold, 0.6f), 0.8f, false);
            GlowOnPrefab("Assets/Prefabs/AmmoPickup.prefab", WithAlpha(Marble, 0.55f), 1.15f, true);

            AssetDatabase.SaveAssets();
            Debug.Log("Vox: procedural VFX placeholders built (presets, glow, shockwave, pickup glows).");
        }

        // ---------------------------------------------------------------- textures

        private static Texture2D Mask(string name, int width, int height, System.Func<float, float, float> alphaAt)
        {
            string path = $"{TexDir}/{name}.png";
            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                {
                    float u = (x + 0.5f) / width * 2f - 1f;
                    float v = (y + 0.5f) / height * 2f - 1f;
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(alphaAt(u, v))));
                }
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = Mathf.Max(width, height);   // a mask is 1 world unit wide; components scale it
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        private static Sprite Sprite(Texture2D texture) => AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GetAssetPath(texture));

        // ---------------------------------------------------------------- particle presets

        private static Material ParticleMaterial(string name, Texture2D texture)
        {
            string path = $"{MatDir}/Mat_Vfx_{name}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Sprites/Default"));
                AssetDatabase.CreateAsset(material, path);
            }
            material.mainTexture = texture;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void Assign(SerializedObject so, string field, ParticleSystem prefab) => so.FindProperty(field).objectReferenceValue = prefab;

        private static ParticleSystem Preset(string name, Texture2D texture, float duration, float lifeMin, float lifeMax, float speedMin, float speedMax,
                                             float gravity, float sizeMin, float sizeMax, Color colorA, Color colorB, int burst,
                                             bool alignToVelocity = false, float growth = 1f, bool spin = false, Color[] palette = null, float radius = 0.08f)
        {
            string path = $"{PrefabDir}/{name}.prefab";
            var go = new GameObject(name);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.duration = duration;
            main.loop = false;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = new ParticleSystem.MinMaxCurve(lifeMin, lifeMax);
            main.startSpeed = new ParticleSystem.MinMaxCurve(speedMin, speedMax);
            main.startSize = new ParticleSystem.MinMaxCurve(sizeMin, sizeMax);
            main.gravityModifier = gravity;
            main.maxParticles = 64;
            main.useUnscaledTime = name == "Vfx_Confetti";   // the round-results screen freezes game time
            main.startColor = new ParticleSystem.MinMaxGradient(colorA, colorB);
            if (palette != null)
            {
                var gradient = new Gradient();
                var colorKeys = new GradientColorKey[palette.Length];
                for (int i = 0; i < palette.Length; i++)
                    colorKeys[i] = new GradientColorKey(palette[i], palette.Length == 1 ? 0f : i / (float)(palette.Length - 1));
                gradient.SetKeys(colorKeys, new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
                main.startColor = new ParticleSystem.MinMaxGradient(gradient) { mode = ParticleSystemGradientMode.RandomColor };
            }
            if (spin)
                main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);

            var emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)burst) });

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = radius;
            shape.arc = 360f;

            var life = ps.colorOverLifetime;
            life.enabled = true;
            var fade = new Gradient();
            fade.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                         new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.6f), new GradientAlphaKey(0f, 1f) });
            life.color = fade;

            var size = ps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, growth));

            if (spin)
            {
                var rotation = ps.rotationOverLifetime;
                rotation.enabled = true;
                rotation.z = new ParticleSystem.MinMaxCurve(-6f, 6f);
            }

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = ParticleMaterial(name.Replace("Vfx_", ""), texture);
            renderer.sortingLayerName = SortingLayers.Bullets;
            renderer.sortingOrder = name == "Vfx_SpawnPuff" ? 2 : 6;
            renderer.renderMode = alignToVelocity ? ParticleSystemRenderMode.Stretch : ParticleSystemRenderMode.Billboard;
            if (alignToVelocity)
            {
                renderer.lengthScale = 2.2f;
                renderer.velocityScale = 0.04f;
            }

            GameObject saved = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return saved.GetComponent<ParticleSystem>();
        }

        // ---------------------------------------------------------------- pickups

        private static void GlowOnPrefab(string path, Color color, float size, bool ammo)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var glow = root.GetComponent<PickupGlow>();
                if (glow == null)
                    glow = root.AddComponent<PickupGlow>();
                var so = new SerializedObject(glow);
                so.FindProperty("color").colorValue = color;
                so.FindProperty("size").floatValue = size;
                so.FindProperty("tintFromAmmo").boolValue = ammo;
                so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }
    }
}
