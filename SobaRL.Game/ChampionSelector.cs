using SobaRL.Core.Models;
using SobaRL.Core.Champions;
using System.Collections.Generic;

namespace SobaRL.Game
{
    public class ChampionSelector
    {
        public List<Champion> AvailableChampions { get; private set; } = new();
        public int SelectedChampionIndex { get; set; } = 0;
        public ChampionSelector()
        {
            CreateAvailableChampions();
        }
        private void CreateAvailableChampions()
        {
            AvailableChampions = new List<Champion>
            {
                ChampionFactory.CreateTank("Ironwall", new Position(0, 0), Team.Player),
                ChampionFactory.CreateMage("Arcane", new Position(0, 0), Team.Player),
                ChampionFactory.CreateAssassin("Shadow", new Position(0, 0), Team.Player)
            };
        }
        public Champion GetSelectedChampion() => AvailableChampions[SelectedChampionIndex];
        public void MoveSelectionUp() => SelectedChampionIndex = SelectedChampionIndex > 0 ? SelectedChampionIndex - 1 : 0;
        public void MoveSelectionDown() => SelectedChampionIndex = SelectedChampionIndex < AvailableChampions.Count - 1 ? SelectedChampionIndex + 1 : SelectedChampionIndex;
    }
}
