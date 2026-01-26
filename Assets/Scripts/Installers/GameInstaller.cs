using Commons;
using Controllers;
using GameObjects;
using IClasses;
using Models;
using Spawners;
using UnityEngine;
using Zenject;

public class GameInstaller : MonoInstaller
{
    [Inject] private GameSetting settings;
    
    public Camera mainCamera;
    public SpriteCompleteBasket completeSprite;
    public SoundController soundController;
    public BGMController bgmController;
    public GameController gameController;
    
    public override void InstallBindings()
    {
        Container.BindInstance(mainCamera).AsSingle();
        Container.BindInstance(completeSprite).AsSingle();
        Container.BindInstance(soundController).AsSingle();
        Container.BindInstance(gameController).AsSingle();
        Container.BindInstance(bgmController).AsSingle();
        
        Container.Bind<DimsumSpawner>().AsSingle();
        Container.Bind<TraySpawner>().AsSingle();
        Container.BindMemoryPool<MDimSum, MDimSum.Pool>().FromComponentInNewPrefab(settings.dimsumPrefab);
        Container.BindMemoryPool<MTray, MTray.Pool>().FromComponentInNewPrefab(settings.trayPrefab);
    }
}