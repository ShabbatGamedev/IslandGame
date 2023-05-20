using System;
using System.Collections.Generic;
using UnityEngine;

namespace Items {
    public class ItemStack {
        public ItemStack(ItemObject item) {
            Icon = item.icon;
            ItemName = item.itemName;
            StackSize = item.stackSize;

            Items.Add(item);
        }

        public bool HasItems => ItemsCount > 0;
        public bool HasSpace => ItemsCount < StackSize;
        public int ItemsCount => Items.Count;

        public Sprite Icon { get; }
        public string ItemName { get; }
        public int StackSize { get; }
        List<ItemObject> Items { get; } = new();

        public event Action StackUpdated;

        public bool AddItem(ItemObject item) {
            if (!HasSpace) return false;

            Items.Add(item);
            StackUpdated?.Invoke();
            return true;
        }

        public void RemoveItem(ItemObject item) {
            Items.Remove(item);
            StackUpdated?.Invoke();
        }
    }
}