using System;
using Marbles.Core.Helpers;
using Marbles.Core.Model;
using Marbles.Core.Model.Components;
using Marbles.Core.Model.Levels;
using Microsoft.Xna.Framework;
using Roboblob.GameEntitySystem.WinRT;
using System.Linq;

namespace Marbles.Core.Systems
{
    public class MarbleGameLevelControllerSystem : IWorldUpdatingSystem
    {
        private MarbleBoard _board;
        private World _world;
        private CurrentGameInformationTrackingSystem _currentGameInforamtionSystem;
        private Aspect _bombsAspect;
        private ITouchSequenceSystem _touchSequenceSystem;
        private Vector2 _position = new Vector2(359,95);
        private BoardRandomizationSystem _boardRandomizerSys;
        private SpecialMarblesClearingAndAddingTrackerSystem _specialMarblesTracker;
        private MarbleSoundsSystem _soundSys;
        private MarbleSpecialEffectsRenderingSystem _specialEffectsSys;


        private void OnLevelCompleted()
        {
            _soundSys.StopAllLoopingSounds();
            var h = LevelCompleted;
            if (h != null)
            {
                h(this, _currentGameInforamtionSystem.LevelState == LevelState.Completed ? LevelCompletionType.Success : LevelCompletionType.Failure);
            }
        }

        public event EventHandler<LevelCompletionType> LevelCompleted;
        public event EventHandler LevelLoaded;
        public event EventHandler LevelStopped;
        public event EventHandler GameLevelStateChanged;
        public event EventHandler CurrentPlayerDataChanged;

        public event EventHandler UserRequestedToGoToMainMenu;

        public void NotifyThatUserRequestedToGoToMainMenu()
        {
            var h = UserRequestedToGoToMainMenu;
            if (h != null)
            {
                h(this, EventArgs.Empty);
            }
        }

        private void OnCurrentPlayerDataChanged()
        {
            var h = CurrentPlayerDataChanged;
            if (h != null)
            {
                h(this, EventArgs.Empty);
            }
        }

        public int Priority { get { return GamePriorities.Systems.IndexOf(GetType()); } }

        public void Initialize(World world)
        {
            _world = world;
            _bombsAspect = new Aspect().HasOneOf(typeof (TimedBombComponent));
        }

        public void LoadLevel(LevelDefinition definition)
        {
            StopLevel();
            _currentGameInforamtionSystem.LevelDefinition = definition;
            InitializeForNewLevel();
            OnLevelLoaded();
        }

        public void DecreaseCurrentLevelTime(TimeSpan timeSpan)
        {
            if (_currentGameInforamtionSystem.IsLevelLoaded && _currentGameInforamtionSystem.LevelState == LevelState.Running)
            {
                _currentGameInforamtionSystem.LevelRemainingTimeInSeconds -= timeSpan.TotalSeconds;
                if (_currentGameInforamtionSystem.LevelRemainingTimeInSeconds < 0)
                {
                    _currentGameInforamtionSystem.LevelRemainingTimeInSeconds = 0;
                }
            }
        }


        private void InitializeForNewLevel()
        {
            _currentGameInforamtionSystem.LevelState = LevelState.Loaded;
            OnLevelStateChanged();
            _currentGameInforamtionSystem.LevelRemainingTimeInSeconds =
                _currentGameInforamtionSystem.LevelDefinition.InitialDuration.TotalSeconds;
            _currentGameInforamtionSystem.TotalLevelPlayTimeInSeconds = 0;
            _currentGameInforamtionSystem.CurrentLevelScore = 0;
            _currentGameInforamtionSystem.CurrentMultiplier = 1;
        }

        private void OnLevelStateChanged()
        {
            var h = GameLevelStateChanged;
            if (h != null)
            {
                GameLevelStateChanged(this, EventArgs.Empty);
            }
        }

        private void OnLevelLoaded()
        {
            var h = LevelLoaded;
            if (h != null)
            {
                h(this, EventArgs.Empty);
            }
        }

        private void SetupBoardCellScreenAttributes()
        {
            _currentPos = new Vector2(_position.X, _position.Y);

            if (_board == null)
            {
                return;
            }

            for (int row = 0; row < _board.RowsCount; row++)
            {
                for (int col = 0; col < _board.ColumnsCount; col++)
                {
                    _board.Cells[row, col].Rectangle = new Rectangle((int)_currentPos.X, (int)_currentPos.Y, GameConstants.MarbleCellSize,
                                                                     GameConstants.MarbleCellSize);
                    _board.Cells[row, col].Center = new Vector2((int)_currentPos.X + GameConstants.MarbleCellSize / 2,
                                                                (int)_currentPos.Y + GameConstants.MarbleCellSize / 2);

                    _currentPos = _currentPos + new Vector2(GameConstants.MarbleCellSize, 0);
                }
                _currentPos = new Vector2(_position.X, _currentPos.Y + GameConstants.MarbleCellSize);
            }
        }

        public LevelState LevelState
        {
            get
            {
                return _currentGameInforamtionSystem.LevelState;
            }
        }

        public MarbleBoard Board
        {
            get { return _board; }
        }

        public bool IsLevelLoaded
        {
            get { return _currentGameInforamtionSystem.IsLevelLoaded; }
        }

        public void Start()
        {
            _boardRandomizerSys = _world.GetSystem<BoardRandomizationSystem>();

            _currentGameInforamtionSystem = _world.GetSystem<CurrentGameInformationTrackingSystem>();

            _touchSequenceSystem = _world.GetSystem<ITouchSequenceSystem>();
            if (_touchSequenceSystem != null)
            {
                _touchSequenceSystem.TouchSequenceGrown += OnNewMarbleTouchedInSequence;
            }

            _specialMarblesTracker = _world.GetSystem<SpecialMarblesClearingAndAddingTrackerSystem>();

            _soundSys = _world.GetSystem<MarbleSoundsSystem>();

            _specialEffectsSys = _world.GetSystem<MarbleSpecialEffectsRenderingSystem>();
        }

        private void OnNewMarbleTouchedInSequence(object sender, FingerTouchSequenceGrownArgs e)
        {
            if (e.Entity != null)
            {

            }
        }

        public void PauseCurrentLevel()
        {
            if (_currentGameInforamtionSystem.LevelState == LevelState.Running)
            {
                _currentGameInforamtionSystem.LevelState = LevelState.Paused;
                _soundSys.TemporarilyMuteCurrentLoopingSounds();
                OnLevelStateChanged();
            }
        }

        public void ResumeCurrentLevel()
        {
            if (_currentGameInforamtionSystem.LevelState == LevelState.Paused)
            {
                _currentGameInforamtionSystem.LevelState = LevelState.Running;
                _soundSys.UnmuteCurrentLoopingSounds();
                OnLevelStateChanged();
            }
        }

        public void Stop()
        {
            if (_touchSequenceSystem != null)
            {
                _touchSequenceSystem.TouchSequenceGrown -= OnNewMarbleTouchedInSequence;
            }
        }

        public void StartLevel()
        {
            if (_currentGameInforamtionSystem.LevelDefinition == null)
            {
                throw new InvalidOperationException("Cannot start level because there is no level loaded.");
            }

            _board = new MarbleBoard(_currentGameInforamtionSystem.LevelDefinition.RowsCount, _currentGameInforamtionSystem.LevelDefinition.ColumnsCount);
            SetupBoardCellScreenAttributes();

            _boardRandomizerSys.PrepareForNewLevel(_board, _currentGameInforamtionSystem.LevelDefinition);

            _boardRandomizerSys.RandomizeBoard();

/*            _board.Cells[3, 3].Item.AddComponent(new ShouldGetNewSpecialMarbleComponent() {EffectType = PostEffectType.Electric});
            _board.Cells[0, 3].Item.AddComponent(new ShouldGetNewSpecialMarbleComponent() { EffectType = PostEffectType.Burn });*/

            InitializeForNewLevel();
                       
            _currentGameInforamtionSystem.LevelState = LevelState.Running;
            OnLevelStateChanged();
            _soundSys.PlayStartGameSound();

        }

        public void StopLevel()
        {
            OnLevelStopped();
            _world.RemoveAllEntities();
        }

        private void OnLevelStopped()
        {
            var h = LevelStopped;
            if (h != null)
            {
                h(this, EventArgs.Empty);
            }
        }

        public void Update(GameTime gameTime)
        {
            if (!_currentGameInforamtionSystem.IsLevelRunning)
                return;

            _currentGameInforamtionSystem.TotalLevelPlayTimeInSeconds += gameTime.ElapsedGameTime.TotalSeconds;

            _currentGameInforamtionSystem.CurrentMultiplier -= gameTime.ElapsedGameTime.TotalSeconds * _currentGameInforamtionSystem.LevelDefinition.MultiplierDecreasePerSecond;

            if (_currentGameInforamtionSystem.LevelDefinition.LevelType == LevelType.Survival)
            {
                _currentGameInforamtionSystem.LevelRemainingTimeInSeconds -= gameTime.ElapsedGameTime.TotalSeconds;
                if (_currentGameInforamtionSystem.CurrentLevelScore >= _currentGameInforamtionSystem.LevelDefinition.CompletionScore)
                {
                    _currentGameInforamtionSystem.LevelState = LevelState.Completed;
                    OnLevelStateChanged();
                }
                else if (_currentGameInforamtionSystem.LevelRemainingTimeInSeconds <= 0)
                {
                    MakeThisLevelFailed();
                }
            }           

            if (_currentGameInforamtionSystem.LevelState != LevelState.Running)
            {
                OnLevelCompleted();
                return;
            }
        }

        public void MakeThisLevelFailed()
        {
            _currentGameInforamtionSystem.LevelRemainingTimeInSeconds = 0;
            _currentGameInforamtionSystem.LevelState = LevelState.Failed;
            _soundSys.StopAllLoopingSounds();
            OnLevelStateChanged();
        }

        public Vector2 _currentPos { get; set; }

        public void RestartCurrentLevel()
        {
            StopLevel();
            _soundSys.UnmuteCurrentLoopingSounds();
            StartLevel();
        }

        public void SetCurrentPlayerAndHighscoreData(string currentPlayerName, int currentPlayerHighScore)
        {
            _currentGameInforamtionSystem.CurrentPlayerName = currentPlayerName;
            _currentGameInforamtionSystem.CurrentPlayerHighScore = currentPlayerHighScore;
            OnCurrentPlayerDataChanged();
        }

        public void IncreaseCurrentLevelTime(TimeSpan timeSpan)
        {
            if (_currentGameInforamtionSystem.IsLevelLoaded && _currentGameInforamtionSystem.LevelState == LevelState.Running)
            {
                _currentGameInforamtionSystem.LevelRemainingTimeInSeconds += timeSpan.TotalSeconds;
            }
           
        }
    }


}