using UnityEngine;

namespace ProjectZx.GanzSe
{
    /// <summary>
    /// Assembles GanzSe modular parts like the pack demo:
    /// one mesh per category, head armor always on, face details off under helmet/hood.
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
                Debug.LogWarning("[GanzSe] ARMOR PARTS not found on modular character.");
                return;
            }

            // Prefab ships with every variant enabled — clear categories first.
            ClearCategory(FindChild(armor, "HEADS"));
            ClearCategory(FindChild(armor, "CHESTS"));
            ClearCategory(FindChild(armor, "ARMS"));
            ClearCategory(FindChild(armor, "LEGS"));
            ClearCategory(FindChild(armor, "FEET"));
            ClearCategory(FindChild(armor, "BELTS"));
            if (face != null)
            {
                ClearCategory(FindChild(face, "HAIRS"));
                ClearCategory(FindChild(face, "FACE HAIRS"));
                ClearCategory(FindChild(face, "EYEBROWS"));
                ClearCategory(FindChild(face, "EYES"));
                ClearCategory(FindChild(face, "NOSES"));
                ClearCategory(FindChild(face, "EARS"));
            }

            switch (role)
            {
                case GanzSeNpcRole.ShopWizard:
                    ApplyHelmetSet(armor, face,
                        head: "Head Armor Type 4 Color 2",
                        chest: "Chest Armor Type 4 Color 2",
                        arms: "Arm Armor Type 4 Color 2",
                        legs: "Legs Armor Type 4 Color 2",
                        feet: "Feet Armor Type 4 Color 2",
                        belt: "Belt Armor Type 4 Color 2");
                    break;

                case GanzSeNpcRole.QuestWizard:
                    ApplyHelmetSet(armor, face,
                        head: "Head Armor Type 5 Color 3",
                        chest: "Chest Armor Type 5 Color 3",
                        arms: "Arm Armor Type 5 Color 3",
                        legs: "Legs Armor Type 5 Color 3",
                        feet: "Feet Armor Type 5 Color 3",
                        belt: "Belt Armor Type 5 Color 3");
                    break;

                case GanzSeNpcRole.GreyWizard:
                    ApplyHelmetSet(armor, face,
                        head: "Head Armor Type 3 Color 1",
                        chest: "Chest Armor Type 3 Color 1",
                        arms: "Arm Armor Type 3 Color 1",
                        legs: "Legs Armor Type 3 Color 1",
                        feet: "Feet Armor Type 3 Color 1",
                        belt: "Belt Armor Type 3 Color 1");
                    break;

                case GanzSeNpcRole.MapKnight:
                    ApplyHelmetSet(armor, face,
                        head: "Head Armor Type 2 Color 1",
                        chest: "Chest Armor Type 1 Color 1",
                        arms: "Arm Armor Type 1 Color 1",
                        legs: "Legs Armor Type 1 Color 1",
                        feet: "Feet Armor Type 1 Color 1",
                        belt: "Belt Armor Type 1 Color 1");
                    break;

                case GanzSeNpcRole.QuestKnight:
                    ApplyHelmetSet(armor, face,
                        head: "Head Armor Type 1 Color 2",
                        chest: "Chest Armor Type 2 Color 2",
                        arms: "Arm Armor Type 2 Color 2",
                        legs: "Legs Armor Type 2 Color 2",
                        feet: "Feet Armor Type 2 Color 2",
                        belt: "Belt Armor Type 2 Color 2");
                    break;
            }
        }

        static void ApplyHelmetSet(
            Transform armor,
            Transform face,
            string head,
            string chest,
            string arms,
            string legs,
            string feet,
            string belt)
        {
            var heads = FindChild(armor, "HEADS");
            if (heads != null)
            {
                heads.gameObject.SetActive(true);
                ActivateExclusive(heads, head);
            }
            else
            {
                Debug.LogWarning("[GanzSe] HEADS category missing — NPC will look headless.");
            }

            ActivateExclusive(FindChild(armor, "CHESTS"), chest);
            ActivateExclusive(FindChild(armor, "ARMS"), arms);
            ActivateExclusive(FindChild(armor, "LEGS"), legs);
            ActivateExclusive(FindChild(armor, "FEET"), feet);
            ActivateExclusive(FindChild(armor, "BELTS"), belt);

            // Demo default: helmet/hood on ⇒ face detail folder off.
            if (face != null)
                face.gameObject.SetActive(false);

            // Make sure the chosen head renderer is actually on.
            if (heads != null)
            {
                for (var i = 0; i < heads.childCount; i++)
                {
                    var child = heads.GetChild(i);
                    if (!child.gameObject.activeSelf) continue;
                    foreach (var smr in child.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                    {
                        smr.enabled = true;
                        smr.updateWhenOffscreen = true;
                    }
                }
            }
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
                var on = !string.IsNullOrEmpty(childName) && child.name == childName;
                child.gameObject.SetActive(on);
                if (on) match = child;
            }

            if (match == null && category.childCount > 0)
            {
                Debug.LogWarning($"[GanzSe] Missing part '{childName}' under {category.name}; using first child.");
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
