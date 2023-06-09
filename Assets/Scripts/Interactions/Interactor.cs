using NPC.Enemies;
using Player.Health;
using Player.Inventory;
using UnityEngine;

namespace Interactions {
    public abstract class Interactor : MonoBehaviour {
        public static Vector2 ScreenCenter => new Vector2(Screen.width, Screen.height) / 2;
        
        [field: SerializeField] public InventorySystem Inventory { get; private set; }
        [field: SerializeField] public HealthSystem Health { get; private set; }

        [SerializeField] float maxInteractionDistance = 5;
        [SerializeField] float maxAttackDistance = 5;
        
        public Transform LookingAt { get; private set; }
        public Interactable LookingAtInteractable { get; private set; }
        public Enemy LookingAtEnemy { get; private set; }

        public void SetLookingAt(RaycastHit? hit) {
            if (hit != null) {
                RaycastHit value = hit.Value;
                
                LookingAt = value.transform;
                LookingAtInteractable = value.distance <= maxInteractionDistance ? LookingAt.GetComponent<Interactable>() : null;
                LookingAtEnemy = value.distance <= maxAttackDistance ? LookingAt.GetComponent<Enemy>() : null;
            } else {
                LookingAt = null;
                LookingAtInteractable = null;
                LookingAtEnemy = null;
            }
        }
        
        public Camera Camera { get; protected set; }
    }
}