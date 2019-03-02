using System;
using Marbles.Core.Helpers;
using Marbles.Core.Model.Levels;
using Marbles.Core.Systems;
using Microsoft.Xna.Framework;
using Roboblob.GameEntitySystem.WinRT;
using System.Linq;

namespace Marbles.Core.Model
{
    public class MarblesWorld : World
    {
        private MarbleBoardTouchedSequencesReplacerSystem _boardTouchedSequencesReplacerSystem;
        private MarbleBoardRendererSystem _boardRendererSystemSys;
        private TouchSequencesSystem _touchSequencesSystem;
        private MarbleGameLevelControllerSystem _marbleGameLevelControllerSys;
        private MarbleSoundsSystem _marbleSoundSys;
        private Game _game;
        private LevelScoringSystem _scoringSys;
        private MarbleSpecialEffectsRenderingSystem _specialEffectsRenderingSys;
        private CurrentGameInformationTrackingSystem _currentGameInformationTrackingSys;
        private NewMarblesBounceInitializationSystem _newMarblesBounceInitializationSys;
        private MarbleVerticalBouncerSys _verticalMarbleVerticalBouncerSys;

        public MarblesWorld(Game game)
        {
            _game = game;
            _boardTouchedSequencesReplacerSystem = new MarbleBoardTouchedSequencesReplacerSystem();
            _boardRendererSystemSys = new MarbleBoardRendererSystem(_game);
            _touchSequencesSystem = new TouchSequencesSystem(_game);
            _marbleGameLevelControllerSys = new MarbleGameLevelControllerSystem();
            _marbleSoundSys = new MarbleSoundsSystem(_game);
            _scoringSys = new LevelScoringSystem();
            _specialEffectsRenderingSys = new MarbleSpecialEffectsRenderingSystem(_game);
            _currentGameInformationTrackingSys = new CurrentGameInformationTrackingSystem();
            _newMarblesBounceInitializationSys = new NewMarblesBounceInitializationSystem();
            _verticalMarbleVerticalBouncerSys = new MarbleVerticalBouncerSys();

            RegisterSystem(_currentGameInformationTrackingSys);
            RegisterSystem(_boardTouchedSequencesReplacerSystem);
            RegisterSystem(_boardRendererSystemSys);
            RegisterSystem(_touchSequencesSystem);
            RegisterSystem(_marbleGameLevelControllerSys);
            RegisterSystem(_marbleSoundSys);
            RegisterSystem(_scoringSys);
            RegisterSystem(_specialEffectsRenderingSys);
            RegisterSystem(_newMarblesBounceInitializationSys);
            RegisterSystem(_verticalMarbleVerticalBouncerSys);
            RegisterSystem(new BoardRandomizationSystem());
            RegisterSystem(new SpecialMarblesClearingAndAddingTrackerSystem());
            RegisterSystem(new SpecialMarblesClearingPostEffectsSystem());
            RegisterSystem(new ScoreChangesVisualizerSystem(_game));
            RegisterSystem(new GuiRendererSystem(_game));
            RegisterSystem(new SequenceVisualizationRenderingSystem(_game));
            RegisterSystem(new SpecialMarblesCullminationSystem(_game));
        }

        public override void Initialize()
        {
            base.Initialize();

            var systemsWithoutPriorities = AllSystems.Select(t => t.GetType()).Except(GamePriorities.Systems).ToList();
            if (systemsWithoutPriorities.Any())
            {
                var msg = systemsWithoutPriorities.Select(s => s.Name).Aggregate(string.Empty,
                                                                                            (p, n) =>
                                                                                            string.Format("{0}, {1}", p,
                                                                                                        n));
                throw new Exception("Systems missing priorities: " + msg);
            }

            var prioritiesWithoutSystems = GamePriorities.Systems.Except(AllSystems.Select(t => t.GetType())).ToList();
            if (prioritiesWithoutSystems.Any())
            {
                var msg = prioritiesWithoutSystems.Select(s => s.Name).Aggregate(string.Empty,
                                                                                            (p, n) =>
                                                                                            string.Format("{0}, {1}", p,
                                                                                                        n));
                throw new Exception("Systems that have priorities setup but not registered in world: " + msg);
            }


            if (AllSystems.Count() < GamePriorities.Systems.Count)
            {
                var duplicateSystems = GamePriorities.Systems.GroupBy(s => s).Where(s => s.Count() > 1).Select(s => s.Key).ToList();
                var msg = duplicateSystems.Select(s => s.Name).Aggregate(string.Empty,
                                                                                         (p, n) =>
                                                                                         string.Format("{0}, {1}", p, n));
                throw new Exception("Duplicate systems in priorities: " + msg);

            }

            EntityManager.InitializeEntityPool(100);
        }
    }
}