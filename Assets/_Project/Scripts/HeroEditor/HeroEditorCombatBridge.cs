using UnityEngine;

namespace ProjectZx.HeroEditor
{
    /// <summary>Shared hooks so class combat scripts can drive HeroEditor anims without duplication.</summary>
    public static class HeroEditorCombatBridge
    {
        public static HeroEditorCharacterView Get(Component host) =>
            host != null ? host.GetComponent<HeroEditorCharacterView>() : null;

        public static bool IsActive(Component host)
        {
            var view = Get(host);
            return view != null && view.IsReady;
        }

        public static void Face(Component host, bool faceRight, SpriteRenderer bodyFallback)
        {
            var view = Get(host);
            if (view != null && view.IsReady)
            {
                view.SetFacing(faceRight);
                return;
            }

            if (bodyFallback != null)
                bodyFallback.flipX = !faceRight;
        }

        public static void Slash(Component host, bool faceRight, SpriteRenderer bodyFallback)
        {
            Face(host, faceRight, bodyFallback);
            var view = Get(host);
            if (view != null && view.IsReady)
                view.PlayMeleeSlash();
        }

        public static void Jab(Component host, bool faceRight, SpriteRenderer bodyFallback)
        {
            Face(host, faceRight, bodyFallback);
            var view = Get(host);
            if (view != null && view.IsReady)
                view.PlayMeleeJab();
        }

        public static void BowShot(Component host, bool faceRight, SpriteRenderer bodyFallback)
        {
            Face(host, faceRight, bodyFallback);
            var view = Get(host);
            if (view != null && view.IsReady)
                view.PlayBowShot();
        }

        public static void HideLegacyWeapons(Component host)
        {
            Get(host)?.HideLegacyWeaponSprites();
        }

        public static void RefreshLoadoutOnPlayer()
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player == null) return;
            var view = player.GetComponent<HeroEditorCharacterView>();
            view?.RefreshEquipmentAndWeapon();
        }
    }
}
