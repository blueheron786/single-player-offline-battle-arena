namespace SobaRL.Core.Models
{
    public enum SkillType
    {
        Active,
        Passive
    }

    public class Skill
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public SkillType Type { get; set; }
        public int Cooldown { get; set; }
        public int CurrentCooldown { get; set; }
        public int ManaCost { get; set; }
        public int Range { get; set; }
        public int Damage { get; set; }
        public char Symbol { get; set; } // For display in UI

        public bool IsReady => CurrentCooldown <= 0;
        public bool IsOnCooldown => CurrentCooldown > 0;

        public Skill(string name, string description, SkillType type, int cooldown, int manaCost, int range, int damage, char symbol)
        {
            Name = name;
            Description = description;
            Type = type;
            Cooldown = cooldown;
            CurrentCooldown = 0;
            ManaCost = manaCost;
            Range = range;
            Damage = damage;
            Symbol = symbol;
        }

        public virtual bool CanUse(Champion caster, Position? targetPosition = null)
        {
            if (!IsReady || caster.CurrentMana < ManaCost)
                return false;

            if (targetPosition.HasValue && Range > 0)
            {
                return caster.Position.ManhattanDistanceTo(targetPosition.Value) <= Range;
            }

            return true;
        }

        public virtual void Use(Champion caster, Position? targetPosition = null, Unit? target = null)
        {
            if (!CanUse(caster, targetPosition))
                return;

            caster.CurrentMana -= ManaCost;
            CurrentCooldown = Cooldown;
            
            // Base implementation - override in specific skills
            if (target != null && target.Team != caster.Team)
            {
                target.TakeDamage(Damage);
            }
        }

        public void ReduceCooldown(int amount = 1)
        {
            CurrentCooldown = Math.Max(0, CurrentCooldown - amount);
        }

        public override string ToString()
        {
            string cooldownText = IsOnCooldown ? $" (CD: {CurrentCooldown})" : "";
            return $"{Symbol} {Name}{cooldownText}";
        }
    }
}