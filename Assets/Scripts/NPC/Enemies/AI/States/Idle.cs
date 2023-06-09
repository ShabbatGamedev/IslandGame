using StateMachine;
using UnityEngine;

namespace NPC.Enemies.AI.States {
    [CreateAssetMenu(fileName = "IdleState", menuName = "Enemies/States/Idle")]
    public class Idle : State {
        [field: SerializeField] public State ChasingState { get; private set; }
        
        EnemyAI AI { get; set; }

        public override void Initialize(StateManager manager) {
            base.Initialize(manager);
            
            AI = manager as EnemyAI;
        }

        public override void Update() {
            if (AI.CanSeePlayer()) AI.SetState(ChasingState);
        }
    }
}