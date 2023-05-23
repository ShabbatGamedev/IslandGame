﻿using UnityEngine;

namespace KinematicCharacterController.Core {
    [CreateAssetMenu]
    public class KCCSettings : ScriptableObject {
        /// <summary>
        /// Determines if the system simulates automatically.
        /// If true, the simulation is done on FixedUpdate
        /// </summary>
        [Tooltip("Determines if the system simulates automatically. If true, the simulation is done on FixedUpdate")]
        public bool autoSimulation = true;

        /// <summary>
        /// Should interpolation of characters and PhysicsMovers be handled
        /// </summary>
        [Tooltip("Should interpolation of characters and PhysicsMovers be handled")]
        public bool interpolate = true;

        /// <summary>
        /// Initial capacity of the system's list of Motors (will resize automatically if needed, but setting a high initial
        /// capacity can help preventing GC allocs)
        /// </summary>
        [Tooltip("Initial capacity of the system's list of Motors (will resize automatically if needed, but setting a high initial capacity can help preventing GC allocs)")]
        public int motorsListInitialCapacity = 100;

        /// <summary>
        /// Initial capacity of the system's list of Movers (will resize automatically if needed, but setting a high initial
        /// capacity can help preventing GC allocs)
        /// </summary>
        [Tooltip("Initial capacity of the system's list of Movers (will resize automatically if needed, but setting a high initial capacity can help preventing GC allocs)")]
        public int moversListInitialCapacity = 100;
    }
}