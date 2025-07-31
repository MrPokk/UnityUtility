
using UnityEngine;

namespace InGame.Script.Component_Sound
{
    [CreateAssetMenu(menuName = "SOUND/Sounds SO", fileName = "Sounds SO")]
    public class SoundsSO : ScriptableObject
    {
        public SoundList[] sounds;
    }
}