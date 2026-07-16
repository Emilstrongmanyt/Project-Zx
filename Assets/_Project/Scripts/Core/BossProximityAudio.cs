using UnityEngine;

namespace ProjectZx.Core
{
    public class BossProximityAudio : MonoBehaviour
    {
        Transform _player;

        public void BindPlayer(Transform player) => _player = player;

        void Update()
        {
            if (AudioManager.Instance == null) return;
            AudioManager.Instance.UpdateBossJProximity(_player);
        }

        void OnDestroy()
        {
            AudioManager.Instance?.StopBossSfx();
        }
    }
}