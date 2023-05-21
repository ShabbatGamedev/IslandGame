using Interactions;
using NPC.Enemies;
using UnityEngine;

namespace Items.UserItems {
    [CreateAssetMenu(fileName = "Stick", menuName = "Inventory/Stick", order = 0)]
    public class Stick : ItemObject {
        [SerializeField] int damage = 1;
        
        public override void Use(Interactor interactor) {
            Ray ray = interactor.playerCamera.ScreenPointToRay(Interactor._screenCenter);
            
            if (!Physics.Raycast(ray, out RaycastHit hit, interactor.maxAttackDistance) ||
                !hit.transform.TryGetComponent(out Enemy enemy)) return;
            
            enemy.Damage(damage);

            Debug.Log($"УЕБАЛ ПИДОРА {enemy.mob.enemyName} НА {damage}ХП");
        }
    }
}