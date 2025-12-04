using Commons;
using UnityEngine;
using Zenject;

[CreateAssetMenu(fileName = "SettingInstaller", menuName = "Installers/SettingInstaller")]
public class SettingInstaller : ScriptableObjectInstaller<SettingInstaller>
{
    public GameSetting gameSetting;
    
    public override void InstallBindings()
    {
        Container.BindInterfacesAndSelfTo<GameSetting>().FromInstance(gameSetting).AsSingle();
    }
}