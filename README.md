# SobaRL - MOBA Roguelike

A turn-based, single-player MOBA roguelike built in C# with ASCII graphics.

## Game Description

SobaRL transforms the MOBA genre into a coffee-break roguelike experience. You pick one champion out of six available, team up with AI allies, and face off against an enemy team in tactical 3v3 battles on a classic three-lane map.

### Key Features

- **Turn-Based Strategy**: Speed-based simultaneous turns where faster units get multiple actions
- **Champion Archetypes**: Tank, Mage, Assassin with unique skills and playstyles  
- **3-Lane MOBA Map**: Push lanes, destroy towers, protect your nexus
- **Team Combat**: 3v3 battles with AI allies and enemies
- **Skill System**: Each champion has 3 unique abilities with cooldowns
- **Death & Respawn**: Level-based respawn timers and experience progression

## Architecture

The project follows clean architecture principles:

- **SobaRL.Core**: Pure game logic library (no UI dependencies)
- **SobaRL.Game**: Presentation layer (currently console-based, designed for SadConsole)

## Champions Available

### 🛡️ Tank - Ironwall
- **High HP and defensive capabilities**
- **Skills**: Taunt, Shield Bash, Defensive Stance
- **Role**: Frontline protector and damage absorber

### 🔮 Mage - Arcane  
- **Ranged magical damage dealer**
- **Skills**: Fireball, Frost Bolt, Teleport
- **Role**: Area damage and crowd control

### 🗡️ Assassin - Shadow
- **High damage, high mobility**
- **Skills**: Backstab, Shadow Step, Poison Blade
- **Role**: Quick strikes and eliminations

## How to Play

### Controls
- **WASD**: Move your champion
- **Space**: Attack nearest enemy
- **1, 2, 3**: Use champion skills
- **.**: Wait/skip turn
- **Q**: Quit game

### Objective
Destroy the enemy nexus while protecting your own. Work with your AI teammates to push through the three lanes, eliminate enemy towers, and achieve victory!

## Getting Started

### Prerequisites
- .NET 9.0 SDK

### Running the Game
```bash
cd d:\temp\soba-rl
dotnet run --project SobaRL.Game
```

### Building
```bash
dotnet build
```

## Project Status

✅ **Completed MVP Features**:
- Core game engine with turn-based combat
- 3 champion archetypes with unique skills
- 3-lane map with towers and nexus
- AI behavior for allies and enemies
- Death/respawn mechanics
- Minion spawning and lane pushing
- Working console interface

🚧 **In Progress**:
- SadConsole ASCII graphics integration
- Enhanced visual feedback

🔮 **Future Enhancements**:
- Additional champion archetypes (Support, Marksman, Fighter)
- Equipment and item system
- Procedural map generation
- Enhanced AI strategies
- Sound effects and animations

## Technical Details

### Game Systems

#### Time System
The game uses a speed-based turn system where:
- Units with higher speed get more frequent actions
- Time advances when the player acts
- All other units process their actions in speed order

#### Combat System
- Attack damage vs health points
- Skills with mana costs and cooldowns
- Area of effect and targeted abilities
- Experience gain and leveling

#### Map System
- 70x35 grid-based map
- Three lanes connecting player and enemy bases
- Towers providing lane control
- Jungle areas for tactical positioning

### Code Structure

```
SobaRL.Core/
├── Models/          # Game entities (Champion, Unit, Map, etc.)
├── Systems/         # Core game systems (Time, Combat)
├── Champions/       # Champion factory and skills
└── GameEngine.cs    # Main game orchestrator

SobaRL.Game/
├── SimpleConsoleGame.cs  # Console interface
├── GameScreen.cs         # SadConsole interface (WIP)
└── Program.cs            # Entry point
```

## Contributing

The codebase is designed for extensibility:

1. **Adding Champions**: Extend `ChampionFactory` and create new skill classes
2. **New Game Modes**: Implement additional `GameEngine` variants
3. **Enhanced AI**: Extend the AI behavior in `ProcessAITurn` methods
4. **Visual Improvements**: Complete the SadConsole integration

## License

This project is a demonstration/learning project. Feel free to use and modify as needed.

---

*Fight for glory in the lanes of SobaRL!* ⚔️