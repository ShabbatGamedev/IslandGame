using System.Collections;
using StateMachine;
using UnityEngine;

namespace NPC.Enemies.AI.States {
    [CreateAssetMenu(fileName = "AttackState", menuName = "Enemies/States/Attack")] 
    public class Attack : State {
        [SerializeField] State chasingState;

        [SerializeField] float swingTime = 0.7f;
        [SerializeField] float timeBetweenAttacks = 0.5f;
        [SerializeField] int damageHP = 1;

        EnemyAI AI { get; set; }

        IEnumerator _attackRoutine;

        public override void Initialize(StateManager manager) {
            base.Initialize(manager);

            AI = manager as EnemyAI;
            _attackRoutine = Attacking();

            AI.StartCoroutine(_attackRoutine);
        }

        public override void Update() {
            if (AI.CanAttack) return;
            
            AI.StopCoroutine(_attackRoutine);
            AI.SetState(chasingState);
        }

        IEnumerator Attacking() {
            while (AI.CanAttack) {
                AI.LookAtPlayer();
                
                yield return new WaitForSeconds(swingTime);
            
                AI.PlayerInteractor.Health.Damage(damageHP);
                AI.LookAtPlayer();

                yield return new WaitForSeconds(timeBetweenAttacks);
            }
        }
    }
}