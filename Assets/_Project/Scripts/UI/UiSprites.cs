using ProjectZx.Core;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectZx.UI
{
    public static class UiSprites
    {
        public static void ApplyButtonSprite(Image image, Vector2 size, StoneButtonStyle style = StoneButtonStyle.Primary)
        {
            if (image == null) return;

            if (StoneUi.Available && StoneUi.ButtonPrimary != null)
            {
                StoneUi.ApplyButton(image, style);
                return;
            }

            Sprite sprite;
            if (Mathf.Abs(size.x - 360f) < 1f && Mathf.Abs(size.y - 56f) < 1f)
                sprite = ArtLibrary.Btn360x56;
            else if (Mathf.Abs(size.x - 220f) < 1f && Mathf.Abs(size.y - 52f) < 1f)
                sprite = ArtLibrary.Btn220x52;
            else if (Mathf.Abs(size.x - 200f) < 1f && Mathf.Abs(size.y - 52f) < 1f)
                sprite = ArtLibrary.Btn200x52;
            else
                sprite = ArtLibrary.BtnPrimary;

            image.sprite = sprite;
            image.type = Image.Type.Sliced;
            image.color = Color.white;
        }

        public static void ApplyPanelSprite(Image image, Sprite fallback, bool largeMenu = true)
        {
            if (image == null) return;

            if (StoneUi.Available)
            {
                StoneUi.ApplyPanel(image, largeMenu);
                return;
            }

            if (fallback != null)
            {
                image.sprite = fallback;
                image.type = Image.Type.Sliced;
                image.color = Color.white;
            }
            else
            {
                image.color = new Color(0.05f, 0.08f, 0.12f, 0.92f);
            }
        }
    }
}
