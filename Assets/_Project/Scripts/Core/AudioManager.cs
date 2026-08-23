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
        /// <summary>Base mix level before user BGM slider (0–1) is applied.</summary>
        const float BgmMix = 0.45f;
        /// <summary>Base mix level before user SFX slider (0–1) is applied.</summary>
        const float SfxMix = 0.7f;

        public static AudioManager Instance { get; private set; }

        AudioSource _bgmSource;
        AudioSource _bossSource;
        AudioSource _sfxSource;
        readonly List<AudioClip> _bossRoars = new();
        int _bossRoarIndex = -1;
        AudioClip _swing1;
        AudioClip _swing2;
        readonly List<AudioClip> _survivalPlaylist = new();
        int _playlistIndex = -1;
        bool _playlistActive;
        /// <summary>Resources subfolder under Music/ — default DnB; Metal when settings pick it.</summary>
        const string DefaultBgmGenreFolder = "DnB";
        float _bossProximityVolume;
        bool _bossNearby;

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

            _bossSource = gameObject.AddComponent<AudioSource>();
            _bossSource.loop = false;
            _bossSource.playOnAwake = false;
            _bossSource.volume = 0f;

            _sfxSource = gameObject.AddComponent<AudioSource>();
            _sfxSource.loop = false;
            _sfxSource.playOnAwake = false;

            BuildBossRoarPlaylist();
            _swing1 = Resources.Load<AudioClip>("SwingSFX1");
            _swing2 = Resources.Load<AudioClip>("SwingSFX2");
            BuildSurvivalPlaylist();
            ApplySavedVolumes();
        }

        void BuildBossRoarPlaylist()
        {
            _bossRoars.Clear();
            TryAddBossRoar("BossRoar_Deep");
            TryAddBossRoar("BossRoar_Vocal");
            TryAddBossRoar("BossRoar_Troll");
            TryAddBossRoar("BossRoar_Avian");

            // Legacy fallback if the new pack is missing from a build.
            if (_bossRoars.Count == 0)
                TryAddBossRoar("BossJ_SFX");
        }

        void TryAddBossRoar(string resourceName)
        {
            var clip = Resources.Load<AudioClip>(resourceName);
            if (clip != null && !_bossRoars.Contains(clip))
                _bossRoars.Add(clip);
        }

        /// <summary>Re-apply BGM/SFX levels from GameSave (settings menu).</summary>
        public void ApplySavedVolumes()
        {
            if (_bgmSource != null)
                _bgmSource.volume = EffectiveBgmVolume();
            if (_sfxSource != null)
                _sfxSource.volume = EffectiveSfxVolume();
            if (_bossSource != null && _bossSource.isPlaying)
                _bossSource.volume = _bossProximityVolume * GameSave.SfxVolume;
        }

        float EffectiveBgmVolume() => BgmMix * GameSave.BgmVolume;
        float EffectiveSfxVolume() => SfxMix * GameSave.SfxVolume;

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void Update()
        {
            if (_playlistActive && _bgmSource != null && _survivalPlaylist.Count > 0 && !_bgmSource.isPlaying)
                PlayNextSurvivalTrack();

            // Short roar clips: keep cycling while a boss is in range.
            if (_bossNearby && _bossSource != null && _bossRoars.Count > 0 && !_bossSource.isPlaying)
                PlayNextBossRoar();
        }

        public void PlayCampBgm()
        {
            _playlistActive = false;
            PlayBgm("Campfire BGM");
        }

        public void PlayOutsideBgm() => PlaySurvivalPlaylist();

        public void PlayInsideBgm() => PlaySurvivalPlaylist();

        public void PlayDungeonBgm() => PlaySurvivalPlaylist();

        /// <summary>Reload genre playlist from settings (DnB default / Metal) and restart if already playing.</summary>
        public void ReloadSurvivalBgmFromSettings()
        {
            BuildSurvivalPlaylist();
            if (_playlistActive)
                PlaySurvivalPlaylist();
        }

        void PlaySurvivalPlaylist()
        {
            if (_survivalPlaylist.Count == 0)
                BuildSurvivalPlaylist();

            if (_survivalPlaylist.Count == 0)
            {
                _playlistActive = false;
                PlayBgm("InsideBGM");
                return;
            }

            _playlistActive = true;
            // Fresh shuffle order each time we enter these maps.
            ShufflePlaylist();
            PlayNextSurvivalTrack();
        }

        void PlayNextSurvivalTrack()
        {
            if (_survivalPlaylist.Count == 0 || _bgmSource == null) return;
            _playlistIndex = (_playlistIndex + 1) % _survivalPlaylist.Count;
            var clip = _survivalPlaylist[_playlistIndex];
            if (clip == null) return;
            if (_bgmSource.clip == clip && _bgmSource.isPlaying) return;

            _bgmSource.loop = false;
            _bgmSource.clip = clip;
            _bgmSource.volume = EffectiveBgmVolume();
            _bgmSource.Play();
        }

        void BuildSurvivalPlaylist()
        {
            _survivalPlaylist.Clear();

            var folder = ResolveBgmGenreFolder();
            var loaded = Resources.LoadAll<AudioClip>("Music/" + folder);
            if (loaded != null)
            {
                for (var i = 0; i < loaded.Length; i++)
                {
                    var clip = loaded[i];
                    if (clip != null && !_survivalPlaylist.Contains(clip))
                        _survivalPlaylist.Add(clip);
                }
            }

            // If the chosen genre folder is empty, fall back to DnB then Metal.
            if (_survivalPlaylist.Count == 0 && !string.Equals(folder, DefaultBgmGenreFolder, System.StringComparison.OrdinalIgnoreCase))
            {
                loaded = Resources.LoadAll<AudioClip>("Music/" + DefaultBgmGenreFolder);
                if (loaded != null)
                {
                    for (var i = 0; i < loaded.Length; i++)
                    {
                        var clip = loaded[i];
                        if (clip != null && !_survivalPlaylist.Contains(clip))
                            _survivalPlaylist.Add(clip);
                    }
                }
            }

            if (_survivalPlaylist.Count == 0)
            {
                loaded = Resources.LoadAll<AudioClip>("Music/Metal");
                if (loaded != null)
                {
                    for (var i = 0; i < loaded.Length; i++)
                    {
                        var clip = loaded[i];
                        if (clip != null && !_survivalPlaylist.Contains(clip))
                            _survivalPlaylist.Add(clip);
                    }
                }
            }

            _survivalPlaylist.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
        }

        static string ResolveBgmGenreFolder()
        {
            var genre = GameSave.BgmGenre;
            if (string.Equals(genre, "Metal", System.StringComparison.OrdinalIgnoreCase))
                return "Metal";
            return DefaultBgmGenreFolder;
        }

        void ShufflePlaylist()
        {
            for (var i = _survivalPlaylist.Count - 1; i > 0; i--)
            {
                var j = Random.Range(0, i + 1);
                (_survivalPlaylist[i], _survivalPlaylist[j]) = (_survivalPlaylist[j], _survivalPlaylist[i]);
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
            _bgmSource.volume = EffectiveBgmVolume();
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
            _bossNearby = false;
            if (_bossSource == null) return;
            _bossSource.Stop();
            _bossSource.volume = 0f;
        }

        public void UpdateBossJProximity(Transform player)
        {
            if (_bossSource == null || _bossRoars.Count == 0)
                return;

            EnemyActor closestBoss = null;
            var bestDist = float.MaxValue;
            var bosses = Object.FindObjectsByType<EnemyActor>();
            foreach (var enemy in bosses)
            {
                if (enemy == null || !enemy.IsAlive || !enemy.IsBoss) continue;
                var dist = player != null
                    ? ProjectZx.World.ArenaBounds.ToroidalDistance(player.position, enemy.transform.position)
                    : BossMaxHearDistance;
                if (dist >= bestDist) continue;
                bestDist = dist;
                closestBoss = enemy;
            }

            if (closestBoss == null)
            {
                _bossProximityVolume = 0f;
                StopBossSfx();
                return;
            }

            _bossNearby = true;
            var t = 1f - Mathf.Clamp01(bestDist / BossMaxHearDistance);
            _bossProximityVolume = Mathf.Lerp(BossMinVolume, BossMaxVolume, t);
            _bossSource.volume = _bossProximityVolume * GameSave.SfxVolume;

            if (!_bossSource.isPlaying)
                PlayNextBossRoar();
        }

        void PlayNextBossRoar()
        {
            if (_bossSource == null || _bossRoars.Count == 0) return;

            if (_bossRoars.Count == 1)
            {
                _bossRoarIndex = 0;
            }
            else
            {
                // Avoid immediate repeat when multiple roars are available.
                var next = Random.Range(0, _bossRoars.Count);
                if (next == _bossRoarIndex)
                    next = (_bossRoarIndex + 1) % _bossRoars.Count;
                _bossRoarIndex = next;
            }

            var clip = _bossRoars[_bossRoarIndex];
            if (clip == null) return;
            _bossSource.clip = clip;
            _bossSource.loop = false;
            _bossSource.volume = _bossProximityVolume * GameSave.SfxVolume;
            _bossSource.Play();
        }

        public void PlaySwingSfx()
        {
            if (_sfxSource == null) return;
            var clip = Random.value < 0.5f ? _swing1 : _swing2;
            if (clip == null) clip = _swing1 ?? _swing2;
            if (clip == null) return;
            _sfxSource.PlayOneShot(clip, GameSave.SfxVolume);
        }
    }
}
