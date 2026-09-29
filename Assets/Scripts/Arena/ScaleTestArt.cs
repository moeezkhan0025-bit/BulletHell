using UnityEngine;

namespace BulletHell.Arena
{
    /// <summary>
    /// Switches for the art scale test (Docs/ART_SPEC.md section 9): a painted flat backdrop in place of the placeholder
    /// floor and walls, and painted enemy sprites in place of the placeholder shapes. Turn a switch off to get the
    /// placeholders back; nothing else changes. Assigned on GameConfig.
    /// </summary>
    [CreateAssetMenu(fileName = "ScaleTestArt", menuName = "BulletHell/Scale Test Art")]
    public sealed class ScaleTestArt : ScriptableObject
    {
        [Header("Backdrop")]
        [Tooltip("Replaces the placeholder floor, walls, stands and torches with one flat painted image. Obstacles, traps, bullets and the foreground stay.")]
        [SerializeField] private bool useBackdrop = true;
        [Tooltip("The painted arena, imported at 220 px per world unit (P = 1 unit) so the player is 110 px tall at 1080p.")]
        [SerializeField] private Sprite backdrop;
        [Tooltip("Pixel rectangle of the painted arena inside the image (x, y from the bottom-left, width, height). The image has dark bars around it that must not be framed. Zero width = the whole image.")]
        [SerializeField] private RectInt backdropCrop = new RectInt(273, 54, 3269, 1840);
        [Tooltip("World position of the centre of the cropped painted area. Chosen so the painted floor's centre sits on the arena bounds' centre.")]
        [SerializeField] private Vector2 backdropPosition = new Vector2(-0.037f, 0.168f);
        [Tooltip("Sorting order on the Background layer (behind everything).")]
        [SerializeField] private int backdropSortingOrder = -100;

        [Tooltip("Hides the placeholder front railing, drapes, posts and torches (the painted image has its own railing). Off = they draw over the backdrop.")]
        [SerializeField] private bool hidePlaceholderForeground = true;

        [Header("Enemies")]
        [Tooltip("Enemies that have a painted sprite use it instead of the placeholder shape.")]
        [SerializeField] private bool usePaintedEnemies = true;

        public bool UseBackdrop => useBackdrop && backdrop != null;
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
        public bool HidePlaceholderForeground => hidePlaceholderForeground;
        public bool UsePaintedEnemies => usePaintedEnemies;
    }
}
