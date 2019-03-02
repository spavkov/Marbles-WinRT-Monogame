using System;
using System.Collections.Generic;
using Marbles.Core.Model;
using Marbles.Core.Model.Components;
using Marbles.Core.Model.Levels;
using Marbles.Core.Model.Levels.LevelDefinitionComponents;
using Marbles.Core.Systems;
using System.Linq;

namespace Marbles.Core.Helpers
{
    public class SpecialItemDecider
    {
        private readonly LevelDefinition _levelDefinition;
        private readonly ISpecialItemsCountTracker _specialItemsCountTracker;

        private Random _rnd;
        private List<SpecialMarbleRandomizationSettingComponent> _initialSettings;
        private List<SpecialMarbleRandomizationSettingComponent> _postInitialSettings;
        private Dictionary<Guid, List<int>> _settingsNumbers = new Dictionary<Guid, List<int>>();
        private Dictionary<Guid,int> _addedCounts = new Dictionary<Guid, int>();
        private InitialRandomizationDistanceBetweenSpecialItems _initialRandomizationDistanceBetweenSpecialItems;
        private PostInitialRandomizationDistanceBetweenSpecialItems _postInitialRandomizationDistanceBetweenSpecialItems;
        private int _currentDistanceBetweenSpecialItemsForInititialRandomization = 0;
        private int _currentDistanceBetweenSpecialItemsForPostInititialRandomization = 0;

        private int _targetDistanceBetweenSpecialItemsForInititialRandomization = 0;
        private int _targetDistanceBetweenSpecialItemsForPostInititialRandomization = 0;


        public SpecialItemDecider(LevelDefinition levelDefinition, ISpecialItemsCountTracker specialItemsCountTracker)
        {
            _levelDefinition = levelDefinition;
            _specialItemsCountTracker = specialItemsCountTracker;
            _rnd = new Random();

            _initialSettings = _levelDefinition.HasComponent<InitialRandomizationSpecialMarbleSettingsComponent>()
                                   ? _levelDefinition.GetComponent<InitialRandomizationSpecialMarbleSettingsComponent>
                                         ().Settings.ToList()
                                   : new List<SpecialMarbleRandomizationSettingComponent>();


            _postInitialSettings = _levelDefinition.HasComponent<PostInitialRandomizationSpecialMarbleSettingsComponent>()
                                   ? _levelDefinition.GetComponent<PostInitialRandomizationSpecialMarbleSettingsComponent>
                                         ().Settings.ToList()
                                   : new List<SpecialMarbleRandomizationSettingComponent>();

            var currentNr = 1;

            var all = _initialSettings.Concat(_postInitialSettings);

            foreach (var specialMarbleRandomizationSettingComponent in all)
            {
                var numbers = new List<int>(specialMarbleRandomizationSettingComponent.ProbabilityPercentage);
                for (int i = 0; i < specialMarbleRandomizationSettingComponent.ProbabilityPercentage; i++ )
                {
                    numbers.Add(currentNr++);
                }
                _settingsNumbers.Add(specialMarbleRandomizationSettingComponent.Id, numbers);
            }

            _initialRandomizationDistanceBetweenSpecialItems =
                _levelDefinition.HasComponent<InitialRandomizationDistanceBetweenSpecialItems>()
                    ? _levelDefinition.GetComponent<InitialRandomizationDistanceBetweenSpecialItems>()
                    : null;

            if (_initialRandomizationDistanceBetweenSpecialItems != null)
            {
                _targetDistanceBetweenSpecialItemsForInititialRandomization =
                    _rnd.Next(_initialRandomizationDistanceBetweenSpecialItems.MinDistance,
                              _initialRandomizationDistanceBetweenSpecialItems.MaxDistance);
            }

            _postInitialRandomizationDistanceBetweenSpecialItems =
                _levelDefinition.HasComponent<PostInitialRandomizationDistanceBetweenSpecialItems>()
                    ? _levelDefinition.GetComponent<PostInitialRandomizationDistanceBetweenSpecialItems>()
                    : null;


            if (_postInitialRandomizationDistanceBetweenSpecialItems != null)
            {
                _targetDistanceBetweenSpecialItemsForPostInititialRandomization =
                    _rnd.Next(_postInitialRandomizationDistanceBetweenSpecialItems.MinDistance,
                              _postInitialRandomizationDistanceBetweenSpecialItems.MaxDistance);
            }
        }

        public void Reset()
        {
            _currentDistanceBetweenSpecialItemsForInititialRandomization = 0;
            _currentDistanceBetweenSpecialItemsForPostInititialRandomization = 0;
            _addedCounts.Clear();
        }

        public bool DoWeNeedToAddSpecialItem(bool isInitialRandomization, out SpecialMarbleRandomizationInstructions specialMarbleInstructions)
        {
            specialMarbleInstructions = null;

            if (_specialItemsCountTracker.CurrentNumberOfSpecialMarbles < _levelDefinition.MaxSpecialMarblesAtTheSameTimeOnBoard)
            {
                if (isInitialRandomization)
                {
                    _currentDistanceBetweenSpecialItemsForInititialRandomization++;
                }
                else
                {
                    _currentDistanceBetweenSpecialItemsForPostInititialRandomization++;
                }
            }

            if (_specialItemsCountTracker.CurrentNumberOfSpecialMarbles >= _levelDefinition.MaxSpecialMarblesAtTheSameTimeOnBoard)
            {
                return false;
            }

            if (isInitialRandomization)
            {
                if (_initialRandomizationDistanceBetweenSpecialItems != null)
                {
                    if (_currentDistanceBetweenSpecialItemsForInititialRandomization < _targetDistanceBetweenSpecialItemsForInititialRandomization)
                    {
                        return false;
                    }
                }
            }
            else
            {
                if (_postInitialRandomizationDistanceBetweenSpecialItems != null)
                {
                    if (_currentDistanceBetweenSpecialItemsForPostInititialRandomization < _targetDistanceBetweenSpecialItemsForPostInititialRandomization)
                    {
                        return false;
                    }
                }
            }

            var relevantSpecialMarbleSettings = isInitialRandomization ? _initialSettings : _postInitialSettings;

            if (_specialItemsCountTracker.CurrentNumberOfSpecialMarbles > 0)
            {
                relevantSpecialMarbleSettings =
                    relevantSpecialMarbleSettings.Where(s => s.CanBeAddedWhenThereAreAlreadySpecialMarblesOnBoard).
                        ToList();
            }

            var notExcluded = relevantSpecialMarbleSettings.Where(s =>
                                                                      {
                                                                          if (_addedCounts.ContainsKey(s.Id))
                                                                              return _addedCounts[s.Id] <
                                                                                     s.MaximumCountToBeAddedDuringLevel;

                                                                          return true;
                                                                      }).Select(s => s.Id);

            var relevantKeys = notExcluded.ToList();

            if (!relevantKeys.Any())
            {
                return false;
            }

            SpecialMarbleRandomizationSettingComponent component;
            if (relevantKeys.Count > 1)
            {
                var allNumbers =
                    _settingsNumbers.Where(n => relevantKeys.Contains(n.Key)).SelectMany(s => s.Value).ToList();
                var nextNr = _rnd.Next(allNumbers.Count);

                var componentId = _settingsNumbers.FirstOrDefault(s => s.Value.Contains(nextNr)).Key;
                component = relevantSpecialMarbleSettings.FirstOrDefault(i => i.Id == componentId);
            }
            else
            {
                component = relevantSpecialMarbleSettings.FirstOrDefault(s => s.Id == relevantKeys.FirstOrDefault());
            }

            if (component == null)
            {
                return false;
            }

            IncreaseAddedCount(component.Id);
            specialMarbleInstructions = new SpecialMarbleRandomizationInstructions()
            {
                SpecialMarbleType = component.SpecialMarbleType,
                RandomizationSettings = component
            };

            if (isInitialRandomization)
            {
                if (_initialRandomizationDistanceBetweenSpecialItems != null)
                {
                    _currentDistanceBetweenSpecialItemsForInititialRandomization = 0;
                    _targetDistanceBetweenSpecialItemsForInititialRandomization =
                        _rnd.Next(_initialRandomizationDistanceBetweenSpecialItems.MinDistance,
                                  _initialRandomizationDistanceBetweenSpecialItems.MaxDistance);
                }
            }
            else
            {
                if (_postInitialRandomizationDistanceBetweenSpecialItems != null)
                {
                    _currentDistanceBetweenSpecialItemsForPostInititialRandomization = 0;
                    _targetDistanceBetweenSpecialItemsForPostInititialRandomization =
                        _rnd.Next(_postInitialRandomizationDistanceBetweenSpecialItems.MinDistance,
                                  _postInitialRandomizationDistanceBetweenSpecialItems.MaxDistance);
                }
                
            }

            return true;

           /* foreach (var currentSetting in relevantSpecialMarbleSettings)
            {
                var numbers = currentSetting.ProbabilityPercentage;

                var type = currentSetting.GetType();

                var currentData = GetCurrentData(type);

                if (currentData.TotalNumberOfItemsAdded < currentSetting.MaximumCountToBeAddedDuringLevel)
                {
                    if (currentData.WeAreCurrentlyCountingUntilTheNextItem)
                    {
                        currentData.ItemsToCountUntilTheNextItem--;
                        if (currentData.ItemsToCountUntilTheNextItem <= 0)
                        {
                            currentData.TotalNumberOfItemsAdded++;

                            specialMarbleInstructions = new SpecialMarbleRandomizationInstructions()
                            {
                                SpecialMarbleType = currentSetting.SpecialMarbleType,
                                RandomizationSettings = currentSetting                               
                            };

                            return true;
                        }

                        continue;
                    }

                    currentData.ItemsToCountUntilTheNextItem =
                        _rnd.Next(currentSetting.MinNumberOfItemsToBeReplacedBeforeOurReplacementCanBeDone,
                                  currentSetting.MaxNumberOfReplacementsToPassBeforeReplacementCanBeDone);
                    currentData.WeAreCurrentlyCountingUntilTheNextItem = true;
                }          
            }

            return false;*/
        }

        private void IncreaseAddedCount(Guid id)
        {
            if (!_addedCounts.ContainsKey(id))
            {
                _addedCounts.Add(id, 0);
            }

            _addedCounts[id]++;

        }

    }

    public class SpecialMarbleRandomizationInstructions
    {
        public SpecialMarbleType SpecialMarbleType = SpecialMarbleType.None;

        public SpecialMarbleRandomizationSettingComponent RandomizationSettings;
    }
}