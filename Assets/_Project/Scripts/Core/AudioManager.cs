using System.Collections.Generic;
using ProjectZx.Enemies;
using UnityEngine;

namespace ProjectZx.Core
{
    public class AudioManager : MonoBehaviour
    {
        const float BossMaxHearDistance = 14f;
        const float BossMinVolume = 0.08f;
        const float BossMaxVolume = 0.95f;
        const float BgmVolume = 0.45f;

        public static AudioManager Instance { get; private set; }

        AudioSource _bgmSource;
        AudioSource _bossSource;
        AudioSource _sfxSource;
        AudioClip _bossClip;
        AudioClip _swing1;
        AudioClip _swing2;
        readonly List<AudioClip> _brothersPlaylist = new();
        int _playlistIndex = -1;
        bool _playlistActive;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            _bgmSource = gameObject.AddComponent<AudioSource>();
            _bgmSource.loop = true;
            _bgmSource.playOnAwake = false;
            _bgmSource.volume = BgmVolume;

            _bossSource = gameObject.AddComponent<AudioSource>();
            _bossSource.loop = true;
            _bossSource.playOnAwake = false;
            _bossSource.volume = 0f;

            _sfxSource = gameObject.AddComponent<AudioSource>();
            _sfxSource.loop = false;
            _sfxSource.playOnAwake = false;
            _sfxSource.volume = 0.7f;

            _bossClip = Resources.Load<AudioClip>("BossJ_SFX");
            _swing1 = Resources.Load<AudioClip>("SwingSFX1");
            _swing2 = Resources.Load<AudioClip>("SwingSFX2");
            BuildBrothersPlaylist();
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void Update()
        {
            if (!_playlistActive || _bgmSource == null || _brothersPlaylist.Count == 0) return;
            if (_bgmSource.isPlaying) return;
            PlayNextBrothersTrack();
        }

        public void PlayCampBgm()
        {
            _playlistActive = false;
            PlayBgm("Campfire BGM");
        }

        public void PlayOutsideBgm()
        {
            _playlistActive = false;
            PlayBgm("Outside BGM1", "OutsideBGM");
        }

        public void PlayInsideBgm()
        {
            // Inside + Dungeon share the 2 Brothers playlist.
            PlayBrothersPlaylist();
        }

        public void PlayDungeonBgm() => PlayBrothersPlaylist();

        void PlayBrothersPlaylist()
        {
            if (_brothersPlaylist.Count == 0)
            {
                _playlistActive = false;
                PlayBgm("InsideBGM");
                return;
            }

            _playlistActive = true;
            // Fresh shuffle order each time we enter these maps.
            ShufflePlaylist();
            PlayNextBrothersTrack();
        }

        void PlayNextBrothersTrack()
        {
            if (_brothersPlaylist.Count == 0 || _bgmSource == null) return;
            _playlistIndex = (_playlistIndex + 1) % _brothersPlaylist.Count;
            var clip = _brothersPlaylist[_playlistIndex];
            if (clip == null) return;
            if (_bgmSource.clip == clip && _bgmSource.isPlaying) return;

            _bgmSource.loop = false;
            _bgmSource.clip = clip;
            _bgmSource.volume = BgmVolume;
            _bgmSource.Play();
        }

        void BuildBrothersPlaylist()
        {
            _brothersPlaylist.Clear();

            // Prefer scanning all Resources audio so long track filenames still match.
            var all = Resources.LoadAll<AudioClip>(string.Empty);
            if (all != null)
            {
                for (var i = 0; i < all.Length; i++)
                {
                    var clip = all[i];
                    if (clip == null || string.IsNullOrEmpty(clip.name)) continue;
                    if (clip.name.StartsWith("2 Brothers", System.StringComparison.OrdinalIgnoreCase))
                        _brothersPlaylist.Add(clip);
                }
            }

            // Explicit fallbacks if LoadAll is empty on some platforms.
            if (_brothersPlaylist.Count == 0)
            {
                TryAddBrothersClip("2 Brothers On The 4th Floor - Come Take My Hand (Extended Version) (From the album 2 1996)");
                TryAddBrothersClip("2 Brothers On The 4th Floor - Fairytales (Charly Lownoise & Mental Theo Rave Edit) (2 1996)");
                TryAddBrothersClip("2 Brothers On The 4th Floor - Fly (From the album 2 1996)");
                TryAddBrothersClip("2 Brothers On The 4th Floor - Happy (Hardcore Megamix) (From the album 2 1996)");
            }

            _brothersPlaylist.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
        }

        void TryAddBrothersClip(string resourceName)
        {
            var clip = Resources.Load<AudioClip>(resourceName);
            if (clip != null && !_brothersPlaylist.Contains(clip))
                _brothersPlaylist.Add(clip);
        }

        void ShufflePlaylist()
        {
            for (var i = _brothersPlaylist.Count - 1; i > 0; i--)
            {
                var j = Random.Range(0, i + 1);
                (_brothersPlaylist[i], _brothersPlaylist[j]) = (_brothersPlaylist[j], _brothersPlaylist[i]);
            }

            _playlistIndex = -1;
        }

        void PlayBgm(string clipName, params string[] fallbackClipNames)
        {
            var clip = LoadBgmClip(clipName, fallbackClipNames);
            if (clip == null || _bgmSource == null) return;
            if (_bgmSource.clip == clip && _bgmSource.isPlaying) return;

            _bgmSource.loop = true;
            _bgmSource.clip = clip;
            _bgmSource.volume = BgmVolume;
            _bgmSource.Play();
        }

        static AudioClip LoadBgmClip(string clipName, params string[] fallbackClipNames)
        {
            var clip = Resources.Load<AudioClip>(clipName);
            if (clip != null) return clip;

            foreach (var fallback in fallbackClipNames)
            {
                if (string.IsNullOrEmpty(fallback)) continue;
                clip = Resources.Load<AudioClip>(fallback);
                if (clip != null) return clip;
            }

            return null;
        }

        public void StopBossSfx()
        {
            if (_bossSource == null) return;
            _bossSource.Stop();
            _bossSource.volume = 0f;
        }

        public void UpdateBossJProximity(Transform player)
        {
            if (_bossSource == null || _bossClip == null)
                return;

            EnemyActor closestBoss = null;
            var bestDist = float.MaxValue;
            var bosses = Object.FindObjectsByType<EnemyActor>();
            foreach (var enemy in bosses)
            {
                if (enemy == null || !enemy.IsAlive || !enemy.IsBoss) continue;
                var dist = player != null
                    ? Vector2.Distance(player.position, enemy.transform.position)
                    : BossMaxHearDistance;
                if (dist >= bestDist) continue;
                bestDist = dist;
                closestBoss = enemy;
            }

            if (closestBoss == null)
            {
                StopBossSfx();
                return;
            }

            if (_bossSource.clip != _bossClip)
                _bossSource.clip = _bossClip;

            if (!_bossSource.isPlaying)
                _bossSource.Play();

            var t = 1f - Mathf.Clamp01(bestDist / BossMaxHearDistance);
            _bossSource.volume = Mathf.Lerp(BossMinVolume, BossMaxVolume, t);
        }

        public void PlaySwingSfx()
        {
            if (_sfxSource == null) return;
            var clip = Random.value < 0.5f ? _swing1 : _swing2;
            if (clip == null) clip = _swing1 ?? _swing2;
            if (clip == null) return;
            _sfxSource.PlayOneShot(clip);
        }
    }
}
