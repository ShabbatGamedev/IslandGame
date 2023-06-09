using System;
using Interactions;
using JetBrains.Annotations;
using Player.Inventory;
using UnityEngine;

namespace Items {
    public abstract class ItemObject : ScriptableObject {
        public readonly Guid _guid = Guid.NewGuid();
        
        public new string name = "New item";
        public string hintText; // Tooltip next to the button when pointing at an object 
        public Sprite icon;
        public GameObject prefab; // Model of the item
        [Range(1, 64)] public int size = 16; // Maximum amount of item in one inventory slot

        public abstract void Use(Interactor interactor);

        public void RemoveFromInventory(Interactor interactor) {
            InventorySystem inventory = interactor.Inventory;
            
            inventory.RemoveItem(inventory.SelectedSlot.GetStack(), this);
        } 

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