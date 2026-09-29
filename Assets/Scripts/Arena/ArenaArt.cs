using UnityEngine;

namespace BulletHell.Arena
{
    /// <summary>
    /// The painted arena backdrop (replaces the placeholder floor, walls, stands and torches) and where it sits.
    /// Painted enemy sprites are per-enemy (EnemyData). Assigned on GameConfig; empty = placeholder arena.
    /// </summary>
    [CreateAssetMenu(fileName = "ArenaArt", menuName = "BulletHell/Arena Art")]
    public sealed class ArenaArt : ScriptableObject
    {
        [Tooltip("The painted arena, imported at 220 px per world unit (arena scale, unchanged by the 1.5x character scale). Replaces the placeholder floor, walls, stands and torches; obstacles, traps, bullets and the foreground layer stay.")]
        [SerializeField] private Sprite backdrop;
        [Tooltip("Pixel rectangle of the painted arena inside the image (x, y from the bottom-left, width, height). The image has dark bars around it that must not be framed. Zero width = the whole image.")]
        [SerializeField] private RectInt backdropCrop = new RectInt(273, 54, 3269, 1840);
        [Tooltip("World position of the centre of the cropped painted area. Chosen so the painted floor's centre sits on the arena bounds' centre.")]
        [SerializeField] private Vector2 backdropPosition = new Vector2(-0.037f, 0.168f);
        [Tooltip("Sorting order on the Background layer (behind everything).")]
        [SerializeField] private int backdropSortingOrder = -100;

        public bool UseBackdrop => backdrop != null;
        private Sprite croppedBackdrop;

        /// <summary>The painted area only (the source image has dark bars), same pixels-per-unit as the import.</summary>
        public Sprite Backdrop
        {
            get
            {
                if (backdrop == null || backdropCrop.width <= 0 || backdropCrop.height <= 0)
                    return backdrop;
                if (croppedBackdrop == null)
                {
                    Rect r = new Rect(backdropCrop.x, backdropCrop.y, backdropCrop.width, backdropCrop.height);
                    croppedBackdrop = Sprite.Create(backdrop.texture, r, new Vector2(0.5f, 0.5f), backdrop.pixelsPerUnit, 0, SpriteMeshType.FullRect);
                    croppedBackdrop.name = backdrop.name + "_crop";
                    croppedBackdrop.hideFlags = HideFlags.HideAndDontSave;
                }
                return croppedBackdrop;
            }
        }
        public Vector2 BackdropPosition => backdropPosition;
        public int BackdropSortingOrder => backdropSortingOrder;
    }
}
