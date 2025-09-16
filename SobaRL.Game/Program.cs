using SadConsole;
using SadConsole.Configuration;

namespace SobaRL.Game
{
    class Program
    {
        static void Main()
        {
            Settings.WindowTitle = "SOBA RL - MOBA Roguelike";

            // Configure how SadConsole starts up using the modern Builder pattern
            Builder startup = new Builder()
                .SetWindowSizeInCells(80, 37)
                .SetStartingScreen<GameScreen>()
                .IsStartingScreenFocused(true)
                .ConfigureFonts();

            // Create and run the game
            SadConsole.Game.Create(startup);
            SadConsole.Game.Instance.Run();
            SadConsole.Game.Instance.Dispose();
        }
    }
}
