using System.Collections.Generic;
using Marbles.Core.Helpers;
using Marbles.Core.Model;
using Marbles.Core.Model.Levels;
using Marbles.Core.Model.Levels.LevelDefinitionComponents;

namespace Marbles.Core
{
    public class MarblesSurvivalModeEpisode : GameEpisode
    {
        public MarblesSurvivalModeEpisode()
        {
            var level = new LevelDefinition()
                                  {
                                      AvailableColors = new List<MarbleColor>()
                                                            {
                                                           MarbleColor.Red,
                                                           MarbleColor.Yellow,
                                                           MarbleColor.Green,
                                                           MarbleColor.Purple,
                                                           MarbleColor.Blue
                                                            },
                                                            MaxSpecialMarblesAtTheSameTimeOnBoard = 0,

                                                            BurningMarbleClearBonus = 10,
                                                            ColumnsCount = GameConstants.ColumnCount,
                                      RowsCount = GameConstants.RowsCount,
                                                            LevelType = LevelType.Survival,
                                                            MaximumMultiplier = 10,
                                                            MinNumberOfMarblesInSequence = 3,
                                                            NumberOfMarblesInSequenceToIncreaseBonusMultiplier = 5,
                                                            NumberOfMarblesInSequenceForCombo = 7,
                                                            MultiplierIncrease = 1.5,
                                                            MultiplierDecreasePerSecond = 0.3
                                  };

            var postInitial = new PostInitialRandomizationSpecialMarbleSettingsComponent()
                              {
                                  Settings = new List<SpecialMarbleRandomizationSettingComponent>()
                                                 {
                                                     new SurpriseSpecialMarbleRandomizationSettingsComonent()
                                                         {
                                                             CanBeAddedWhenThereAreAlreadySpecialMarblesOnBoard = true,
                                                             MaximumCountToBeAddedDuringLevel = int.MaxValue,
                                                             ProbabilityPercentage = 20,
                                                             SurpriseType = SpecialMarbleType.LineClearerMarble
                                                         },
                                                         new ColorBombSpecialMarbleRandomizationSettingsComponent()
                                                             {
                                                                 CanBeAddedWhenThereAreAlreadySpecialMarblesOnBoard = true,
                                                                 MaximumCountToBeAddedDuringLevel = int.MaxValue,
                                                                 ProbabilityPercentage = 20
                                                             },
                                                         new HorizontalLineClearerSpecialMarbleRandomizationSettingsComponent()
                                                             {
                                                                 CanBeAddedWhenThereAreAlreadySpecialMarblesOnBoard = true,
                                                                 MaximumCountToBeAddedDuringLevel = int.MaxValue,
                                                                 ProbabilityPercentage = 20
                                                             },
                                                         new VerticalLineClearerSpecialMarbleRandomizationSettingsComponent()
                                                             {
                                                                 CanBeAddedWhenThereAreAlreadySpecialMarblesOnBoard = true,
                                                                 MaximumCountToBeAddedDuringLevel = int.MaxValue,
                                                                 ProbabilityPercentage = 20
                                                             },
                                                         new HorizontalAndVerticalLineClearerSpecialMarbleRandomizationSettingsComponent()
                                                             {
                                                                 CanBeAddedWhenThereAreAlreadySpecialMarblesOnBoard = true,
                                                                 MaximumCountToBeAddedDuringLevel = int.MaxValue,
                                                                 ProbabilityPercentage = 20
                                                             }
                                                 }
                              };
            level.AddComponent(postInitial);
            //level.AddComponent(new PostInitialRandomizationDistanceBetweenSpecialItems() {MinDistance = 10, MaxDistance = 60});
            level.MaxSpecialMarblesAtTheSameTimeOnBoard = 0;
            Levels.Add(level);
        }
    }
}