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
        
        /// <summary>
        /// Deals a piece of <paramref name="dimsumType"/>, iced over when <paramref name="frozen"/>.
        /// The flag has to come in through the pool: a pooled piece is reused rather than built,
        /// so setting it afterwards would leave one frame in which the piece is drawn as food and
        /// can be grabbed.
        /// </summary>
        public MDimSum Create(int dimsumType, bool frozen = false)
        {
            MDimSum newDimsum = _pool.Spawn(dimsumType, frozen);
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