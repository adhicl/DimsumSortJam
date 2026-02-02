using UnityEngine;

namespace Commons
{
    [CreateAssetMenu(fileName = "CommonSeting", menuName = "CommonSetting", order = 0)]
    public class CommonSetting : ScriptableObject
    {
        public Material defaultSpriteMaterial;
        public Material itemOnGameMaterial;
        public Material itemDragGameMaterial;
    }
}