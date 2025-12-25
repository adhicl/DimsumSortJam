using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace DefaultNamespace
{
    public class LevelCreator : MonoBehaviour
    {
        [Header("Level Data")]
        public int TotalGoal;
        public int AvailableVariation;
        [Range(10,22)]
        public int TotalVariation;
        
        [Range(6,12)]
        public int TotalBasket;
        [Range(0,3)]
        public int TotalBasketLock;
        [Range(0,3)]
        public int TotalBasketAds;

        [SerializeField] private TextMeshProUGUI DataLevelText;
        [SerializeField] private TextMeshProUGUI DataResultText;
        
        public void OnTryCreate()
        {
            Debug.Log("OnTryCreate");
            
            List<int> variations = new List<int>();
            while (variations.Count < TotalVariation)
            {
                int newItem = Random.Range(0, AvailableVariation);
                if (!variations.Contains(newItem))
                {
                    variations.Add(newItem);
                }
            }
            
            List<int> input = new();
            int totalItem = Mathf.RoundToInt(TotalGoal / 3);
            for (int i = 0; i < totalItem; i++)
            {
                int pickItem = variations[Random.Range(0, variations.Count)];
                input.Add(pickItem);
                input.Add(pickItem);
                input.Add(pickItem);
            }

            for (int i = 0; i < TotalGoal / 3; i++)
            {
                input.Add(-1);
            }

            input = Shuffle(input);
            //
            // string showArr = "";
            // foreach (var arr in input)
            // {
            //     showArr += arr+",";
            // }
            // Debug.Log($"Array {showArr}");

            // Generate arrays with difficulty bias
            List<int[]> result = GenerateArrays(input);

            // Print results
            int idx = 1;
            int score = 0;
            foreach (var arr in result)
            {
                score += ScoreArray(arr);
                Debug.Log($"Array {idx} (Score {score}): [{string.Join(", ", arr)}]");
                idx++;
            }
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

        private List<int[]> GenerateArrays(List<int> items)
        {
            List<int[]> groups = new List<int[]>();
            
            int[] group = new int[3];
            int index = 0;
            int count = 0;
            int firstItem = items[0];

            while (items.Count > 0)
            {
                if (items[0] == firstItem) count++;

                if (count >= 3)
                {
                    int j = 1;
                    for (j = 1; j < items.Count; j++)
                    {
                        if (items[j] != firstItem)
                        {
                            break;
                        }
                    }

                    if (j == items.Count - 1)
                    {
                        group[index] = -1;
                    }
                    else
                    {
                        group[index] = items[j];
                        items.RemoveAt(j);
                    }
                }
                else
                {
                    group[index] = items[0];
                    items.RemoveAt(0);
                }

                if (index == 2)
                {
                    //Debug.Log($"Score for this array is [{group[0]}.{group[1]},{group[2]}] is {ScoreArray(group)} sisa {items.Count}");
                    groups.Add(group);

                    index = 0;
                    count = 0;
                    group = new int[3];
                    firstItem = items[0];
                }
                else
                {
                    index++;
                }

                // // Bias selection based on difficulty
                // bool accept = false;
                // switch (difficulty)
                // {
                //     case 1:
                //         accept = (score == 1) || UnityEngine.Random.value < 0.2f; // mostly score 1
                //         break;
                //     case 2:
                //         accept = true; // allow all
                //         break;
                //     case 3:
                //         accept = (score == 3) || UnityEngine.Random.value < 0.2f; // mostly score 3
                //         break;
                // }
                //
                // if (accept)
                //     groups.Add(group);
            }

            if (index > 0)
            {
                for (int i = index; i < 3; i++)
                {
                    group[i] = -1;
                }

                groups.Add(group);
            }

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