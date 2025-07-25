using UnityEngine;

namespace InGame.Script.Component_Sound
{
    public class PlaySoundExit : StateMachineBehaviour
    {
        [SerializeField] private SoundType sound;
        [SerializeField, Range(0, 1)] private float volume = 1;
        override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            SoundManager.PlaySound(sound, null, volume);
        }
    }
}