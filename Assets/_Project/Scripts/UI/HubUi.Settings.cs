using ProjectZx.Core;
using ProjectZx.Player;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectZx.UI
{
    /// <summary>
    /// Settings / BGM / movement-control panel for <see cref="HubUi"/>.
    /// Behavior-unchanged extract (PR-02) — keeps HubUi.cs from growing further.
    /// </summary>
    public partial class HubUi
    {
        GameObject _settingsPanel;
        Text _bgmVolumeLabel;
        Text _sfxVolumeLabel;
        Text _bgmGenreStatusText;
        Button _bgmDnBButton;
        Button _bgmMetalButton;
        Button _movementJoystickButton;
        Button _movementTapHoldButton;
        Button _largeDamageNumbersButton;
        Text _largeDamageNumbersLabel;

        GameObject BuildSettingsPanel(Transform parent)
        {
            var panel = CreateDialogPanel(parent, "SettingsPanel", Vector2.zero, HubMenuPanelSize, ArtLibrary.ShopUi);
            CreateText(panel.transform, "Settings", HubLayout.TitleFont, TextAnchor.MiddleCenter, new Vector2(0, 316), new Vector2(700, 48));

            // 768×480 at y=20 spans 260 to -220, between the title and Close.
            const float contentHeight = 720f;
            var scroll = HubLayout.CreateVerticalScroll(
                panel.transform,
                new Vector2(0f, 20f),
                new Vector2(768f, 480f),
                contentHeight);
            var body = scroll.content;
            // Old stack top was y=310. That edge becomes the content top.
            float Y(float oldCenter) => oldCenter + (contentHeight * 0.5f - 310f);

            CreateText(body, "Movement Control", 28, TextAnchor.MiddleCenter, new Vector2(0, Y(290)), new Vector2(620, 40));
            CreateText(body, "Only one control style is active at a time.", 20, TextAnchor.MiddleCenter, new Vector2(0, Y(250)), new Vector2(700, 32));
            _movementJoystickButton = CreateButton(body, "Joystick", new Vector2(-160, Y(185)), () => SelectMovementControl(MovementControlType.Joystick));
            _movementTapHoldButton = CreateButton(body, "Tap / Hold", new Vector2(160, Y(185)), () => SelectMovementControl(MovementControlType.TapHold));
            var joystickHint = CreateText(body, "Drag the on-screen joystick to place it. Position locks when you close Settings.", 18, TextAnchor.MiddleCenter, new Vector2(0, Y(125)), new Vector2(748, 40));
            joystickHint.horizontalOverflow = HorizontalWrapMode.Wrap;

            // Survival map playlist genre (campfire BGM stays separate).
            CreateText(body, "Survival Music", 26, TextAnchor.MiddleCenter, new Vector2(0, Y(70)), new Vector2(400, 36));
            _bgmGenreStatusText = CreateText(body, "", 18, TextAnchor.MiddleCenter, new Vector2(0, Y(35)), new Vector2(748, 32));
            _bgmGenreStatusText.horizontalOverflow = HorizontalWrapMode.Wrap;
            _bgmDnBButton = CreateButton(body, "DnB", new Vector2(-160, Y(-15)), () => SelectBgmGenre(GameSave.BgmGenreDnB));
            _bgmMetalButton = CreateButton(body, "Metal", new Vector2(160, Y(-15)), () => SelectBgmGenre(GameSave.BgmGenreMetal));

            CreateText(body, "Music Volume", 26, TextAnchor.MiddleCenter, new Vector2(0, Y(-90)), new Vector2(400, 36));
            _bgmVolumeLabel = CreateText(body, "70%", 22, TextAnchor.MiddleCenter, new Vector2(0, Y(-130)), new Vector2(120, 32));
            CreateButton(body, "−", new Vector2(-200, Y(-130)), () => AdjustBgmVolume(-0.1f));
            CreateButton(body, "+", new Vector2(200, Y(-130)), () => AdjustBgmVolume(0.1f));

            CreateText(body, "SFX Volume", 26, TextAnchor.MiddleCenter, new Vector2(0, Y(-200)), new Vector2(400, 36));
            _sfxVolumeLabel = CreateText(body, "85%", 22, TextAnchor.MiddleCenter, new Vector2(0, Y(-240)), new Vector2(120, 32));
            CreateButton(body, "−", new Vector2(-200, Y(-240)), () => AdjustSfxVolume(-0.1f));
            CreateButton(body, "+", new Vector2(200, Y(-240)), () => AdjustSfxVolume(0.1f));

            CreateText(body, "Accessibility", 26, TextAnchor.MiddleCenter, new Vector2(0, Y(-290)), new Vector2(400, 36));
            _largeDamageNumbersButton = CreateButton(body, "Large Damage Numbers", new Vector2(0, Y(-335)), ToggleLargeDamageNumbers);
            _largeDamageNumbersLabel = _largeDamageNumbersButton != null
                ? _largeDamageNumbersButton.GetComponentInChildren<Text>()
                : null;

            CreateButton(panel.transform, "Close", new Vector2(0, HubLayout.FooterCenterY), () => CloseSettings(), large: true);
            panel.SetActive(false);
            return panel;
        }

        void ToggleLargeDamageNumbers()
        {
            GameSave.LargeDamageNumbers = !GameSave.LargeDamageNumbers;
            RefreshLargeDamageNumbersButton();
        }

        void RefreshLargeDamageNumbersButton()
        {
            if (_largeDamageNumbersLabel == null) return;
            _largeDamageNumbersLabel.text = GameSave.LargeDamageNumbers
                ? "Large Damage Numbers: ON"
                : "Large Damage Numbers: OFF";
        }

        void OpenSettings()
        {
            CloseAllHubPanels();
            GameSave.HasOpenedSettings = true;
            RefreshSettingsPanel();
            if (_settingsPanel != null)
                _settingsPanel.SetActive(true);
            // Allow dragging the stick while Settings is open; lock on close.
            MovementJoystick.EnsureExists();
            MovementJoystick.SetRepositionMode(GameSave.UsesJoystickMovement);
        }

        void CloseSettings()
        {
            MovementJoystick.SetRepositionMode(false);
            if (_settingsPanel != null)
                _settingsPanel.SetActive(false);
        }

        void RefreshSettingsPanel()
        {
            RefreshMovementControlPicker();
            RefreshBgmGenrePicker();
            RefreshVolumeLabels();
            RefreshLargeDamageNumbersButton();
        }

        void SelectBgmGenre(string genre)
        {
            GameSave.BgmGenre = genre;
            RefreshBgmGenrePicker();
            // Rebuild survival playlist immediately if already in a run (or next map enter).
            AudioManager.Instance?.ReloadSurvivalBgmFromSettings();
        }

        void RefreshBgmGenrePicker()
        {
            var metal = string.Equals(GameSave.BgmGenre, GameSave.BgmGenreMetal, System.StringComparison.OrdinalIgnoreCase);
            if (_bgmGenreStatusText != null)
            {
                _bgmGenreStatusText.text = metal
                    ? "Survival maps play Metal. Campfire music is unchanged."
                    : "Survival maps play DnB (default). Campfire music is unchanged.";
            }

            RefreshToggleButton(_bgmDnBButton, selected: !metal, interactable: true, "DnB");
            RefreshToggleButton(_bgmMetalButton, selected: metal, interactable: true, "Metal");
        }

        static void RefreshToggleButton(Button button, bool selected, bool interactable, string label)
        {
            if (button == null) return;
            button.interactable = interactable;
            var image = button.GetComponent<Image>();
            if (image != null)
            {
                image.color = !interactable
                    ? new Color(0.45f, 0.45f, 0.5f, 0.85f)
                    : selected
                        ? new Color(0.35f, 0.72f, 0.42f, 1f)
                        : Color.white;
            }

            var text = button.GetComponentInChildren<Text>();
            if (text != null) text.text = label;
        }

        void AdjustBgmVolume(float delta)
        {
            GameSave.BgmVolume = Mathf.Clamp01(GameSave.BgmVolume + delta);
            AudioManager.Instance?.ApplySavedVolumes();
            RefreshVolumeLabels();
        }

        void AdjustSfxVolume(float delta)
        {
            GameSave.SfxVolume = Mathf.Clamp01(GameSave.SfxVolume + delta);
            AudioManager.Instance?.ApplySavedVolumes();
            RefreshVolumeLabels();
            // Audible click feedback at the new SFX level.
            AudioManager.Instance?.PlaySwingSfx();
        }

        void RefreshVolumeLabels()
        {
            if (_bgmVolumeLabel != null)
                _bgmVolumeLabel.text = $"{Mathf.RoundToInt(GameSave.BgmVolume * 100f)}%";
            if (_sfxVolumeLabel != null)
                _sfxVolumeLabel.text = $"{Mathf.RoundToInt(GameSave.SfxVolume * 100f)}%";
        }

        void SelectMovementControl(MovementControlType controlType)
        {
            GameSave.SelectedMovementControl = controlType;
            MovementJoystick.ApplyControlMode();
            // Keep reposition mode only while Settings is open and joystick is selected.
            var settingsOpen = _settingsPanel != null && _settingsPanel.activeSelf;
            MovementJoystick.SetRepositionMode(settingsOpen && controlType == MovementControlType.Joystick);
            RefreshMovementControlPicker();
        }

        void RefreshMovementControlPicker()
        {
            var selected = GameSave.SelectedMovementControl;
            RefreshMovementControlButton(_movementJoystickButton, MovementControlType.Joystick, selected, "Joystick");
            RefreshMovementControlButton(_movementTapHoldButton, MovementControlType.TapHold, selected, "Tap / Hold");
        }

        static void RefreshMovementControlButton(Button button, MovementControlType mode, MovementControlType selected, string label)
        {
            if (button == null) return;
            button.interactable = true;
            var image = button.GetComponent<Image>();
            if (image != null)
            {
                image.color = selected == mode
                    ? new Color(0.28f, 0.5f, 0.32f, 0.98f)
                    : new Color(0.2f, 0.35f, 0.55f, 0.95f);
            }

            var buttonLabel = button.GetComponentInChildren<Text>();
            if (buttonLabel != null)
                buttonLabel.text = label;
        }
    }
}
