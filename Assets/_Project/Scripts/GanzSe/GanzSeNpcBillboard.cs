using ProjectZx.World;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectZx.GanzSe
{
    /// <summary>
    /// World NPC visual: world-space RawImage fed by the GanzSe studio RenderTexture.
    /// Keeps interaction / Y-sort on the root GameObject.
    /// </summary>
    public class GanzSeNpcBillboard : MonoBehaviour
    {
        GanzSeNpcRole _role;
        SpriteRenderer _fallbackRenderer;
        Canvas _canvas;
        RawImage _raw;
        float _worldHeight = 1.55f;

        public GanzSeNpcRole Role => _role;

        public void Initialize(GanzSeNpcRole role, float worldHeight = 1.55f)
        {
            _role = role;
            _worldHeight = worldHeight;
            _fallbackRenderer = GetComponent<SpriteRenderer>();

            if (!GanzSeRenderStudio.TryGetTexture(role, out var rt) || rt == null)
                return;

            // Hide the placeholder sprite once the 3D RT is available.
            if (_fallbackRenderer != null)
                _fallbackRenderer.enabled = false;

            var canvasGo = new GameObject("GanzSeBillboardCanvas");
            canvasGo.transform.SetParent(transform, false);
            canvasGo.transform.localPosition = new Vector3(0f, worldHeight * 0.45f, 0f);
            canvasGo.transform.localRotation = Quaternion.identity;
            canvasGo.transform.localScale = Vector3.one;

            _canvas = canvasGo.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.WorldSpace;
            _canvas.sortingOrder = 20;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.dynamicPixelsPerUnit = 16f;
            canvasGo.AddComponent<GraphicRaycaster>().enabled = false;

            var rect = canvasGo.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(100f, 100f);
            // World size: 100 canvas units → worldHeight meters.
            var s = worldHeight / 100f;
            rect.localScale = new Vector3(s, s, s);

            var rawGo = new GameObject("Portrait");
            rawGo.transform.SetParent(canvasGo.transform, false);
            var rawRect = rawGo.AddComponent<RectTransform>();
            rawRect.anchorMin = Vector2.zero;
            rawRect.anchorMax = Vector2.one;
            rawRect.offsetMin = Vector2.zero;
            rawRect.offsetMax = Vector2.zero;
            _raw = rawGo.AddComponent<RawImage>();
            _raw.texture = rt;
            _raw.raycastTarget = false;
            _raw.color = Color.white;
        }

        void LateUpdate()
        {
            if (_canvas == null) return;
            _canvas.sortingOrder = ArenaBounds.GetYSortOrder(transform.position.y, 4);
            // Face the camera so perspective doesn't shear the billboard.
            var cam = Camera.main;
            if (cam != null)
                _canvas.transform.rotation = cam.transform.rotation;
        }
    }
}
