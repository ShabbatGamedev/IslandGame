using Interactions;
using NPC.Enemies;
using UnityEngine;

namespace Items.UserItems {
    [CreateAssetMenu(fileName = "Stick", menuName = "Inventory/Stick", order = 0)]
    public class Stick : ItemObject, IWeapon {
        [SerializeField] int damage = 1;
        [SerializeField] float knockBackForce = 2.5f;
        
        public override void Use(Interactor interactor) {
            Enemy enemy = interactor.LookingAtEnemy;
            
            if (enemy == null) return;
            
            enemy.Damage(interactor, this);
        }

        public int Damage => damage;

        public float KnockBackForce => knockBackForce;
    }
}