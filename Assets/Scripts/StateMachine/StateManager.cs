using UnityEngine;

namespace StateMachine {
    public abstract class StateManager : MonoBehaviour {
        [SerializeField] protected State startState;
        protected State _currentState;

        protected virtual void Start() => SetState(startState);

        protected virtual void Update() {
            if (!_currentState.Finished) _currentState.Update();
        }

        public State SetState(State state) {
            _currentState = Instantiate(state);
            _currentState.Initialize(this);
            return _currentState;
        }
    }
}