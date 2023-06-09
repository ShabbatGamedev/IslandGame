using Interactions;
using UnityEngine;

namespace Items.UserItems {
    [CreateAssetMenu(fileName = "Worm", menuName = "Inventory/Worm")]
    public class Worm : ItemObject {
        [SerializeField] int healAmount = 1;
        
        public override void Use(Interactor interactor) {
            RemoveFromInventory(interactor);
            
            interactor.Health.Heal(healAmount);
        }
    }
}