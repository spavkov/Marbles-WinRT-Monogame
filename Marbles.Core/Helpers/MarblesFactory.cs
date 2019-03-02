using System;
using System.Linq;
using Marbles.Core.Model;
using Marbles.Core.Model.Components;
using Marbles.Core.Model.Components.SpecialMarbles;
using Marbles.Core.Model.Levels.LevelDefinitionComponents;
using Microsoft.Xna.Framework;
using Roboblob.GameEntitySystem.WinRT;
using Roboblob.XNA.WinRT.Performance;

namespace Marbles.Core.Helpers
{
    public class MarblesFactory
    {
        private const string _marbleForCellStringFormat = "Marble for {0}:{1}";
        private readonly World _world;
        private readonly Random _random;
        private SpecialMarbleType[] _allSpecialExceptRandomAndNone;
        private InstancePool<MarbleComponent> _marbleComponentPool;
        private InstancePool<TouchableComponent> _touchableComponentPool;
        private InstancePool<BoardCellChildEntityComponent> _cellChildComponentPool;
        private InstancePool<NeedsToBeBouncedDownComponent> _needsToBeBouncedDownPool;

        public MarblesFactory(World world)
        {
            _world = world;
            _random = new Random();
            _allSpecialExceptRandomAndNone = Enum.GetValues(typeof(SpecialMarbleType)).Cast<SpecialMarbleType>().Except(new [] {SpecialMarbleType.None, SpecialMarbleType.SurpriseMarble}).ToArray();           
        }

        public GameEntity CreateSimpleMarbleForCell(MarbleColor color, BoardCell<GameEntity> cell)
        {
            var marble = CreateColoredMarbleForCell(color, cell);
            return marble;
        }

        private GameEntity CreateColoredMarbleForCell(MarbleColor color, BoardCell<GameEntity> cell)
        {
            var marble = _world.CreateEntity();
            
            marble.Name = string.Format(_marbleForCellStringFormat, cell.Row, cell.Column);
            marble.AddComponent(new MarbleComponent() { Color = color});
            marble.AddComponent(new BoardCellChildEntityComponent(cell));
            marble.AddComponent(new NeedsToBeBouncedDownComponent());
            marble.AddComponent(new TouchableComponent());

            if (cell.Item != null)
            {
                _world.RemoveEntity(cell.Item);
            }

            cell.Item = marble;
            return marble;
        }

        public GameEntity GenerateSpecialMarbleForCell(BoardCell<GameEntity> cell, SpecialMarbleRandomizationInstructions instructions, MarbleColor color)
        {
            var marble = CreateColoredMarbleForCell(color, cell);
            if (instructions.SpecialMarbleType == SpecialMarbleType.TimeExtensionMarble)
            {
                var details = instructions.RandomizationSettings as TimeIncreaseSpecialMarbleRandomizationSettingsComponent;
                var timeExtenderComponent = new TimeExtenderSpecialMarbleDetails() { TimeToAdd = details.SecondsToAdd};
                marble.AddComponent(new SpecialMarbleComponent()
                                        {
                                            Details = timeExtenderComponent,
                                            SpecialMarbleType = instructions.SpecialMarbleType
                                        });
            }
            if (instructions.SpecialMarbleType == SpecialMarbleType.GameOverMarble)
            {
                var details = instructions.RandomizationSettings as GameOverSpecialMarbleRandomizationSettingsComponent;
                var component = new GameOverSpecialMarbleDetails() { RemainingTimeInSeconds  = details.TimeUntilEndInSeconds };
                marble.AddComponent(new SpecialMarbleComponent()
                {
                    Details = component,
                    SpecialMarbleType = instructions.SpecialMarbleType
                });
            }
            else if (instructions.SpecialMarbleType == SpecialMarbleType.ColorBombMarble)
            {
                var bombSpecialMarbleDetails = new ColorBombSpecialMarbleDetails() { MarbleColorToClear = color };
                marble.AddComponent(new SpecialMarbleComponent()
                {
                    Details = bombSpecialMarbleDetails,
                    SpecialMarbleType = SpecialMarbleType.ColorBombMarble
                });
            }
            else if (instructions.SpecialMarbleType == SpecialMarbleType.SurpriseMarble)
            {
                var details = new SurpriseSpecialMarbleDetails() { SurpriseType = _allSpecialExceptRandomAndNone[_random.Next(_allSpecialExceptRandomAndNone.Count())]};
                marble.AddComponent(new SpecialMarbleComponent()
                {
                    Details = details,
                    SpecialMarbleType = SpecialMarbleType.SurpriseMarble
                });
            }
            else if (instructions.SpecialMarbleType == SpecialMarbleType.LineClearerMarble)
            {
                var component = new SpecialMarbleComponent() {SpecialMarbleType = SpecialMarbleType.LineClearerMarble};
                var details = new LineClearerSpecialMarbleDetails();
                if (instructions.RandomizationSettings is VerticalLineClearerSpecialMarbleRandomizationSettingsComponent)
                {
                    details.ClearerType = LineClearerType.VerticalClearer;
                }
                else if (instructions.RandomizationSettings is HorizontalLineClearerSpecialMarbleRandomizationSettingsComponent)
                {
                    details.ClearerType = LineClearerType.HorizontalClearer;
                }
                else if (instructions.RandomizationSettings is HorizontalAndVerticalLineClearerSpecialMarbleRandomizationSettingsComponent)
                {
                    details.ClearerType = LineClearerType.HorizontalAndVerticalClearer;
                }
                component.Details = details;
                marble.AddComponent(component);
            }

            return marble;
        }

        public GameEntity CreateBombMarbleForCell(MarbleColor color, BoardCell<GameEntity> cell)
        {
            var marble = CreateColoredMarbleForCell(color, cell);
            var seconds = _random.Next(5,10);
            var bomb = new TimedBombComponent(seconds);
            marble.AddComponent(bomb);
            return marble;
        }
    }
}