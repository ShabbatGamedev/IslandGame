using StateMachine;
using UnityEngine;

namespace NPC.Enemies.AI.States {
    [CreateAssetMenu(fileName = "ChasingState", menuName = "Enemies/States/Chasing")] 
    public class Chasing : State {
        [SerializeField] State attackState;
        
        EnemyAI AI { get; set; }

        public override void Initialize(StateManager manager) {
            base.Initialize(manager);
            
            AI = manager as EnemyAI;
        }

        public override void Update() {
            AI.LookAtPlayer();

            if (AI.CanAttack) {
                if (AI.Agent.hasPath) AI.Agent.ResetPath();
                
                AI.SetState(attackState);
            } else AI.Agent.SetDestination(AI.PlayerPosition);
        }
    }
}