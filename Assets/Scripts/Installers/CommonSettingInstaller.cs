using Commons;
using UnityEngine;
using Zenject;

[CreateAssetMenu(fileName = "CommonSettingInstaller", menuName = "Installers/CommonSettingInstaller")]
public class CommonSettingInstaller : ScriptableObjectInstaller<CommonSettingInstaller>
{
    public CommonSetting commonSetting;
    
    public override void InstallBindings()
    {
        Container.BindInterfacesAndSelfTo<CommonSetting>().FromInstance(commonSetting).AsSingle();
    }
}