using System;
using JetBrains.Annotations;
using UnityEngine;

namespace Items {
    [CreateAssetMenu(fileName = "Item", menuName = "Inventory/Item", order = 0)]
    public class ItemObject : ScriptableObject {
        public readonly Guid _guid = Guid.NewGuid();
        
        public string itemName = "New item";
        public string hintText; // Tooltip next to the button when pointing at an object 
        public Sprite icon;
        public GameObject prefab; // Model of the item
        [Range(1, 64)] public int stackSize = 16; // Maximum amount of item in one inventory slot

        public override int GetHashCode() => _guid.GetHashCode();

        public override bool Equals(object other) {
            if (other is not ItemObject item) return false;

            return item == this;
        }

        public static bool operator ==([CanBeNull] ItemObject item, [CanBeNull] ItemObject otherItem) {
            if (item is not null && otherItem is not null) return item._guid == otherItem._guid;
            return item is null && otherItem is null;
        }
        
        public static bool operator !=(ItemObject item, ItemObject otherItem) => !(item == otherItem);
    }
}