using System;
using System.Collections.Generic;
using Assets.HeroEditor.Common.Scripts.CharacterScripts;
using HeroEditor.Common.Data;
using ProjectZx.Core;
using ProjectZx.HeroEditor;
using ProjectZx.Player;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectZx.UI
{
    /// <summary>
    /// First-launch (or forced) mobile character maker for RollZy using HeroEditor Human.
    /// </summary>
    public class CharacterCreatorUi : MonoBehaviour
    {
        public static CharacterCreatorUi Instance { get; private set; }
        public static bool IsOpen => Instance != null && Instance._root != null && Instance._root.activeSelf;

        enum Category
        {
            Hair,
            Eyes,
            Mouth,
            Eyebrows,
            HairColor,
            SkinColor
        }

        static readonly Color32[] HairColors =
        {
            new(40, 30, 25, 255),
            new(90, 55, 30, 255),
            new(150, 50, 0, 255),
            new(200, 160, 80, 255),
            new(220, 200, 160, 255),
            new(60, 60, 70, 255),
            new(180, 40, 40, 255),
            new(40, 90, 160, 255)
        };

        static readonly Color32[] SkinColors =
        {
            new(255, 220, 180, 255),
            new(255, 200, 120, 255),
            new(230, 170, 120, 255),
            new(190, 130, 90, 255),
            new(140, 90, 60, 255),
            new(90, 60, 40, 255)
        };

        GameObject _root;
        Text _titleText;
        Text _categoryText;
        Text _statusText;
        CharacterAppearance _appearance;
        HeroEditorCharacterView _previewView;
        GameObject _previewRoot;
        Category _category = Category.Hair;
        readonly List<ItemSprite> _hair = new();
        readonly List<ItemSprite> _eyes = new();
        readonly List<ItemSprite> _mouth = new();
        readonly List<ItemSprite> _eyebrows = new();
        int _optionIndex;
        Action _onComplete;

        public static void Show(Action onComplete = null)
        {
            if (Instance == null)
            {
                var go = new GameObject("CharacterCreatorUi");
                Instance = go.AddComponent<CharacterCreatorUi>();
                DontDestroyOnLoad(go);
            }

            Instance._onComplete = onComplete;
            Instance.Open();
        }

        void Open()
        {
            EventSystemSetup.EnsureExists();

            if (_root == null)
                BuildUi();

            _appearance = string.IsNullOrEmpty(GameSave.CharacterAppearanceJson)
                ? new CharacterAppearance()
                : CharacterAppearance.FromJson(GameSave.CharacterAppearanceJson);

            EnsurePreview();
            CacheOptions();
            _category = Category.Hair;
            _optionIndex = 0;
            SnapIndexToCurrent();
            ApplyPreview();
            RefreshLabels();
            _root.SetActive(true);
        }

        void EnsurePreview()
        {
            if (_previewRoot != null) return;

            _previewRoot = new GameObject("CreatorPreview");
            _previewRoot.transform.SetParent(transform, false);
            // Centered for landscape orthographic camera (size ~6).
            _previewRoot.transform.position = new Vector3(0f, -0.35f, 0f);
            _previewRoot.transform.localScale = Vector3.one * 0.85f;

            // Dummy sprite renderer so Attach can disable it.
            _previewRoot.AddComponent<SpriteRenderer>().enabled = false;
            _previewView = HeroEditorCharacterView.Attach(_previewRoot, GameSave.SelectedClass, applyLoadout: true);
        }

        void CacheOptions()
        {
            _hair.Clear();
            _eyes.Clear();
            _mouth.Clear();
            _eyebrows.Clear();

            var character = _previewView != null ? _previewView.Character : null;
            var collection = character != null ? character.SpriteCollection : null;
            if (collection == null) return;

            AddCommon(collection.Hair, _hair);
            AddCommon(collection.Eyes, _eyes);
            AddCommon(collection.Mouth, _mouth);
            AddCommon(collection.Eyebrows, _eyebrows);
        }

        static void AddCommon(List<ItemSprite> source, List<ItemSprite> dest)
        {
            if (source == null) return;
            for (var i = 0; i < source.Count; i++)
            {
                var item = source[i];
                if (item == null || string.IsNullOrEmpty(item.Id)) continue;
                // Prefer Common.Basic face parts; allow FantasyHeroes hair too.
                if (item.Id.StartsWith("Common.Basic.", StringComparison.Ordinal)
                    || item.Id.Contains(".Hair.", StringComparison.Ordinal))
                    dest.Add(item);
            }
        }

        void BuildUi()
        {
            var canvasGo = new GameObject("CharacterCreatorCanvas");
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 250;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            // Match HubUi — portrait 1080x1920 put every control off-screen on devices.
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();

            _root = new GameObject("CreatorPanel");
            _root.transform.SetParent(canvasGo.transform, false);
            var rootRt = _root.AddComponent<RectTransform>();
            StretchFull(rootRt);

            // Landscape chrome: top title strip + bottom control dock; center open for preview.
            CreateChromePanel(_root.transform, new Vector2(0, 470), new Vector2(2000, 160));
            CreateChromePanel(_root.transform, new Vector2(0, -420), new Vector2(2000, 280));

            _titleText = CreateLabel(_root.transform, "Create Your Hero", 40, new Vector2(0, 500), new Vector2(900, 56));
            CreateLabel(_root.transform, "Customize look — weapons & gear apply in-game", 22,
                new Vector2(0, 452), new Vector2(1000, 36));

            _categoryText = CreateLabel(_root.transform, "Hair", 26, new Vector2(0, -320), new Vector2(900, 40));
            _statusText = CreateLabel(_root.transform, "", 18, new Vector2(0, -352), new Vector2(1000, 28));
            if (_statusText != null)
                _statusText.color = new Color(0.75f, 0.8f, 0.9f, 1f);

            CreateButton(_root.transform, "◀ Prev", new Vector2(-260, -400), () => Cycle(-1));
            CreateButton(_root.transform, "Next ▶", new Vector2(260, -400), () => Cycle(1));
            CreateButton(_root.transform, "Hair", new Vector2(-520, -470), () => SetCategory(Category.Hair), compact: true);
            CreateButton(_root.transform, "Eyes", new Vector2(-350, -470), () => SetCategory(Category.Eyes), compact: true);
            CreateButton(_root.transform, "Mouth", new Vector2(-180, -470), () => SetCategory(Category.Mouth), compact: true);
            CreateButton(_root.transform, "Brows", new Vector2(-10, -470), () => SetCategory(Category.Eyebrows), compact: true);
            CreateButton(_root.transform, "Hair Dye", new Vector2(180, -470), () => SetCategory(Category.HairColor), compact: true);
            CreateButton(_root.transform, "Skin", new Vector2(350, -470), () => SetCategory(Category.SkinColor), compact: true);
            CreateButton(_root.transform, "Confirm", new Vector2(560, -400), Confirm, large: true);
        }

        static void CreateChromePanel(Transform parent, Vector2 anchoredPos, Vector2 sizeDelta)
        {
            var go = new GameObject("Chrome");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = sizeDelta;
            rt.anchoredPosition = anchoredPos;
            var img = go.AddComponent<Image>();
            img.color = new Color(0.05f, 0.07f, 0.12f, 0.96f);
            img.raycastTarget = false;
        }

        void SetCategory(Category category)
        {
            _category = category;
            SnapIndexToCurrent();
            RefreshLabels();
        }

        void SnapIndexToCurrent()
        {
            switch (_category)
            {
                case Category.Hair:
                    _optionIndex = Mathf.Max(0, _hair.FindIndex(i => i.Id == _appearance.Hair));
                    break;
                case Category.Eyes:
                    _optionIndex = Mathf.Max(0, _eyes.FindIndex(i => i.Id == _appearance.Eyes));
                    break;
                case Category.Mouth:
                    _optionIndex = Mathf.Max(0, _mouth.FindIndex(i => i.Id == _appearance.Mouth));
                    break;
                case Category.Eyebrows:
                    _optionIndex = Mathf.Max(0, _eyebrows.FindIndex(i => i.Id == _appearance.Eyebrows));
                    break;
                case Category.HairColor:
                    _optionIndex = NearestColorIndex(HairColors, _appearance.HairColor);
                    break;
                case Category.SkinColor:
                    _optionIndex = NearestColorIndex(SkinColors, _appearance.BodyColor);
                    break;
            }
        }

        static int NearestColorIndex(Color32[] palette, Color32 current)
        {
            var best = 0;
            var bestDist = int.MaxValue;
            for (var i = 0; i < palette.Length; i++)
            {
                var c = palette[i];
                var d = Mathf.Abs(c.r - current.r) + Mathf.Abs(c.g - current.g) + Mathf.Abs(c.b - current.b);
                if (d < bestDist)
                {
                    bestDist = d;
                    best = i;
                }
            }

            return best;
        }

        void Cycle(int delta)
        {
            var count = CurrentCount();
            if (count <= 0) return;
            _optionIndex = (_optionIndex + delta + count) % count;
            ApplySelection();
            ApplyPreview();
            RefreshLabels();
        }

        int CurrentCount() => _category switch
        {
            Category.Hair => Mathf.Max(1, _hair.Count),
            Category.Eyes => Mathf.Max(1, _eyes.Count),
            Category.Mouth => Mathf.Max(1, _mouth.Count),
            Category.Eyebrows => Mathf.Max(1, _eyebrows.Count),
            Category.HairColor => HairColors.Length,
            Category.SkinColor => SkinColors.Length,
            _ => 1
        };

        void ApplySelection()
        {
            switch (_category)
            {
                case Category.Hair when _hair.Count > 0:
                    _appearance.Hair = _hair[Mathf.Clamp(_optionIndex, 0, _hair.Count - 1)].Id;
                    break;
                case Category.Eyes when _eyes.Count > 0:
                    _appearance.Eyes = _eyes[Mathf.Clamp(_optionIndex, 0, _eyes.Count - 1)].Id;
                    break;
                case Category.Mouth when _mouth.Count > 0:
                    _appearance.Mouth = _mouth[Mathf.Clamp(_optionIndex, 0, _mouth.Count - 1)].Id;
                    break;
                case Category.Eyebrows when _eyebrows.Count > 0:
                    _appearance.Eyebrows = _eyebrows[Mathf.Clamp(_optionIndex, 0, _eyebrows.Count - 1)].Id;
                    break;
                case Category.HairColor:
                    _appearance.HairColor = HairColors[Mathf.Clamp(_optionIndex, 0, HairColors.Length - 1)];
                    _appearance.BeardColor = _appearance.HairColor;
                    break;
                case Category.SkinColor:
                    _appearance.BodyColor = SkinColors[Mathf.Clamp(_optionIndex, 0, SkinColors.Length - 1)];
                    break;
            }
        }

        void ApplyPreview()
        {
            if (_previewView == null || _previewView.Character == null) return;
            try
            {
                _appearance.Setup(_previewView.Character);
                _previewView.RefreshEquipmentAndWeapon();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[CharacterCreator] Preview failed: {ex.Message}");
            }
        }

        void RefreshLabels()
        {
            if (_categoryText == null) return;
            var count = CurrentCount();
            var name = _category switch
            {
                Category.Hair => ShortName(_appearance.Hair),
                Category.Eyes => ShortName(_appearance.Eyes),
                Category.Mouth => ShortName(_appearance.Mouth),
                Category.Eyebrows => ShortName(_appearance.Eyebrows),
                Category.HairColor => $"Hair color {_optionIndex + 1}/{HairColors.Length}",
                Category.SkinColor => $"Skin tone {_optionIndex + 1}/{SkinColors.Length}",
                _ => ""
            };
            _categoryText.text = $"{_category}: {name}  ({_optionIndex + 1}/{count})";

            if (_statusText != null)
            {
                var ready = _previewView != null && _previewView.IsReady;
                _statusText.text = ready
                    ? $"Options loaded — Hair {_hair.Count} · Eyes {_eyes.Count} · Mouth {_mouth.Count} · Brows {_eyebrows.Count}"
                    : "Preview failed to load — check HeroEditor Human prefab / SpriteCollection";
            }
        }

        static string ShortName(string id)
        {
            if (string.IsNullOrEmpty(id)) return "None";
            var dot = id.LastIndexOf('.');
            return dot >= 0 && dot < id.Length - 1 ? id[(dot + 1)..] : id;
        }

        void Confirm()
        {
            GameSave.CharacterAppearanceJson = _appearance.ToJson();
            GameSave.CharacterCreated = true;

            if (_previewRoot != null)
            {
                Destroy(_previewRoot);
                _previewRoot = null;
                _previewView = null;
            }

            if (_root != null)
                _root.SetActive(false);

            CampHeroManager.Instance?.RefreshAppearance();
            _onComplete?.Invoke();
            _onComplete = null;
        }

        static void StretchFull(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        static Text CreateLabel(Transform parent, string text, int size, Vector2 anchoredPos, Vector2 sizeDelta)
        {
            var go = new GameObject("Label");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = sizeDelta;
            rt.anchoredPosition = anchoredPos;
            var t = go.AddComponent<Text>();
            t.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            t.fontSize = size;
            t.alignment = TextAnchor.MiddleCenter;
            t.color = Color.white;
            t.text = text;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            return t;
        }

        static void CreateButton(Transform parent, string label, Vector2 pos, Action onClick, bool large = false, bool compact = false)
        {
            var go = new GameObject(label);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = compact ? new Vector2(150, 56) : large ? new Vector2(320, 90) : new Vector2(200, 70);
            rt.anchoredPosition = pos;
            var img = go.AddComponent<Image>();
            img.color = large ? new Color(0.2f, 0.55f, 0.35f, 1f) : new Color(0.18f, 0.22f, 0.32f, 1f);
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(() => onClick?.Invoke());

            var textGo = new GameObject("Text");
            textGo.transform.SetParent(go.transform, false);
            var trt = textGo.AddComponent<RectTransform>();
            StretchFull(trt);
            var t = textGo.AddComponent<Text>();
            t.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            t.fontSize = compact ? 22 : large ? 34 : 28;
            t.alignment = TextAnchor.MiddleCenter;
            t.color = Color.white;
            t.text = label;
        }
    }
}
