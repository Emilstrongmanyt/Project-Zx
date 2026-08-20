using UnityEngine;

namespace ProjectZx.GanzSe
{
    /// <summary>
    /// Do not rewrite bone localEulers — that crumples skinned heads/armor into a
    /// headless scarecrow. Only freeze the empty Humanoid Animator on the prefab
    /// pose and keep skinned meshes updating for the off-screen bake camera.
    /// </summary>
    public static class GanzSeIdlePose
    {
        public static void Apply(GameObject characterRoot)
        {
            if (characterRoot == null) return;

            foreach (var animator in characterRoot.GetComponentsInChildren<Animator>(true))
            {
                if (animator == null) continue;
                animator.runtimeAnimatorController = null;
                // Keep enabled=false so Unity does not force a muscle T-pose reset
                // after we finish assembly. Bones stay at the prefab hierarchy pose.
                animator.enabled = false;
            }

            foreach (var smr in characterRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (smr == null) continue;
                smr.updateWhenOffscreen = true;
                smr.enabled = true;
            }
        }
    }
}
