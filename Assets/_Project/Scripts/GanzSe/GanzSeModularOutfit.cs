using GanzSe;
using UnityEngine;

namespace ProjectZx.GanzSe
{
    /// <summary>
    /// Build NPCs the same way the pack demo does:
    /// ModularHeroController + one skinned part per armor category + helmet ON
    /// (HEADS active, FACE DETAILS off). Do not disable the skinned HEADS folder.
    /// </summary>
    public static class GanzSeModularOutfit
    {
        public static void Apply(GameObject characterRoot, GanzSeNpcRole role)
        {
            if (characterRoot == null) return;

            var armor = FindChild(characterRoot.transform, "ARMOR PARTS");
            var face = FindChild(characterRoot.transform, "FACE DETAILS PARTS");
            if (armor == null)
            {
                Debug.LogWarning("[GanzSe] ARMOR PARTS not found.");
                return;
            }

            // Demo ships with every variant on — clear, then enable one per slot.
            ClearCategory(armor.Find("HEADS"));
            ClearCategory(armor.Find("CHESTS"));
            ClearCategory(armor.Find("ARMS"));
            ClearCategory(armor.Find("LEGS"));
            ClearCategory(armor.Find("FEET"));
            ClearCategory(armor.Find("BELTS"));
            if (face != null)
            {
                ClearCategory(face.Find("HAIRS"));
                ClearCategory(face.Find("FACE HAIRS"));
                ClearCategory(face.Find("EYEBROWS"));
                ClearCategory(face.Find("EYES"));
                ClearCategory(face.Find("NOSES"));
                ClearCategory(face.Find("EARS"));
            }

            string head, chest, arms, legs, feet, belt;
            switch (role)
            {
                case GanzSeNpcRole.ShopWizard:
                    head = "Head Armor Type 4 Color 2";
                    chest = "Chest Armor Type 4 Color 2";
                    arms = "Arm Armor Type 4 Color 2";
                    legs = "Legs Armor Type 4 Color 2";
                    feet = "Feet Armor Type 4 Color 2";
                    belt = "Belt Armor Type 4 Color 2";
                    break;
                case GanzSeNpcRole.QuestWizard:
                    head = "Head Armor Type 5 Color 3";
                    chest = "Chest Armor Type 5 Color 3";
                    arms = "Arm Armor Type 5 Color 3";
                    legs = "Legs Armor Type 5 Color 3";
                    feet = "Feet Armor Type 5 Color 3";
                    belt = "Belt Armor Type 5 Color 3";
                    break;
                case GanzSeNpcRole.GreyWizard:
                    head = "Head Armor Type 3 Color 1";
                    chest = "Chest Armor Type 3 Color 1";
                    arms = "Arm Armor Type 3 Color 1";
                    legs = "Legs Armor Type 3 Color 1";
                    feet = "Feet Armor Type 3 Color 1";
                    belt = "Belt Armor Type 3 Color 1";
                    break;
                case GanzSeNpcRole.MapKnight:
                    head = "Head Armor Type 2 Color 1";
                    chest = "Chest Armor Type 1 Color 1";
                    arms = "Arm Armor Type 1 Color 1";
                    legs = "Legs Armor Type 1 Color 1";
                    feet = "Feet Armor Type 1 Color 1";
                    belt = "Belt Armor Type 1 Color 1";
                    break;
                default: // QuestKnight
                    head = "Head Armor Type 1 Color 2";
                    chest = "Chest Armor Type 2 Color 2";
                    arms = "Arm Armor Type 2 Color 2";
                    legs = "Legs Armor Type 2 Color 2";
                    feet = "Feet Armor Type 2 Color 2";
                    belt = "Belt Armor Type 2 Color 2";
                    break;
            }

            var heads = armor.Find("HEADS");
            if (heads != null)
            {
                heads.gameObject.SetActive(true);
                ActivateExclusive(heads, head);
            }

            ActivateExclusive(armor.Find("CHESTS"), chest);
            ActivateExclusive(armor.Find("ARMS"), arms);
            ActivateExclusive(armor.Find("LEGS"), legs);
            ActivateExclusive(armor.Find("FEET"), feet);
            ActivateExclusive(armor.Find("BELTS"), belt);

            // Exact demo ModularHeroController.ToggleHelmet with showHelmet=true.
            var hero = characterRoot.GetComponent<ModularHeroController>();
            if (hero == null)
                hero = characterRoot.AddComponent<ModularHeroController>();
            hero.armorPartsRoot = armor;
            hero.facePartsRoot = face;
            hero.showHelmet = true;
            hero.ToggleHelmet();

            // Belt-and-suspenders: if skinned head still inactive, force it.
            if (heads != null)
            {
                heads.gameObject.SetActive(true);
                var anyHead = false;
                for (var i = 0; i < heads.childCount; i++)
                {
                    if (!heads.GetChild(i).gameObject.activeSelf) continue;
                    anyHead = true;
                    foreach (var smr in heads.GetChild(i).GetComponentsInChildren<SkinnedMeshRenderer>(true))
                    {
                        smr.enabled = true;
                        smr.updateWhenOffscreen = true;
                        smr.forceMatrixRecalculationPerRender = true;
                    }
                }

                if (!anyHead)
                {
                    Debug.LogWarning("[GanzSe] No active head after ToggleHelmet — forcing first head.");
                    ActivateExclusive(heads, head);
                }
            }

            if (face != null)
                face.gameObject.SetActive(false);
        }

        static void ClearCategory(Transform category)
        {
            if (category == null) return;
            for (var i = 0; i < category.childCount; i++)
                category.GetChild(i).gameObject.SetActive(false);
        }

        static void ActivateExclusive(Transform category, string childName)
        {
            if (category == null) return;
            Transform match = null;
            for (var i = 0; i < category.childCount; i++)
            {
                var child = category.GetChild(i);
                var on = child.name == childName;
                child.gameObject.SetActive(on);
                if (on) match = child;
            }

            if (match == null && category.childCount > 0)
            {
                Debug.LogWarning($"[GanzSe] Missing '{childName}' under {category.name}; using first.");
                category.GetChild(0).gameObject.SetActive(true);
            }
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
