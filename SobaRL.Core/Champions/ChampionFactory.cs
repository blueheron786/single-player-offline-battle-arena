using SobaRL.Core.Models;

namespace SobaRL.Core.Champions
{
    public static class ChampionFactory
    {
        public static Champion CreateTank(string name, Position position, Team team)
        {
            var tank = new Champion(name, "Tank", position, team)
            {
                MaxHealth = 200,
                CurrentHealth = 200,
                MaxMana = 100,
                CurrentMana = 100,
                AttackDamage = 30,
                AttackRange = 1,
                Speed = 80, // Slower movement
                MovementRange = 2, // Allow diagonal movement
                ManaRegeneration = 2,
                HealthRegeneration = 3
            };

            tank.Skills.Add(new TauntSkill());
            tank.Skills.Add(new ShieldBashSkill());
            tank.Skills.Add(new DefensiveStanceSkill());

            return tank;
        }

        public static Champion CreateMage(string name, Position position, Team team)
        {
            var mage = new Champion(name, "Mage", position, team)
            {
                MaxHealth = 120,
                CurrentHealth = 120,
                MaxMana = 150,
                CurrentMana = 150,
                AttackDamage = 25,
                AttackRange = 3,
                Speed = 90, // Moderate speed
                MovementRange = 2, // Allow diagonal movement
                ManaRegeneration = 5,
                HealthRegeneration = 1
            };

            mage.Skills.Add(new FireballSkill());
            mage.Skills.Add(new FrostBoltSkill());
            mage.Skills.Add(new TeleportSkill());

            return mage;
        }

        public static Champion CreateAssassin(string name, Position position, Team team)
        {
            var assassin = new Champion(name, "Assassin", position, team)
            {
                MaxHealth = 100,
                CurrentHealth = 100,
                MaxMana = 120,
                CurrentMana = 120,
                AttackDamage = 45,
                AttackRange = 1,
                Speed = 120, // Fastest movement
                MovementRange = 2, // Can move further
                ManaRegeneration = 3,
                HealthRegeneration = 2
            };

            assassin.Skills.Add(new BackstabSkill());
            assassin.Skills.Add(new ShadowStepSkill());
            assassin.Skills.Add(new PoisonBladeSkill());

            return assassin;
        }

        public static List<Champion> CreateRandomTeam(int count, Team team, List<Position> spawnPositions)
        {
            var champions = new List<Champion>();
            var random = new Random();
            var archetypes = new Func<string, Position, Team, Champion>[]
            {
                CreateTank,
                CreateMage,
                CreateAssassin
            };

            var names = new Dictionary<string, string[]>
            {
                ["Tank"] = new[] { "Ironwall", "Bulwark", "Fortress", "Guardian", "Bastion", "Aegis" },
                ["Mage"] = new[] { "Arcane", "Mystic", "Ember", "Frost", "Storm", "Void" },
                ["Assassin"] = new[] { "Shadow", "Viper", "Blade", "Wraith", "Phantom", "Silent" }
            };

            for (int i = 0; i < count && i < spawnPositions.Count; i++)
            {
                var archetypeIndex = random.Next(archetypes.Length);
                var createFunction = archetypes[archetypeIndex];
                var archetypeName = archetypeIndex switch
                {
                    0 => "Tank",
                    1 => "Mage",
                    2 => "Assassin",
                    _ => "Tank"
                };
                
                var nameArray = names[archetypeName];
                var championName = nameArray[random.Next(nameArray.Length)];
                
                champions.Add(createFunction(championName, spawnPositions[i], team));
            }

            return champions;
        }
    }

    // Tank Skills
    public class TauntSkill : Skill
    {
        public TauntSkill() : base("Taunt", "Forces nearby enemies to attack you", SkillType.Active, 5, 20, 2, 0, 'T')
        {
        }

        public override void Use(Champion caster, Position? targetPosition = null, Unit? target = null)
        {
            if (!CanUse(caster, targetPosition)) return;

            base.Use(caster, targetPosition, target);
            
            // Taunt effect would be implemented with status effects in a more complete system
            // For now, just provide a defensive bonus
            caster.TakeDamage(-10); // Heal for 10
        }
    }

    public class ShieldBashSkill : Skill
    {
        public ShieldBashSkill() : base("Shield Bash", "Stuns and damages target", SkillType.Active, 4, 25, 1, 40, 'S')
        {
        }
    }

    public class DefensiveStanceSkill : Skill
    {
        public DefensiveStanceSkill() : base("Defensive Stance", "Reduces damage taken", SkillType.Active, 8, 30, 0, 0, 'D')
        {
        }

        public override void Use(Champion caster, Position? targetPosition = null, Unit? target = null)
        {
            if (!CanUse(caster, targetPosition)) return;

            base.Use(caster, targetPosition, target);
            
            // Defensive boost - heal the caster
            caster.Heal(20);
        }
    }

    // Mage Skills
    public class FireballSkill : Skill
    {
        public FireballSkill() : base("Fireball", "Launches a fireball at target", SkillType.Active, 3, 30, 4, 60, 'F')
        {
        }
    }

    public class FrostBoltSkill : Skill
    {
        public FrostBoltSkill() : base("Frost Bolt", "Slows and damages target", SkillType.Active, 4, 25, 3, 45, 'I')
        {
        }
    }

    public class TeleportSkill : Skill
    {
        public TeleportSkill() : base("Teleport", "Instantly move to target location", SkillType.Active, 6, 40, 5, 0, 'P')
        {
        }

        public override void Use(Champion caster, Position? targetPosition = null, Unit? target = null)
        {
            if (!CanUse(caster, targetPosition) || !targetPosition.HasValue) return;

            caster.CurrentMana -= ManaCost;
            CurrentCooldown = Cooldown;
            
            // Move the caster to the target position
            caster.MoveTo(targetPosition.Value);
        }
    }

    // Assassin Skills
    public class BackstabSkill : Skill
    {
        public BackstabSkill() : base("Backstab", "High damage attack from behind", SkillType.Active, 3, 20, 1, 80, 'B')
        {
        }
    }

    public class ShadowStepSkill : Skill
    {
        public ShadowStepSkill() : base("Shadow Step", "Move behind target and attack", SkillType.Active, 5, 35, 3, 50, 'H')
        {
        }

        public override void Use(Champion caster, Position? targetPosition = null, Unit? target = null)
        {
            if (!CanUse(caster, targetPosition) || target == null) return;

            base.Use(caster, targetPosition, target);
            
            // Move caster next to target
            var behindTarget = new Position(target.Position.X - 1, target.Position.Y);
            caster.MoveTo(behindTarget);
        }
    }

    public class PoisonBladeSkill : Skill
    {
        public PoisonBladeSkill() : base("Poison Blade", "Poisons target over time", SkillType.Active, 4, 25, 1, 30, 'V')
        {
        }

        public override void Use(Champion caster, Position? targetPosition = null, Unit? target = null)
        {
            if (!CanUse(caster, targetPosition) || target == null) return;

            base.Use(caster, targetPosition, target);
            
            // Apply additional poison damage
            target.TakeDamage(15);
        }
    }
}