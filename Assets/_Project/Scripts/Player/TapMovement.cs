using ProjectZx.Core;
using UnityEngine;

namespace ProjectZx.Player
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class TapMovement : MonoBehaviour
    {
        [SerializeField] float baseSpeed = 4.5f;
        Vector2? _target;
        Camera _camera;
        Rigidbody2D _rb;
        SpriteRenderer _renderer;
        Sprite _idle;
        Sprite _walk;

        void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _renderer = GetComponent<SpriteRenderer>();
            _camera = Camera.main;
            _idle = ArtLibrary.PlayerIdle;
            _walk = ArtLibrary.PlayerWalk;
        }

        void Update()
        {
            ReadPointer();
            UpdateSprite();
        }

        void FixedUpdate()
        {
            if (_target == null)
            {
                _rb.linearVelocity = Vector2.zero;
                return;
            }

            var delta = _target.Value - (Vector2)transform.position;
            if (delta.magnitude < 0.08f)
            {
                _target = null;
                _rb.linearVelocity = Vector2.zero;
                return;
            }

            var speed = baseSpeed * GameSave.SpeedMultiplier;
            _rb.linearVelocity = delta.normalized * speed;
        }

        void ReadPointer()
        {
            if (_camera == null) _camera = Camera.main;
            if (_camera == null) return;

            if (Input.touchCount > 0)
            {
                var touch = Input.GetTouch(0);
                if (touch.phase == TouchPhase.Began || touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary)
                {
                    _target = ScreenToWorld(touch.position);
                }
                return;
            }

            if (Input.GetMouseButton(0))
                _target = ScreenToWorld(Input.mousePosition);
        }

        Vector2 ScreenToWorld(Vector2 screen)
        {
            var z = -_camera.transform.position.z;
            return _camera.ScreenToWorldPoint(new Vector3(screen.x, screen.y, z));
        }

        void UpdateSprite()
        {
            if (_renderer == null) return;
            var moving = _rb.linearVelocity.sqrMagnitude > 0.01f;
            _renderer.sprite = moving ? _walk : _idle;
            if (moving && _rb.linearVelocity.x != 0f)
                _renderer.flipX = _rb.linearVelocity.x < 0f;
        }
    }
}