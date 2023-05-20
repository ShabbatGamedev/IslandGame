using Interactions;
using UnityEngine;

namespace Items.ItemLogic {
    public class DigWorms : BaseItem {
        [SerializeField] ItemObject item;
        [SerializeField] int minimumWorms = 1;
        [SerializeField] int maximumWorms = 3;

        public override ItemObject Item => item;

        public override void Interact(Interactor interactor) {
            int dugWorms = Random.Range(minimumWorms, maximumWorms + 1);

            for (int i = 0; i < dugWorms; i++) {
                if (interactor.inventory.AddItem(item)) continue;

                Instantiate(item.prefab, RandomSpawnpoint(transform), Quaternion.identity);
            }

            Destroy(gameObject);
        }

        static Vector3 RandomSpawnpoint(Transform t) {
            Vector3 spawnPoint = t.position;
            Vector3 spawnRadius = t.localScale / 2;

            spawnPoint.x += Random.Range(-spawnRadius.x, spawnRadius.x);
            spawnPoint.z += Random.Range(-spawnRadius.z, spawnRadius.z);

            return spawnPoint;
        }
    }
}