using Commons;
using UnityEngine;
using Zenject;

namespace Installers
{
    public class ProjectInstaller : MonoInstaller
    {
        [Inject] private GameSetting _settings;

        public override void InstallBindings()
        {
        }

        
    }
}