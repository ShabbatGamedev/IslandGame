using UnityEngine;
using UnityEngine.AI;

namespace NPC.Enemies {
    [RequireComponent(typeof(NavMeshAgent))]
    public abstract class EnemyAI : MonoBehaviour {
        [SerializeField] float viewRadius = 5;
        [SerializeField] float chaseRadius = 3;
        [SerializeField] float stopRadius = 0.7f;
        [SerializeField] float lookingSpeed = 5;

        public NavMeshAgent Agent { get; private set; }

        Transform _player;
        float _playerRadius;
        float _agentRadius;
        
        Vector3 _directionToPlayer;
        bool _isChasing;
        bool _needToLookAtPlayer;
        bool _canMove = true;

        protected virtual void Awake() {
            _player = GameObject.FindGameObjectWithTag("Player").transform;
            _playerRadius = _player.GetComponent<CapsuleCollider>().radius;
            
            Agent = GetComponent<NavMeshAgent>();
        }

        protected virtual void Update() {
            if (_needToLookAtPlayer) LookAtPlayer();
        }

        public void PathFinding() {
            if (!_canMove) return;
            
            _agentRadius = Agent.radius;
            Vector3 agentPosition = Agent.transform.position;
            Vector3 playerPosition = _player.position;

            _directionToPlayer = (playerPosition - agentPosition).normalized;
            
            float chasingDistance = viewRadius + chaseRadius;
            float stoppingDistance = _agentRadius + _playerRadius + stopRadius;
            float distanceToPlayer = Vector3.Distance(agentPosition, playerPosition);
            
            if (distanceToPlayer <= stoppingDistance) {
                _needToLookAtPlayer = true;
                if (Agent.hasPath) Agent.ResetPath();
            } else {
                if (_isChasing) {
                    if (distanceToPlayer >= chasingDistance) {
                        _needToLookAtPlayer = false;
                        _isChasing = false;
                        if (Agent.hasPath) Agent.ResetPath();
                    } else {
                        _needToLookAtPlayer = true;
                        Agent.SetDestination(playerPosition);
                    }
                } else if (distanceToPlayer < viewRadius && PathToPlayerClear(agentPosition)) {
                    _needToLookAtPlayer = true;
                    _isChasing = true;
                    Agent.SetDestination(playerPosition);
                }
            }
        }
        
        public bool PathToPlayerClear(Vector3 origin) {
            Ray rayToPlayer = new(origin, _directionToPlayer);
            float maxDistance = viewRadius + _agentRadius;

            if (Physics.Raycast(rayToPlayer, out RaycastHit hit, maxDistance)) return hit.transform == _player;
            
            return false;
        }
        
        public void DisableAI() => _canMove = false;

        void LookAtPlayer() {
            Quaternion lookRotation = Quaternion.LookRotation(new Vector3(_directionToPlayer.x, 0, _directionToPlayer.z));
            Agent.transform.rotation = Quaternion.Slerp(Agent.transform.rotation, lookRotation, Time.deltaTime * lookingSpeed);
        }
    }
}