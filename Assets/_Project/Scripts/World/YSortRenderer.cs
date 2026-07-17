using UnityEngine;

namespace ProjectZx.World
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class YSortRenderer : MonoBehaviour
    {
        [SerializeField] int sortOffset;
        [SerializeField] float sortYBias;

        SpriteRenderer _renderer;

        public void Configure(int offset = 0, float yBias = 0f)
        {
            sortOffset = offset;
            sortYBias = yBias;
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

            _renderer.sortingOrder = ArenaBounds.GetYSortOrder(transform.position.y + sortYBias, sortOffset);
        }
    }
}