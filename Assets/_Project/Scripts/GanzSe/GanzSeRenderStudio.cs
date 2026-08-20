using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace ProjectZx.GanzSe
{
    /// <summary>
    /// Off-screen GanzSe stages. Builds a live RenderTexture (quest portraits) and a
    /// baked Sprite (camp billboards) so NPCs work with the project's Renderer2D camera.
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
        bool _loggedFail;

        struct Stage
        {
            public GameObject Root;
            public GameObject Character;
            public Camera Camera;
            public RenderTexture Texture;
            public Sprite BakedSprite;
            public Texture2D BakedTexture;
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

        public static bool IsReady =>
            Ensure() is { _failed: false } studio
            && GanzSeNpcCatalog.LoadPrefab() != null;

        public static bool TryGetTexture(GanzSeNpcRole role, out RenderTexture texture)
        {
            texture = null;
            if (!TryGetStage(role, out var stage)) return false;
            texture = stage.Texture;
            return texture != null;
        }

        public static bool TryGetSprite(GanzSeNpcRole role, out Sprite sprite)
        {
            sprite = null;
            if (!TryGetStage(role, out var stage)) return false;
            sprite = stage.BakedSprite;
            return sprite != null;
        }

        public static void Warm(GanzSeNpcRole role) => TryGetStage(role, out _);

        static bool TryGetStage(GanzSeNpcRole role, out Stage stage)
        {
            stage = default;
            var studio = Ensure();
            if (studio == null || studio._failed) return false;
            if (studio._stages.TryGetValue(role, out stage))
                return stage.Texture != null || stage.BakedSprite != null;

            if (!studio.TryBuildStage(role, out stage))
                return false;
            studio._stages[role] = stage;
            return true;
        }

        void LateUpdate()
        {
            if (_failed) return;
            foreach (var kv in _stages)
            {
                var stage = kv.Value;
                if (stage.Character == null) continue;
                stage.IdlePhase += Time.unscaledDeltaTime;
                var yaw = Mathf.Sin(stage.IdlePhase * 1.1f) * 8f;
                stage.Character.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
                _stages[kv.Key] = stage;
            }
        }

        void OnDestroy()
        {
            foreach (var kv in _stages)
            {
                if (kv.Value.Texture != null) kv.Value.Texture.Release();
                if (kv.Value.BakedTexture != null) Destroy(kv.Value.BakedTexture);
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
                Fail("Modular prefab missing from Resources/GanzSe/ModularCharacter.");
                return false;
            }

            if (_layer < 0)
            {
                _layer = LayerMask.NameToLayer(LayerName);
                if (_layer < 0)
                {
                    Fail("Layer 'GanzSe' missing from Tag Manager.");
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
            light.intensity = 1.35f;
            light.color = new Color(1f, 0.97f, 0.92f);
            light.cullingMask = 1 << _layer;

            var fillGo = new GameObject("FillLight");
            fillGo.transform.SetParent(root.transform, false);
            fillGo.transform.localRotation = Quaternion.Euler(15f, 140f, 0f);
            var fill = fillGo.AddComponent<Light>();
            fill.type = LightType.Directional;
            fill.intensity = 0.45f;
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

            var rt = new RenderTexture(RtSize, RtSize, 24, RenderTextureFormat.ARGB32)
            {
                name = $"GanzSe_{role}",
                antiAliasing = 1,
                filterMode = FilterMode.Bilinear
            };
            rt.Create();

            var camGo = new GameObject("StageCamera");
            camGo.transform.SetParent(root.transform, false);
            camGo.transform.localPosition = new Vector3(0f, 1.05f, 2.2f);
            camGo.transform.localRotation = Quaternion.Euler(6f, 180f, 0f);
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.1f, 0.12f, 0.16f, 0f);
            cam.orthographic = false;
            cam.fieldOfView = 30f;
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 20f;
            cam.cullingMask = 1 << _layer;
            cam.targetTexture = rt;
            cam.depth = -100 - index;
            cam.allowHDR = false;
            cam.allowMSAA = false;
            cam.enabled = true;

            var urp = cam.GetComponent<UniversalAdditionalCameraData>();
            if (urp == null) urp = camGo.AddComponent<UniversalAdditionalCameraData>();
            urp.renderType = CameraRenderType.Base;
            urp.renderPostProcessing = false;
            urp.SetRenderer(Universal3DRendererIndex);

            // Force a bake so camp SpriteRenderers work even if live RT cameras misbehave.
            cam.Render();
            var bakedTex = new Texture2D(RtSize, RtSize, TextureFormat.RGBA32, false)
            {
                name = $"GanzSeBake_{role}",
                filterMode = FilterMode.Bilinear
            };
            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            bakedTex.ReadPixels(new Rect(0, 0, RtSize, RtSize), 0, 0);
            bakedTex.Apply(false, false);
            RenderTexture.active = prev;

            if (!BakeLooksValid(bakedTex))
            {
                Object.Destroy(bakedTex);
                Object.Destroy(root);
                rt.Release();
                Fail(
                    $"3D bake for {role} was empty — Universal3DRenderer may be missing or invalid. " +
                    "Check Settings/UniversalRP has renderer index 1.");
                return false;
            }

            var bakedSprite = Sprite.Create(
                bakedTex,
                new Rect(0f, 0f, RtSize, RtSize),
                new Vector2(0.5f, 0.06f),
                100f);
            bakedSprite.name = $"GanzSeSprite_{role}";

            stage = new Stage
            {
                Root = root,
                Character = character,
                Camera = cam,
                Texture = rt,
                BakedSprite = bakedSprite,
                BakedTexture = bakedTex,
                IdlePhase = Random.Range(0f, 10f)
            };
            return true;
        }

        static bool BakeLooksValid(Texture2D tex)
        {
            if (tex == null) return false;
            var pixels = tex.GetPixels32();
            var lit = 0;
            for (var i = 0; i < pixels.Length; i++)
            {
                var p = pixels[i];
                if (p.a < 12) continue;
                if (p.r + p.g + p.b > 40) lit++;
            }

            return lit > pixels.Length / 200;
        }

        void Fail(string reason)
        {
            _failed = true;
            if (_loggedFail) return;
            _loggedFail = true;
            Debug.LogError("[GanzSe] " + reason + " Falling back to legacy NPC sprites.");
        }

        static void StripDemoComponents(GameObject character)
        {
            var behaviours = character.GetComponentsInChildren<MonoBehaviour>(true);
            foreach (var b in behaviours)
            {
                if (b == null) continue;
                if (b.GetType().Namespace == "GanzSe")
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
