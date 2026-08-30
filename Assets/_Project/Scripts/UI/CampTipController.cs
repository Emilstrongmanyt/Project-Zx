using ProjectZx.Core;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectZx.UI
{
    /// <summary>
    /// Rotating beginner tips on the camp hub. Calm cadence; pauses while menus are open.
    /// </summary>
    public sealed class CampTipController : MonoBehaviour
    {
        const float FirstDelaySeconds = 2.5f;
        const float DisplaySeconds = 5.5f;
        const float IntervalSeconds = 55f;
        const float FadeSeconds = 0.35f;

        CanvasGroup _group;
        Text _text;
        float _timer;
        int _nextIndex;
        enum Phase { WaitFirst, Show, WaitNext }
        Phase _phase = Phase.WaitFirst;
        float _phaseElapsed;

        public static CampTipController EnsureExists()
        {
            var existing = Object.FindFirstObjectByType<CampTipController>();
            if (existing != null) return existing;
            var go = new GameObject("CampTipController");
            return go.AddComponent<CampTipController>();
        }

        void Awake()
        {
            BuildUi();
            _timer = FirstDelaySeconds;
            _phase = Phase.WaitFirst;
            if (_group != null) _group.alpha = 0f;
        }

        void BuildUi()
        {
            EventSystemSetup.EnsureExists();

            var canvasGo = new GameObject("CampTipCanvas");
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 60;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            canvasGo.AddComponent<GraphicRaycaster>();

            var chip = new GameObject("TipChip");
            chip.transform.SetParent(canvasGo.transform, false);
            var chipRect = chip.AddComponent<RectTransform>();
            chipRect.anchorMin = new Vector2(0.5f, 0f);
            chipRect.anchorMax = new Vector2(0.5f, 0f);
            chipRect.pivot = new Vector2(0.5f, 0f);
            chipRect.anchoredPosition = new Vector2(0f, 28f);
            chipRect.sizeDelta = new Vector2(920f, 64f);

            _group = chip.AddComponent<CanvasGroup>();
            _group.blocksRaycasts = false;
            _group.interactable = false;
            _group.alpha = 0f;

            var bg = chip.AddComponent<Image>();
            if (StoneUi.Available && StoneUi.ResourceBarBg != null)
            {
                bg.sprite = StoneUi.ResourceBarBg;
                bg.type = Image.Type.Sliced;
                bg.color = Color.white;
            }
            else
            {
                bg.color = new Color(0.08f, 0.07f, 0.1f, 0.88f);
            }

            var textGo = new GameObject("TipText");
            textGo.transform.SetParent(chip.transform, false);
            var textRect = textGo.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(22f, 8f);
            textRect.offsetMax = new Vector2(-22f, -8f);
            _text = textGo.AddComponent<Text>();
            _text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _text.fontSize = 22;
            _text.fontStyle = FontStyle.Bold;
            _text.color = new Color(1f, 0.94f, 0.82f);
            _text.alignment = TextAnchor.MiddleCenter;
            _text.horizontalOverflow = HorizontalWrapMode.Wrap;
            _text.verticalOverflow = VerticalWrapMode.Truncate;
            _text.raycastTarget = false;
            _text.text = "";
        }

        void Update()
        {
            if (_group == null || _text == null) return;

            // Pause rotation while any hub menu is open.
            if (HubUi.Instance != null && HubUi.Instance.IsAnyMenuOpen)
            {
                if (_group.alpha > 0.01f)
                    _group.alpha = Mathf.MoveTowards(_group.alpha, 0f, Time.unscaledDeltaTime / FadeSeconds);
                return;
            }

            _phaseElapsed += Time.unscaledDeltaTime;

            switch (_phase)
            {
                case Phase.WaitFirst:
                case Phase.WaitNext:
                    if (_group.alpha > 0.01f)
                        _group.alpha = Mathf.MoveTowards(_group.alpha, 0f, Time.unscaledDeltaTime / FadeSeconds);
                    if (_phaseElapsed >= _timer)
                        TryShowNextTip();
                    break;

                case Phase.Show:
                    _group.alpha = Mathf.MoveTowards(_group.alpha, 1f, Time.unscaledDeltaTime / FadeSeconds);
                    var showFor = _usingForcedDisplay ? _forcedDisplaySeconds : DisplaySeconds;
                    if (_phaseElapsed >= showFor)
                    {
                        _usingForcedDisplay = false;
                        _phase = Phase.WaitNext;
                        _phaseElapsed = 0f;
                        _timer = IntervalSeconds;
                    }
                    break;
            }
        }

        void TryShowNextTip()
        {
            if (BeginnerTipCatalog.Count <= 0) return;

            for (var attempt = 0; attempt < BeginnerTipCatalog.Count; attempt++)
            {
                var tip = BeginnerTipCatalog.Get(_nextIndex++);
                if (!BeginnerTipCatalog.IsEligible(tip.Id)) continue;

                _text.text = tip.Text;
                _phase = Phase.Show;
                _phaseElapsed = 0f;
                return;
            }

            // Nothing eligible — wait a long beat before retrying.
            _phase = Phase.WaitNext;
            _phaseElapsed = 0f;
            _timer = IntervalSeconds * 2f;
        }

        /// <summary>One-shot camp message (quest ready, etc.) — interrupts the tip rotation briefly.</summary>
        public void ShowForcedTip(string message, float displaySeconds = 6.5f)
        {
            if (_text == null || _group == null || string.IsNullOrEmpty(message)) return;
            EnsureUi();
            _text.text = message;
            _phase = Phase.Show;
            _phaseElapsed = 0f;
            // Borrow Show phase length via temporary stretch of elapsed target.
            _forcedDisplaySeconds = Mathf.Max(3f, displaySeconds);
            _usingForcedDisplay = true;
            _group.alpha = 1f;
        }

        float _forcedDisplaySeconds = DisplaySeconds;
        bool _usingForcedDisplay;

        void EnsureUi()
        {
            if (_group == null || _text == null)
                BuildUi();
        }
    }
}
