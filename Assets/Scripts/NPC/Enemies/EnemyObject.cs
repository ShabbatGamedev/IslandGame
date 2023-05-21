using UnityEngine;

namespace NPC.Enemies {
    [CreateAssetMenu(fileName = "Enemy", menuName = "Enemies/Enemy")]
    public class EnemyObject : ScriptableObject {
        public string enemyName = "New Enemy";
        public NPCParams parameters;

        public bool IsAlive => parameters.health > 0;
    }
}