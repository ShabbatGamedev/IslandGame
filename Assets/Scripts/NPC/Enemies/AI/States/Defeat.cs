using StateMachine;
using UnityEngine;

namespace NPC.Enemies.AI.States {
    [CreateAssetMenu(fileName = "DefeatState", menuName = "Enemies/States/Defeat")]
    public class Defeat : State {
        public override void Initialize(StateManager manager) {
            base.Initialize(manager);
            
            ((EnemyAI)manager).Agent.isStopped = true;
        }
    }
}