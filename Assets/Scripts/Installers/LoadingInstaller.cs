using Commons;
using Controllers;
using Zenject;

namespace Installers
{
    public class LoadingInstaller:MonoInstaller
    {
        [Inject] private GameSetting settings;
    
        public override void InstallBindings()
        {
        }
        
    }
}