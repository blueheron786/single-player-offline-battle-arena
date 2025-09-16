namespace SobaRL.Core.Models
{
    public class Champion : Unit
    {
        public int Level { get; set; } = 1;
        public int Experience { get; set; } = 0;
        public int ExperienceToNextLevel => Level * 100; // Simple XP curve
        
        public int MaxMana { get; set; }
        public int CurrentMana { get; set; }
        public int ManaRegeneration { get; set; }
        public int HealthRegeneration { get; set; }
        
        public List<Skill> Skills { get; set; } = new List<Skill>();
        
        // Respawn mechanics
        public bool IsRespawning { get; set; } = false;
        public int RespawnTime { get; set; } = 0;
        public Position RespawnPosition { get; set; }
        
        // Character archetype
        public string Archetype { get; set; }
        
        public Champion(string name, string archetype, Position position, Team team) 
            : base(name, position, team, UnitType.Champion)
        {
            Archetype = archetype;
            RespawnPosition = position; // Start position is default respawn
        }

        public void AddExperience(int amount)
        {
            Experience += amount;
            while (Experience >= ExperienceToNextLevel && Level < 20) // Max level 20
            {
                Experience -= ExperienceToNextLevel;
                LevelUp();
            }
        }

        private void LevelUp()
        {
            Level++;
            // Increase stats on level up
            MaxHealth += 20;
            CurrentHealth = MaxHealth; // Full heal on level up
            MaxMana += 10;
            CurrentMana = MaxMana;
            AttackDamage += 5;
        }

        public override void TakeDamage(int damage)
        {
            base.TakeDamage(damage);
            if (IsDead && !IsRespawning)
            {
                StartRespawn();
            }
        }

        public void StartRespawn()
        {
            IsRespawning = true;
            RespawnTime = 10 + (Level * 2); // Longer respawn at higher levels
        }

        public void UpdateRespawn()
        {
            if (IsRespawning)
            {
                RespawnTime--;
                if (RespawnTime <= 0)
                {
                    Respawn();
                }
            }
        }

        private void Respawn()
        {
            CurrentHealth = MaxHealth;
            CurrentMana = MaxMana;
            Position = RespawnPosition;
            IsRespawning = false;
            RespawnTime = 0;
        }

        public void RegenerateManaAndHealth()
        {
            if (!IsDead)
            {
                CurrentMana = Math.Min(MaxMana, CurrentMana + ManaRegeneration);
                CurrentHealth = Math.Min(MaxHealth, CurrentHealth + HealthRegeneration);
            }
        }

        public void ReduceSkillCooldowns()
        {
            foreach (var skill in Skills)
            {
                skill.ReduceCooldown();
            }
        }

        public Skill? GetSkillByIndex(int index)
        {
            if (index >= 0 && index < Skills.Count)
                return Skills[index];
            return null;
        }

        public bool CanUseSkill(int skillIndex, Position? targetPosition = null)
        {
            var skill = GetSkillByIndex(skillIndex);
            return skill?.CanUse(this, targetPosition) ?? false;
        }

        public void UseSkill(int skillIndex, Position? targetPosition = null, Unit? target = null)
        {
            var skill = GetSkillByIndex(skillIndex);
            skill?.Use(this, targetPosition, target);
        }

        public override string ToString()
        {
            string respawnText = IsRespawning ? $" (Respawning: {RespawnTime})" : "";
            return $"{Name} Lv.{Level} ({Archetype}) - {CurrentHealth}/{MaxHealth} HP, {CurrentMana}/{MaxMana} MP{respawnText}";
        }
    }
}