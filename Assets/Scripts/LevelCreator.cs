using System.Collections.Generic;
using Commons;
using TMPro;
using UnityEngine;

namespace DefaultNamespace
{
    public class LevelCreator : MonoBehaviour
    {
        [Header("Level Data")]
        public int TotalGoal;
        public int AvailableVariation;
        
        [Range(1,35)]
        public int TotalVariation;

        [Range(1,15)]
        public int TotalDoubleBasket;
        [Range(1,30)]
        public int TotalSingleBasket;
        
        [Range(6,12)]
        public int TotalBasket;
        [Range(0,3)]
        public int TotalBasketLock;
        [Range(0,3)]
        public int TotalBasketAds;

        [SerializeField] private TextMeshProUGUI DataLevelText;
        [SerializeField] private TextMeshProUGUI DataResultText;

        public LevelData _LevelData;
        
        public void OnTryCreate()
        {
            TotalVariation = _LevelData.TotalVariation;
            TotalGoal = _LevelData.TotalGoal;
            
            //set up available number for each array
            List<int> variations = new List<int>();
            for (int i = 0; i < TotalVariation; i++)
            {
                variations.Add(i);
            }
            
            //set up available array to contain 3 of each random variants
            List<int[]> input = new();
            int totalItem = Mathf.RoundToInt((float) TotalGoal / 3f);
            for (int i = 0; i < totalItem; i++)
            {
                int pickItem = i < TotalVariation ? variations[i] : variations[Random.Range(0, variations.Count)];
                int[] arr = new int[3] { pickItem, pickItem, pickItem };
                input.Add(arr);
            }
            
            string showArr = "";
            foreach (var arr in input)
            {
                showArr += arr+",";
            }
            //Debug.Log($"Array {showArr}");

            // Generate arrays with difficulty bias
            List<int[]> result = GenerateArrays(input);

            //shuffle the array
            //input = Shuffle(input);

            // Print results
            int idx = 1;
            int score = 0;
            List<DimsumCombination> combinations = new();
            foreach (var arr in result)
            {
                DimsumCombination newDimsum = new DimsumCombination(arr[0], arr[1], arr[2]);
                combinations.Add(newDimsum);
                
                score += ScoreArray(arr);
                Debug.Log($"Array {idx} (Score {score}): [{string.Join(", ", arr)}]");
                idx++;
            }

            _LevelData.currentLevel = combinations.ToArray();
            
            Debug.Log($"Total Score {score}");
        }
        
        private List<int> Shuffle(List<int> list)
        {
            System.Random rng = new System.Random();
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                (list[k], list[n]) = (list[n], list[k]);
            }

            return list;
        }

        private List<int[]> GenerateArrays(List<int[]> items)
        {
            List<int[]> groups = new List<int[]>();
            List<int> trashes = new List<int>();
            
            int[] group = new int[3];
            int index = 0;
            int count = 0;
            
            Debug.Log($"Total {items.Count}");
            
            //create double item basket
            for (index = 0; index < TotalDoubleBasket; index++)
            {
                group = items[index];
                int value = group[0];
                int rndIdx = Random.Range(0, 2);
                group[rndIdx] = -1;
                groups.Add(group);
                
                Debug.Log($"1 {string.Join(", ", group)}]");
                
                trashes.Add(value);
            }

            Debug.Log($"Total index 1 {index}");
            
            //put the rest to new list
            for (index = index; index < items.Count; index++)
            {
                for (int i = 0; i < 3; i++)
                    trashes.Add(items[index][i]);
            }
            
            Debug.Log($"Total index 2 {index}");
            
            //create single item basket
            for (int i = 0; i < TotalSingleBasket; i++)
            {
                if (trashes.Count <= 0) break;

                int Rand = Random.Range(0, trashes.Count - 1);
                
                group = new int[3] { -1, -1, -1 };
                int rndPos = Random.Range(0, 2);
                group[rndPos] = trashes[Rand];
                
                trashes.RemoveAt(Rand);
                groups.Add(group);
                
                Debug.Log($"2 {string.Join(", ", group)}]");
            }

            while (trashes.Count > 0)
            {
                if (trashes.Count >= 3)
                {
                    int RandType = Random.Range(0, 1);
                    if (RandType == 0)
                    {
                        group = new int[3] { -1, -1, -1 };
                        int rndPos = Random.Range(0, 2);
                        for (int k = 0; k < 3; k++)
                        {
                            if (rndPos == k) continue;
                            int Rand = Random.Range(0, trashes.Count -1);
                            group[k] = trashes[Rand];
                            trashes.RemoveAt(Rand);
                        }
                        groups.Add(group);
                
                        Debug.Log($"3 {string.Join(", ", group)}]");
                    }
                    else
                    {
                        group = new int[3] { -1, -1, -1 };
                        int rndPos = Random.Range(0, 2);
                        int Rand = Random.Range(0, trashes.Count -1);
                        group[rndPos] = trashes[Rand];
                        trashes.RemoveAt(Rand);
                        groups.Add(group);
                
                        Debug.Log($"4 {string.Join(", ", group)}]");
                    }
                }
                else if (trashes.Count == 2)
                {
                    group = new int[3] { -1, -1, -1 };
                    int rndPos = Random.Range(0, 2);
                    for (int k = 0; k < 3; k++)
                    {
                        if (rndPos == k) continue;
                        int Rand = Random.Range(0, trashes.Count -1);
                        group[k] = trashes[Rand];
                        trashes.RemoveAt(Rand);
                    }
                    groups.Add(group);
                
                    Debug.Log($"5 {string.Join(", ", group)}]");
                }
                else if (trashes.Count == 1)
                {
                    group = new int[3] { -1, -1, -1 };
                    int rndPos = Random.Range(0, 2);
                    int Rand = Random.Range(0, trashes.Count -1);
                    group[rndPos] = trashes[Rand];
                    trashes.RemoveAt(Rand);
                    groups.Add(group);
                
                    Debug.Log($"6 {string.Join(", ", group)}]");
                }
                else
                {
                    Debug.Log("Should not get here");
                }
            }
            Debug.Log($"Total final array length is :{groups.Count}");
            
            
            //shuffle the array
            //input = Shuffle(input);
            
            //int firstItem = items[0];
            // while (items.Count > 0)
            // {
            //     if (items[0] == firstItem) count++;
            //
            //     if (count >= 3)
            //     {
            //         int j = 1;
            //         for (j = 1; j < items.Count; j++)
            //         {
            //             if (items[j] != firstItem)
            //             {
            //                 break;
            //             }
            //         }
            //
            //         if (j == items.Count - 1)
            //         {
            //             group[index] = -1;
            //         }
            //         else
            //         {
            //             group[index] = items[j];
            //             items.RemoveAt(j);
            //         }
            //     }
            //     else
            //     {
            //         group[index] = items[0];
            //         items.RemoveAt(0);
            //     }
            //
            //     if (index == 2)
            //     {
            //         //Debug.Log($"Score for this array is [{group[0]}.{group[1]},{group[2]}] is {ScoreArray(group)} sisa {items.Count}");
            //         groups.Add(group);
            //         index = 0;
            //         count = 0;
            //
            //         if (items.Count > 0)
            //         {
            //             group = new int[3];
            //             firstItem = items[0];
            //         }
            //     }
            //     else
            //     {
            //         index++;
            //     }
            //
            //     // // Bias selection based on difficulty
            //     // bool accept = false;
            //     // switch (difficulty)
            //     // {
            //     //     case 1:
            //     //         accept = (score == 1) || UnityEngine.Random.value < 0.2f; // mostly score 1
            //     //         break;
            //     //     case 2:
            //     //         accept = true; // allow all
            //     //         break;
            //     //     case 3:
            //     //         accept = (score == 3) || UnityEngine.Random.value < 0.2f; // mostly score 3
            //     //         break;
            //     // }
            //     //
            //     // if (accept)
            //     //     groups.Add(group);
            // }
            //
            // if (index > 0)
            // {
            //     for (int i = index; i < 3; i++)
            //     {
            //         group[i] = -1;
            //     }
            //
            //     groups.Add(group);
            // }

            return groups;
        }

        private int ScoreArray(int[] arr)
        {
            List<int> nonNull = new List<int>();
            foreach (var item in arr)
                if (item != -1) nonNull.Add(item);

            if (nonNull.Count == 1) return 1;
            if (nonNull.Count == 2 && nonNull[0] == nonNull[1]) return 1;

            Dictionary<int, int> counts = new Dictionary<int, int>();
            foreach (var item in nonNull)
            {
                if (!counts.ContainsKey(item)) counts[item] = 0;
                counts[item]++;
            }

            if (counts.ContainsValue(2)) return 2;
            if (counts.Count == 3) return 3;

            return 3;
        }
    }
}