using System.Collections.Generic;
using Commons;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class LevelConfigEditor : EditorWindow
{
    [SerializeField]
    private VisualTreeAsset m_VisualTreeAsset = default;

    [MenuItem("Game Config/LevelConfigEditor")]
    public static void ShowExample()
    {
        LevelConfigEditor wnd = GetWindow<LevelConfigEditor>();
        wnd.titleContent = new GUIContent("LevelConfigEditor");
    }

    public void CreateGUI()
    {
        // Each editor window contains a root VisualElement object
        VisualElement root = rootVisualElement;

        // Instantiate UXML
        VisualElement labelFromUXML = m_VisualTreeAsset.Instantiate();
        root.Add(labelFromUXML);
        
        Button bGenerate = root.Q<Button>("bGenerate");
        Button bSaveData = root.Q<Button>("bSaveToObject");

        bGenerate.RegisterCallback<ClickEvent>(GenerateLevelMethod);
        bSaveData.RegisterCallback<ClickEvent>(SaveLevel);
    }

    private void GenerateLevelMethod(ClickEvent evt)
    {
        // Each editor window contains a root VisualElement object
        VisualElement root = rootVisualElement;
        SliderInt iAvailableVariation = root.Q<SliderInt>("iAvailableVariation");
        SliderInt iMaxVariation = root.Q<SliderInt>("iMaxVariation");
        SliderInt iTotalGoal = root.Q<SliderInt>("iTotalGoal");
        SliderInt iTotalEmpty = root.Q<SliderInt>("iTotalEmpty");

        int TotalVariation = iMaxVariation.value;
        int AvailableVariation = iAvailableVariation.value;
        int TotalGoal = iTotalGoal.value;
        int TotalEmpty = iTotalEmpty.value;
        
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

        for (int i = 0; i < TotalEmpty; i++)
        {
            input.Add(-1);
        }

        input = Shuffle(input);
        
        TextField txtResultField = root.Q<TextField>("txtResultField");

        txtResultField.value = "";
        
        // Generate arrays with difficulty bias
        List<int[]> result = GenerateArrays(input);

        // Print results
        int idx = 1;
        int score = 0;
        foreach (var arr in result)
        {
            int scoreArray = ScoreArray(arr);
            txtResultField.value += $"Array {idx} (Score {score}): [{string.Join(", ", arr)}]\n";
            score += scoreArray;
            idx++;
        }

        float scoreAverage = (float) score / (float) result.Count;
        txtResultField.value += $"Total Score {score} from {result.Count} items with average score {scoreAverage}";

        CurrentResult = result;
    }

    private List<int[]> CurrentResult;
    
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
                else if (index < 2)
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
                if (!(group[0] == -1 && group[1] == -1 && group[2] == -1))
                {
                    groups.Add(group);
                }

                index = 0;
                count = 0;
                group = new int[3];
                if (items.Count > 0)
                {
                    firstItem = items[0];
                }
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
            if (!(group[0] == -1 && group[1] == -1 && group[2] == -1))
            {
                groups.Add(group);
            }
        }

        return groups;
    }

    private int ScoreArray(int[] arr)
    {
        List<int> nonNull = new List<int>();
        foreach (var item in arr)
            if (item != -1) nonNull.Add(item);

        if (nonNull.Count == 1) return 1;                               //only single item
        if (nonNull.Count == 2 && nonNull[0] == nonNull[1]) return 1;   //duplicate item only

        //check value in non null
        Dictionary<int, int> counts = new Dictionary<int, int>();
        foreach (var item in nonNull)
        {
            if (!counts.ContainsKey(item)) counts[item] = 0;
            counts[item]++;
        }

        if (counts.ContainsValue(2)) return 2;  //double single item
        if (counts.Count == 3) return 3;    //different item only

        return 3;
    }

    private void SaveLevel(ClickEvent evt)
    {
        Debug.Log("Save data");
        LevelData asset = ScriptableObject.CreateInstance<LevelData>();

        DimsumCombination[] combinations = new DimsumCombination[CurrentResult.Count];
        for (int i = 0; i < CurrentResult.Count; i++)
        {
            combinations[i] = new DimsumCombination(CurrentResult[i][0],CurrentResult[i][1],CurrentResult[i][2]);
        }

        asset.currentLevel = combinations;
        
        AssetDatabase.CreateAsset(asset, "Assets/ScriptObjects/NewLevelData.asset");
        AssetDatabase.SaveAssets();

        EditorUtility.FocusProjectWindow();

        Selection.activeObject = asset;
    }
}
