namespace NPC.Enemies {
    public class Zombie : Enemy {
        bool GetAwayFromPlayer { get; set; }
        
        protected override void Awake() {
            base.Awake();
            
            
        }

        protected override void Update() {
            base.Update();
            
            PathFinding();
        }
    }
}