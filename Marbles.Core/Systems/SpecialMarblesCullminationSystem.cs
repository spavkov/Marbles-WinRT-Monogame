using Marbles.Core.Model.Components;
using Microsoft.Xna.Framework;
using Roboblob.GameEntitySystem.WinRT;
using System.Linq;

namespace Marbles.Core.Systems
{
    public class SpecialMarblesCullminationSystem : IWorldUpdatingSystem
    {
        private World _world;
        private Aspect _postEffectMarblesAspect;
        private BoardRandomizationSystem _randomizationSys;
        private Game _game;
        private LevelScoringSystem _levelScoringSys;
        private MarbleSoundsSystem _soundSys;

        public SpecialMarblesCullminationSystem(Game game)
        {
            _game = game;
        }

        public int Priority { get { return GamePriorities.Systems.IndexOf(GetType()); } }
        public void Initialize(World world)
        {
            _world = world;
            _postEffectMarblesAspect = new Aspect().HasAllOf(typeof(NewSpecialMarblePostEffectComponent));
        }

        public void Start()
        {
            _randomizationSys = _world.GetSystem<BoardRandomizationSystem>();
            _levelScoringSys = _world.GetSystem<LevelScoringSystem>();
            _soundSys = _world.GetSystem<MarbleSoundsSystem>();
        }

        public void Stop()
        {
            
        }

        public void Update(GameTime gameTime)
        {
            var electricmarbles = _world.EntityManager.GetLiveEntities(_postEffectMarblesAspect);
            if (!electricmarbles.Any())
            {
                return;
            }

            var count = electricmarbles.Count;
            for (int i = 0; i < count; i++)
            {
                var sourceMarble = electricmarbles[i];
                var theComponent = sourceMarble.GetComponent<NewSpecialMarblePostEffectComponent>();
                theComponent.RemainingDurationinSeconds -= gameTime.ElapsedGameTime.TotalSeconds;

                if (theComponent.RemainingDurationinSeconds <= 0)
                {
                    sourceMarble.RemoveComponent<NewSpecialMarblePostEffectComponent>();
                    switch (theComponent.EffectType)
                    {
                        case PostEffectType.Electric:
                            var cells =
                                theComponent.TargetEntities.Select(e => e.GetComponent<BoardCellChildEntityComponent>().Cell).ToList();
                            _levelScoringSys.NotifyScoringSystemThatUserClearedCellsAsSpecialMarbleClearingConsequence(cells,
                                sourceMarble.GetComponent<BoardCellChildEntityComponent>().Cell);
                            cells.Add(sourceMarble.GetComponent<BoardCellChildEntityComponent>().Cell);
                            _randomizationSys.AddCellsToRandomizationQueue(cells);
                            break;
                        case PostEffectType.Burn:
                            var cells2 =
                                theComponent.TargetEntities.Select(e => e.GetComponent<BoardCellChildEntityComponent>().Cell).ToList();

                            _levelScoringSys.NotifyScoringSystemThatUserClearedCellsAsSpecialMarbleClearingConsequence(cells2,
                                sourceMarble.GetComponent<BoardCellChildEntityComponent>().Cell);
                            cells2.Add(sourceMarble.GetComponent<BoardCellChildEntityComponent>().Cell);
                            _randomizationSys.AddCellsToRandomizationQueue(cells2);
                            _soundSys.StopPlayingBurnSound();
                            break;
                    }
                }
            }
        }
    }
}