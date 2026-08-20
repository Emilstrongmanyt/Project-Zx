using UnityEngine;

namespace ProjectZx.GanzSe
{
    /// <summary>
    /// The GanzSe FREE pack ships with no AnimationClips / AnimatorControllers.
    /// We synthesize a Mixamo-style standing idle via Humanoid arm muscles only.
    /// Optional: drop an AnimationClip at Resources/GanzSe/Anim/Idle to override.
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
                if (smr.gameObject.activeInHierarchy)
                    smr.enabled = true;
            }

            var animator = characterRoot.GetComponentInChildren<Animator>(true);
            if (animator == null) return;

            var clip = Resources.Load<AnimationClip>("GanzSe/Anim/Idle");
            if (clip != null)
            {
                animator.enabled = true;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                clip.SampleAnimation(characterRoot, 0f);
                animator.Update(0f);
                animator.runtimeAnimatorController = null;
                animator.enabled = false;
                return;
            }

            ApplySyntheticStandingIdle(animator);
        }

        static void ApplySyntheticStandingIdle(Animator animator)
        {
            var avatar = animator.avatar;
            if (avatar == null || !avatar.isValid || !avatar.isHuman)
            {
                animator.runtimeAnimatorController = null;
                animator.enabled = false;
                return;
            }

            animator.enabled = true;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.runtimeAnimatorController = null;

            try
            {
                var handler = new HumanPoseHandler(avatar, animator.transform);
                var pose = new HumanPose();
                handler.GetHumanPose(ref pose);

                for (var i = 0; i < pose.muscles.Length && i < HumanTrait.MuscleCount; i++)
                {
                    var name = HumanTrait.MuscleName[i];
                    // Fold arms down into a resting stance (Mixamo Idle-ish).
                    if (name.Contains("Arm Down-Out"))
                        pose.muscles[i] = 0.9f;
                    else if (name.Contains("Arm Front-Back"))
                        pose.muscles[i] = 0.2f;
                    else if (name.Contains("Arm Twist"))
                        pose.muscles[i] = 0.05f;
                    else if (name.Contains("Forearm Stretch"))
                        pose.muscles[i] = 0.25f;
                    else if (name.Contains("Hand Down-Up"))
                        pose.muscles[i] = 0.15f;
                    else if (name.Contains("Hand In-Out"))
                        pose.muscles[i] = 0.1f;
                }

                handler.SetHumanPose(ref pose);
                handler.Dispose();
                animator.Update(0f);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[GanzSe] Synthetic idle failed: " + e.Message);
            }

            // Freeze posed bones so the empty Humanoid Animator cannot snap back to T-pose.
            animator.enabled = false;
        }
    }
}
