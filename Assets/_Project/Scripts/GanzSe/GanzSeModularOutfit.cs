using UnityEngine;

namespace ProjectZx.GanzSe
{
    /// <summary>
    /// Assembles GanzSe NPCs. Body armor uses the modular skinned parts; heads use the
    /// pack's Non-Skinned Mesh Parts parented to the head bone (skinned HEADS render as
    /// a neck stump in our off-screen bake path).
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
                face.gameObject.SetActive(false);
            }

            // Skinned HEADS do not show in our bake — keep the folder off.
            var skinnedHeads = FindChild(armor, "HEADS");
            if (skinnedHeads != null)
                skinnedHeads.gameObject.SetActive(false);

            switch (role)
            {
                case GanzSeNpcRole.ShopWizard:
                    ApplyBody(armor,
                        chest: "Chest Armor Type 4 Color 2",
                        arms: "Arm Armor Type 4 Color 2",
                        legs: "Legs Armor Type 4 Color 2",
                        feet: "Feet Armor Type 4 Color 2",
                        belt: "Belt Armor Type 4 Color 2");
                    AttachHeadPart(characterRoot, "Head Armor Type 4 Color 2 Part");
                    break;

                case GanzSeNpcRole.QuestWizard:
                    ApplyBody(armor,
                        chest: "Chest Armor Type 5 Color 3",
                        arms: "Arm Armor Type 5 Color 3",
                        legs: "Legs Armor Type 5 Color 3",
                        feet: "Feet Armor Type 5 Color 3",
                        belt: "Belt Armor Type 5 Color 3");
                    AttachHeadPart(characterRoot, "Head Armor Type 5 Color 3 Part");
                    break;

                case GanzSeNpcRole.GreyWizard:
                    ApplyBody(armor,
                        chest: "Chest Armor Type 3 Color 1",
                        arms: "Arm Armor Type 3 Color 1",
                        legs: "Legs Armor Type 3 Color 1",
                        feet: "Feet Armor Type 3 Color 1",
                        belt: "Belt Armor Type 3 Color 1");
                    AttachHeadPart(characterRoot, "Head Armor Type 3 Color 1 Part");
                    break;

                case GanzSeNpcRole.MapKnight:
                    ApplyBody(armor,
                        chest: "Chest Armor Type 1 Color 1",
                        arms: "Arm Armor Type 1 Color 1",
                        legs: "Legs Armor Type 1 Color 1",
                        feet: "Feet Armor Type 1 Color 1",
                        belt: "Belt Armor Type 1 Color 1");
                    AttachHeadPart(characterRoot, "Head Armor Type 2 Color 1 Part");
                    break;

                case GanzSeNpcRole.QuestKnight:
                    ApplyBody(armor,
                        chest: "Chest Armor Type 2 Color 2",
                        arms: "Arm Armor Type 2 Color 2",
                        legs: "Legs Armor Type 2 Color 2",
                        feet: "Feet Armor Type 2 Color 2",
                        belt: "Belt Armor Type 2 Color 2");
                    AttachHeadPart(characterRoot, "Head Armor Type 1 Color 2 Part");
                    break;
            }
        }

        static void ApplyBody(
            Transform armor,
            string chest,
            string arms,
            string legs,
            string feet,
            string belt)
        {
            ActivateExclusive(FindChild(armor, "CHESTS"), chest);
            ActivateExclusive(FindChild(armor, "ARMS"), arms);
            ActivateExclusive(FindChild(armor, "LEGS"), legs);
            ActivateExclusive(FindChild(armor, "FEET"), feet);
            ActivateExclusive(FindChild(armor, "BELTS"), belt);
        }

        /// <summary>
        /// Parent a non-skinned head/helmet mesh to the head bone so it always shows.
        /// </summary>
        static void AttachHeadPart(GameObject characterRoot, string resourcesPartName)
        {
            var headBone = FindChild(characterRoot.transform, "head");
            if (headBone == null)
            {
                Debug.LogWarning("[GanzSe] head bone missing — cannot attach helmet.");
                return;
            }

            // Remove any previous attachment (re-warm / rebuild).
            for (var i = headBone.childCount - 1; i >= 0; i--)
            {
                var child = headBone.GetChild(i);
                if (child.name.StartsWith("HeadArmor_"))
                    Object.Destroy(child.gameObject);
            }

            var prefab = Resources.Load<GameObject>("GanzSe/Parts/" + resourcesPartName);
            if (prefab == null)
            {
                Debug.LogWarning($"[GanzSe] Missing Resources/GanzSe/Parts/{resourcesPartName}");
                return;
            }

            var head = Object.Instantiate(prefab, headBone, false);
            head.name = "HeadArmor_" + resourcesPartName;
            head.transform.localPosition = Vector3.zero;
            head.transform.localRotation = Quaternion.identity;
            head.transform.localScale = Vector3.one;
            head.SetActive(true);

            foreach (var r in head.GetComponentsInChildren<Renderer>(true))
            {
                if (r == null) continue;
                r.enabled = true;
                r.gameObject.layer = characterRoot.layer;
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
