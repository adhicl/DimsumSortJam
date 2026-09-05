using System;
using Commons;
using Controllers;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;
using Zenject;

namespace UI
{
    public class SettingPopup : MonoBehaviour
    {
        [Tooltip("Assets/Sounds/GameAudioMixer.mixer. The only thing that actually silences the " +
                 "game - soundMute/musicMute are saved, but nothing else reads them.")]
        public AudioMixer mixer;
        private GameSetting _setting;
        
        [SerializeField] private Slider soundSlider;
        [SerializeField] private Slider musicSlider;

        private void Start()
        {
            _setting = GameController.Instance.GameSetting;
            
            soundSlider.SetValueWithoutNotify(_setting.soundMute ? 0 : 1);
            musicSlider.SetValueWithoutNotify(_setting.musicMute ? 0 : 1);
        }

        public void ToggleSound()
        {
            _setting.soundMute = !_setting.soundMute;
            soundSlider.value = _setting.soundMute ? 0 : 1;
            AudioMix.ApplySound(mixer, _setting.soundMute, this);
            _setting.SaveData();
        }
        
        public void ToggleMusic()
        {
            _setting.musicMute = !_setting.musicMute;
            musicSlider.value = _setting.musicMute ? 0 : 1;
            AudioMix.ApplyMusic(mixer, _setting.musicMute, this);
            _setting.SaveData();
        }
        
        public void PlaySoundButton()
        {
            SoundController.Instance.PlayButtonClickClip();            
        }
    }
}