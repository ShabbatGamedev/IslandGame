using UnityEngine;
using UnityEngine.AI;

namespace NPC.Enemies {
    public abstract class EnemyAI : MonoBehaviour {
        [SerializeField] NavMeshAgent navMeshAgent;
        [SerializeField] float viewRadius = 5;
        [SerializeField] float chaseRadius = 3;
        [SerializeField] float stopRadius = 0.7f;
        [SerializeField] float lookingSpeed = 5;

        Transform _player;
        float _playerRadius;
        bool _isChasing;
        
        public virtual void Awake() {
            _player = GameObject.FindGameObjectWithTag("Player").transform;
            _playerRadius = _player.GetComponent<CapsuleCollider>().radius;
        }

        public virtual void Update() {
            Vector3 playerPosition = _player.position;
            float distanceToPlayer = Vector3.Distance(navMeshAgent.transform.position, playerPosition);

            if (distanceToPlayer <= _playerRadius + stopRadius) {
                LookAtPlayer();
                navMeshAgent.ResetPath();
            } else {
                if (_isChasing) {
                    if (distanceToPlayer >= viewRadius + chaseRadius) {
                        navMeshAgent.ResetPath();
                        _isChasing = false;
                    } else {
                        LookAtPlayer();
                        navMeshAgent.SetDestination(playerPosition);
                    }
                } else {
                    if (distanceToPlayer >= viewRadius) return;
                    
                    LookAtPlayer();
                    _isChasing = true;
                    navMeshAgent.SetDestination(playerPosition);
                }
            }
        }

        void LookAtPlayer() {
            Vector3 direction = (_player.position - navMeshAgent.transform.position).normalized;
            Quaternion lookRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));
            navMeshAgent.transform.rotation = Quaternion.Slerp(navMeshAgent.transform.rotation, lookRotation, Time.deltaTime * lookingSpeed);
        }
    }
}