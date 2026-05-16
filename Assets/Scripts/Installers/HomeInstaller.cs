using Commons;
using Controllers;
using UnityEngine;
using Zenject;

namespace Installers
{
    public class HomeInstaller : MonoInstaller
    {
        [Inject] private GameSetting settings;
        public SoundController soundController;
    
        public override void InstallBindings()
        {
            Container.BindInstance(soundController).AsSingle();
        }
        
    }
}