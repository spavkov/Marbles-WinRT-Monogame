using System;

namespace Marbles.Core.Model.Levels.LevelDefinitionComponents
{
    public abstract class SpecialMarbleRandomizationSettingComponent : LevelDefinitionComponent
    {
        public int MaximumCountToBeAddedDuringLevel;

        public bool CanBeAddedWhenThereAreAlreadySpecialMarblesOnBoard;

        public int ProbabilityPercentage;

        public abstract SpecialMarbleType SpecialMarbleType { get; }

        public Guid Id = Guid.NewGuid();
    }
}