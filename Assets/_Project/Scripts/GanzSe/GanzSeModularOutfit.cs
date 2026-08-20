using UnityEngine;

namespace ProjectZx.GanzSe
{
    /// <summary>
    /// Assembles GanzSe modular parts the same way the pack demo does:
    /// exactly one mesh per armor/face category, and a head piece always equipped.
    /// (Face-only mode without HEADS reads as a headless neck stump on this pack.)
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

            // Pack prefab ships with every variant active — clear that first.
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

            switch (role)
            {
                case GanzSeNpcRole.ShopWizard:
                    // Soft mage set + matching hood (demo-style: helmet/hood on, face off).
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

        /// <summary>
        /// Matches ModularHeroController with showHelmet=true: HEADS on, FACE DETAILS off.
        /// </summary>
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
