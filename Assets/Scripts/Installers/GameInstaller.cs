using Commons;
using Models;
using Spawners;
using UnityEngine;
using Zenject;

public class GameInstaller : MonoInstaller
{
    [Inject] public GameSetting settings;
    
    public Camera mainCamera;
    public GameObject dimsumPrefab;
    
    public override void InstallBindings()
    {
        Container.BindInstance(mainCamera).AsSingle();
        Container.BindMemoryPool<MDimSum, MDimSum.Pool>().FromComponentInNewPrefab(settings.dimsumPrefab);
    }
}