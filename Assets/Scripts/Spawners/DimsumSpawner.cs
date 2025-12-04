using System;
using System.Collections.Generic;
using Models;
using UnityEngine;
using Zenject;

namespace Spawners
{
    public class DimsumSpawner : MonoBehaviour
    {
        [Inject] private MDimSum.Pool _pool;
        
        readonly List<MDimSum> _dimsums = new List<MDimSum>();

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                int random = UnityEngine.Random.Range(0, 10);
                Create(random);
            }
        }

        public void Create(int dimsumType)
        {
            _dimsums.Add(_pool.Spawn(dimsumType));
        }

        public void Remove(MDimSum dimsum)
        {
            _pool.Despawn(dimsum);
            _dimsums.Remove(dimsum);
        }
    }
}