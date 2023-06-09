using System;
using Interactions;
using Items.UserItems;
using NPC.Enemies.AI;
using StateMachine;
using UnityEngine;

namespace NPC.Enemies {
    public abstract class Enemy : EnemyAI {
        [field: SerializeField] public State DefeatState { get; private set; }
        
        public EnemyObject mob;

        protected event Action<Interactor, IWeapon> Damaged;
        protected event Action<Interactor, IWeapon> KnockBacked;

        public virtual void Damage(Interactor interactor, IWeapon weapon) {
            bool damaged = mob.Damage(weapon.Damage);

            if (!damaged) return;
            
            Damaged?.Invoke(interactor, weapon);
            
            KnockBack(interactor, weapon.KnockBackForce);
            KnockBacked?.Invoke(interactor, weapon);

            if (!mob.IsAlive) SetState(DefeatState);
        }
        
        protected virtual void KnockBack(Interactor interactor, float force) => Agent.velocity = interactor.Camera.transform.forward * force;
    }
}