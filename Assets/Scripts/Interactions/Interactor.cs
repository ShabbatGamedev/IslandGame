using NPC.Enemies;
using Player.Health;
using Player.Inventory;
using UnityEngine;

namespace Interactions {
    public abstract class Interactor : MonoBehaviour {
        public static Vector2 _screenCenter => new Vector2(Screen.width, Screen.height) / 2;
        
        public InventorySystem inventory;
        public HealthSystem health;

        public float maxInteractionDistance = 5;
        public float maxAttackDistance = 5;

        public RaycastHit RaycastHit { get; protected set; }
        
        public Interactable LookingAtInteractable { get; private set; }
        public Enemy LookingAtEnemy { get; private set; }

        public Transform LookingAt {
            get => _lookingAt;
            protected set {
                _lookingAt = value;

                if (_lookingAt != null) {
                    LookingAtInteractable = _lookingAt.GetComponent<Interactable>();
                    LookingAtEnemy = _lookingAt.GetComponent<Enemy>();
                } else {
                    LookingAtInteractable = null;
                    LookingAtEnemy = null;
                }
            }
        }

        public Camera Camera { get; protected set; }
        
        Transform _lookingAt;
    }
}