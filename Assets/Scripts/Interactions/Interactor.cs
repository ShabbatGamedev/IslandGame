using Player.Health;
using Player.Inventory;
using UnityEngine;

namespace Interactions {
    public abstract class Interactor : MonoBehaviour {
        public InventorySystem inventory;
        public HealthSystem health;
    }
}