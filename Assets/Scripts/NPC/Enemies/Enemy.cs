using System;
using Interactions;
using Items.UserItems;
using UnityEngine;

namespace NPC.Enemies {
    public abstract class Enemy : EnemyAI {
        public EnemyObject mob;

        protected event Action<Interactor, IWeapon> Damaged;
        protected event Action<Interactor, IWeapon> KnockBacked;

        public virtual void Damage(Interactor interactor, IWeapon weapon) {
            if (!mob.IsAlive) return;
            
            mob.parameters.health -= weapon.Damage;
            Damaged?.Invoke(interactor, weapon);
            
            Debug.Log($"УЕБАЛ ПИДОРА {mob.name} НА {weapon.Damage}ХП");
            
            KnockBack(interactor, weapon.KnockBackForce);
            KnockBacked?.Invoke(interactor, weapon);

            if (!mob.IsAlive) AIEnabled = false;
        }
        
        protected virtual void KnockBack(Interactor interactor, float force) => Agent.velocity = interactor.Camera.transform.forward * force;
    }
}