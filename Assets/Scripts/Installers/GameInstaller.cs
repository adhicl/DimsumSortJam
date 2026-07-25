using Commons;
using Controllers;
using GameObjects;
using IClasses;
using Models;
using Spawners;
using Unity.Burst.Intrinsics;
using UnityEngine;
using Zenject;

public class GameInstaller : MonoInstaller
{
    public Camera mainCamera;
    public SpriteCompleteBasket completeSprite;
    public SoundController soundController;
    public BGMController bgmController;
    public GameController gameController;

    public GameObject dimsumPrefab;
    public GameObject trayPrefab;
    public GameObject characterPrefab;
    
    public override void InstallBindings()
    {
        //
        Container.BindInstance(mainCamera).AsSingle();
        Container.BindInstance(completeSprite).AsSingle();
        Container.BindInstance(soundController).AsSingle();
        Container.BindInstance(gameController).AsSingle();
        Container.BindInstance(bgmController).AsSingle();
        
        Container.Bind<DimsumSpawner>().AsSingle();
        Container.BindMemoryPool<MDimSum, MDimSum.Pool>().FromComponentInNewPrefab(dimsumPrefab);
        Container.Bind<TraySpawner>().AsSingle();
        Container.BindMemoryPool<MTray, MTray.Pool>().FromComponentInNewPrefab(trayPrefab);
        Container.Bind<CharacterSpawner>().AsSingle();
        Container.BindMemoryPool<MCharacter, MCharacter.Pool>().FromComponentInNewPrefab(characterPrefab);
    }
}