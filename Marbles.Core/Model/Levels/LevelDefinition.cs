using System;
using System.Collections.Generic;
using Marbles.Core.Model.Components;
using Marbles.Core.Model.Levels.LevelDefinitionComponents;

namespace Marbles.Core.Model.Levels
{
    public class LevelDefinition
    {
        private Dictionary<Type, LevelDefinitionComponent> _components = new Dictionary<Type, LevelDefinitionComponent>();

        public string Name = string.Empty;
        public int BurningMarbleClearBonus = 100;

        public int RowsCount = 10;
        public int ColumnsCount = 10;
        public TimeSpan InitialDuration = TimeSpan.FromMinutes(1);
        public int CompletionScore = 1000;
        public int NumberOfMarblesInSequenceToIncreaseBonusMultiplier = 5;
        public double MultiplierDecreasePerSecond = 0.5;
        public double MultiplierIncrease = 1;
        public bool ElectricMarblesEnabled = true;
        public List<MarbleColor> AvailableColors = new List<MarbleColor>()
                                                         {
                                                             MarbleColor.Yellow,
                                                             MarbleColor.Red,
                                                             MarbleColor.Purple,
                                                             MarbleColor.Blue,
                                                             MarbleColor.Brown,
                                                             MarbleColor.Silver,
                                                             MarbleColor.Orange,
                                                             MarbleColor.Green
                                                         };

        public IEnumerable<LevelDefinitionComponent> Components
        {
            get { return _components.Values; }
        }

        public int Index { get; set; }

        public int TimeIncreaseForCombo = 10;

        public int NumberOfMarblesInSequenceForCombo = 10;


        public double MaximumMultiplier = 5;

        public int MinNumberOfMarblesInSequence = 3;

        public int MaxSpecialMarblesAtTheSameTimeOnBoard = 1;

        public bool HasComponent<T>() where T : LevelDefinitionComponent
        {
            return _components.ContainsKey(typeof (T));
        }

        public T GetComponent<T>() where T : LevelDefinitionComponent
        {
            return (T) _components[typeof(T)];
        }

        public void AddComponent<T>(T levelDefinitionComponent) where T : LevelDefinitionComponent
        {
            _components.Add(typeof(T), levelDefinitionComponent);
        }

        public LevelType LevelType = LevelType.Arcade;
    }
}