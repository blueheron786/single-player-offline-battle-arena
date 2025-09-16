using SadConsole;
using SadConsole.Input;
using SobaRL.Core;
using SobaRL.Core.Models;
using SobaRL.Core.Champions;
using SadRogue.Primitives;

namespace SobaRL.Game
{
    public class GameScreen : SadConsole.Console
    {
        private GameEngine _gameEngine;
        private List<Champion> _availableChampions = new();
        private bool _championSelected = false;
        private int _selectedChampionIndex = 0;
        private Queue<string> _messageLog = new Queue<string>();
        private const int MAX_MESSAGES = 5;
        
        // Attack animation system
        private Dictionary<Position, AttackAnimation> _attackAnimations = new Dictionary<Position, AttackAnimation>();

        private class AttackAnimation
        {
            public char Character { get; set; }
            public SadRogue.Primitives.Color Color { get; set; }
            public DateTime StartTime { get; set; }
            public int DurationMs { get; set; }
            
            public AttackAnimation(char character, SadRogue.Primitives.Color color, int durationMs = 150)
            {
                Character = character;
                Color = color;
                StartTime = DateTime.Now;
                DurationMs = durationMs;
            }
            
            public bool IsExpired => DateTime.Now.Subtract(StartTime).TotalMilliseconds > DurationMs;
        }

        public GameScreen() : base(80, 37)
        {
            _gameEngine = new GameEngine();
            _gameEngine.OnGameMessage += OnGameMessage;
            _gameEngine.OnGameStateChanged += OnGameStateChanged;

            // Create available champions
            CreateAvailableChampions();

            // Start with champion selection
            ShowChampionSelection();

            // Make this console focused for input
            UseKeyboard = true;
            IsFocused = true;
        }

        public override void Update(TimeSpan delta)
        {
            base.Update(delta);
            
            // Clean up expired animations
            if (_attackAnimations.Count > 0)
            {
                var expiredAnimations = _attackAnimations
                    .Where(kvp => kvp.Value.IsExpired)
                    .Select(kvp => kvp.Key)
                    .ToList();
                    
                bool hasExpired = false;
                foreach (var pos in expiredAnimations)
                {
                    _attackAnimations.Remove(pos);
                    hasExpired = true;
                }
                
                // Only update display if animations expired
                if (hasExpired && _championSelected)
                {
                    UpdateDisplay();
                }
            }
        }

        private void CreateAvailableChampions()
        {
            _availableChampions = new List<Champion>
            {
                ChampionFactory.CreateTank("Ironwall", new Position(0, 0), Team.Player),
                ChampionFactory.CreateMage("Arcane", new Position(0, 0), Team.Player),
                ChampionFactory.CreateAssassin("Shadow", new Position(0, 0), Team.Player)
            };
        }

        private void ShowChampionSelection()
        {
            this.Clear();
            this.Print(1, 1, "=== CHAMPION SELECT ===", SadRogue.Primitives.Color.Yellow);
            this.Print(1, 3, "Choose your champion:");

            for (int i = 0; i < _availableChampions.Count; i++)
            {
                var champion = _availableChampions[i];
                var color = i == _selectedChampionIndex ? SadRogue.Primitives.Color.White : SadRogue.Primitives.Color.Gray;
                var marker = i == _selectedChampionIndex ? "> " : "  ";
                
                this.Print(1, 5 + i * 3, $"{marker}{i + 1}. {champion.Name} ({champion.Archetype})", color);
                this.Print(3, 6 + i * 3, $"   HP: {champion.MaxHealth}, MP: {champion.MaxMana}", color);
                this.Print(3, 7 + i * 3, $"   ATK: {champion.AttackDamage}, SPD: {champion.Speed}", color);
            }

            this.Print(1, 16, "Use UP/DOWN arrows to select, ENTER to confirm", SadRogue.Primitives.Color.Cyan);
        }

        private void StartGame()
        {
            var playerChampion = _availableChampions[_selectedChampionIndex];
            
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
            _championSelected = true;
            
            UpdateDisplay();
        }

        private void UpdateDisplay()
        {
            if (!_championSelected) return;

            UpdateMapDisplay();
            UpdateUIDisplay();
        }

        private void UpdateMapDisplay()
        {
            // Clear only the map area (leave UI at bottom)
            for (int x = 0; x < this.Width; x++)
            {
                for (int y = 1; y < this.Height - 2; y++) // Reserve top and bottom lines for UI
                {
                    this.SetGlyph(x, y, ' ', SadRogue.Primitives.Color.Black);
                }
            }
            
            // Ensure we don't exceed console bounds for the map
            int maxWidth = Math.Min(_gameEngine.Map.Width, this.Width);
            int maxHeight = Math.Min(_gameEngine.Map.Height, this.Height - 3); // Reserve space for UI
            
            // Draw the map starting at row 1
            for (int x = 0; x < maxWidth; x++)
            {
                for (int y = 0; y < maxHeight; y++)
                {
                    var pos = new Position(x, y);
                    var displayChar = _gameEngine.Map.GetDisplayChar(pos);
                    var unit = _gameEngine.Map.GetUnitAt(pos);
                    
                    // Check if there's an attack animation at this position
                    if (_attackAnimations.ContainsKey(pos))
                    {
                        var animation = _attackAnimations[pos];
                        this.SetGlyph(x, y + 1, animation.Character, animation.Color);
                        continue; // Skip normal rendering for this position
                    }
                    
                    SadRogue.Primitives.Color color = SadRogue.Primitives.Color.White;
                    if (unit != null)
                    {
                        color = unit.Team switch
                        {
                            Team.Player => SadRogue.Primitives.Color.Blue,
                            Team.Enemy => SadRogue.Primitives.Color.Red,
                            Team.Neutral => SadRogue.Primitives.Color.Yellow,
                            _ => SadRogue.Primitives.Color.White
                        };
                        
                        if (unit == _gameEngine.PlayerChampion)
                            color = SadRogue.Primitives.Color.LightBlue;
                    }
                    else
                    {
                        color = displayChar switch
                        {
                            'T' => SadRogue.Primitives.Color.Gray,
                            'N' => SadRogue.Primitives.Color.Purple,
                            '-' => SadRogue.Primitives.Color.DarkGray,
                            '#' => SadRogue.Primitives.Color.Green,
                            '█' => SadRogue.Primitives.Color.DarkRed,
                            _ => SadRogue.Primitives.Color.DarkGray
                        };
                    }
                    
                    this.SetGlyph(x, y + 1, displayChar, color); // Offset by 1 to leave top row for title
                }
            }
        }

        private void UpdateUIDisplay()
        {
            // Clear the UI area and draw title
            this.Print(1, 0, "=== SOBA ROGUELIKE ===", SadRogue.Primitives.Color.Yellow);
            
            // Draw messages in the middle area (between map and player info)
            int messageStartY = this.Height - 8;
            int messageIndex = 0;
            foreach (var message in _messageLog.Reverse().Take(5))
            {
                if (messageStartY + messageIndex < this.Height - 2)
                {
                    // Clear the line first
                    for (int x = 1; x < this.Width - 1; x++)
                    {
                        this.SetGlyph(x, messageStartY + messageIndex, ' ');
                    }
                    
                    // Truncate message if too long
                    var displayMessage = message.Length > 70 ? message.Substring(0, 67) + "..." : message;
                    this.Print(1, messageStartY + messageIndex, displayMessage, SadRogue.Primitives.Color.LightGray);
                }
                messageIndex++;
            }
            
            // Only show player info if we have a valid player champion
            if (_gameEngine.PlayerChampion != null)
            {
                var player = _gameEngine.PlayerChampion;
                // Draw player info at the bottom of the screen
                int bottomRow = this.Height - 1;
                this.Print(1, bottomRow, $"{player.Name} Lv.{player.Level}", SadRogue.Primitives.Color.White);
                this.Print(20, bottomRow, $"HP: {player.CurrentHealth}/{player.MaxHealth}", SadRogue.Primitives.Color.Red);
                this.Print(35, bottomRow, $"MP: {player.CurrentMana}/{player.MaxMana}", SadRogue.Primitives.Color.Blue);
                
                // Skills
                this.Print(50, bottomRow, "Skills:", SadRogue.Primitives.Color.Yellow);
                for (int i = 0; i < player.Skills.Count && i < 3; i++)
                {
                    var skill = player.Skills[i];
                    var skillText = $"{i + 1}:{skill.Symbol}";
                    var color = skill.IsReady ? SadRogue.Primitives.Color.White : SadRogue.Primitives.Color.Gray;
                    var xPos = 57 + i * 4;
                    if (xPos < this.Width - 4) // Ensure we have space for the text
                    {
                        this.Print(xPos, bottomRow, skillText, color);
                    }
                }
            }

            // Controls (on the right side, but within bounds)
            if (this.Width > 75)
            {
                this.Print(72, 1, "Move:", SadRogue.Primitives.Color.Cyan);
                this.Print(72, 2, "WASD", SadRogue.Primitives.Color.White);
                this.Print(72, 4, "Att:", SadRogue.Primitives.Color.Cyan);
                this.Print(72, 5, "Space", SadRogue.Primitives.Color.White);
                this.Print(72, 7, "Skills:", SadRogue.Primitives.Color.Cyan);
                this.Print(72, 8, "1,2,3", SadRogue.Primitives.Color.White);
                this.Print(72, 10, "Wait:", SadRogue.Primitives.Color.Cyan);
                this.Print(72, 11, ".", SadRogue.Primitives.Color.White);
            }

            // Game state info
            if (_gameEngine.State == GameState.GameOver)
            {
                this.Print(1, 18, "GAME OVER!", SadRogue.Primitives.Color.Red);
                this.Print(1, 19, _gameEngine.GameOverReason, SadRogue.Primitives.Color.White);
                this.Print(1, 20, "Press ESC to exit", SadRogue.Primitives.Color.Cyan);
            }
        }

        public override bool ProcessKeyboard(Keyboard keyboard)
        {
            if (!_championSelected)
            {
                return ProcessChampionSelection(keyboard);
            }
            else
            {
                return ProcessGameInput(keyboard);
            }
        }

        private bool ProcessChampionSelection(Keyboard keyboard)
        {
            if (keyboard.IsKeyPressed(Keys.Up))
            {
                _selectedChampionIndex = Math.Max(0, _selectedChampionIndex - 1);
                ShowChampionSelection();
                return true;
            }
            if (keyboard.IsKeyPressed(Keys.Down))
            {
                _selectedChampionIndex = Math.Min(_availableChampions.Count - 1, _selectedChampionIndex + 1);
                ShowChampionSelection();
                return true;
            }
            if (keyboard.IsKeyPressed(Keys.Enter))
            {
                StartGame();
                return true;
            }
            
            return false;
        }

        private bool ProcessGameInput(Keyboard keyboard)
        {
            if (_gameEngine.State != GameState.Playing)
            {
                if (keyboard.IsKeyPressed(Keys.Escape))
                {
                    Environment.Exit(0);
                }
                return false;
            }

            var player = _gameEngine.PlayerChampion;
            var currentPos = player.Position;
            PlayerActionData? action = null;

            // Movement
            if (keyboard.IsKeyPressed(Keys.W))
                action = new PlayerActionData { Action = PlayerAction.Move, TargetPosition = new Position(currentPos.X, currentPos.Y - 1) };
            else if (keyboard.IsKeyPressed(Keys.S))
                action = new PlayerActionData { Action = PlayerAction.Move, TargetPosition = new Position(currentPos.X, currentPos.Y + 1) };
            else if (keyboard.IsKeyPressed(Keys.A))
                action = new PlayerActionData { Action = PlayerAction.Move, TargetPosition = new Position(currentPos.X - 1, currentPos.Y) };
            else if (keyboard.IsKeyPressed(Keys.D))
                action = new PlayerActionData { Action = PlayerAction.Move, TargetPosition = new Position(currentPos.X + 1, currentPos.Y) };
            
            // Attack (target nearest enemy)
            else if (keyboard.IsKeyPressed(Keys.Space))
            {
                var nearestEnemy = FindNearestEnemy(player);
                if (nearestEnemy != null)
                {
                    action = new PlayerActionData { Action = PlayerAction.Attack, TargetUnit = nearestEnemy };
                }
            }
            
            // Skills
            else if (keyboard.IsKeyPressed(Keys.D1))
                action = new PlayerActionData { Action = PlayerAction.UseSkill, SkillIndex = 0, TargetUnit = FindNearestEnemy(player) };
            else if (keyboard.IsKeyPressed(Keys.D2))
                action = new PlayerActionData { Action = PlayerAction.UseSkill, SkillIndex = 1, TargetUnit = FindNearestEnemy(player) };
            else if (keyboard.IsKeyPressed(Keys.D3))
                action = new PlayerActionData { Action = PlayerAction.UseSkill, SkillIndex = 2, TargetUnit = FindNearestEnemy(player) };
            
            // Wait
            else if (keyboard.IsKeyPressed(Keys.OemPeriod))
                action = new PlayerActionData { Action = PlayerAction.Wait };

            if (action != null)
            {
                _gameEngine.ProcessPlayerAction(action);
                UpdateDisplay();
                return true;
            }

            return false;
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
            // Add message to the log
            _messageLog.Enqueue(message);
            
            // Keep only the last MAX_MESSAGES messages
            while (_messageLog.Count > MAX_MESSAGES)
            {
                _messageLog.Dequeue();
            }
            
            // Check for attack messages to trigger animations
            if (message.Contains("attacks") && message.Contains("(bump attack)"))
            {
                // Extract attacker name and show animation at their position
                var parts = message.Split(' ');
                if (parts.Length > 0)
                {
                    var attackerName = parts[0];
                    var attacker = _gameEngine.AllUnits.FirstOrDefault(u => u.Name == attackerName);
                    if (attacker != null)
                    {
                        ShowAttackAnimation(attacker.Position);
                    }
                }
            }
            else if (message.Contains("attacked") && !message.Contains("(bump attack)"))
            {
                // Regular attack animation
                var parts = message.Split(' ');
                if (parts.Length > 2)
                {
                    var attackerName = parts[0];
                    var attacker = _gameEngine.AllUnits.FirstOrDefault(u => u.Name == attackerName);
                    if (attacker != null)
                    {
                        ShowAttackAnimation(attacker.Position);
                    }
                }
            }
            
            // Update the display to show new message
            if (_championSelected)
            {
                UpdateDisplay();
            }
        }

        private void ShowAttackAnimation(Position attackerPos)
        {
            // Create attack animation at attacker's position
            var random = new Random();
            var attackChars = new[] { '/', '\\', '*', '+' };
            var attackChar = attackChars[random.Next(attackChars.Length)];
            var attackColor = SadRogue.Primitives.Color.Yellow;
            
            // Short 100ms flicker animation
            _attackAnimations[attackerPos] = new AttackAnimation(attackChar, attackColor, 100);
        }

        private void OnGameStateChanged(GameState newState)
        {
            UpdateDisplay();
        }
    }
}