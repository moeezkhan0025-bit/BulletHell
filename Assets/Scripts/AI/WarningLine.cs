using BulletHell.Core;
using UnityEngine;

namespace BulletHell.AI
{
    /// <summary>
    /// A telegraph line on the ground (charger dash, sniper shot). One LineRenderer created once per enemy, drawn over
    /// the characters on the Bullets layer with the same lift as bullets. Not parented under the enemy, so the enemy's
    /// sorting group can't bury it.
    /// </summary>
    public sealed class WarningLine
    {
        private readonly LineRenderer line;
        private readonly float lift;

        public WarningLine(Transform parent, Material material, float bulletLift)
        {
            lift = bulletLift;
            var go = new GameObject("WarningLine");
            go.transform.SetParent(parent, false);
            line = go.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.positionCount = 2;
            line.sharedMaterial = material;
            line.sortingLayerID = SortingLayers.Id(SortingLayers.Bullets);
            line.sortingOrder = 3;
            line.numCapVertices = 2;
            line.enabled = false;
        }

        public void Show(Vector2 from, Vector2 to, Color color, float width)
        {
            Vector3 up = new Vector3(0f, lift, 0f);
            line.startColor = line.endColor = color;
            line.widthMultiplier = width;
            line.SetPosition(0, (Vector3)from + up);
            line.SetPosition(1, (Vector3)to + up);
            line.enabled = true;
        }

        public void Hide() => line.enabled = false;

        public void Destroy()
        {
            // On leaving Play mode the scene objects are already being torn down: nothing to clean up.
            if (line != null && Application.isPlaying)
                Object.Destroy(line.gameObject);
        }
    }
}
