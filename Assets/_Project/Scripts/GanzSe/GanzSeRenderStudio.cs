using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace ProjectZx.GanzSe
{
    /// <summary>
    /// Off-screen stages that render GanzSe modular characters into RenderTextures.
    /// World NPCs and quest portraits sample those RTs so the main Renderer2D camera
    /// never has to draw skinned meshes directly.
    /// </summary>
    public class GanzSeRenderStudio : MonoBehaviour
    {
        public const int Universal3DRendererIndex = 1;
        public const string LayerName = "GanzSe";

        const int RtSize = 256;
        const float SlotSpacing = 8f;

        static GanzSeRenderStudio _instance;

        readonly Dictionary<GanzSeNpcRole, Stage> _stages = new();
        int _layer = -1;
        bool _failed;

        struct Stage
        {
            public GameObject Root;
            public GameObject Character;
            public Camera Camera;
            public RenderTexture Texture;
            public float IdlePhase;
        }

        public static GanzSeRenderStudio Ensure()
        {
            if (_instance != null) return _instance;
            var go = new GameObject("GanzSeRenderStudio");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<GanzSeRenderStudio>();
            return _instance;
        }

        public static bool TryGetTexture(GanzSeNpcRole role, out RenderTexture texture)
        {
            texture = null;
            var studio = Ensure();
            if (studio == null || studio._failed) return false;
            if (!studio._stages.TryGetValue(role, out var stage))
            {
                if (!studio.TryBuildStage(role, out stage))
                    return false;
                studio._stages[role] = stage;
            }

            texture = stage.Texture;
            return texture != null;
        }

        public static void Warm(GanzSeNpcRole role) => TryGetTexture(role, out _);

        void LateUpdate()
        {
            if (_failed) return;
            foreach (var kv in _stages)
            {
                var stage = kv.Value;
                if (stage.Character == null) continue;
                stage.IdlePhase += Time.unscaledDeltaTime;
                // Gentle yaw sway so portraits/world billboards feel alive.
                var yaw = Mathf.Sin(stage.IdlePhase * 1.1f) * 8f;
                stage.Character.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
                _stages[kv.Key] = stage;
            }
        }

        void OnDestroy()
        {
            foreach (var kv in _stages)
            {
                if (kv.Value.Texture != null)
                    kv.Value.Texture.Release();
            }

            _stages.Clear();
            if (_instance == this) _instance = null;
        }

        bool TryBuildStage(GanzSeNpcRole role, out Stage stage)
        {
            stage = default;
            var prefab = GanzSeNpcCatalog.LoadPrefab();
            if (prefab == null)
            {
                Debug.LogWarning("[GanzSe] Modular prefab missing — keeping sprite NPCs.");
                _failed = true;
                return false;
            }

            if (_layer < 0)
            {
                _layer = LayerMask.NameToLayer(LayerName);
                if (_layer < 0)
                {
                    Debug.LogWarning("[GanzSe] Layer 'GanzSe' missing — add it in Tag Manager.");
                    _failed = true;
                    return false;
                }
            }

            var index = _stages.Count;
            var root = new GameObject($"Stage_{role}");
            root.transform.SetParent(transform, false);
            root.transform.position = new Vector3(-400f - index * SlotSpacing, 0f, 0f);

            var lightGo = new GameObject("KeyLight");
            lightGo.transform.SetParent(root.transform, false);
            lightGo.transform.localPosition = new Vector3(0.6f, 2.2f, -1.2f);
            lightGo.transform.localRotation = Quaternion.Euler(35f, -25f, 0f);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.15f;
            light.color = new Color(1f, 0.97f, 0.92f);
            light.cullingMask = 1 << _layer;

            var fillGo = new GameObject("FillLight");
            fillGo.transform.SetParent(root.transform, false);
            fillGo.transform.localRotation = Quaternion.Euler(15f, 140f, 0f);
            var fill = fillGo.AddComponent<Light>();
            fill.type = LightType.Directional;
            fill.intensity = 0.35f;
            fill.color = new Color(0.75f, 0.82f, 1f);
            fill.cullingMask = 1 << _layer;

            var character = Instantiate(prefab, root.transform, false);
            character.name = "Character";
            character.transform.localPosition = Vector3.zero;
            character.transform.localRotation = Quaternion.identity;
            character.transform.localScale = Vector3.one;
            SetLayerRecursive(character, _layer);
            StripDemoComponents(character);
            GanzSeModularOutfit.Apply(character, role);

            var rt = new RenderTexture(RtSize, RtSize, 16, RenderTextureFormat.ARGB32)
            {
                name = $"GanzSe_{role}",
                antiAliasing = 1,
                filterMode = FilterMode.Bilinear
            };
            rt.Create();

            var camGo = new GameObject("StageCamera");
            camGo.transform.SetParent(root.transform, false);
            // Bust / upper-body framing for dialogue + readable camp billboards.
            camGo.transform.localPosition = new Vector3(0f, 1.15f, 2.05f);
            camGo.transform.localRotation = Quaternion.Euler(8f, 180f, 0f);
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            // Transparent clear so world billboards don't show a solid box (URP may still composite alpha).
            cam.backgroundColor = new Color(0f, 0f, 0f, 0f);
            cam.orthographic = false;
            cam.fieldOfView = 28f;
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 20f;
            cam.cullingMask = 1 << _layer;
            cam.targetTexture = rt;
            cam.depth = -100 - index;
            cam.allowHDR = false;
            cam.allowMSAA = false;

            var urp = cam.GetComponent<UniversalAdditionalCameraData>();
            if (urp == null) urp = camGo.AddComponent<UniversalAdditionalCameraData>();
            urp.renderType = CameraRenderType.Base;
            urp.renderPostProcessing = false;
            urp.SetRenderer(Universal3DRendererIndex);

            stage = new Stage
            {
                Root = root,
                Character = character,
                Camera = cam,
                Texture = rt,
                IdlePhase = Random.Range(0f, 10f)
            };
            return true;
        }

        static void StripDemoComponents(GameObject character)
        {
            // Pack demo controller is editor-oriented; outfits are applied by us.
            var behaviours = character.GetComponentsInChildren<MonoBehaviour>(true);
            foreach (var b in behaviours)
            {
                if (b == null) continue;
                var ns = b.GetType().Namespace;
                if (ns == "GanzSe")
                    Destroy(b);
            }
        }

        static void SetLayerRecursive(GameObject go, int layer)
        {
            go.layer = layer;
            var t = go.transform;
            for (var i = 0; i < t.childCount; i++)
                SetLayerRecursive(t.GetChild(i).gameObject, layer);
        }
    }
}
