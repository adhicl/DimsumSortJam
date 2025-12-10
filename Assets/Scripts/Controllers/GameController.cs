using System;
using System.Linq;
using Commons;
using IClasses;
using Models;
using Spawners;
using UnityEngine;
using Zenject;
using Zenject.SpaceFighter;
using Random = System.Random;

namespace Controllers
{
    public class GameController : MonoBehaviour
    {
        [Inject] DimsumSpawner _dimsumSpawner;
        [Inject] GameSetting _gameSetting;

        [SerializeField] private Transform[] baskets;

        private void Start()
        {
            CreateLevel();
        }

        private void CreateLevel()
        {
            int[][] currentLevel = _gameSetting.currentLevel;

            Random rand = new Random();

            // Shuffle and take 6
            var randomPick = currentLevel
                .OrderBy(x => rand.Next())
                .Take(6)
                .ToArray();

            // Print result
            for (int j = 0; j < randomPick.Length; j++) 
            {
                int[] row = randomPick[j];
                IDropable dropBasket = baskets[j].GetComponent<IDropable>();
                for (int i = 0; i < row.Length; i++)
                {
                    if (row[i] != -1)
                    {
                        MDimSum newDimsum = _dimsumSpawner.Create(row[i]);
                        newDimsum.DoDropPlaceAt(dropBasket, i, false);
                    }
                    else
                    {
                        
                    }
                }
            }
        }
    }
}