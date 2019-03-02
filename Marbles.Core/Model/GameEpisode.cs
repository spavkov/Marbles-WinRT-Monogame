using System.Collections.Generic;
using Marbles.Core.Model.Levels;

namespace Marbles.Core
{
    public class GameEpisode
    {
        public string Name;

        public List<LevelDefinition> Levels = new List<LevelDefinition>();
    }
}