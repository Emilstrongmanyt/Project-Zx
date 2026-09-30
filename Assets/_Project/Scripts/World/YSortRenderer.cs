using UnityEngine;

namespace ProjectZx.World
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class YSortRenderer : MonoBehaviour
    {
        [SerializeField] int sortOffset;
        [SerializeField] float sortYBias;

        SpriteRenderer _renderer;
        float _lastWorldY = float.NaN;
        float _lastCamY = float.NaN;
        // SortDepthScale is 40 → ~0.025 world units per order step.
        const float DirtyEpsilon = 0.02f;

        public void Configure(int offset = 0, float yBias = 0f)
        {
            sortOffset = offset;
            sortYBias = yBias;
            _lastWorldY = float.NaN;
            Apply();
        }

        void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
        }

        void LateUpdate()
        {
            var y = transform.position.y + sortYBias;
            var cam = ArenaBounds.CachedMainCamera;
            var camY = cam != null ? cam.transform.position.y : 0f;
            if (!float.IsNaN(_lastWorldY)
                && Mathf.Abs(y - _lastWorldY) < DirtyEpsilon
                && Mathf.Abs(camY - _lastCamY) < DirtyEpsilon)
                return;

            _lastWorldY = y;
            _lastCamY = camY;
            Apply();
        }

        void Apply()
        {
            if (_renderer == null) _renderer = GetComponent<SpriteRenderer>();
            if (_renderer == null) return;

            _renderer.sortingOrder = ArenaBounds.GetYSortOrder(transform.position.y + sortYBias, sortOffset);
        }
    }
}
