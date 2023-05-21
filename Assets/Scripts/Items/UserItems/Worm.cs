using Interactions;
using UnityEngine;

namespace Items.UserItems {
    [CreateAssetMenu(fileName = "Worm", menuName = "Inventory/Worm", order = 0)]
    public class Worm : ItemObject {
        [SerializeField] int healAmount = 1;
        
        public override void Use(Interactor interactor) {
            RemoveFromInventory(interactor);
            
            interactor.health.Heal(healAmount);
            
            Debug.Log($"ПОХИЛИЛСЯ ЧЕРВЁМ НА {healAmount}ХП");
        }
    }
}