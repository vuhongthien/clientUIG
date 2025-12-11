// using UnityEngine;

// [System.Serializable]
// public class PrizeSection
// {
//     public string prizeName;
//     public int weight = 1;          // Trọng số xác suất
//     public Color displayColor;      // Màu hiển thị
//     public bool isSpecial = false;  // Có phải phần thưởng đặc biệt
// }

// public class WeightedProbability : MonoBehaviour
// {
//     public PrizeSection[] prizeSections;
    
//     public int GetWeightedRandomSection()
//     {
//         int totalWeight = 0;
        
//         // Tính tổng trọng số
//         foreach (var section in prizeSections)
//         {
//             totalWeight += section.weight;
//         }
        
//         // Random theo trọng số
//         int randomValue = Random.Range(0, totalWeight);
//         int currentWeight = 0;
        
//         for (int i = 0; i < prizeSections.Length; i++)
//         {
//             currentWeight += prizeSections[i].weight;
//             if (randomValue < currentWeight)
//             {
//                 return i;
//             }
//         }
        
//         return 0;
//     }
    
//     // Tính phần trăm xác suất cho mỗi phần
//     public float[] GetProbabilityPercentages()
//     {
//         int totalWeight = 0;
//         foreach (var section in prizeSections)
//         {
//             totalWeight += section.weight;
//         }
        
//         float[] percentages = new float[prizeSections.Length];
//         for (int i = 0; i < prizeSections.Length; i++)
//         {
//             percentages[i] = (float)prizeSections[i].weight / totalWeight * 100f;
//         }
        
//         return percentages;
//     }
// }