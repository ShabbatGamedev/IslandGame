using UnityEngine;

namespace StateMachine {
    public abstract class State : ScriptableObject {
        public StateManager StateManager { get; protected set; }
        
        public bool Finished { get; protected set; }

        public virtual void Initialize(StateManager manager) => StateManager = manager;

        public virtual void Update() { }
    }
}