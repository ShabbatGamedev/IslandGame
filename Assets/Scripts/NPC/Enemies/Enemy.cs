using Interactions;
using Items.UserItems;

namespace NPC.Enemies {
    public abstract class Enemy : EnemyAI {
        public EnemyObject mob;

        public virtual void Damage(Interactor interactor, IWeapon weapon) {
            if (!mob.IsAlive) return;
            
            mob.parameters.health -= weapon.Damage;
            KnockBack(interactor, weapon.KnockBackForce);

            if (!mob.IsAlive) DisableAI();
        }
        
        protected virtual void KnockBack(Interactor interactor, float force) => Agent.velocity = interactor.Camera.transform.forward * force;
    }
}