using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Items {
    public class ItemStack {
        /// <summary>
        /// Initialize the stack with the item.
        /// </summary>
        /// <param name="item">Item in the stack.</param>
        public ItemStack(ItemObject item) {
            StackItem = item;

            Items.Add(item);
        }

        public bool HasItems => Items.Any();
        public bool HasSpace => ItemsCount < StackSize;
        public int ItemsCount => Items.Count;

        public Sprite Icon => StackItem.icon;
        public string ItemName => StackItem.itemName;
        public int StackSize => StackItem.stackSize;
        
        ItemObject StackItem { get; } 
        List<ItemObject> Items { get; } = new();

        public event Action StackUpdated;

        public bool AddItem(ItemObject item) {
            if (!HasSpace || StackItem != item) return false;

            Items.Add(item);
            StackUpdated?.Invoke();
            return true;
        }

        public void RemoveItem(ItemObject item) {
            if (StackItem != item) return;
            
            if (Items.Remove(item)) 
                StackUpdated?.Invoke();
        }

        public ItemObject LastItem() => Items.LastOrDefault();
    }
}