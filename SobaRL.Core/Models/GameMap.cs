namespace SobaRL.Core.Models
{
    public enum CellType
    {
        Empty,
        Wall,
        Lane,
        Jungle,
        Base
    }

    public class MapCell
    {
        public CellType Type { get; set; }
        public Unit? Occupant { get; set; }
        public char DisplayChar { get; set; }
        public bool IsWalkable => Type != CellType.Wall && Occupant == null;

        public MapCell(CellType type, char displayChar = '.')
        {
            Type = type;
            DisplayChar = displayChar;
        }
    }

    public class GameMap
    {
        public int Width { get; private set; }
        public int Height { get; private set; }
        public MapCell[,] Cells { get; private set; }
        
        public Position PlayerNexusPosition { get; private set; }
        public Position EnemyNexusPosition { get; private set; }
        
        public List<Position> PlayerTowerPositions { get; private set; } = new List<Position>();
        public List<Position> EnemyTowerPositions { get; private set; } = new List<Position>();
        
        public List<Position> PlayerSpawnPositions { get; private set; } = new List<Position>();
        public List<Position> EnemySpawnPositions { get; private set; } = new List<Position>();

        public GameMap(int width, int height)
        {
            Width = width;
            Height = height;
            Cells = new MapCell[width, height];
            InitializeMap();
        }

        private void InitializeMap()
        {
            // Initialize all cells as empty
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    Cells[x, y] = new MapCell(CellType.Empty, '.');
                }
            }
            
            GenerateThreeLaneMap();
        }

        private void GenerateThreeLaneMap()
        {
            // Create DOTA-style map: square path with diagonal
            // Player base at bottom-left corner, enemy base at top-right corner
            
            PlayerNexusPosition = new Position(8, Height - 8);
            EnemyNexusPosition = new Position(Width - 8, 7);
            
            // Fill map with jungle/trees first
            FillWithJungle();
            
            // Create the square path around the perimeter
            CreateSquarePath();
            
            // Create diagonal path from bottom-left to top-right
            CreateDiagonalPath();
            
            // Create bases
            CreateBase(PlayerNexusPosition, Team.Player);
            CreateBase(EnemyNexusPosition, Team.Enemy);
            
            // Add towers along the paths
            PlaceTowers();
            
            // Create spawn positions
            SetupSpawnPositions();
            
            // Add some clearings and strategic empty areas
            AddStrategicClearings();
        }

        private void CreateBase(Position nexusPos, Team team)
        {
            // Create 3x3 base area
            for (int x = nexusPos.X - 1; x <= nexusPos.X + 1; x++)
            {
                for (int y = nexusPos.Y - 1; y <= nexusPos.Y + 1; y++)
                {
                    if (IsValidPosition(new Position(x, y)))
                    {
                        Cells[x, y] = new MapCell(CellType.Base, '█');
                    }
                }
            }
            
            // Clear the nexus position
            Cells[nexusPos.X, nexusPos.Y] = new MapCell(CellType.Base, 'N');
        }

        private void FillWithJungle()
        {
            // Fill most of the map with jungle/trees
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    // Leave some border areas clear and add random variation
                    if (x < 3 || x >= Width - 3 || y < 3 || y >= Height - 3)
                    {
                        Cells[x, y] = new MapCell(CellType.Empty, '.');
                    }
                    else
                    {
                        // Create jungle with some random variation
                        var random = new Random(x * 1000 + y);
                        if (random.NextDouble() < 0.7) // 70% jungle coverage
                        {
                            Cells[x, y] = new MapCell(CellType.Jungle, '♠');
                        }
                    }
                }
            }
        }

        private void CreateSquarePath()
        {
            int pathWidth = 2;
            int margin = 5;
            
            // Top horizontal path
            for (int x = margin; x < Width - margin; x++)
            {
                for (int i = 0; i < pathWidth; i++)
                {
                    Cells[x, margin + i] = new MapCell(CellType.Lane, '=');
                }
            }
            
            // Bottom horizontal path
            for (int x = margin; x < Width - margin; x++)
            {
                for (int i = 0; i < pathWidth; i++)
                {
                    Cells[x, Height - margin - pathWidth + i] = new MapCell(CellType.Lane, '=');
                }
            }
            
            // Left vertical path
            for (int y = margin; y < Height - margin; y++)
            {
                for (int i = 0; i < pathWidth; i++)
                {
                    Cells[margin + i, y] = new MapCell(CellType.Lane, '|');
                }
            }
            
            // Right vertical path
            for (int y = margin; y < Height - margin; y++)
            {
                for (int i = 0; i < pathWidth; i++)
                {
                    Cells[Width - margin - pathWidth + i, y] = new MapCell(CellType.Lane, '|');
                }
            }
        }

        private void CreateDiagonalPath()
        {
            // Create diagonal path from bottom-left to top-right
            int startX = 7;
            int startY = Height - 7;
            int endX = Width - 7;
            int endY = 7;
            
            // Calculate the path points using Bresenham-like algorithm
            int dx = Math.Abs(endX - startX);
            int dy = Math.Abs(endY - startY);
            int x = startX;
            int y = startY;
            int xStep = startX < endX ? 1 : -1;
            int yStep = startY < endY ? 1 : -1;
            int error = dx - dy;
            
            while (true)
            {
                // Create a wider path (2-3 cells wide)
                for (int i = -1; i <= 1; i++)
                {
                    for (int j = -1; j <= 1; j++)
                    {
                        int newX = x + i;
                        int newY = y + j;
                        if (IsValidPosition(new Position(newX, newY)))
                        {
                            Cells[newX, newY] = new MapCell(CellType.Lane, '~');
                        }
                    }
                }
                
                if (x == endX && y == endY) break;
                
                int error2 = 2 * error;
                if (error2 > -dy)
                {
                    error -= dy;
                    x += xStep;
                }
                if (error2 < dx)
                {
                    error += dx;
                    y += yStep;
                }
            }
        }

        private void PlaceTowers()
        {
            // Clear the lists first
            PlayerTowerPositions.Clear();
            EnemyTowerPositions.Clear();
            
            // Place towers at strategic locations along the paths
            
            // Player towers (bottom-left area)
            var playerTowers = new[]
            {
                new Position(12, Height - 7),  // Bottom path
                new Position(7, Height - 12),  // Left path  
                new Position(15, Height - 15)  // Diagonal path
            };
            
            // Enemy towers (top-right area)
            var enemyTowers = new[]
            {
                new Position(Width - 12, 7),   // Top path
                new Position(Width - 7, 12),   // Right path
                new Position(Width - 15, 15)   // Diagonal path
            };
            
            foreach (var pos in playerTowers)
            {
                if (IsValidPosition(pos))
                {
                    PlayerTowerPositions.Add(pos);
                    Cells[pos.X, pos.Y] = new MapCell(CellType.Lane, '♦');
                }
            }
            
            foreach (var pos in enemyTowers)
            {
                if (IsValidPosition(pos))
                {
                    EnemyTowerPositions.Add(pos);
                    Cells[pos.X, pos.Y] = new MapCell(CellType.Lane, '♦');
                }
            }
        }

        private void SetupSpawnPositions()
        {
            // Clear the lists first
            PlayerSpawnPositions.Clear();
            EnemySpawnPositions.Clear();
            
            // Player spawn positions near player base
            PlayerSpawnPositions.Add(new Position(10, Height - 7));
            PlayerSpawnPositions.Add(new Position(7, Height - 10));
            PlayerSpawnPositions.Add(new Position(12, Height - 12));
            
            // Enemy spawn positions near enemy base
            EnemySpawnPositions.Add(new Position(Width - 10, 7));
            EnemySpawnPositions.Add(new Position(Width - 7, 10));
            EnemySpawnPositions.Add(new Position(Width - 12, 12));
        }

        private void AddStrategicClearings()
        {
            // Add some strategic empty areas for tactical gameplay
            var clearings = new[]
            {
                new Position(Width / 2, Height / 2),     // Center clearing
                new Position(Width / 3, Height / 3),     // Lower clearing
                new Position(2 * Width / 3, 2 * Height / 3), // Upper clearing
            };
            
            foreach (var center in clearings)
            {
                // Create 3x3 clearings
                for (int x = center.X - 1; x <= center.X + 1; x++)
                {
                    for (int y = center.Y - 1; y <= center.Y + 1; y++)
                    {
                        if (IsValidPosition(new Position(x, y)))
                        {
                            Cells[x, y] = new MapCell(CellType.Empty, '.');
                        }
                    }
                }
            }
        }

        public bool IsValidPosition(Position pos)
        {
            return pos.X >= 0 && pos.X < Width && pos.Y >= 0 && pos.Y < Height;
        }

        public bool IsPositionEmpty(Position pos)
        {
            if (!IsValidPosition(pos))
                return false;
                
            return Cells[pos.X, pos.Y].IsWalkable;
        }

        public Unit? GetUnitAt(Position pos)
        {
            if (!IsValidPosition(pos))
                return null;
                
            return Cells[pos.X, pos.Y].Occupant;
        }

        public void PlaceUnit(Unit unit, Position pos)
        {
            if (!IsValidPosition(pos))
                return;
                
            // Remove unit from old position
            RemoveUnit(unit);
            
            // Place unit at new position
            Cells[pos.X, pos.Y].Occupant = unit;
            unit.Position = pos;
        }

        public void RemoveUnit(Unit unit)
        {
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    if (Cells[x, y].Occupant == unit)
                    {
                        Cells[x, y].Occupant = null;
                        break;
                    }
                }
            }
        }

        public List<Unit> GetUnitsInRange(Position center, int range)
        {
            var units = new List<Unit>();
            
            for (int x = center.X - range; x <= center.X + range; x++)
            {
                for (int y = center.Y - range; y <= center.Y + range; y++)
                {
                    var pos = new Position(x, y);
                    if (IsValidPosition(pos))
                    {
                        var unit = GetUnitAt(pos);
                        if (unit != null && center.ManhattanDistanceTo(pos) <= range)
                        {
                            units.Add(unit);
                        }
                    }
                }
            }
            
            return units;
        }

        public char GetDisplayChar(Position pos)
        {
            if (!IsValidPosition(pos))
                return ' ';
                
            var cell = Cells[pos.X, pos.Y];
            if (cell.Occupant != null)
            {
                // Return character representation of unit
                return cell.Occupant.UnitType switch
                {
                    UnitType.Champion => cell.Occupant.Team == Team.Player ? '@' : 
                                        cell.Occupant.Team == Team.Enemy ? '@' : '?',
                    UnitType.Minion => cell.Occupant.Team == Team.Player ? 'm' : 'M',
                    UnitType.Tower => 'T',
                    UnitType.Nexus => 'N',
                    _ => '?'
                };
            }
            
            return cell.DisplayChar;
        }
    }
}