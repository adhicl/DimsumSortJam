using System;
using System.Collections.Generic;
using Models;
using UnityEngine;
using Zenject;

namespace Spawners
{
    public class DimsumSpawner
    {
        [Inject] private MDimSum.Pool _pool;
        
        readonly List<MDimSum> _dimsums = new();

        // private void Update()
        // {
        //     if (Input.GetKeyDown(KeyCode.Space))
        //     {
        //         int random = UnityEngine.Random.Range(0, 10);
        //         Create(random);
        //     }
        // }

        public MDimSum Create(int dimsumType)
        {
            MDimSum newDimsum = _pool.Spawn(dimsumType);
            _dimsums.Add(newDimsum);
            return newDimsum;
        }

        public void Remove(MDimSum dimsum)
        {
            _pool.Despawn(dimsum);
            _dimsums.Remove(dimsum);
        }
    }
}