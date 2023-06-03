using UnityEngine;
using UnityEngine.AI;

namespace NPC.Enemies {
    [RequireComponent(typeof(NavMeshAgent))]
    public abstract class EnemyAI : MonoBehaviour {
        [SerializeField] float viewRadius = 5;
        [SerializeField] float stopRadius = 0.7f;
        [SerializeField] float lookingSpeed = 5;

        protected NavMeshAgent Agent { get; private set; }
        protected Vector3 DirectionToPlayer { get; private set; }
        protected bool Chasing { get; private set; }
        protected bool AIEnabled { get; set; } = true;
        protected bool NeedToLookAtPlayer { get; set; }

        Transform _player;
        float _playerRadius;
        float _agentRadius;

        protected virtual void Awake() {
            _player = GameObject.FindGameObjectWithTag("Player").transform;
            _playerRadius = _player.GetComponent<CapsuleCollider>().radius;
            
            Agent = GetComponent<NavMeshAgent>();
        }

        protected virtual void Update() {
            if (NeedToLookAtPlayer) LookAtPlayer();
        }

        public void PathFinding() {
            if (!AIEnabled) return;
            
            _agentRadius = Agent.radius;
            Vector3 agentPosition = Agent.transform.position;
            Vector3 playerPosition = _player.position;

            DirectionToPlayer = (playerPosition - agentPosition).normalized;
            
            float stoppingDistance = _agentRadius + _playerRadius + stopRadius;
            float distanceToPlayer = Vector3.Distance(agentPosition, playerPosition);

            if (distanceToPlayer >= stoppingDistance) {
                if (Chasing) {
                    NeedToLookAtPlayer = true;
                    Agent.SetDestination(playerPosition);
                } else if (distanceToPlayer < viewRadius && PathToPlayerClear(agentPosition)) {
                    NeedToLookAtPlayer = true;
                    Chasing = true;
                    Agent.SetDestination(playerPosition);
                }
            } else {
                NeedToLookAtPlayer = true;
                if (Agent.hasPath) Agent.ResetPath();
            }
        }
        
        public bool PathToPlayerClear(Vector3 origin) {
            Ray rayToPlayer = new(origin, DirectionToPlayer);
            float maxDistance = viewRadius + _agentRadius;

            if (Physics.Raycast(rayToPlayer, out RaycastHit hit, maxDistance)) return hit.transform == _player;
            
            return false;
        }
        
        void LookAtPlayer() {
            Quaternion lookRotation = Quaternion.LookRotation(new Vector3(DirectionToPlayer.x, 0, DirectionToPlayer.z));
            Agent.transform.rotation = Quaternion.Slerp(Agent.transform.rotation, lookRotation, Time.deltaTime * lookingSpeed);
        }
    }
}