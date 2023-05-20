using Interactions;
using UnityEngine;

namespace Items.ItemLogic {
    public class PickupItem : BaseItemLogic {
        [SerializeField] ItemObject item;

        public override ItemObject Item => item;

        public override void Interact(Interactor interactor) => Pickup(interactor);

        void Pickup(Interactor interactor) {
            Debug.Log($"Picking up {Item.itemName}");

            if (PickupItem(interactor)) Destroy(gameObject);
            else Debug.LogWarning("Not enough space");
        }
    }
}