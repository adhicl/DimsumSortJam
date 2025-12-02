using UnityEngine;
using Zenject;

public class GameInstaller : MonoInstaller
{
    public Camera mainCamera;
    
    public override void InstallBindings()
    {
        Container.BindInstance(mainCamera);
    }
}