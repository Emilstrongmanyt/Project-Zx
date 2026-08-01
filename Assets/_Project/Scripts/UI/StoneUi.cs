using UnityEngine;
using UnityEngine.UI;

namespace ProjectZx.UI
{
    /// <summary>
    /// Runtime access to Layer Lab "GUI - The Stone" sprites (copied under Resources/UI/Stone).
    /// Sprites are rebuilt with 9-slice borders so panels/buttons scale cleanly.
    /// </summary>
    public static class StoneUi
    {
        const string Root = "UI/Stone/";

        static Sprite _panelFrame;
        static Sprite _panelFrameAlt;
        static Sprite _popupBg;
        static Sprite _popupTitle;
        static Sprite _buttonPrimary;
        static Sprite _buttonGreen;
        static Sprite _buttonBlue;
        static Sprite _buttonRed;
        static Sprite _buttonSquare;
        static Sprite _resourceBarBg;
        static Sprite _resourceIconCoin;
        static Sprite _resourceIconGem;
        static Sprite _hudBarBorder;
        static Sprite _hudBarInner;
        static Sprite _hudBarFillHp;
        static Sprite _hudBarFillXp;
        static Sprite _joystickBg;
        static Sprite _joystickKnob;
        static Sprite _iconGold;
        static Sprite _iconHp;
        static Sprite _iconXp;
        static Sprite _titleRibbon;
        static Sprite _listFrame;
        static Sprite _itemFrame;
        static bool _availabilityChecked;
        static bool _available;

        public static bool Available
        {
            get
            {
                if (!_availabilityChecked)
                {
                    _availabilityChecked = true;
                    _available = Resources.Load<Texture2D>(Root + "panel_frame") != null
                                 || Resources.Load<Sprite>(Root + "panel_frame") != null;
                }
                return _available;
            }
        }

        public static Sprite PanelFrame => _panelFrame ??= LoadSliced("panel_frame", 68, 49, 76, 105);
        public static Sprite PanelFrameAlt => _panelFrameAlt ??= LoadSliced("panel_frame_alt", 48, 48, 48, 48);
        public static Sprite PopupBg => _popupBg ??= LoadSliced("popup_bg", 80, 80, 80, 80);
        public static Sprite PopupTitle => _popupTitle ??= LoadSliced("popup_title", 40, 20, 40, 20);
        public static Sprite ButtonPrimary => _buttonPrimary ??= LoadSliced("button_primary", 73, 30, 86, 47);
        public static Sprite ButtonGreen => _buttonGreen ??= LoadSliced("button_green", 73, 30, 86, 47);
        public static Sprite ButtonBlue => _buttonBlue ??= LoadSliced("button_blue", 73, 30, 86, 47);
        public static Sprite ButtonRed => _buttonRed ??= LoadSliced("button_red", 73, 30, 86, 47);
        public static Sprite ButtonSquare => _buttonSquare ??= LoadSliced("button_square", 24, 24, 24, 24);
        public static Sprite ResourceBarBg => _resourceBarBg ??= LoadSliced("resource_bar_bg", 40, 20, 40, 20);
        public static Sprite ResourceIconCoin => _resourceIconCoin ??= LoadSimple("resource_icon_coin");
        public static Sprite ResourceIconGem => _resourceIconGem ??= LoadSimple("resource_icon_gem");
        public static Sprite HudBarBorder => _hudBarBorder ??= LoadSliced("hud_bar_border", 24, 12, 24, 12);
        public static Sprite HudBarInner => _hudBarInner ??= LoadSliced("hud_bar_inner", 12, 8, 12, 8);
        public static Sprite HudBarFillHp => _hudBarFillHp ??= LoadSliced("hud_bar_fill_hp", 8, 6, 8, 6);
        public static Sprite HudBarFillXp => _hudBarFillXp ??= LoadSliced("hud_bar_fill_xp", 8, 6, 8, 6);
        public static Sprite JoystickBg => _joystickBg ??= LoadSimple("joystick_bg");
        public static Sprite JoystickKnob => _joystickKnob ??= LoadSimple("joystick_knob");
        public static Sprite IconGold => _iconGold ??= LoadSimple("icon_gold");
        public static Sprite IconHp => _iconHp ??= LoadSimple("icon_hp");
        public static Sprite IconXp => _iconXp ??= LoadSimple("icon_xp");
        public static Sprite TitleRibbon => _titleRibbon ??= LoadSliced("title_ribbon", 60, 20, 60, 20);
        public static Sprite ListFrame => _listFrame ??= LoadSliced("list_frame", 16, 16, 16, 16);
        public static Sprite ItemFrame => _itemFrame ??= LoadSliced("item_frame", 20, 20, 20, 20);

        /// <summary>Best panel sprite for large hub menus / shop dialogs.</summary>
        public static Sprite MenuPanel => PanelFrame != null ? PanelFrame : PopupBg;

        /// <summary>Best panel for smaller dialogs (level-up, retreat, toast).</summary>
        public static Sprite DialogPanel => PopupBg != null ? PopupBg : PanelFrameAlt;

        public static void ApplyPanel(Image image, bool largeMenu = true)
        {
            if (image == null) return;
            var sprite = largeMenu ? MenuPanel : DialogPanel;
            if (sprite == null) return;
            image.sprite = sprite;
            image.type = Image.Type.Sliced;
            image.color = Color.white;
            image.pixelsPerUnitMultiplier = largeMenu ? 1.15f : 1f;
        }

        public static void ApplyButton(Image image, StoneButtonStyle style = StoneButtonStyle.Primary)
        {
            if (image == null) return;
            var sprite = style switch
            {
                StoneButtonStyle.Green => ButtonGreen,
                StoneButtonStyle.Blue => ButtonBlue,
                StoneButtonStyle.Red => ButtonRed,
                StoneButtonStyle.Square => ButtonSquare,
                _ => ButtonPrimary
            };
            if (sprite == null) return;
            image.sprite = sprite;
            image.type = Image.Type.Sliced;
            image.color = Color.white;
            image.pixelsPerUnitMultiplier = 1.1f;
        }

        public static void ApplyFillBar(Image track, Image fill, Sprite fillSprite)
        {
            if (track != null && HudBarBorder != null)
            {
                track.sprite = HudBarBorder;
                track.type = Image.Type.Sliced;
                track.color = Color.white;
            }

            if (fill != null && fillSprite != null)
            {
                fill.sprite = fillSprite;
                fill.type = Image.Type.Sliced;
                fill.color = Color.white;
            }
        }

        static Sprite LoadSliced(string name, float left, float bottom, float right, float top)
        {
            var tex = Resources.Load<Texture2D>(Root + name);
            if (tex != null)
            {
                // Preserve import borders when present; otherwise use pack-tuned defaults.
                var imported = Resources.LoadAll<Sprite>(Root + name);
                if (imported != null && imported.Length > 0 && imported[0] != null && imported[0].border.sqrMagnitude > 0.01f)
                    return imported[0];

                return Sprite.Create(
                    tex,
                    new Rect(0f, 0f, tex.width, tex.height),
                    new Vector2(0.5f, 0.5f),
                    100f,
                    0,
                    SpriteMeshType.FullRect,
                    new Vector4(left, bottom, right, top));
            }

            var sprite = Resources.Load<Sprite>(Root + name);
            return sprite;
        }

        static Sprite LoadSimple(string name)
        {
            var sprite = Resources.Load<Sprite>(Root + name);
            if (sprite != null) return sprite;

            var sprites = Resources.LoadAll<Sprite>(Root + name);
            if (sprites != null && sprites.Length > 0) return sprites[0];

            var tex = Resources.Load<Texture2D>(Root + name);
            if (tex == null) return null;

            return Sprite.Create(
                tex,
                new Rect(0f, 0f, tex.width, tex.height),
                new Vector2(0.5f, 0.5f),
                100f);
        }
    }

    public enum StoneButtonStyle
    {
        Primary,
        Green,
        Blue,
        Red,
        Square
    }
}
