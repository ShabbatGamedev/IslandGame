using Player.Health;
using Player.Inventory;
using UnityEngine;

namespace Interactions {
    public abstract class Interactor : MonoBehaviour {
        public static Vector2 _screenCenter => new Vector2(Screen.width, Screen.height) / 2;
        
        public InventorySystem inventory;
        public HealthSystem health;
        public UnityEngine.Camera playerCamera;

        public float maxInteractionDistance = 5;
        public float maxAttackDistance = 5;
    }
}