using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using BulletHell.Feedback;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Animation;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.U2D;

namespace BulletHell.EditorTools
{
    /// <summary>
    /// Builds the M8.6 rigged test character from nothing: writes a small layered PSD (one layer per body part, named like
    /// the Procreate layers the real bosses will use: head, torso, arm_L, arm_R, leg_L, leg_R), lets the 2D PSD Importer
    /// import it in Character mode, then writes the skeleton and rigid single-bone weights through the sprite-editor data
    /// providers, and puts a demo instance with a procedural bone animation (RigTestAnim) into Assets/Scenes/RigTest.unity.
    /// Placeholder art only: replace the PSD with a real Procreate export and re-rig it in the Skinning Editor.
    /// </summary>
    public static class M86Rig
    {
        private const string Dir = "Assets/Art/Placeholder/RigTest";
        private const string PsdPath = Dir + "/RigTest.psd";
        private const string ScenePath = "Assets/Scenes/RigTest.unity";
        private const int W = 256, H = 320;

        private struct Part
        {
            public string Name;
            public RectInt Rect;      // pixel rect in image space (origin top-left)
            public Color Color;
            public bool Ellipse;
            public string Bone;
        }

        private static readonly Part[] Parts =
        {
            new Part { Name = "leg_L", Rect = new RectInt(96, 208, 32, 88), Color = new Color(0.35f, 0.6f, 0.95f), Bone = "leg_L" },
            new Part { Name = "leg_R", Rect = new RectInt(132, 208, 32, 88), Color = new Color(0.3f, 0.55f, 0.9f), Bone = "leg_R" },
            new Part { Name = "arm_R", Rect = new RectInt(168, 110, 30, 88), Color = new Color(0.95f, 0.55f, 0.75f), Bone = "arm_R" },
            new Part { Name = "torso", Rect = new RectInt(92, 100, 76, 116), Color = new Color(0.95f, 0.4f, 0.55f), Bone = "torso" },
            new Part { Name = "head", Rect = new RectInt(92, 20, 76, 76), Color = new Color(1f, 0.8f, 0.5f), Ellipse = true, Bone = "head" },
            new Part { Name = "arm_L", Rect = new RectInt(62, 110, 30, 88), Color = new Color(0.95f, 0.5f, 0.7f), Bone = "arm_L" },
        };

        // Skeleton in character space: pixels, origin bottom-left of the canvas, positions relative to the parent bone.
        private struct BoneDef
        {
            public string Name;
            public int Parent;
            public Vector2 WorldPixel; // absolute, converted to parent-relative when written
            public float Length;
        }

        private static readonly BoneDef[] Bones =
        {
            new BoneDef { Name = "hip",   Parent = -1, WorldPixel = new Vector2(128, H - 210), Length = 20 },
            new BoneDef { Name = "torso", Parent = 0,  WorldPixel = new Vector2(128, H - 200), Length = 90 },
            new BoneDef { Name = "head",  Parent = 1,  WorldPixel = new Vector2(130, H - 100), Length = 55 },
            new BoneDef { Name = "arm_L", Parent = 1,  WorldPixel = new Vector2(78, H - 112), Length = 80 },
            new BoneDef { Name = "arm_R", Parent = 1,  WorldPixel = new Vector2(182, H - 112), Length = 80 },
            new BoneDef { Name = "leg_L", Parent = 0,  WorldPixel = new Vector2(112, H - 210), Length = 80 },
            new BoneDef { Name = "leg_R", Parent = 0,  WorldPixel = new Vector2(148, H - 210), Length = 80 },
        };

        [MenuItem("BulletHell/M8.6/Build Rig Test Character")]
        public static void Build()
        {
            Directory.CreateDirectory(Dir);
            AssetDatabase.DeleteAsset(PsdPath); // start clean: stale sprite/bone data would survive in the .meta
            WritePsd(PsdPath);
            AssetDatabase.ImportAsset(PsdPath, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
            AssetDatabase.SetImporterOverride<UnityEditor.U2D.PSD.PSDImporter>(PsdPath); // .psd defaults to the texture importer
            AssetDatabase.ImportAsset(PsdPath, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
            RigImportedPsd();
            BuildScene();
            Debug.Log("M8.6 rig test character built: " + PsdPath + " + " + ScenePath);
        }

        // ------------------------------------------------------------------------------------ PSD writer

        private static void WritePsd(string path)
        {
            var layers = new List<(Part part, byte[] rgba, RectInt rect)>();
            foreach (Part p in Parts)
                layers.Add((p, RenderPart(p), p.Rect));

            using (var ms = new MemoryStream())
            using (var w = new BigEndianWriter(ms))
            {
                // Header
                w.Bytes(Encoding.ASCII.GetBytes("8BPS"));
                w.U16(1);
                w.Bytes(new byte[6]);
                w.U16(4);            // channels (RGBA)
                w.U32(H);
                w.U32(W);
                w.U16(8);            // depth
                w.U16(3);            // RGB
                w.U32(0);            // colour mode data
                w.U32(0);            // image resources

                // Layer and mask information
                byte[] layerInfo = BuildLayerInfo(layers);
                w.U32((uint)(layerInfo.Length + 4 + 0)); // layer info (with its own length) + global mask length
                w.U32((uint)layerInfo.Length);
                w.Bytes(layerInfo);
                w.U32(0);            // global layer mask info

                // Merged image (flattened composite), planar raw
                w.U16(0);
                byte[] merged = Composite(layers);
                for (int channel = 0; channel < 4; channel++)
                    for (int i = 0; i < W * H; i++)
                        w.Bytes(new[] { merged[i * 4 + channel] });
                File.WriteAllBytes(path, ms.ToArray());
            }
        }

        private static byte[] BuildLayerInfo(List<(Part part, byte[] rgba, RectInt rect)> layers)
        {
            using (var ms = new MemoryStream())
            using (var w = new BigEndianWriter(ms))
            {
                w.S16((short)-layers.Count); // negative: the merged image has transparency
                foreach (var (part, rgba, rect) in layers)
                {
                    w.U32((uint)rect.y);
                    w.U32((uint)rect.x);
                    w.U32((uint)(rect.y + rect.height));
                    w.U32((uint)(rect.x + rect.width));
                    w.U16(4);
                    int channelBytes = 2 + rect.width * rect.height;
                    foreach (short id in new short[] { 0, 1, 2, -1 })
                    {
                        w.S16(id);
                        w.U32((uint)channelBytes);
                    }
                    w.Bytes(Encoding.ASCII.GetBytes("8BIM"));
                    w.Bytes(Encoding.ASCII.GetBytes("norm"));
                    w.Bytes(new byte[] { 255, 0, 0, 0 }); // opacity, clipping, flags (visible), filler

                    byte[] name = Encoding.ASCII.GetBytes(part.Name);
                    int nameField = 1 + name.Length;
                    int pad = (4 - nameField % 4) % 4;
                    w.U32((uint)(4 + 4 + nameField + pad)); // extra data length: mask + blending ranges + name
                    w.U32(0);                               // layer mask data
                    w.U32(0);                               // blending ranges
                    w.Bytes(new[] { (byte)name.Length });
                    w.Bytes(name);
                    w.Bytes(new byte[pad]);
                }
                foreach (var (part, rgba, rect) in layers)
                    foreach (int channel in new[] { 0, 1, 2, 3 })
                    {
                        w.U16(0); // raw
                        for (int i = 0; i < rect.width * rect.height; i++)
                            w.Bytes(new[] { rgba[i * 4 + channel] });
                    }
                if (ms.Length % 2 != 0)
                    w.Bytes(new byte[1]);
                return ms.ToArray();
            }
        }

        private static byte[] RenderPart(Part p)
        {
            var data = new byte[p.Rect.width * p.Rect.height * 4];
            for (int y = 0; y < p.Rect.height; y++)
                for (int x = 0; x < p.Rect.width; x++)
                {
                    float u = (x + 0.5f) / p.Rect.width * 2f - 1f;
                    float v = (y + 0.5f) / p.Rect.height * 2f - 1f;
                    bool inside = p.Ellipse ? u * u + v * v <= 1f : Mathf.Abs(u) <= 0.94f && Mathf.Abs(v) <= 0.96f;
                    bool edge = p.Ellipse ? u * u + v * v > 0.85f : Mathf.Abs(u) > 0.8f || Mathf.Abs(v) > 0.88f;
                    Color c = edge ? p.Color * 0.7f : p.Color;
                    int i = (y * p.Rect.width + x) * 4;
                    data[i] = (byte)(Mathf.Clamp01(c.r) * 255);
                    data[i + 1] = (byte)(Mathf.Clamp01(c.g) * 255);
                    data[i + 2] = (byte)(Mathf.Clamp01(c.b) * 255);
                    data[i + 3] = (byte)(inside ? 255 : 0);
                }
            return data;
        }

        private static byte[] Composite(List<(Part part, byte[] rgba, RectInt rect)> layers)
        {
            var image = new byte[W * H * 4];
            foreach (var (part, rgba, rect) in layers) // painter's order: first layer at the back
                for (int y = 0; y < rect.height; y++)
                    for (int x = 0; x < rect.width; x++)
                    {
                        int s = (y * rect.width + x) * 4;
                        if (rgba[s + 3] == 0)
                            continue;
                        int d = ((rect.y + y) * W + rect.x + x) * 4;
                        Array.Copy(rgba, s, image, d, 4);
                    }
            return image;
        }

        // ------------------------------------------------------------------------------------ rigging

        private static void RigImportedPsd()
        {
            var importer = (AssetImporter)AssetImporter.GetAtPath(PsdPath);
            var factory = new SpriteDataProviderFactories();
            factory.Init();
            ISpriteEditorDataProvider dataProvider = factory.GetSpriteEditorDataProviderFromObject(importer);
            dataProvider.InitSpriteEditorDataProvider();

            SpriteRect[] rects = dataProvider.GetSpriteRects();
            var characterProvider = dataProvider.GetDataProvider<ICharacterDataProvider>();
            var meshProvider = dataProvider.GetDataProvider<ISpriteMeshDataProvider>();
            var boneProvider = dataProvider.GetDataProvider<ISpriteBoneDataProvider>();
            CharacterData character = characterProvider.GetCharacterData();

            // Skeleton: positions are relative to the parent bone.
            var spriteBones = new SpriteBone[Bones.Length];
            for (int i = 0; i < Bones.Length; i++)
            {
                Vector2 parentWorld = Bones[i].Parent >= 0 ? Bones[Bones[i].Parent].WorldPixel : Vector2.zero;
                spriteBones[i] = new SpriteBone
                {
                    name = "bone_" + Bones[i].Name,
                    parentId = Bones[i].Parent,
                    position = Bones[i].WorldPixel - parentWorld,
                    rotation = Quaternion.identity,
                    length = Bones[i].Length,
                    color = Color.HSVToRGB(i / (float)Bones.Length, 0.6f, 1f),
                };
            }
            character.bones = spriteBones;

            for (int i = 0; i < character.parts.Length; i++)
            {
                CharacterPart part = character.parts[i];
                SpriteRect rect = rects.FirstOrDefault(r => r.spriteID == new GUID(part.spriteId));
                bool known = Array.FindIndex(Bones, b => b.Name == rect?.name) >= 0;
                part.bones = known ? Enumerable.Range(0, Bones.Length).ToArray() : new int[0]; // every sprite carries the whole skeleton
                character.parts[i] = part;
            }
            characterProvider.SetCharacterData(character);

            // Rigid weights: each part is a quad fully weighted to its own bone (index 0 of that part's bone list).
            foreach (SpriteRect rect in rects)
            {
                // A sprite stores its skeleton in its own space: the root bone is measured from the sprite's bottom-left corner.
                Part def = Parts.First(p => p.Name == rect.name);
                var boneList = new List<SpriteBone>(spriteBones);
                SpriteBone root = boneList[0];
                root.position = Bones[0].WorldPixel - new Vector2(def.Rect.x, H - (def.Rect.y + def.Rect.height));
                boneList[0] = root;
                int ownBone = Array.FindIndex(Bones, b => b.Name == rect.name);
                CharacterPart part = character.parts.FirstOrDefault(p => new GUID(p.spriteId) == rect.spriteID);
                if (part.bones == null || part.bones.Length == 0)
                    continue;
                boneProvider.SetBones(rect.spriteID, boneList);

                Rect r = rect.rect;
                var vertices = new[]
                {
                    Vertex(new Vector2(0, 0), ownBone), Vertex(new Vector2(r.width, 0), ownBone),
                    Vertex(new Vector2(r.width, r.height), ownBone), Vertex(new Vector2(0, r.height), ownBone),
                };
                meshProvider.SetVertices(rect.spriteID, vertices);
                meshProvider.SetIndices(rect.spriteID, new[] { 0, 1, 2, 0, 2, 3 });
                meshProvider.SetEdges(rect.spriteID, new[]
                {
                    new Vector2Int(0, 1), new Vector2Int(1, 2), new Vector2Int(2, 3), new Vector2Int(3, 0),
                });
            }

            dataProvider.Apply();
            importer.SaveAndReimport();
        }

        private static Vertex2DMetaData Vertex(Vector2 position, int bone) => new Vertex2DMetaData
        {
            position = position,
            boneWeight = new BoneWeight { boneIndex0 = bone, weight0 = 1f },
        };

        // ------------------------------------------------------------------------------------ scene

        private static void BuildScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var cameraGo = new GameObject("Main Camera") { tag = "MainCamera" };
            Camera camera = cameraGo.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 2.2f;
            camera.backgroundColor = new Color(0.16f, 0.13f, 0.2f);
            camera.clearFlags = CameraClearFlags.SolidColor;
            cameraGo.transform.position = new Vector3(0f, 1.45f, -10f);

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PsdPath);
            if (prefab == null)
            {
                Debug.LogError("M8.6 rig: the PSD produced no prefab (is the PSD Importer in Character mode?).");
                return;
            }
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.name = "RigTestCharacter";
            var anim = instance.AddComponent<RigTestAnim>();
            var animSo = new SerializedObject(anim);
            animSo.FindProperty("walking").boolValue = true;
            animSo.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.SaveScene(scene, ScenePath);
        }

        // ------------------------------------------------------------------------------------ helpers

        private sealed class BigEndianWriter : IDisposable
        {
            private readonly Stream stream;
            public BigEndianWriter(Stream s) => stream = s;
            public void Bytes(byte[] b) => stream.Write(b, 0, b.Length);
            public void U16(ushort v) => Bytes(new[] { (byte)(v >> 8), (byte)v });
            public void S16(short v) => U16((ushort)v);
            public void U32(uint v) => Bytes(new[] { (byte)(v >> 24), (byte)(v >> 16), (byte)(v >> 8), (byte)v });
            public void Dispose() { }
        }
    }
}
