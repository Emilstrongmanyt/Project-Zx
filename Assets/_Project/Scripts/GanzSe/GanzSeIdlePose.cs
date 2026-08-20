using UnityEngine;

namespace ProjectZx.GanzSe
{
    /// <summary>
    /// The GanzSe prefab has a Humanoid Animator with no controller, which snaps to T-pose.
    /// Disable the Animator and fold the arms into a neutral standing pose before baking.
    /// </summary>
    public static class GanzSeIdlePose
    {
        public static void Apply(GameObject characterRoot)
        {
            if (characterRoot == null) return;

            foreach (var animator in characterRoot.GetComponentsInChildren<Animator>(true))
            {
                animator.enabled = false;
                animator.runtimeAnimatorController = null;
            }

            // Bone axes on this rig point along +Y toward the child; Z rotates arms in/out of T-pose.
            SetLocalEuler(characterRoot, "shoulder_l", 6f, 8f, 12f);
            SetLocalEuler(characterRoot, "shoulder_r", 6f, -8f, -12f);
            SetLocalEuler(characterRoot, "upperarm_l", 10f, 12f, 78f);
            SetLocalEuler(characterRoot, "upperarm_r", 10f, -12f, -78f);
            SetLocalEuler(characterRoot, "forearm_l", 8f, 0f, 18f);
            SetLocalEuler(characterRoot, "forearm_r", 8f, 0f, -18f);
            SetLocalEuler(characterRoot, "hand_l", 0f, 0f, 8f);
            SetLocalEuler(characterRoot, "hand_r", 0f, 0f, -8f);
            SetLocalEuler(characterRoot, "spine_02", 4f, 0f, 0f);
            SetLocalEuler(characterRoot, "spine_03", 3f, 0f, 0f);
            SetLocalEuler(characterRoot, "neck", -4f, 0f, 0f);
            SetLocalEuler(characterRoot, "head", -6f, 0f, 0f);
            SetLocalEuler(characterRoot, "upperleg_l", -4f, 2f, -2f);
            SetLocalEuler(characterRoot, "upperleg_r", -4f, -2f, 2f);
            SetLocalEuler(characterRoot, "shin_l", 6f, 0f, 0f);
            SetLocalEuler(characterRoot, "shin_r", 6f, 0f, 0f);

            foreach (var smr in characterRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (smr == null) continue;
                smr.updateWhenOffscreen = true;
                smr.enabled = true;
            }
        }

        static void SetLocalEuler(GameObject root, string boneName, float x, float y, float z)
        {
            var bone = FindChild(root.transform, boneName);
            if (bone == null) return;
            bone.localRotation = Quaternion.Euler(x, y, z);
        }

        static Transform FindChild(Transform root, string name)
        {
            if (root == null) return null;
            if (root.name == name) return root;
            for (var i = 0; i < root.childCount; i++)
            {
                var found = FindChild(root.GetChild(i), name);
                if (found != null) return found;
            }

            return null;
        }
    }
}
