namespace NPC.Enemies {
    public abstract class Enemy : EnemyAI {
        public EnemyObject mob;

        public virtual void Damage(int hp) {
            mob.parameters.health -= hp;

            if (!mob.IsAlive) Destroy(gameObject);
        }
    }
}