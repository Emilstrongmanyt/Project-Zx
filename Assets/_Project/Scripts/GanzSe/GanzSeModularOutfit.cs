using UnityEngine;

namespace ProjectZx.GanzSe
{
    /// <summary>
    /// Enables one mesh per modular category so each NPC role reads as a distinct character.
    /// Part names match the GanzSe Update 1.1 prefab hierarchy.
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

            switch (role)
            {
                case GanzSeNpcRole.ShopWizard:
                    ApplyArmor(armor, helmet: false,
                        head: null,
                        chest: "Chest Armor Type 4 Color 2",
                        arms: "Arm Armor Type 4 Color 2",
                        legs: "Legs Armor Type 4 Color 2",
                        feet: "Feet Armor Type 4 Color 2",
                        belt: "Belt Armor Type 4 Color 2");
                    ApplyFace(face, show: true,
                        hair: "Hair Type 3 Color 4",
                        faceHair: "Face Hair Type 2 Color 4",
                        brows: "Eyebrow Type 2 Color 4",
                        eyes: "Eyes Type 2 Color 4",
                        nose: "Nose Type 3",
                        ears: "Ears Type 1");
                    break;

                case GanzSeNpcRole.QuestWizard:
                    ApplyArmor(armor, helmet: false,
                        head: null,
                        chest: "Chest Armor Type 5 Color 3",
                        arms: "Arm Armor Type 5 Color 3",
                        legs: "Legs Armor Type 5 Color 3",
                        feet: "Feet Armor Type 5 Color 3",
                        belt: "Belt Armor Type 5 Color 3");
                    ApplyFace(face, show: true,
                        hair: "Hair Type 5 Color 5",
                        faceHair: "Face Hair Type 4 Color 5",
                        brows: "Eyebrow Type 4 Color 5",
                        eyes: "Eyes Type 4 Color 5",
                        nose: "Nose Type 2",
                        ears: "Ears Type 1");
                    break;

                case GanzSeNpcRole.GreyWizard:
                    ApplyArmor(armor, helmet: false,
                        head: null,
                        chest: "Chest Armor Type 3 Color 1",
                        arms: "Arm Armor Type 3 Color 1",
                        legs: "Legs Armor Type 3 Color 1",
                        feet: "Feet Armor Type 3 Color 1",
                        belt: "Belt Armor Type 3 Color 1");
                    ApplyFace(face, show: true,
                        hair: "Hair Type 2 Color 1",
                        faceHair: "Face Hair Type 5 Color 1",
                        brows: "Eyebrow Type 3 Color 1",
                        eyes: "Eyes Type 3 Color 1",
                        nose: "Nose Type 4",
                        ears: "Ears Type 2");
                    break;

                case GanzSeNpcRole.MapKnight:
                    ApplyArmor(armor, helmet: true,
                        head: "Head Armor Type 2 Color 1",
                        chest: "Chest Armor Type 1 Color 1",
                        arms: "Arm Armor Type 1 Color 1",
                        legs: "Legs Armor Type 1 Color 1",
                        feet: "Feet Armor Type 1 Color 1",
                        belt: "Belt Armor Type 1 Color 1");
                    ApplyFace(face, show: false,
                        hair: null, faceHair: null, brows: null, eyes: null, nose: null, ears: null);
                    break;

                case GanzSeNpcRole.QuestKnight:
                    ApplyArmor(armor, helmet: true,
                        head: "Head Armor Type 1 Color 2",
                        chest: "Chest Armor Type 2 Color 2",
                        arms: "Arm Armor Type 2 Color 2",
                        legs: "Legs Armor Type 2 Color 2",
                        feet: "Feet Armor Type 2 Color 2",
                        belt: "Belt Armor Type 2 Color 2");
                    ApplyFace(face, show: false,
                        hair: null, faceHair: null, brows: null, eyes: null, nose: null, ears: null);
                    break;
            }
        }

        static void ApplyArmor(
            Transform armor,
            bool helmet,
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
                heads.gameObject.SetActive(helmet);
                if (helmet) ActivateExclusive(heads, head);
            }

            ActivateExclusive(armor.Find("CHESTS"), chest);
            ActivateExclusive(armor.Find("ARMS"), arms);
            ActivateExclusive(armor.Find("LEGS"), legs);
            ActivateExclusive(armor.Find("FEET"), feet);
            ActivateExclusive(armor.Find("BELTS"), belt);
        }

        static void ApplyFace(
            Transform face,
            bool show,
            string hair,
            string faceHair,
            string brows,
            string eyes,
            string nose,
            string ears)
        {
            if (face == null) return;
            face.gameObject.SetActive(show);
            if (!show) return;

            ActivateExclusive(face.Find("HAIRS"), hair);
            ActivateExclusive(face.Find("FACE HAIRS"), faceHair);
            ActivateExclusive(face.Find("EYEBROWS"), brows);
            ActivateExclusive(face.Find("EYES"), eyes);
            ActivateExclusive(face.Find("NOSES"), nose);
            ActivateExclusive(face.Find("EARS"), ears);
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

            // Fallback: first child if the named part is missing (pack version drift).
            if (match == null && category.childCount > 0 && !string.IsNullOrEmpty(childName))
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
