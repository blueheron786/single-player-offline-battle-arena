namespace SobaRL.Core.Models
{
    public class Minion : Unit
    {
        public int LaneIndex { get; set; } // 0, 1, 2 for the three lanes
        public Position TargetPosition { get; set; }
        
        public Minion(string name, Position position, Team team, int laneIndex) 
            : base(name, position, team, UnitType.Minion)
        {
            LaneIndex = laneIndex;
            
            // Default minion stats
            MaxHealth = 100;
            CurrentHealth = MaxHealth;
            AttackDamage = 20;
            AttackRange = 1;
            Speed = 100; // Standard speed
            MovementRange = 1;
        }

        public void SetTarget(Position target)
        {
            TargetPosition = target;
        }

        public Position GetNextMovePosition()
        {
            // Simple pathfinding - move towards target
            int dx = Math.Sign(TargetPosition.X - Position.X);
            int dy = Math.Sign(TargetPosition.Y - Position.Y);
            
            return new Position(Position.X + dx, Position.Y + dy);
        }
    }

    public class Tower : Unit
    {
        public int LaneIndex { get; set; }
        
        public Tower(string name, Position position, Team team, int laneIndex) 
            : base(name, position, team, UnitType.Tower)
        {
            LaneIndex = laneIndex;
            
            // Tower stats
            MaxHealth = 500;
            CurrentHealth = MaxHealth;
            AttackDamage = 80;
            AttackRange = 3;
            Speed = 0; // Towers don't move
            MovementRange = 0;
        }

        public override bool CanMoveTo(Position targetPosition, GameMap map)
        {
            return false; // Towers can't move
        }

        public override void MoveTo(Position newPosition)
        {
            // Towers can't move
        }
    }

    public class Nexus : Unit
    {
        public Nexus(string name, Position position, Team team) 
            : base(name, position, team, UnitType.Nexus)
        {
            // Nexus stats
            MaxHealth = 1000;
            CurrentHealth = MaxHealth;
            AttackDamage = 0;
            AttackRange = 0;
            Speed = 0;
            MovementRange = 0;
        }

        public override bool CanMoveTo(Position targetPosition, GameMap map)
        {
            return false; // Nexus can't move
        }

        public override void MoveTo(Position newPosition)
        {
            // Nexus can't move
        }

        public override void Attack(Unit target)
        {
            // Nexus doesn't attack
        }
    }
}