using SobaRL.Core.Models;
using SobaRL.Core.Systems;

namespace SobaRL.Core
{
    public enum GameState
    {
        ChampionSelect,
        Playing,
        GameOver,
        Paused
    }

    public enum PlayerAction
    {
        Move,
        Attack,
        UseSkill,
        Wait
    }

    public class PlayerActionData
    {
        public PlayerAction Action { get; set; }
        public Position? TargetPosition { get; set; }
        public Unit? TargetUnit { get; set; }
        public int SkillIndex { get; set; }
    }

    public class GameEngine
    {
        public GameMap Map { get; private set; }
        public TimeSystem TimeSystem { get; private set; }
        public GameState State { get; private set; }
        
        public Champion PlayerChampion { get; private set; }
        public List<Champion> AllChampions { get; private set; } = new List<Champion>();
        public List<Unit> AllUnits { get; private set; } = new List<Unit>();
        
        public Team WinningTeam { get; private set; } = Team.Neutral;
        public string GameOverReason { get; private set; } = "";
        
        private Random _random = new Random();
        private int _minionSpawnTimer = 0;
        private const int MINION_SPAWN_INTERVAL = 10; // Spawn minions every 10 turns

        public event Action<string>? OnGameMessage;
        public event Action<GameState>? OnGameStateChanged;

        public GameEngine()
        {
            Map = new GameMap(70, 35);
            TimeSystem = new TimeSystem();
            State = GameState.ChampionSelect;
        }

        public void StartGame(Champion playerChampion, List<Champion> allChampions)
        {
            PlayerChampion = playerChampion;
            AllChampions = allChampions;
            
            // Place champions on the map
            PlaceChampions();
            
            // Create towers and nexuses
            CreateStructures();
            
            // Register units in time system (exclude nexuses which don't act)
            foreach (var unit in AllUnits)
            {
                // Only register units that can take actions (exclude nexuses)
                if (unit.UnitType != UnitType.Nexus)
                {
                    TimeSystem.RegisterUnit(unit);
                }
            }
            
            State = GameState.Playing;
            OnGameStateChanged?.Invoke(State);
            OnGameMessage?.Invoke($"Game started! You are playing as {PlayerChampion.Name}");
        }

        private void PlaceChampions()
        {
            // Place player team champions near player base
            var playerChampions = AllChampions.Where(c => c.Team == Team.Player).ToList();
            for (int i = 0; i < playerChampions.Count; i++)
            {
                var champion = playerChampions[i];
                var spawnPos = new Position(8 + i * 2, Map.Height - 8 - i);
                Map.PlaceUnit(champion, spawnPos);
                champion.RespawnPosition = spawnPos;
                AllUnits.Add(champion);
            }

            // Place enemy team champions near enemy base  
            var enemyChampions = AllChampions.Where(c => c.Team == Team.Enemy).ToList();
            for (int i = 0; i < enemyChampions.Count; i++)
            {
                var champion = enemyChampions[i];
                var spawnPos = new Position(Map.Width - 9 - i * 2, 7 + i);
                Map.PlaceUnit(champion, spawnPos);
                champion.RespawnPosition = spawnPos;
                AllUnits.Add(champion);
            }
        }

        private void CreateStructures()
        {
            // Create nexuses
            var playerNexus = new Nexus("Player Nexus", Map.PlayerNexusPosition, Team.Player);
            var enemyNexus = new Nexus("Enemy Nexus", Map.EnemyNexusPosition, Team.Enemy);
            
            Map.PlaceUnit(playerNexus, Map.PlayerNexusPosition);
            Map.PlaceUnit(enemyNexus, Map.EnemyNexusPosition);
            AllUnits.Add(playerNexus);
            AllUnits.Add(enemyNexus);

            // Create towers
            for (int i = 0; i < Map.PlayerTowerPositions.Count; i++)
            {
                var playerTower = new Tower($"Player Tower {i + 1}", Map.PlayerTowerPositions[i], Team.Player, i);
                var enemyTower = new Tower($"Enemy Tower {i + 1}", Map.EnemyTowerPositions[i], Team.Enemy, i);
                
                Map.PlaceUnit(playerTower, Map.PlayerTowerPositions[i]);
                Map.PlaceUnit(enemyTower, Map.EnemyTowerPositions[i]);
                AllUnits.Add(playerTower);
                AllUnits.Add(enemyTower);
            }
        }

        public bool ProcessPlayerAction(PlayerActionData actionData)
        {
            if (State != GameState.Playing || !TimeSystem.CanUnitAct(PlayerChampion))
                return false;

            bool actionTaken = false;

            switch (actionData.Action)
            {
                case PlayerAction.Move:
                    if (actionData.TargetPosition.HasValue)
                    {
                        actionTaken = TryMoveUnit(PlayerChampion, actionData.TargetPosition.Value);
                    }
                    break;

                case PlayerAction.Attack:
                    if (actionData.TargetUnit != null)
                    {
                        actionTaken = TryAttackUnit(PlayerChampion, actionData.TargetUnit);
                    }
                    break;

                case PlayerAction.UseSkill:
                    actionTaken = TryUseSkill(PlayerChampion, actionData.SkillIndex, 
                        actionData.TargetPosition, actionData.TargetUnit);
                    break;

                case PlayerAction.Wait:
                    actionTaken = true;
                    break;
            }

            if (actionTaken)
            {
                TimeSystem.ConsumeUnitAction(PlayerChampion);
                ProcessGameTurn();
            }

            return actionTaken;
        }

        private bool TryMoveUnit(Unit unit, Position targetPosition)
        {
            if (unit.CanMoveTo(targetPosition, Map))
            {
                Map.PlaceUnit(unit, targetPosition);
                OnGameMessage?.Invoke($"{unit.Name} moved to {targetPosition}");
                return true;
            }
            return false;
        }

        private bool TryAttackUnit(Unit attacker, Unit target)
        {
            if (attacker.CanAttack(target))
            {
                attacker.Attack(target);
                OnGameMessage?.Invoke($"{attacker.Name} attacked {target.Name} for {attacker.AttackDamage} damage!");
                
                if (target.IsDead)
                {
                    HandleUnitDeath(target);
                }
                return true;
            }
            return false;
        }

        private bool TryUseSkill(Champion champion, int skillIndex, Position? targetPosition, Unit? target)
        {
            if (champion.CanUseSkill(skillIndex, targetPosition))
            {
                var skill = champion.GetSkillByIndex(skillIndex);
                champion.UseSkill(skillIndex, targetPosition, target);
                OnGameMessage?.Invoke($"{champion.Name} used {skill?.Name}!");
                
                if (target != null && target.IsDead)
                {
                    HandleUnitDeath(target);
                }
                return true;
            }
            return false;
        }

        private void ProcessGameTurn()
        {
            TimeSystem.AdvanceTime();
            
            // Process all units that can act this turn
            var readyUnits = TimeSystem.GetUnitsReadyToAct();
            
            foreach (var unit in readyUnits)
            {
                if (unit != PlayerChampion && unit.IsAlive)
                {
                    ProcessAITurn(unit);
                    TimeSystem.ConsumeUnitAction(unit);
                }
            }

            // Update champions
            foreach (var champion in AllChampions)
            {
                champion.RegenerateManaAndHealth();
                champion.ReduceSkillCooldowns();
                champion.UpdateRespawn();
            }

            // Spawn minions periodically
            _minionSpawnTimer++;
            if (_minionSpawnTimer >= MINION_SPAWN_INTERVAL)
            {
                SpawnMinions();
                _minionSpawnTimer = 0;
            }

            // Check win conditions
            CheckWinConditions();
        }

        private void ProcessAITurn(Unit unit)
        {
            // Simple AI behavior - move towards enemy nexus or attack nearby enemies
            switch (unit.UnitType)
            {
                case UnitType.Champion:
                    ProcessChampionAI(unit as Champion);
                    break;
                case UnitType.Minion:
                    ProcessMinionAI(unit as Minion);
                    break;
                case UnitType.Tower:
                    ProcessTowerAI(unit as Tower);
                    break;
            }
        }

        private void ProcessChampionAI(Champion? champion)
        {
            if (champion == null) return;

            // Find nearest enemy
            var enemies = AllUnits.Where(u => u.Team != champion.Team && u.IsAlive).ToList();
            var nearestEnemy = enemies.OrderBy(e => champion.Position.ManhattanDistanceTo(e.Position)).FirstOrDefault();

            if (nearestEnemy != null)
            {
                // Try to attack if in range
                if (champion.CanAttack(nearestEnemy))
                {
                    TryAttackUnit(champion, nearestEnemy);
                }
                else
                {
                    // Move towards enemy
                    var targetPos = GetPositionTowardsTarget(champion.Position, nearestEnemy.Position);
                    TryMoveUnit(champion, targetPos);
                }
            }
        }

        private void ProcessMinionAI(Minion? minion)
        {
            if (minion == null) return;

            // Move towards target lane position
            var nextPos = minion.GetNextMovePosition();
            TryMoveUnit(minion, nextPos);

            // Attack nearby enemies
            var enemiesInRange = Map.GetUnitsInRange(minion.Position, minion.AttackRange)
                .Where(u => u.Team != minion.Team && u.IsAlive).ToList();
            
            var target = enemiesInRange.FirstOrDefault();
            if (target != null)
            {
                TryAttackUnit(minion, target);
            }
        }

        private void ProcessTowerAI(Tower? tower)
        {
            if (tower == null) return;

            // Attack nearest enemy in range
            var enemiesInRange = Map.GetUnitsInRange(tower.Position, tower.AttackRange)
                .Where(u => u.Team != tower.Team && u.IsAlive).ToList();
            
            var target = enemiesInRange.OrderBy(e => tower.Position.ManhattanDistanceTo(e.Position)).FirstOrDefault();
            if (target != null)
            {
                TryAttackUnit(tower, target);
            }
        }

        private Position GetPositionTowardsTarget(Position from, Position to)
        {
            int dx = Math.Sign(to.X - from.X);
            int dy = Math.Sign(to.Y - from.Y);
            return new Position(from.X + dx, from.Y + dy);
        }

        private void SpawnMinions()
        {
            // Spawn minions for each team in each lane
            for (int lane = 0; lane < 3; lane++)
            {
                // Player minions
                if (lane < Map.PlayerSpawnPositions.Count)
                {
                    var playerMinionPos = Map.PlayerSpawnPositions[lane];
                    if (Map.IsPositionEmpty(playerMinionPos))
                    {
                        var playerMinion = new Minion($"Player Minion", playerMinionPos, Team.Player, lane);
                        playerMinion.SetTarget(Map.EnemyNexusPosition);
                        Map.PlaceUnit(playerMinion, playerMinionPos);
                        AllUnits.Add(playerMinion);
                        TimeSystem.RegisterUnit(playerMinion);
                    }
                }

                // Enemy minions
                if (lane < Map.EnemySpawnPositions.Count)
                {
                    var enemyMinionPos = Map.EnemySpawnPositions[lane];
                    if (Map.IsPositionEmpty(enemyMinionPos))
                    {
                        var enemyMinion = new Minion($"Enemy Minion", enemyMinionPos, Team.Enemy, lane);
                        enemyMinion.SetTarget(Map.PlayerNexusPosition);
                        Map.PlaceUnit(enemyMinion, enemyMinionPos);
                        AllUnits.Add(enemyMinion);
                        TimeSystem.RegisterUnit(enemyMinion);
                    }
                }
            }
        }

        private void HandleUnitDeath(Unit unit)
        {
            OnGameMessage?.Invoke($"{unit.Name} has been defeated!");
            
            if (unit.UnitType == UnitType.Champion)
            {
                var champion = unit as Champion;
                champion?.StartRespawn();
                
                // Award experience to nearby enemies
                var enemies = Map.GetUnitsInRange(unit.Position, 3)
                    .OfType<Champion>()
                    .Where(c => c.Team != unit.Team && c.IsAlive);
                
                foreach (var enemy in enemies)
                {
                    enemy.AddExperience(50);
                    OnGameMessage?.Invoke($"{enemy.Name} gained experience!");
                }
            }
            else if (unit.UnitType != UnitType.Champion)
            {
                // Remove non-champion units from the game
                Map.RemoveUnit(unit);
                AllUnits.Remove(unit);
                TimeSystem.UnregisterUnit(unit);
            }
        }

        private void CheckWinConditions()
        {
            var playerNexus = AllUnits.OfType<Nexus>().FirstOrDefault(n => n.Team == Team.Player);
            var enemyNexus = AllUnits.OfType<Nexus>().FirstOrDefault(n => n.Team == Team.Enemy);

            if (playerNexus?.IsDead == true)
            {
                WinningTeam = Team.Enemy;
                GameOverReason = "Your nexus has been destroyed!";
                State = GameState.GameOver;
                OnGameStateChanged?.Invoke(State);
            }
            else if (enemyNexus?.IsDead == true)
            {
                WinningTeam = Team.Player;
                GameOverReason = "You destroyed the enemy nexus! Victory!";
                State = GameState.GameOver;
                OnGameStateChanged?.Invoke(State);
            }
        }

        public List<Unit> GetUnitsInRange(Position center, int range)
        {
            return Map.GetUnitsInRange(center, range);
        }

        public Unit? GetUnitAt(Position position)
        {
            return Map.GetUnitAt(position);
        }

        public bool IsValidMove(Position position)
        {
            return Map.IsPositionEmpty(position);
        }
    }
}