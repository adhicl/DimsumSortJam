using System.Collections.Generic;
using Models;
using UnityEngine;
using Zenject;

namespace Spawners
{
    public class TraySpawner
    {
        [Inject] private MTray.Pool _pool;
        
        readonly List<MTray> _trays = new();
        
        public MTray Create(Transform parent)
        {
            MTray newTray = _pool.Spawn();
            newTray.transform.SetParent(parent);
            _trays.Add(newTray);
            return newTray;
        }

        public void Remove(MTray tray)
        {
            tray.transform.SetParent(null);
            _pool.Despawn(tray);
            _trays.Remove(tray);
        }
    }
}