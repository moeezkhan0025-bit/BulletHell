using UnityEngine;

namespace BulletHell.Feedback
{
    /// <summary>
    /// Demo animation for the rigged test character (Assets/Scenes/RigTest.unity): finds the skeleton's bones by name and
    /// drives them procedurally: an idle sway and breathing, or a walk cycle with swinging arms and legs. It is the model
    /// for how boss and merchant parts will be animated by code instead of drawn frames.
    /// </summary>
    public sealed class RigTestAnim : MonoBehaviour
    {
        [SerializeField] private bool walking;
        [SerializeField, Min(0.1f)] private float speed = 2.4f;

        private Transform hip, torso, head, armL, armR, legL, legR;
        private Vector3 hipHome;

        private void Awake()
        {
            hip = Find("bone_hip");
            torso = Find("bone_torso");
            head = Find("bone_head");
            armL = Find("bone_arm_L");
            armR = Find("bone_arm_R");
            legL = Find("bone_leg_L");
            legR = Find("bone_leg_R");
            if (hip != null)
                hipHome = hip.localPosition;
        }

        private Transform Find(string boneName)
        {
            foreach (Transform t in GetComponentsInChildren<Transform>(true))
                if (t.name == boneName)
                    return t;
            return null;
        }

        private void Update()
        {
            float t = Time.time * speed;
            float swing = walking ? 28f : 6f;
            float bob = walking ? Mathf.Abs(Mathf.Sin(t)) * 0.06f : Mathf.Sin(t * 0.5f) * 0.015f;

            if (hip != null)
                hip.localPosition = hipHome + new Vector3(0f, bob, 0f);
            Rotate(torso, Mathf.Sin(t) * (walking ? 4f : 2f));
            Rotate(head, Mathf.Sin(t - 0.6f) * 5f);
            Rotate(armL, Mathf.Sin(t) * swing);
            Rotate(armR, -Mathf.Sin(t) * swing);
            Rotate(legL, walking ? -Mathf.Sin(t) * swing : 0f);
            Rotate(legR, walking ? Mathf.Sin(t) * swing : 0f);
        }

        private static void Rotate(Transform bone, float degrees)
        {
            if (bone != null)
                bone.localRotation = Quaternion.Euler(0f, 0f, degrees);
        }
    }
}
