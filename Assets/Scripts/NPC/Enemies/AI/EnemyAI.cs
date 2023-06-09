using Interactions;
using StateMachine;
using UnityEngine;
using UnityEngine.AI;

namespace NPC.Enemies.AI {
    [RequireComponent(typeof(NavMeshAgent))]
    public abstract class EnemyAI : StateManager {
        [field: SerializeField] public NavMeshAgent Agent { get; private set; }
        
        [field: SerializeField] public float ViewRadius { get; private set; } = 5;
        [field: SerializeField] public float StopRadius { get; private set; } = 1.5f;
        [field: SerializeField] public float LookingSpeed { get; private set; } = 5;
        
        public Vector3 PlayerPosition { get; private set; }
        public float PlayerRadius { get; private set; }
        
        public Vector3 AgentPosition { get; private set; }
        public float AgentRadius { get; private set; }

        public float DistanceToPlayer => Vector3.Distance(AgentPosition, PlayerPosition);
        public Vector3 DirectionToPlayer => (PlayerPosition - AgentPosition).normalized;
        public Interactor PlayerInteractor => Player.GetComponentInChildren<Interactor>();
        
        public float AttackDistance => AgentRadius + PlayerRadius + StopRadius;

        public bool CanAttack => AttackDistance > DistanceToPlayer;
        
        Transform Player { get; set; }

        protected virtual void Awake() {
            Player = GameObject.FindGameObjectWithTag("Player").transform;
            PlayerRadius = Player.GetComponent<CapsuleCollider>().radius;
            
            AgentRadius = Agent.radius;
        }

        protected override void Update() {
            PlayerPosition = Player.position;
            AgentPosition = Agent.transform.position;

            base.Update();
        }

        public void LookAtPlayer() {
            Quaternion lookRotation = Quaternion.LookRotation(new Vector3(DirectionToPlayer.x, 0, DirectionToPlayer.z));
            Agent.transform.rotation = Quaternion.Slerp(Agent.transform.rotation, lookRotation, Time.deltaTime * LookingSpeed);
        }
        
        public bool CanSeePlayer() {
            Ray rayToPlayer = new(AgentPosition, DirectionToPlayer);
            float maxDistance = ViewRadius + AgentRadius;

            if (Physics.Raycast(rayToPlayer, out RaycastHit hit, maxDistance)) return hit.transform == Player;
            
            return false;
        }
    }
}