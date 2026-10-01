using BulletHell.Capture;
using UnityEditor;

namespace BulletHell.EditorTools.GameplayCapture
{
    /// <summary>
    /// R1 portfolio capture kit - the menu toggles, stored in EditorPrefs (per machine, survive restarts) and mirrored into
    /// <see cref="CaptureFlags"/> for the runtime on every domain reload and before every scenario.
    /// Defaults: Clean capture ON, Invincible ON, Auto record scenarios OFF.
    /// </summary>
    [InitializeOnLoad]
    public static class CaptureMenuPrefs
    {
        private const string CleanKey = "BulletHell.Capture.CleanCapture";
        private const string InvincibleKey = "BulletHell.Capture.Invincible";
        private const string AutoRecordKey = "BulletHell.Capture.AutoRecordScenarios";

        static CaptureMenuPrefs() => Sync();

        public static bool Clean
        {
            get => EditorPrefs.GetBool(CleanKey, true);
            set
            {
                EditorPrefs.SetBool(CleanKey, value);
                Sync();
            }
        }

        public static bool Invincible
        {
            get => EditorPrefs.GetBool(InvincibleKey, true);
            set
            {
                EditorPrefs.SetBool(InvincibleKey, value);
                Sync();
            }
        }

        public static bool AutoRecord
        {
            get => EditorPrefs.GetBool(AutoRecordKey, false);
            set
            {
                EditorPrefs.SetBool(AutoRecordKey, value);
                Sync();
            }
        }

        /// <summary>Copies the stored toggles into the runtime flags.</summary>
        public static void Sync()
        {
            CaptureFlags.CleanCapture = Clean;
            CaptureFlags.Invincible = Invincible;
            CaptureFlags.AutoRecordScenarios = AutoRecord;
        }
    }
}
