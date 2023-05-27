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
            Item = item;

            Items.Add(item);
        }

        public bool HasItems => Items.Any();
        public bool HasSpace => ItemsCount < Size;
        public int ItemsCount => Items.Count;

        public Sprite Icon => Item.icon;
        public string Name => Item.name;
        public int Size => Item.size;
        
        ItemObject Item { get; } 
        List<ItemObject> Items { get; } = new();

        public event Action StackUpdated;

        public bool AddItem(ItemObject item) {
            if (!HasSpace || Item != item) return false;

            Items.Add(item);
            StackUpdated?.Invoke();
            return true;
        }

        public void RemoveItem(ItemObject item) {
            if (Item != item) return;
            
            if (Items.Remove(item)) 
                StackUpdated?.Invoke();
        }

        public ItemObject LastItem() => Items.LastOrDefault();

        public bool ContainsThisItem(ItemObject item) => Item == item;

        public bool CanTakeItem(ItemObject item) => HasSpace && ContainsThisItem(item);
    }
}