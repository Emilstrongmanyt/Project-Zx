using UnityEngine;

namespace ProjectZx.World
{
    public class CenterCamera : MonoBehaviour
    {
        Transform _target;

        public void BindWhenReady()
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) _target = player.transform;
        }

        void LateUpdate()
        {
            if (_target == null)
            {
                BindWhenReady();
                return;
            }

            transform.position = new Vector3(_target.position.x, _target.position.y, transform.position.z);
        }
    }
}