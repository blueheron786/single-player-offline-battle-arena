using SobaRL.Core;
using SobaRL.Core.Models;
using SobaRL.Core.Champions;

namespace SobaRL.Game
{
    public class SimpleConsoleGame
    {
        private GameEngine _gameEngine;
        private List<Champion> _availableChampions;
        private bool _gameRunning = true;

        public SimpleConsoleGame()
        {
            _gameEngine = new GameEngine();
            _gameEngine.OnGameMessage += OnGameMessage;
            _gameEngine.OnGameStateChanged += OnGameStateChanged;

            // Create available champions
            _availableChampions = new List<Champion>
            {
                ChampionFactory.CreateTank("Ironwall", new Position(0, 0), Team.Player),
                ChampionFactory.CreateMage("Arcane", new Position(0, 0), Team.Player),
                ChampionFactory.CreateAssassin("Shadow", new Position(0, 0), Team.Player)
            };
        }

        public void Run()
        {
            try
            {
                Console.Clear();
            }
            catch (IOException)
            {
                // Console.Clear() failed, continue without clearing
            }
            
            Console.WriteLine("=== SOBA ROGUELIKE - MOBA Console Edition ===");
            Console.WriteLine();

            // Champion selection
            var playerChampion = SelectChampion();
            if (playerChampion == null)
            {
                Console.WriteLine("No champion selected. Exiting...");
                return;
            }

            // Start the game
            StartGame(playerChampion);

            // Main game loop
            while (_gameRunning && _gameEngine.State == GameState.Playing)
            {
                DisplayGame();
                ProcessInput();
            }

            // Game over
            if (_gameEngine.State == GameState.GameOver)
            {
                Console.WriteLine();
                Console.WriteLine($"GAME OVER! {_gameEngine.GameOverReason}");
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
            }
        }

        private Champion? SelectChampion()
        {
            Console.WriteLine("Choose your champion:");
            for (int i = 0; i < _availableChampions.Count; i++)
            {
                var champion = _availableChampions[i];
                Console.WriteLine($"{i + 1}. {champion.Name} ({champion.Archetype})");
                Console.WriteLine($"   HP: {champion.MaxHealth}, MP: {champion.MaxMana}, ATK: {champion.AttackDamage}, SPD: {champion.Speed}");
                Console.WriteLine();
            }

            Console.Write("Enter your choice (1-3): ");
            if (int.TryParse(Console.ReadLine(), out int choice) && choice >= 1 && choice <= _availableChampions.Count)
            {
                return _availableChampions[choice - 1];
            }

            return null;
        }

        private void StartGame(Champion playerChampion)
        {
            // Create all champions for the game
            var allChampions = new List<Champion> { playerChampion };
            
            // Add AI champions for player team
            var playerTeamSpawns = new List<Position>
            {
                new Position(8, 27),
                new Position(10, 29)
            };
            var aiPlayerChampions = ChampionFactory.CreateRandomTeam(2, Team.Player, playerTeamSpawns);
            allChampions.AddRange(aiPlayerChampions);
            
            // Add enemy team champions
            var enemyTeamSpawns = new List<Position>
            {
                new Position(61, 7),
                new Position(59, 9),
                new Position(57, 11)
            };
            var enemyChampions = ChampionFactory.CreateRandomTeam(3, Team.Enemy, enemyTeamSpawns);
            allChampions.AddRange(enemyChampions);

            _gameEngine.StartGame(playerChampion, allChampions);
        }

        private void DisplayGame()
        {
            try
            {
                Console.Clear();
            }
            catch (IOException)
            {
                // Console.Clear() failed, add some separation instead
                Console.WriteLine("\n" + new string('=', 50) + "\n");
            }
            
            Console.WriteLine("=== SOBA ROGUELIKE ===");
            Console.WriteLine();

            // Display map (simplified for console)
            DisplayMap();
            
            Console.WriteLine();
            
            // Player info
            var player = _gameEngine.PlayerChampion;
            Console.WriteLine($"Player: {player.Name} Lv.{player.Level} ({player.Archetype})");
            Console.WriteLine($"HP: {player.CurrentHealth}/{player.MaxHealth} | MP: {player.CurrentMana}/{player.MaxMana}");
            
            // Skills
            Console.Write("Skills: ");
            for (int i = 0; i < player.Skills.Count; i++)
            {
                var skill = player.Skills[i];
                var status = skill.IsReady ? "Ready" : $"CD:{skill.CurrentCooldown}";
                Console.Write($"{i + 1}.{skill.Name}({status}) ");
            }
            Console.WriteLine();
            Console.WriteLine();

            // Controls
            Console.WriteLine("Controls:");
            Console.WriteLine("WASD - Move | Space - Attack | 1,2,3 - Use Skills | . - Wait | Q - Quit");
            Console.WriteLine();
        }

        private void DisplayMap()
        {
            // Display a simplified view of the map
            Console.WriteLine("Map (simplified view):");
            
            // Show a 30x15 section of the map centered on the player
            var player = _gameEngine.PlayerChampion;
            int startX = Math.Max(0, player.Position.X - 15);
            int endX = Math.Min(_gameEngine.Map.Width, player.Position.X + 15);
            int startY = Math.Max(0, player.Position.Y - 7);
            int endY = Math.Min(_gameEngine.Map.Height, player.Position.Y + 8);

            for (int y = startY; y < endY; y++)
            {
                for (int x = startX; x < endX; x++)
                {
                    char displayChar = _gameEngine.Map.GetDisplayChar(new Position(x, y));
                    Console.Write(displayChar);
                }
                Console.WriteLine();
            }
        }

        private void ProcessInput()
        {
            Console.Write("Your move: ");
            ConsoleKeyInfo key;
            try
            {
                key = Console.ReadKey(true);
            }
            catch (InvalidOperationException)
            {
                // ReadKey failed, try ReadLine instead
                Console.WriteLine("Enter command (w/s/a/d/space/1/2/3/./q): ");
                var input = Console.ReadLine()?.ToLower().Trim();
                key = input switch
                {
                    "w" => new ConsoleKeyInfo('w', ConsoleKey.W, false, false, false),
                    "s" => new ConsoleKeyInfo('s', ConsoleKey.S, false, false, false),
                    "a" => new ConsoleKeyInfo('a', ConsoleKey.A, false, false, false),
                    "d" => new ConsoleKeyInfo('d', ConsoleKey.D, false, false, false),
                    "space" or " " => new ConsoleKeyInfo(' ', ConsoleKey.Spacebar, false, false, false),
                    "1" => new ConsoleKeyInfo('1', ConsoleKey.D1, false, false, false),
                    "2" => new ConsoleKeyInfo('2', ConsoleKey.D2, false, false, false),
                    "3" => new ConsoleKeyInfo('3', ConsoleKey.D3, false, false, false),
                    "." => new ConsoleKeyInfo('.', ConsoleKey.OemPeriod, false, false, false),
                    "q" => new ConsoleKeyInfo('q', ConsoleKey.Q, false, false, false),
                    _ => new ConsoleKeyInfo('?', ConsoleKey.Escape, false, false, false)
                };
            }
            Console.WriteLine();

            var player = _gameEngine.PlayerChampion;
            var currentPos = player.Position;
            PlayerActionData? action = null;

            switch (key.Key)
            {
                case ConsoleKey.W:
                    action = new PlayerActionData { Action = PlayerAction.Move, TargetPosition = new Position(currentPos.X, currentPos.Y - 1) };
                    break;
                case ConsoleKey.S:
                    action = new PlayerActionData { Action = PlayerAction.Move, TargetPosition = new Position(currentPos.X, currentPos.Y + 1) };
                    break;
                case ConsoleKey.A:
                    action = new PlayerActionData { Action = PlayerAction.Move, TargetPosition = new Position(currentPos.X - 1, currentPos.Y) };
                    break;
                case ConsoleKey.D:
                    action = new PlayerActionData { Action = PlayerAction.Move, TargetPosition = new Position(currentPos.X + 1, currentPos.Y) };
                    break;
                case ConsoleKey.Spacebar:
                    var nearestEnemy = FindNearestEnemy(player);
                    if (nearestEnemy != null)
                    {
                        action = new PlayerActionData { Action = PlayerAction.Attack, TargetUnit = nearestEnemy };
                    }
                    break;
                case ConsoleKey.D1:
                    action = new PlayerActionData { Action = PlayerAction.UseSkill, SkillIndex = 0, TargetUnit = FindNearestEnemy(player) };
                    break;
                case ConsoleKey.D2:
                    action = new PlayerActionData { Action = PlayerAction.UseSkill, SkillIndex = 1, TargetUnit = FindNearestEnemy(player) };
                    break;
                case ConsoleKey.D3:
                    action = new PlayerActionData { Action = PlayerAction.UseSkill, SkillIndex = 2, TargetUnit = FindNearestEnemy(player) };
                    break;
                case ConsoleKey.OemPeriod:
                    action = new PlayerActionData { Action = PlayerAction.Wait };
                    break;
                case ConsoleKey.Q:
                    _gameRunning = false;
                    return;
            }

            if (action != null)
            {
                bool actionTaken = _gameEngine.ProcessPlayerAction(action);
                if (!actionTaken)
                {
                    Console.WriteLine("Invalid action! Press any key to continue...");
                    Console.ReadKey(true);
                }
            }
            else
            {
                Console.WriteLine("Unknown command! Press any key to continue...");
                Console.ReadKey(true);
            }
        }

        private Unit? FindNearestEnemy(Champion player)
        {
            return _gameEngine.GetUnitsInRange(player.Position, player.AttackRange)
                .Where(u => u.Team != player.Team && u.IsAlive)
                .OrderBy(u => player.Position.ManhattanDistanceTo(u.Position))
                .FirstOrDefault();
        }

        private void OnGameMessage(string message)
        {
            // Could implement a message log or immediate display
        }

        private void OnGameStateChanged(GameState newState)
        {
            if (newState == GameState.GameOver)
            {
                _gameRunning = false;
            }
        }
    }
}