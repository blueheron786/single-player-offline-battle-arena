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
            // Create a 70x35 map that fits well in 80x37 console (leaving room for UI)
            // Player base at bottom-left corner, enemy base at top-right corner
            
            PlayerNexusPosition = new Position(5, Height - 6);
            EnemyNexusPosition = new Position(Width - 6, 5);
            
            // Create bases
            CreateBase(PlayerNexusPosition, Team.Player);
            CreateBase(EnemyNexusPosition, Team.Enemy);
            
            // Create three lanes with towers
            CreateLane(0); // Top lane
            CreateLane(1); // Middle lane  
            CreateLane(2); // Bottom lane
            
            // Add some jungle areas (walls and obstacles)
            AddJungleAreas();
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

        private void CreateLane(int laneIndex)
        {
            // Calculate lane positions
            int laneY = (Height / 4) + (laneIndex * (Height / 4));
            
            // Create lane path from player side to enemy side
            for (int x = 10; x < Width - 10; x++)
            {
                if (IsValidPosition(new Position(x, laneY)))
                {
                    Cells[x, laneY] = new MapCell(CellType.Lane, '-');
                }
            }
            
            // Add towers along the lane
            Position playerTower = new Position(15, laneY);
            Position enemyTower = new Position(Width - 16, laneY);
            
            PlayerTowerPositions.Add(playerTower);
            EnemyTowerPositions.Add(enemyTower);
            
            Cells[playerTower.X, playerTower.Y] = new MapCell(CellType.Lane, 'T');
            Cells[enemyTower.X, enemyTower.Y] = new MapCell(CellType.Lane, 'T');
            
            // Add spawn positions near player base
            Position playerSpawn = new Position(10, laneY);
            Position enemySpawn = new Position(Width - 11, laneY);
            
            PlayerSpawnPositions.Add(playerSpawn);
            EnemySpawnPositions.Add(enemySpawn);
        }

        private void AddJungleAreas()
        {
            // Add some walls and jungle areas between lanes
            for (int x = 20; x < Width - 20; x += 10)
            {
                for (int y = 10; y < Height - 10; y += 8)
                {
                    if (IsValidPosition(new Position(x, y)) && Cells[x, y].Type == CellType.Empty)
                    {
                        Cells[x, y] = new MapCell(CellType.Jungle, '#');
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
                                        cell.Occupant.Team == Team.Enemy ? 'E' : '?',
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