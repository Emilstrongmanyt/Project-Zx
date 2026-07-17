using UnityEngine;

namespace ProjectZx.World
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class YSortRenderer : MonoBehaviour
    {
        const float SortPrecision = 100f;

        [SerializeField] int sortOffset;

        SpriteRenderer _renderer;

        public void Configure(int offset = 0)
        {
            sortOffset = offset;
            Apply();
        }

        void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
        }

        void LateUpdate()
        {
            Apply();
        }

        void Apply()
        {
            if (_renderer == null) _renderer = GetComponent<SpriteRenderer>();
            if (_renderer == null) return;

            _renderer.sortingOrder = Mathf.RoundToInt(-transform.position.y * SortPrecision) + sortOffset;
        }
    }
}