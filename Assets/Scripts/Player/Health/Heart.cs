using UnityEngine;
using UnityEngine.UI;

namespace Player.Health {
    public class Heart : MonoBehaviour {
        [SerializeField] Sprite heart, deadHeart;
        [SerializeField] Image icon;
        
        public bool isAlive;

        public void SetAlive(bool value) {
            isAlive = value;

            icon.sprite = isAlive ? heart : deadHeart; // If heart is alive - set the alive sprite, otherwise set the dead sprite 
        }
    }
}