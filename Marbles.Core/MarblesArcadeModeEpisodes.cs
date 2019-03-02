using System;
using System.Collections.Generic;
using Marbles.Core.Helpers;
using Marbles.Core.Model;
using Marbles.Core.Model.Levels;
using Marbles.Core.Model.Levels.LevelDefinitionComponents;
using Roboblob.XNA.WinRT.GameStateManagement;

namespace Marbles.Core
{
    public class MarblesArcadeModeEpisodes
    {
        public List<GameEpisode> Episodes = new List<GameEpisode>();

        public MarblesArcadeModeEpisodes()
        {
            var episode = new GameEpisode()
                              {
                                  Name = "Warm Up"
                              };

            Episodes.Add(episode);


            LevelDefinition level;


            level = new LevelDefinition()
            {
                CompletionScore = 1000,
                Name = "Start Up",
                ColumnsCount = GameConstants.ColumnCount,
                RowsCount = GameConstants.RowsCount,
                LevelType = LevelType.Arcade,
                InitialDuration = TimeSpan.FromMinutes(1),
                ElectricMarblesEnabled = false,
                MaxSpecialMarblesAtTheSameTimeOnBoard = 1,
                AvailableColors = new List<MarbleColor>()
                                                       {
                                                           MarbleColor.Red,
                                                           MarbleColor.Yellow,
                                                           MarbleColor.Green,
                                                           MarbleColor.Purple,
                                                           MarbleColor.Orange
                                                       },
            };

/*            var postInitial = new PostInitialRandomizationSpecialMarbleSettingsComponent();
            level.AddComponent(postInitial);
            level.AddComponent(new PostInitialRandomizationDistanceBetweenSpecialItems() { MinDistance = 1, MaxDistance = 5 });

            postInitial.Settings.Add(new GameOverSpecialMarbleRandomizationSettingsComponent()
            {
                MaximumCountToBeAddedDuringLevel = 1,
                ProbabilityPercentage = 100,
                CanBeAddedWhenThereAreAlreadySpecialMarblesOnBoard = true,
                TimeUntilEndInSeconds = 11
            });*/

            episode.Levels.Add(level);

            level = new LevelDefinition()
            {
                CompletionScore = 1500,
                Name = "Burn Them!",
                ColumnsCount = GameConstants.ColumnCount,
                ElectricMarblesEnabled = true,
                RowsCount = GameConstants.RowsCount,
                LevelType = LevelType.Arcade,
                InitialDuration = TimeSpan.FromMinutes(1),
                MaxSpecialMarblesAtTheSameTimeOnBoard = 1,
                AvailableColors = new List<MarbleColor>()
                                                       {
                                                           MarbleColor.Purple,
                                                           MarbleColor.Yellow,
                                                           MarbleColor.Green
                                                       },
            };
            episode.Levels.Add(level);


            level = new LevelDefinition()
            {
                CompletionScore = 2000,
                Name = "Bonus Time",
                ColumnsCount = GameConstants.ColumnCount,
                RowsCount = GameConstants.RowsCount,
                ElectricMarblesEnabled = true,
                LevelType = LevelType.Arcade,
                InitialDuration = TimeSpan.FromMinutes(1),
                MaxSpecialMarblesAtTheSameTimeOnBoard = 1,
                AvailableColors = new List<MarbleColor>()
                                                       {
                                                           MarbleColor.Green,
                                                           MarbleColor.Red,
                                                           MarbleColor.Yellow
                                                       },
            };

            var initial = new InitialRandomizationSpecialMarbleSettingsComponent();
            level.AddComponent(initial);
            level.AddComponent(new InitialRandomizationDistanceBetweenSpecialItems() {MinDistance = 0, MaxDistance = level.RowsCount * level.ColumnsCount});

            initial.Settings.Add(new TimeIncreaseSpecialMarbleRandomizationSettingsComponent()
            {
                MaximumCountToBeAddedDuringLevel = 1,
                ProbabilityPercentage = 100,
                CanBeAddedWhenThereAreAlreadySpecialMarblesOnBoard = true,
            });

            episode.Levels.Add(level);

            level = new LevelDefinition()
            {
                CompletionScore = 2100,
                Name = "Top Down",
                ColumnsCount = GameConstants.ColumnCount,
                RowsCount = GameConstants.RowsCount,
                ElectricMarblesEnabled = true,               
                LevelType = LevelType.Arcade,
                InitialDuration = TimeSpan.FromMinutes(1),
                MaxSpecialMarblesAtTheSameTimeOnBoard = 2,
                AvailableColors = new List<MarbleColor>()
                                                       {
                                                           MarbleColor.Purple,
                                                           MarbleColor.Yellow,
                                                           MarbleColor.Green                                                          
                                                       },
            };

            initial = new InitialRandomizationSpecialMarbleSettingsComponent();
            level.AddComponent(initial);
            level.AddComponent(new InitialRandomizationDistanceBetweenSpecialItems() { MinDistance = 0, MaxDistance = level.RowsCount /2 * level.ColumnsCount });

            initial.Settings.Add(new TimeIncreaseSpecialMarbleRandomizationSettingsComponent()
            {
                MaximumCountToBeAddedDuringLevel = 1,
                ProbabilityPercentage = 50,
                CanBeAddedWhenThereAreAlreadySpecialMarblesOnBoard = true,
            });

            initial.Settings.Add(new VerticalLineClearerSpecialMarbleRandomizationSettingsComponent()
            {
                MaximumCountToBeAddedDuringLevel = 1,
                ProbabilityPercentage = 50,
                CanBeAddedWhenThereAreAlreadySpecialMarblesOnBoard = true,
            });


            episode.Levels.Add(level);

            level = new LevelDefinition()
            {
                CompletionScore = 2200,
                Name = "Left Right",
                ColumnsCount = GameConstants.ColumnCount,
                RowsCount = GameConstants.RowsCount,
                ElectricMarblesEnabled = true,
                LevelType = LevelType.Arcade,
                InitialDuration = TimeSpan.FromMinutes(1),
                MaxSpecialMarblesAtTheSameTimeOnBoard = 2,
                AvailableColors = new List<MarbleColor>()
                                                       {
                                                           MarbleColor.Green,
                                                           MarbleColor.Red,
                                                           MarbleColor.Purple,                                                           
                                                       },
            };

            initial = new InitialRandomizationSpecialMarbleSettingsComponent();
            level.AddComponent(initial);
            level.AddComponent(new InitialRandomizationDistanceBetweenSpecialItems() { MinDistance = 0, MaxDistance = level.RowsCount / 2 * level.ColumnsCount });

            initial.Settings.Add(new TimeIncreaseSpecialMarbleRandomizationSettingsComponent()
            {
                MaximumCountToBeAddedDuringLevel = 1,
                ProbabilityPercentage = 50,
                CanBeAddedWhenThereAreAlreadySpecialMarblesOnBoard = true,
            });

            initial.Settings.Add(new HorizontalLineClearerSpecialMarbleRandomizationSettingsComponent()
            {
                MaximumCountToBeAddedDuringLevel = 1,
                ProbabilityPercentage = 50,
                CanBeAddedWhenThereAreAlreadySpecialMarblesOnBoard = true,
            });

            episode.Levels.Add(level);

            level = new LevelDefinition()
            {
                CompletionScore = 2400,
                Name = "Diagonals",
                ColumnsCount = GameConstants.ColumnCount,
                RowsCount = GameConstants.RowsCount,
                ElectricMarblesEnabled = true,
                LevelType = LevelType.Arcade,
                InitialDuration = TimeSpan.FromMinutes(1),
                MaxSpecialMarblesAtTheSameTimeOnBoard = 2,
                AvailableColors = new List<MarbleColor>()
                                                       {
                                                           MarbleColor.Red,
                                                           MarbleColor.Yellow,
                                                           MarbleColor.Green                                                         
                                                       },
            };

            initial = new InitialRandomizationSpecialMarbleSettingsComponent();
            level.AddComponent(initial);
            level.AddComponent(new InitialRandomizationDistanceBetweenSpecialItems() { MinDistance = 0, MaxDistance = level.RowsCount / 2 * level.ColumnsCount });

            initial.Settings.Add(new TimeIncreaseSpecialMarbleRandomizationSettingsComponent()
            {
                MaximumCountToBeAddedDuringLevel = 1,
                ProbabilityPercentage = 50,
                CanBeAddedWhenThereAreAlreadySpecialMarblesOnBoard = true,
            });

            initial.Settings.Add(new HorizontalAndVerticalLineClearerSpecialMarbleRandomizationSettingsComponent()
            {
                MaximumCountToBeAddedDuringLevel = 1,
                ProbabilityPercentage = 50,
                CanBeAddedWhenThereAreAlreadySpecialMarblesOnBoard = true,
            });


            episode.Levels.Add(level);

            level = new LevelDefinition()
            {
                CompletionScore = 2600,
                Name = "Color Matters",
                ColumnsCount = GameConstants.ColumnCount,
                RowsCount = GameConstants.RowsCount,
                ElectricMarblesEnabled = true,
                LevelType = LevelType.Arcade,
                InitialDuration = TimeSpan.FromMinutes(1),
                MaxSpecialMarblesAtTheSameTimeOnBoard = 2,
                AvailableColors = new List<MarbleColor>()
                                                       {
                                                           MarbleColor.Green,
                                                           MarbleColor.Red,
                                                           MarbleColor.Purple,                                                    
                                                       },
            };

            initial = new InitialRandomizationSpecialMarbleSettingsComponent();
            level.AddComponent(initial);
            level.AddComponent(new InitialRandomizationDistanceBetweenSpecialItems() { MinDistance = 0, MaxDistance = level.RowsCount / 2 * level.ColumnsCount });

            initial.Settings.Add(new TimeIncreaseSpecialMarbleRandomizationSettingsComponent()
            {
                MaximumCountToBeAddedDuringLevel = 1,
                ProbabilityPercentage = 50,
                CanBeAddedWhenThereAreAlreadySpecialMarblesOnBoard = true,
            });

            initial.Settings.Add(new ColorBombSpecialMarbleRandomizationSettingsComponent()
            {
                MaximumCountToBeAddedDuringLevel = 1,
                ProbabilityPercentage = 50,
                CanBeAddedWhenThereAreAlreadySpecialMarblesOnBoard = true,
            });


            episode.Levels.Add(level);

            var id = 1;
            foreach (var currentLevel in episode.Levels)
                currentLevel.Index = id++;

        }

        public int CurrentEpisodeIndex = 0;

        public int CurrentLevelIndex = 0;

        public int FurthestEpisodeIndex = 0;

        public int FurthestLevelIndex = 0;

        public GameEpisode CurrentEpisode
        {
            get
            {
                if (CurrentEpisodeIndex < 0 || CurrentEpisodeIndex > Episodes.Count - 1)
                {
                    return null;
                }

                return Episodes[CurrentEpisodeIndex];
            }
        }

        public LevelDefinition CurrentLevel
        {
            get
            {
                if (CurrentEpisodeIndex < 0 || CurrentEpisodeIndex > Episodes.Count-1)
                {
                    CurrentEpisodeIndex = 0;
                }

                if (CurrentLevelIndex < 0 || CurrentLevelIndex > Episodes[CurrentEpisodeIndex].Levels.Count - 1)
                {
                    CurrentLevelIndex = 0;
                }

                return Episodes[CurrentEpisodeIndex].Levels[CurrentLevelIndex];
            }
        }

        public bool ThereAreMoreLevels
        {
            get
            {
                return CurrentLevelIndex < Episodes[CurrentEpisodeIndex].Levels.Count - 1
                       ||
                       CurrentEpisodeIndex < Episodes.Count - 1;
            }
        }

        public bool ThereArePreviousLevels
        {
            get
            {
                return CurrentLevelIndex > 0
                       ||
                       CurrentEpisodeIndex > 0;
            }
        }

        public void GoToPreviousLevel()
        {
            if (!ThereArePreviousLevels)
            {
                return;
            }

            if (CurrentLevelIndex > 0)
            {
                CurrentLevelIndex--;
            }
            else
            {
                CurrentEpisodeIndex--;
                CurrentLevelIndex = Episodes[CurrentEpisodeIndex].Levels.Count - 1;
            }
        }

        public void GoToNextLevel()
        {
            if (!ThereAreMoreLevels)
            {
                return;
            }

            if (CurrentLevelIndex < CurrentEpisode.Levels.Count - 1)
            {
                CurrentLevelIndex++;
            }
            else
            {
                CurrentEpisodeIndex++;
                CurrentLevelIndex = 0;
            }
        }
    }
}