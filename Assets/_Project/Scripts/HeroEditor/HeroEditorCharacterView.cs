using Assets.HeroEditor.Common.Scripts.CharacterScripts;
using Assets.HeroEditor.Common.Scripts.ExampleScripts;
using HeroEditor.Common.Data;
using HeroEditor.Common.Enums;
using ProjectZx.Core;
using ProjectZx.World;
using UnityEngine;

namespace ProjectZx.HeroEditor
{
    /// <summary>Which CharacterAppearance JSON to apply on a HeroEditor body.</summary>
    public enum HeroAppearanceSource
    {
        /// <summary>RollZy — player's saved creator look.</summary>
        PlayerSave,
        /// <summary>RowZi — fixed alt look (not the pink sheet, not a player clone).</summary>
        RowZiAlt
    }

    /// <summary>
    /// Owns a FantasyHeroes Human Character under the Player/companion root.
    /// Drives idle/walk, facing, cape/helm/weapon visuals. Combat math stays on *Combat scripts.
    /// </summary>
    public class HeroEditorCharacterView : MonoBehaviour
    {
        const string PrefabResourcePath = "HeroEditor/Human";
        /// <summary>Visual size relative to Project Zx player root scale (~0.55).</summary>
        const float ChildLocalScale = 0.92f;

        Character _character;
        LayerManager _layerManager;
        float _absScale = ChildLocalScale;
        bool _faceRight = true;
        bool _moving;
        PlayerClass _weaponClass = PlayerClass.Batter;
        HeroAppearanceSource _appearanceSource = HeroAppearanceSource.PlayerSave;

        public Character Character => _character;
        public bool IsReady => _character != null;

        /// <summary>
        /// Attach HeroEditor body. Hides the root sheet SpriteRenderer.
        /// Returns null if the prefab cannot be loaded (pack missing).
        /// </summary>
        public static HeroEditorCharacterView Attach(
            GameObject playerRoot,
            PlayerClass weaponClass,
            bool applyLoadout,
            HeroAppearanceSource appearanceSource = HeroAppearanceSource.PlayerSave)
        {
            if (playerRoot == null) return null;

            var existing = playerRoot.GetComponent<HeroEditorCharacterView>();
            if (existing != null)
            {
                existing._weaponClass = weaponClass;
                existing._appearanceSource = appearanceSource;
                existing.RefreshAll(applyLoadout);
                return existing;
            }

            var prefab = Resources.Load<GameObject>(PrefabResourcePath);
            if (prefab == null)
            {
                Debug.LogWarning("[HeroEditor] Missing Resources/HeroEditor/Human prefab — keeping sheet sprites.");
                return null;
            }

            var view = playerRoot.AddComponent<HeroEditorCharacterView>();
            view._weaponClass = weaponClass;
            view._appearanceSource = appearanceSource;
            view.Build(prefab, applyLoadout);
            return view;
        }

        void Build(GameObject prefab, bool applyLoadout)
        {
            var rootSr = GetComponent<SpriteRenderer>();
            if (rootSr != null)
                rootSr.enabled = false;

            var instance = Instantiate(prefab, transform, false);
            instance.name = "HeroEditorCharacter";
            instance.transform.localPosition = new Vector3(0f, -0.05f, 0f);
            instance.transform.localRotation = Quaternion.identity;
            _absScale = ChildLocalScale;
            instance.transform.localScale = new Vector3(_absScale, _absScale, _absScale);

            _character = instance.GetComponent<Character>();
            _layerManager = instance.GetComponent<LayerManager>();
            if (_character == null)
            {
                Debug.LogError("[HeroEditor] Human prefab missing Character component.");
                Destroy(instance);
                return;
            }

            // Strip example melee hit hooks that expect 3D demo setup.
            var melee = instance.GetComponent<MeleeWeapon>();
            if (melee != null) melee.enabled = false;

            ApplyAppearance();
            if (applyLoadout)
                RefreshEquipmentAndWeapon();
            else
                RefreshCapeAndHelmOnly();

            _character.SetState(CharacterState.Idle);
            _character.GetReady();
            ApplyFacing();
        }

        public void RefreshAll(bool applyWeapon)
        {
            if (_character == null) return;
            ApplyAppearance();
            if (applyWeapon)
                RefreshEquipmentAndWeapon();
            else
                RefreshCapeAndHelmOnly();
            SetMoving(_moving);
            ApplyFacing();
        }

        public void ApplyAppearanceFromSave() => ApplyAppearance();

        public void ApplyAppearance()
        {
            if (_character == null) return;

            string json;
            if (_appearanceSource == HeroAppearanceSource.RowZiAlt)
                json = GameSave.CreateRowZiAppearanceJson();
            else
            {
                json = GameSave.CharacterAppearanceJson;
                if (string.IsNullOrEmpty(json))
                    json = GameSave.CreateDefaultAppearanceJson();
            }

            try
            {
                var appearance = CharacterAppearance.FromJson(json);
                appearance.Setup(_character);
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[HeroEditor] Appearance apply failed, using defaults: {ex.Message}");
                new CharacterAppearance().Setup(_character);
            }
        }

        public void RefreshCapeAndHelmOnly()
        {
            if (_character == null) return;
            EquipById(HeroEditorEquipmentMap.GetCapeSpriteId(GameSave.EquippedCape), EquipmentPart.Cape);
            EquipById(HeroEditorEquipmentMap.GetHelmetSpriteId(GameSave.EquippedHelm), EquipmentPart.Helmet);
            _character.GetReady();
        }

        public void RefreshEquipmentAndWeapon()
        {
            if (_character == null) return;

            RefreshCapeAndHelmOnly();

            var tier = WeaponCatalog.GetEquippedTier(_weaponClass);
            var visual = HeroEditorWeaponMap.GetVisual(_weaponClass, tier);

            // Clear other weapon slots first so bow/melee don't stack.
            _character.UnEquip(EquipmentPart.MeleeWeapon1H);
            _character.UnEquip(EquipmentPart.MeleeWeapon2H);
            _character.UnEquip(EquipmentPart.Bow);
            _character.UnEquip(EquipmentPart.Shield);

            if (visual.IsBow)
                EquipById(visual.SpriteId, EquipmentPart.Bow, visual.Paint);
            else if (visual.IsTwoHanded)
                EquipById(visual.SpriteId, EquipmentPart.MeleeWeapon2H, visual.Paint);
            else
                EquipById(visual.SpriteId, EquipmentPart.MeleeWeapon1H, visual.Paint);

            _character.GetReady();
            HideLegacyWeaponSprites();
        }

        void EquipById(string id, EquipmentPart part, Color? paint = null)
        {
            if (_character == null) return;
            if (string.IsNullOrEmpty(id))
            {
                _character.UnEquip(part);
                return;
            }

            var entry = FindEntry(id, part);
            if (entry == null)
            {
                Debug.LogWarning($"[HeroEditor] Sprite id not found: {id}");
                return;
            }

            if (paint.HasValue)
                _character.Equip(entry, part, paint.Value);
            else
                _character.Equip(entry, part);
        }

        static ItemSprite FindIn(System.Collections.Generic.List<ItemSprite> list, string id)
        {
            if (list == null || string.IsNullOrEmpty(id)) return null;
            for (var i = 0; i < list.Count; i++)
            {
                if (list[i] != null && list[i].Id == id)
                    return list[i];
            }

            return null;
        }

        ItemSprite FindEntry(string id, EquipmentPart part)
        {
            var collection = _character.SpriteCollection;
            if (collection == null) return null;

            return part switch
            {
                EquipmentPart.Cape => FindIn(collection.Cape, id),
                EquipmentPart.Helmet => FindIn(collection.Helmet, id),
                EquipmentPart.MeleeWeapon1H => FindIn(collection.MeleeWeapon1H, id),
                EquipmentPart.MeleeWeapon2H => FindIn(collection.MeleeWeapon2H, id),
                EquipmentPart.Bow => FindIn(collection.Bow, id),
                _ => null
            };
        }

        /// <summary>Hide ArtLibrary bat/spear/bow/staff child sprites when HE weapons are shown.</summary>
        public void HideLegacyWeaponSprites()
        {
            HideNamed("BatPivot");
            HideNamed("SpearPivot");
            HideNamed("KatanaPivot");
            HideNamed("BowPivot");
            HideNamed("StaffPivot");
        }

        void HideNamed(string childName)
        {
            var t = transform.Find(childName);
            if (t == null) return;
            foreach (var sr in t.GetComponentsInChildren<SpriteRenderer>(true))
                sr.enabled = false;
        }

        public void SetFacing(bool faceRight)
        {
            _faceRight = faceRight;
            ApplyFacing();
        }

        void ApplyFacing()
        {
            if (_character == null) return;
            var x = _faceRight ? _absScale : -_absScale;
            _character.transform.localScale = new Vector3(x, _absScale, _absScale);
        }

        public void SetMoving(bool moving)
        {
            _moving = moving;
            if (_character == null) return;
            _character.SetState(moving ? CharacterState.Walk : CharacterState.Idle);
        }

        public void PlayMeleeSlash()
        {
            if (_character == null) return;
            _character.Slash();
        }

        public void PlayMeleeJab()
        {
            if (_character == null) return;
            _character.Jab();
        }

        public void PlayBowShot()
        {
            if (_character == null) return;
            StartCoroutine(_character.Shoot());
        }

        void LateUpdate()
        {
            if (_layerManager == null) return;
            var order = ArenaBounds.GetYSortOrder(transform.position.y, 2);
            _layerManager.SetSortingGroupOrder(order);
        }
    }
}
