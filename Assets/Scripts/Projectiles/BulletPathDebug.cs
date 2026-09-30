using UnityEngine;
using UnityEngine.Rendering;

namespace BulletHell.Projectiles
{
    /// <summary>
    /// Debug visualisation of bullet paths, toggled with the debug key. While off it costs nothing (bullets check one
    /// bool). While on, every player bullet records the segment it flew each frame and the events on it; a small view
    /// draws them with GL lines over the game camera and lets them fade after a moment.
    /// Colours: cyan = straight flight, green = homing steered, yellow X = pierced an enemy, magenta X = ricochet off a wall,
    /// red X = bullet stopped.
    /// </summary>
    public static class BulletPathDebug
    {
        public enum MarkerKind { Pierce, Ricochet, Stop }

        public const float SecondsVisible = 2f;
        private const int Capacity = 4096;

        private static bool enabled;
        private static readonly Vector2[] From = new Vector2[Capacity];
        private static readonly Vector2[] To = new Vector2[Capacity];
        private static readonly byte[] Kinds = new byte[Capacity]; // 0 straight, 1 homing, 2/3/4 = pierce/ricochet/stop marker + 2
        private static readonly float[] Times = new float[Capacity];
        private static int next;
        private static int count;
        private static BulletPathView view;

        public static bool Enabled
        {
            get => enabled;
            set
            {
                enabled = value;
                if (enabled)
                    EnsureView();
                else
                    Clear();
            }
        }

        public static void Toggle() => Enabled = !enabled;

        public static int Count => count;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            enabled = false;
            next = 0;
            count = 0;
            view = null;
        }

        public static void Clear()
        {
            next = 0;
            count = 0;
        }

        /// <summary>One frame of flight. Call only while <see cref="Enabled"/>.</summary>
        public static void Segment(Vector2 from, Vector2 to, bool homing) => Add(from, to, homing ? (byte)1 : (byte)0);

        /// <summary>An event on a bullet (pierce, ricochet, stop) at a position. Call only while <see cref="Enabled"/>.</summary>
        public static void Marker(Vector2 position, MarkerKind kind) => Add(position, position, (byte)(2 + (int)kind));

        private static void Add(Vector2 from, Vector2 to, byte kind)
        {
            From[next] = from;
            To[next] = to;
            Kinds[next] = kind;
            Times[next] = Time.unscaledTime;
            next = (next + 1) % Capacity;
            if (count < Capacity)
                count++;
        }

        private static void EnsureView()
        {
            if (view != null || !Application.isPlaying)
                return;
            view = new GameObject("BulletPathDebug").AddComponent<BulletPathView>();
        }

        /// <summary>Draws the recorded paths for a camera (called by the view after that camera renders).</summary>
        internal static void Draw(Camera camera, Material material)
        {
            if (count == 0)
                return;

            GL.PushMatrix();
            GL.LoadProjectionMatrix(camera.projectionMatrix);
            GL.modelview = camera.worldToCameraMatrix;
            material.SetPass(0);
            GL.Begin(GL.LINES);
            float now = Time.unscaledTime;
            for (int i = 0; i < count; i++)
            {
                float age = now - Times[i];
                if (age > SecondsVisible)
                    continue;
                float alpha = 1f - age / SecondsVisible;
                byte kind = Kinds[i];
                if (kind < 2)
                {
                    GL.Color(kind == 1 ? new Color(0.3f, 1f, 0.35f, alpha) : new Color(0.3f, 0.9f, 1f, alpha));
                    GL.Vertex3(From[i].x, From[i].y, 0f);
                    GL.Vertex3(To[i].x, To[i].y, 0f);
                }
                else
                {
                    GL.Color(MarkerColor(kind - 2, alpha));
                    Vector2 p = From[i];
                    const float size = 0.18f;
                    GL.Vertex3(p.x - size, p.y - size, 0f);
                    GL.Vertex3(p.x + size, p.y + size, 0f);
                    GL.Vertex3(p.x - size, p.y + size, 0f);
                    GL.Vertex3(p.x + size, p.y - size, 0f);
                }
            }
            GL.End();
            GL.PopMatrix();
        }

        private static Color MarkerColor(int kind, float alpha)
        {
            switch ((MarkerKind)kind)
            {
                case MarkerKind.Pierce: return new Color(1f, 0.92f, 0.2f, alpha);
                case MarkerKind.Ricochet: return new Color(1f, 0.3f, 0.95f, alpha);
                default: return new Color(1f, 0.25f, 0.2f, alpha);
            }
        }

        private sealed class BulletPathView : MonoBehaviour
        {
            private Material material;

            private void OnEnable()
            {
                Shader shader = Shader.Find("Hidden/Internal-Colored");
                if (shader != null)
                {
                    material = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
                    material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
                    material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
                    material.SetInt("_Cull", (int)CullMode.Off);
                    material.SetInt("_ZWrite", 0);
                    material.SetInt("_ZTest", (int)CompareFunction.Always);
                }
                RenderPipelineManager.endCameraRendering += OnEndCamera;
            }

            private void OnDisable() => RenderPipelineManager.endCameraRendering -= OnEndCamera;

            private void OnDestroy()
            {
                if (material != null)
                    Destroy(material);
                view = null;
            }

            private void OnEndCamera(ScriptableRenderContext context, Camera camera)
            {
                if (material == null || !BulletPathDebug.Enabled || camera.cameraType != CameraType.Game)
                    return;
                Draw(camera, material);
            }
        }
    }
}
