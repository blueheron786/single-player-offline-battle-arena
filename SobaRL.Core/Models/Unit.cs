namespace SobaRL.Core.Models
{
    public enum Team
    {
        Player = 0,
        Enemy = 1,
        Neutral = 2
    }

    public enum UnitType
    {
        Champion,
        Minion,
        Tower,
        Nexus
    }

    public abstract class Unit
    {
        public string Name { get; set; }
        public Position Position { get; set; }
        public Team Team { get; set; }
        public UnitType UnitType { get; set; }
        
        public int MaxHealth { get; set; }
        public int CurrentHealth { get; set; }
        public int AttackDamage { get; set; }
        public int AttackRange { get; set; }
        public int Speed { get; set; }
        public int MovementRange { get; set; }
        
        public bool IsAlive => CurrentHealth > 0;
        public bool IsDead => CurrentHealth <= 0;
        public float HealthPercentage => MaxHealth > 0 ? (float)CurrentHealth / MaxHealth : 0f;

        // Time system - tracks when this unit can act next
        public int NextActionTime { get; set; } = 0;

        protected Unit(string name, Position position, Team team, UnitType unitType)
        {
            Name = name;
            Position = position;
            Team = team;
            UnitType = unitType;
        }

        public virtual void TakeDamage(int damage)
        {
            CurrentHealth = Math.Max(0, CurrentHealth - damage);
        }

        public virtual void Heal(int amount)
        {
            CurrentHealth = Math.Min(MaxHealth, CurrentHealth + amount);
        }

        public virtual bool CanAttack(Unit target)
        {
            if (target == null || target.IsDead || target.Team == Team)
                return false;
            
            return Position.ManhattanDistanceTo(target.Position) <= AttackRange;
        }

        public virtual bool CanMoveTo(Position targetPosition, GameMap map)
        {
            if (Position.ManhattanDistanceTo(targetPosition) > MovementRange)
                return false;
            
            return map.IsValidPosition(targetPosition) && map.IsPositionEmpty(targetPosition);
        }

        public virtual void Attack(Unit target)
        {
            if (CanAttack(target))
            {
                target.TakeDamage(AttackDamage);
            }
        }

        public virtual void MoveTo(Position newPosition)
        {
            Position = newPosition;
        }

        public override string ToString()
        {
            return $"{Name} ({Team}) at {Position} - {CurrentHealth}/{MaxHealth} HP";
        }
    }
}