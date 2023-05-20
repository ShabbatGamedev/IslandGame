using Interactions;
using UnityEngine;

namespace Items.ItemLogic {
    public class ItemPickering : BaseItem {
        [SerializeField] ItemObject item;

        public override ItemObject Item => item;

        public override void Interact(Interactor interactor) => Pickup(interactor);

        void Pickup(Interactor interactor) {
            Debug.Log($"Picking up {item.itemName}");

            if (interactor.inventory.AddItem(item)) Destroy(gameObject);
            else Debug.LogWarning("Not enough space");
        }
    }
}