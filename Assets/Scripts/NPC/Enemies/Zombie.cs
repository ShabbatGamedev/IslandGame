namespace NPC.Enemies {
    public class Zombie : Enemy {
        protected override void Update() {
            base.Update();
            
            PathFinding();
        }
    }
}