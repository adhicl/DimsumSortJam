using System.Collections.Generic;
using Models;
using UnityEngine;
using Zenject;

namespace Spawners
{
    public class CharacterSpawner
    {
        [Inject] private MCharacter.Pool _pool;
        
        readonly List<MCharacter> _characters = new();
        private Vector2 _spawnPosition = new Vector2(-5f, 2.7f);
        
        public MCharacter Create()
        {
            MCharacter newCharacter = _pool.Spawn();
            newCharacter.transform.position = _spawnPosition;
            _characters.Add(newCharacter);
            return newCharacter;
        }

        public void Remove(MCharacter _character)
        {
            _pool.Despawn(_character);
            _characters.Remove(_character);
        }
    }
}