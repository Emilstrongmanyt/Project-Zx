using UnityEngine;

namespace ProjectZx.GanzSe
{
    /// <summary>
    /// Pack ships with no clips (demo is T-pose too). Keep the Humanoid Animator
    /// enabled so skinned HEADS update, and force off-screen skin updates for bake/RT.
    /// </summary>
    public static class GanzSeIdlePose
    {
        public static void Apply(GameObject characterRoot)
        {
            if (characterRoot == null) return;

            foreach (var smr in characterRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (smr == null) continue;
                smr.updateWhenOffscreen = true;
                smr.forceMatrixRecalculationPerRender = true;
                if (smr.gameObject.activeInHierarchy)
                    smr.enabled = true;
            }

            foreach (var animator in characterRoot.GetComponentsInChildren<Animator>(true))
            {
                if (animator == null) continue;
                // Keep enabled — disabling it was correlated with missing skinned heads.
                animator.enabled = true;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                animator.updateMode = AnimatorUpdateMode.Normal;
                // No controller in the pack; leave null (demo does the same → T-pose).
                animator.runtimeAnimatorController = null;
            }
        }
    }
}
