using Commons;
using Controllers;
using UnityEngine;
using Zenject;

namespace Installers
{
    public class HomeInstaller : MonoInstaller
    {
        public SoundController soundController;
    
        public override void InstallBindings()
        {
            Container.BindInstance(soundController).AsSingle();
        }
        
    }
}