using ProjectZx.Core;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectZx.UI
{
    public static class UiSprites
    {
        public static void ApplyButtonSprite(Image image, Vector2 size)
        {
            if (image == null) return;

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
    }
}