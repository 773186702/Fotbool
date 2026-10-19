using System.Collections.Generic;
using UnityEngine;
using Fotbool.Core;

namespace Fotbool.Audio
{
    /// <summary>
    /// مدير الصوت المركزي للعبة. يشغل تأثيرات المباريات (صافرة، هدف، بطاقة، ركلة، إلخ)
    /// وموسيقى الخلفية (القوائم، الاستراحة، premio). يستخدم ObjectPool if available.
    /// </summary>
    public sealed class SoundManager : MonoBehaviour
    {
        public static SoundManager Instance { get; private set; }

        [Header("Sources")]
        public AudioSource musicSource;
        public AudioSource sfxSource;
        public AudioSource ambientSource;

        [Header("Clips - Match")]
        public AudioClip whistleStart;
        public AudioClip whistleHalfTime;
        public AudioClip whistleFullTime;
        public AudioClip whistleFoul;
        public AudioClip whistleGoal;
        public AudioClip crowdCheer;
        public AudioClip crowdBoo;
        public AudioClip ballKick;
        public AudioClip ballTackle;
        public AudioClip cardYellow;
        public AudioClip cardRed;
        public AudioClip substitution;

        [Header("Clips - Menu")]
        public AudioClip menuClick;
        public AudioClip menuNavigate;
        public AudioClip menuConfirm;
        public AudioClip menuBack;

        [Header("Settings")]
        [Range(0f, 1f)] public float masterVolume = 1f;
        [Range(0f, 1f)] public float musicVolume = 0.5f;
        [Range(0f, 1f)] public float sfxVolume = 0.8f;

        private readonly Dictionary<string, AudioClip> clipCache = new Dictionary<string, AudioClip>();

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            EnsureSource(ref musicSource, "Music");
            EnsureSource(ref sfxSource, "SFX");
            EnsureSource(ref ambientSource, "Ambient");

            musicSource.loop = true;
            musicSource.volume = musicVolume * masterVolume;
            sfxSource.volume = sfxVolume * masterVolume;
            ambientSource.loop = true;
            ambientSource.volume = musicVolume * masterVolume;
        }

        private void EnsureSource(ref AudioSource source, string name)
        {
            if (source == null)
            {
                var go = new GameObject(name);
                go.transform.SetParent(transform);
                source = go.AddComponent<AudioSource>();
            }
        }

        public void PlayMusic(AudioClip clip, bool fade = true)
        {
            if (musicSource == null || clip == null) return;
            if (musicSource.clip == clip && musicSource.isPlaying) return;
            musicSource.clip = clip;
            musicSource.Play();
        }

        public void StopMusic()
        {
            if (musicSource != null) musicSource.Stop();
        }

        public void PlaySFX(AudioClip clip, float volumeScale = 1f)
        {
            if (sfxSource == null || clip == null) return;
            sfxSource.PlayOneShot(clip, sfxVolume * masterVolume * volumeScale);
        }

        public void PlayAmbient(AudioClip clip)
        {
            if (ambientSource == null || clip == null) return;
            ambientSource.clip = clip;
            ambientSource.Play();
        }

        public void StopAmbient()
        {
            if (ambientSource != null) ambientSource.Stop();
        }

        // --- Match events shortcuts ---
        public void PlayWhistleStart() => PlaySFX(whistleStart, 1.2f);
        public void PlayWhistleHalfTime() => PlaySFX(whistleHalfTime, 1f);
        public void PlayWhistleFullTime() => PlaySFX(whistleFullTime, 1f);
        public void PlayWhistleFoul() => PlaySFX(whistleFoul, 0.8f);
        public void PlayGoal() => PlaySFX(whistleGoal, 1.3f);
        public void PlayCrowdCheer() => PlaySFX(crowdCheer, 0.7f);
        public void PlayCrowdBoo() => PlaySFX(crowdBoo, 0.5f);
        public void PlayBallKick() => PlaySFX(ballKick, 0.6f);
        public void PlayBallTackle() => PlaySFX(ballTackle, 0.7f);
        public void PlayYellowCard() => PlaySFX(cardYellow, 1f);
        public void PlayRedCard() => PlaySFX(cardRed, 1.2f);
        public void PlaySubstitution() => PlaySFX(substitution, 0.8f);

        // --- Menu events shortcuts ---
        public void PlayMenuClick() => PlaySFX(menuClick, 0.5f);
        public void PlayMenuNavigate() => PlaySFX(menuNavigate, 0.4f);
        public void PlayMenuConfirm() => PlaySFX(menuConfirm, 0.6f);
        public void PlayMenuBack() => PlaySFX(menuBack, 0.5f);

        public void SetMasterVolume(float v) { masterVolume = Mathf.Clamp01(v); ApplyVolumes(); }
        public void SetMusicVolume(float v) { musicVolume = Mathf.Clamp01(v); ApplyVolumes(); }
        public void SetSfxVolume(float v) { sfxVolume = Mathf.Clamp01(v); ApplyVolumes(); }

        private void ApplyVolumes()
        {
            if (musicSource != null) musicSource.volume = musicVolume * masterVolume;
            if (sfxSource != null) sfxSource.volume = sfxVolume * masterVolume;
            if (ambientSource != null) ambientSource.volume = musicVolume * masterVolume;
        }
    }
}