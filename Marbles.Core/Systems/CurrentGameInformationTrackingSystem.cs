using Marbles.Core.Model.Levels;
using Roboblob.GameEntitySystem.WinRT;

namespace Marbles.Core.Systems
{
    public class CurrentGameInformationTrackingSystem : IWorldSystem
    {
        private LevelDefinition _levelDefinition;
        private double _currentMultiplier = 1;

        public int Priority { get { return GamePriorities.Systems.IndexOf(GetType()); } }

        public void Initialize(World world)
        {
            LevelState = LevelState.Uninitialized;
        }

        public void Start()
        {
           
        }

        public void Stop()
        {
           
        }

        public string CurrentPlayerName = string.Empty;
        public int CurrentPlayerHighScore = 0;

        public int CurrentLevelScore;

        public int TotalScore;

        public double CurrentMultiplier
        {
            get
            {
                return _currentMultiplier;
            }
            set
            {
                if (value < 1.0)
                    value = 1.0;

                _currentMultiplier = value;
            }
        }

        public double LevelRemainingTimeInSeconds;

        public LevelState LevelState
        {
            get { return _levelState; }
            set
            {
                _levelState = value;
            }
        }

        public bool IsLevelLoaded
        {
            get { return LevelDefinition != null && LevelState != LevelState.Uninitialized; }
        }

        public LevelDefinition LevelDefinition
        {
            get { return _levelDefinition; }
            set { _levelDefinition = value; }
        }

        public bool IsLevelRunning
        {
            get
            {
                if (_levelDefinition == null)
                {
                    return false;
                }

                return LevelState == LevelState.Running;
            }
        }

        public double TotalLevelPlayTimeInSeconds;
        private LevelState _levelState;
    }

    public enum LevelState
    {
        Uninitialized,
        Loaded,
        Running,
        Paused,
        Failed,
        Completed
    }
}