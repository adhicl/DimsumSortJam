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
            mixer.SetFloat("SfxVolume", _setting.soundMute ? -80f : 0f);
            _setting.SaveData();
        }
        
        public void ToggleMusic()
        {
            _setting.musicMute = !_setting.musicMute;
            musicSlider.value = _setting.musicMute ? 0 : 1;
            mixer.SetFloat("MusicVolume", _setting.musicMute ? -80f : 0f);
            _setting.SaveData();
        }
        
        public void PlaySoundButton()
        {
            SoundController.Instance.PlayButtonClickClip();            
        }
    }
}