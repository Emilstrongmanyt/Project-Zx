using ProjectZx.Core;
using ProjectZx.World;
using UnityEngine;

namespace ProjectZx.Combat
{
    /// <summary>
    /// Small looping fire sprite for weapon tips and ignited enemies.
    /// </summary>
    public class FlameEnchantVfx : MonoBehaviour
    {
        public enum FlameKind
        {
            Weapon,
            EnemyBurn
        }

        float _animTimer;
        int _frame;
        SpriteRenderer _renderer;
        FlameKind _kind;

        public static FlameEnchantVfx Attach(Transform parent, FlameKind kind, Vector3 localPos, float scale)
        {
            if (parent == null) return null;

            var existing = parent.Find("FlameEnchantVfx");
            if (existing != null)
            {
                var fx = existing.GetComponent<FlameEnchantVfx>();
                if (fx != null)
                {
                    existing.localPosition = localPos;
                    existing.localScale = Vector3.one * scale;
                    fx._kind = kind;
                    fx.gameObject.SetActive(true);
                    return fx;
                }
            }

            var go = new GameObject("FlameEnchantVfx");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = Vector3.one * scale;
            go.transform.localRotation = Quaternion.identity;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 25;
            go.AddComponent<YSortRenderer>().Configure(8);

            var vfx = go.AddComponent<FlameEnchantVfx>();
            vfx._renderer = sr;
            vfx._kind = kind;
            vfx.ApplyFrame(0);
            return vfx;
        }

        public void SetActive(bool active)
        {
            if (gameObject != null)
                gameObject.SetActive(active);
        }

        void Update()
        {
            if (_renderer == null) return;
            _animTimer -= Time.deltaTime;
            if (_animTimer > 0f) return;
            _animTimer = 0.09f;
            _frame++;
            ApplyFrame(_frame);
        }

        void ApplyFrame(int frame)
        {
            if (_renderer == null) return;
            _renderer.sprite = _kind == FlameKind.Weapon
                ? ArtLibrary.GetWeaponFireFrame(frame)
                : ArtLibrary.GetEnemyBurnFrame(frame);
        }
    }
}
