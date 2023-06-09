using UnityEngine;

namespace NPC.Enemies {
    [CreateAssetMenu(fileName = "Enemy", menuName = "Enemies/Enemy")]
    public class EnemyObject : ScriptableObject {
        [field: SerializeField] public string Name { get; private set; } = "New Enemy";
        [SerializeField] NPCParams parameters;

        public bool IsAlive => parameters.health > 0;

        public bool Damage(int hp) {
            if (!IsAlive) return false;
            
            parameters.health -= hp;
            return true;
        }

        public bool Heal(int hp) {
            if (!IsAlive) return false;

            parameters.health += hp;
            return true;
        }
    }
}