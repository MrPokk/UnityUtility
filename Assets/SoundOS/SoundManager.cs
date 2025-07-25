using System;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Serialization;

namespace InGame.Script.Component_Sound
{
    [RequireComponent(typeof(AudioSource))]
    public class SoundManager : MonoBehaviour
    {
        [FormerlySerializedAs("_so")]
        [SerializeField] private SoundsSO So;
        private static SoundManager _instance = null;
        private AudioSource _audioSource;
        private AudioSource _musicSource; 
        private SoundType _currentMusicType;
        private AudioClip _currentMusicClip;
        private bool _isMusicLooping = false;

        private void Start()
        {
            if (_instance)
                return;
            
            _instance = this;
            _audioSource = GetComponent<AudioSource>();
                
            _musicSource = gameObject.AddComponent<AudioSource>();
            _musicSource.loop = true; 
                
            PlayMusic(SoundType.Music);
                
            DontDestroyOnLoad(gameObject);
        }

        private void Update()
        {
            if (_isMusicLooping && !_musicSource.isPlaying)
                _musicSource.Play();
        }

        public static void PlaySound(SoundType sound, AudioSource source = null, float volume = 1)
        {
            if (_instance == null || _instance.So == null || _instance.So.sounds == null)
            {
                Debug.LogWarning("SoundManager not initialized or SoundsSO not set");
                return;
            }

            var soundIndex = (int)sound;
            if (soundIndex < 0 || soundIndex >= _instance.So.sounds.Length)
            {
                Debug.LogWarning($"Invalid sound index: {soundIndex} (SoundType: {sound})");
                return;
            }

            var soundList = _instance.So.sounds[soundIndex];
            if (soundList.sounds == null || soundList.sounds.Length == 0)
            {
                Debug.LogWarning($"No audio clips defined for sound type: {sound}");
                return;
            }

            var randomClip = soundList.sounds[UnityEngine.Random.Range(0, soundList.sounds.Length)];
    
            if (source != null)
            {
                source.outputAudioMixerGroup = soundList.mixer;
                source.clip = randomClip;
                source.volume = volume * soundList.volume;
                source.Play();
            }
            else
            {
                _instance._audioSource.outputAudioMixerGroup = soundList.mixer;
                _instance._audioSource.PlayOneShot(randomClip, volume * soundList.volume);
            }
        }

        public static void PlayMusic(SoundType musicType, bool loop = true, float volume = 1f)
        {
            var musicList = _instance.So.sounds[(int)musicType];
            var clips = musicList.sounds;
            
            if (clips.Length == 0) return;
            
            var clip = clips[UnityEngine.Random.Range(0, clips.Length)];
            
            _instance._musicSource.outputAudioMixerGroup = musicList.mixer;
            _instance._musicSource.clip = clip;
            _instance._musicSource.volume = volume * musicList.volume;
            _instance._musicSource.loop = loop;
            _instance._isMusicLooping = loop;
            _instance._currentMusicType = musicType;
            _instance._currentMusicClip = clip;
            
            _instance._musicSource.Play();
        }

        public static void StopMusic()
        {
            if (!_instance._musicSource.isPlaying)
                return;
            _instance._musicSource.Stop();
            _instance._isMusicLooping = false;
        }

        public static void PauseMusic()
        {
            if (_instance._musicSource.isPlaying)
                _instance._musicSource.Pause();
        }

        public static void ResumeMusic()
        {
            if (!_instance._musicSource.isPlaying && _instance._currentMusicClip != null)
                _instance._musicSource.Play();
        }
        
    }

    [Serializable]
    public struct SoundList
    {
        [HideInInspector] public string name;
        [Range(0, 1)] public float volume;
        public AudioMixerGroup mixer;
        public AudioClip[] sounds;
    }
}