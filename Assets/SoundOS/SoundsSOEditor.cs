#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;

namespace InGame.Script.Component_Sound
{
    [CustomEditor(typeof(SoundsSO))]
    public class SoundsSOEditor : Editor
    {
        private void OnEnable()
        {
            var soundsSo = (SoundsSO)target;
            ref var soundList = ref soundsSo.sounds;

            var names = Enum.GetNames(typeof(SoundType));

            if (soundList != null && soundList.Length == names.Length)
                return;
            var oldSounds = new Dictionary<string, SoundList>();
                
            if (soundList != null)
            {
                foreach (var sound in soundList)
                {
                    if (!string.IsNullOrEmpty(sound.name))
                    {
                        oldSounds[sound.name] = sound;
                    }
                }
            }
                
            soundList = new SoundList[names.Length];
            
            for (int i = 0; i < names.Length; i++)
            {
                string currentName = names[i];
                soundList[i].name = currentName;
                    
                if (oldSounds.TryGetValue(currentName, out var existingSound))
                {
                    soundList[i].volume = existingSound.volume;
                    soundList[i].sounds = existingSound.sounds;
                    soundList[i].mixer = existingSound.mixer;
                }
                else
                {
                    soundList[i].volume = 1f;
                    soundList[i].sounds = Array.Empty<AudioClip>();
                    soundList[i].mixer = null;
                }
            }
                
            EditorUtility.SetDirty(soundsSo);
        }
    }
}
#endif