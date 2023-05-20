using UnityEngine;

namespace Items {
    [CreateAssetMenu(fileName = "Item", menuName = "Inventory/Item", order = 0)]
    public class ItemObject : ScriptableObject {
        public string itemName = "New item";
        public string hintText = "Pickup";
        public Sprite icon;
        public GameObject prefab;
        [Range(1, 64)] public int stackSize = 16;
    }
}