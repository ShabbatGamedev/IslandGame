using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Player.Health {
    public class HealthSystem : MonoBehaviour {
        /// <summary>
        /// Prefab with <see cref="Heart"/> component.
        /// </summary>
        [SerializeField] GameObject prefab;
        [SerializeField] int maxHP = 7;

        public int CurrentHP { get; private set; }
        public bool IsAlive => CurrentHP > 0;

        /// <summary>
        /// List with <see cref="Heart"/> components>
        /// </summary>
        List<Heart> _hearts;

        public event Action<bool, int> HealthChanged;   
        
        void Awake() {
            CurrentHP = maxHP;
            _hearts = new List<Heart>(maxHP);

            for (int i = 0; i < maxHP; i++) { 
                GameObject heart = Instantiate(prefab, transform); // Create a heart on screen
                Heart heartComponent = heart.GetComponent<Heart>(); // Getting heart component from created heart
                
                heartComponent.SetAlive(CurrentHP > i); // If current hp is more than the creating heart index, then the heart is alive
                
                _hearts.Add(heartComponent); // Adding the heart component to the list
            }
        }

        /// <summary>
        /// Damage the player.
        /// </summary>
        /// <param name="hp">Amount of HP to deal damage.</param>
        public void Damage(int hp) {
            if (!IsAlive) return;
            
            _hearts.Where(heart => heart.isAlive) // Get all living hearts
                .TakeLast(hp) // Get damaging hearts from the living hearts
                .ToList() // Convert it to the list to access the ForEach method
                .ForEach(heart => heart.SetAlive(false)); // Set the dead state for the hearts

            CurrentHP = _hearts.Count(heart => heart.isAlive);

            HealthChanged?.Invoke(IsAlive, CurrentHP);
        }

        /// <summary>
        /// Heal the player.
        /// </summary>
        /// <param name="hp">Amount of HP to heal.</param>
        public void Heal(int hp) {
            if (!IsAlive) return;
            
            _hearts.Where(heart => !heart.isAlive) // Get all dead hearts
                .Take(hp) // Get healing hearts from the dead hearts
                .ToList() // Convert it to a list to access the ForEach method
                .ForEach(heart => heart.SetAlive(true)); // Set the alive state for the hearts
            
            CurrentHP = _hearts.Count(heart => heart.isAlive);
            
            HealthChanged?.Invoke(IsAlive, CurrentHP);
        }
    }
}