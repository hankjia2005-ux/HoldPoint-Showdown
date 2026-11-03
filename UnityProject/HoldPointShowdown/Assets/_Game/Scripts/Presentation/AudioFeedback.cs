using HoldPointShowdown.Data;
using UnityEngine;

namespace HoldPointShowdown.Presentation
{
    [DisallowMultipleComponent]
    public sealed class AudioFeedback : MonoBehaviour
    {
        public const int PoolSize = 8;
        public const float SpatialMaxDistance = 60f;

        private static AudioClip _pistolClip;
        private static AudioClip _automaticClip;
        private static AudioClip _shotgunClip;
        private static AudioClip _impactClip;
        private static AudioClip _deathClip;
        private static AudioClip _respawnClip;

        private readonly AudioSource[] _sources = new AudioSource[PoolSize];
        private int _nextSource;

        public int SourceCount => _sources.Length;

        private void Awake()
        {
            Initialize();
        }

        public void Initialize()
        {
            EnsureClips();
            EnsurePool();
        }

        public void PlayGunshot(WeaponConfig weapon)
        {
            AudioClip clip = weapon != null && weapon.Archetype == WeaponArchetype.AutoShotgun
                ? _shotgunClip
                : weapon != null && weapon.Archetype != WeaponArchetype.Pistol
                    ? _automaticClip
                    : _pistolClip;
            float pitch = weapon != null ? Mathf.Clamp(1.08f - (weapon.Damage * 0.008f), 0.72f, 1.08f) : 1f;
            Play(clip, 0.86f, pitch);
        }

        public void PlayImpact(float damage)
        {
            Play(_impactClip, Mathf.Clamp(0.24f + (damage * 0.012f), 0.25f, 0.72f), Random.Range(0.92f, 1.08f));
        }

        public void PlayDeath()
        {
            Play(_deathClip, 0.72f, 1f);
        }

        public void PlayRespawn()
        {
            Play(_respawnClip, 0.58f, 1f);
        }

        public void PlayExternalGunshot(float damage = 12f)
        {
            Play(_automaticClip, 0.82f, Mathf.Clamp(1.04f - (damage * 0.005f), 0.82f, 1.05f));
        }

        private void EnsurePool()
        {
            for (int index = 0; index < _sources.Length; index++)
            {
                if (_sources[index] != null)
                {
                    continue;
                }

                GameObject voice = new($"空间音频声源 {index + 1}");
                voice.transform.SetParent(transform, false);
                AudioSource source = voice.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.spatialBlend = 1f;
                source.dopplerLevel = 0f;
                source.rolloffMode = AudioRolloffMode.Linear;
                source.minDistance = 2f;
                source.maxDistance = SpatialMaxDistance;
                _sources[index] = source;
            }
        }

        private void Play(AudioClip clip, float volume, float pitch)
        {
            if (clip == null)
            {
                Initialize();
                clip = _impactClip;
            }

            EnsurePool();
            AudioSource source = _sources[_nextSource];
            _nextSource = (_nextSource + 1) % _sources.Length;
            source.Stop();
            source.clip = clip;
            source.volume = Mathf.Clamp01(volume);
            source.pitch = Mathf.Clamp(pitch, 0.5f, 1.5f);
            source.Play();
        }

        private static void EnsureClips()
        {
            _pistolClip ??= CreateNoiseClip("手枪声", 0.14f, 150f, 0.72f, 31);
            _automaticClip ??= CreateNoiseClip("自动武器声", 0.1f, 110f, 0.58f, 53);
            _shotgunClip ??= CreateNoiseClip("霰弹枪声", 0.22f, 82f, 0.9f, 79);
            _impactClip ??= CreateNoiseClip("命中声", 0.08f, 360f, 0.34f, 101);
            _deathClip ??= CreateToneClip("淘汰声", 0.32f, 118f, 62f, 0.5f);
            _respawnClip ??= CreateToneClip("复活声", 0.36f, 420f, 720f, 0.42f);
        }

        private static AudioClip CreateNoiseClip(string name, float seconds, float toneFrequency, float noiseMix, int seed)
        {
            const int sampleRate = 44100;
            int sampleCount = Mathf.CeilToInt(seconds * sampleRate);
            float[] samples = new float[sampleCount];
            System.Random random = new(seed);
            for (int index = 0; index < sampleCount; index++)
            {
                float time = index / (float)sampleRate;
                float envelope = Mathf.Exp(-time * (8f / seconds));
                float noise = ((float)random.NextDouble() * 2f) - 1f;
                float tone = Mathf.Sin(time * toneFrequency * Mathf.PI * 2f);
                samples[index] = ((noise * noiseMix) + (tone * (1f - noiseMix))) * envelope * 0.8f;
            }

            AudioClip clip = AudioClip.Create(name, sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static AudioClip CreateToneClip(string name, float seconds, float startFrequency, float endFrequency, float volume)
        {
            const int sampleRate = 44100;
            int sampleCount = Mathf.CeilToInt(seconds * sampleRate);
            float[] samples = new float[sampleCount];
            float phase = 0f;
            for (int index = 0; index < sampleCount; index++)
            {
                float normalized = index / (float)Mathf.Max(1, sampleCount - 1);
                float frequency = Mathf.Lerp(startFrequency, endFrequency, normalized);
                phase += frequency / sampleRate;
                float envelope = Mathf.Sin(normalized * Mathf.PI);
                samples[index] = Mathf.Sin(phase * Mathf.PI * 2f) * envelope * volume;
            }

            AudioClip clip = AudioClip.Create(name, sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
