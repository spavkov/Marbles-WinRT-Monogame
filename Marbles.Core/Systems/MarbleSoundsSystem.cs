using Marbles.Core.Repositories;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Roboblob.GameEntitySystem.WinRT;

namespace Marbles.Core.Systems
{
    public class MarbleSoundsSystem :IWorldSystem, IWorldContentSystem
    {
        private const float MiddleVolume = 0.4f;
        private const float FullVolume = 1f;

        private World _world;
        private ITouchSequenceSystem _touchSequenceSystem;
        private Game _game;
        private SoundEffect _collectSequenceGrownOkSound;
        private SoundEffectInstance _marbleCollectedCompletedSoundInstance;

        private SoundEffectInstance _collectSequenceGrownInstance;
        private readonly GameSettingsRepository _settingsRepository;

        private SoundEffectInstance _goSoundInstnce;
        private SoundEffectInstance _gameOverSoundInstance;
        private SoundEffectInstance _keyboardClickSoundInstance;
        private SoundEffectInstance _burnInstance;
        private int _totalBurners;
        private SoundEffectInstance _electricity1Instance;
        private SoundEffectInstance _levelCompletedInstance;
        private SoundEffectInstance _levelFailedInstance;
        private SoundEffectInstance _menuLoopInstance;
        private bool _menuLoopInstanceIsPlaying;
        private int _burnersCount;

        public MarbleSoundsSystem(Game game)
        {
            _game = game;
            _settingsRepository = _game.Services.GetService(typeof(GameSettingsRepository)) as GameSettingsRepository;
        }

        public int Priority { get { return GamePriorities.Systems.IndexOf(GetType()); } }
        public void Initialize(World world)
        {
            _world = world;
        }

        public void Start()
        {
            _touchSequenceSystem = _world.GetSystem<ITouchSequenceSystem>();
            if (_touchSequenceSystem != null)
            {
                _touchSequenceSystem.TouchSequenceGrown += OnNewMarbleTouchedInSequence;
                _touchSequenceSystem.TouchSequenceEnded += OnTouchSequenceEnded;
            }
        }

        public void StartPlayingBurnSound()
        {
            if (_burnersCount < 1)           
            {
                if (_settingsRepository.Settings.SoundsEffectsEnabled)
                {
                    _burnInstance.IsLooped = true;
                    _burnInstance.Volume = MiddleVolume;
                    _burnInstance.Play();
                }
            }

            _burnersCount++;
        }

        public void StopAllLoopingSounds()
        {
            if (_burnInstance != null)
                _burnInstance.Stop();

            if (_electricity1Instance != null)
                _electricity1Instance.Stop();
            
            if (_menuLoopInstance != null)            
                _menuLoopInstance.Stop();
        }

        public void TemporarilyMuteCurrentLoopingSounds()
        {
            if (_burnInstance != null)
                _burnInstance.Volume = 0;

            if (_menuLoopInstance != null)
                _menuLoopInstance.Volume = 0f;
        }

        public void UnmuteCurrentLoopingSounds()
        {
            if (_burnInstance != null)
            {
                if (_settingsRepository.Settings.SoundsEffectsEnabled && _burnersCount > 0)
                {
                    _burnInstance.Volume = MiddleVolume;
                    _burnInstance.IsLooped = true;
                    _burnInstance.Play();
                }
            }

            if (_menuLoopInstance != null)
                _menuLoopInstance.Volume = FullVolume;
        }

        public void StopPlayingBurnSound()
        {
            _burnersCount--;

            if (_burnersCount <= 0)
            {
                _burnInstance.IsLooped = false;
                _burnInstance.Stop();
                _burnersCount = 0;
            }
        }

        public void PlayStartGameSound()
        {
            if (!_settingsRepository.Settings.SoundsEffectsEnabled)
            {
                return;
            }

            _goSoundInstnce.Play();
        }

        public void PlayKeyboardClickSound()
        {
            if (!_settingsRepository.Settings.SoundsEffectsEnabled)
            {
                return;
            }

            _keyboardClickSoundInstance.Play();
        }

        private void OnTouchSequenceEnded(object sender, FingerTouchSequenceEndedArgs e)
        {
            if (!_settingsRepository.Settings.SoundsEffectsEnabled)
            {
                return;
            }

            if (e.NumberOfItemsInSequence > 1)
                _marbleCollectedCompletedSoundInstance.Play();
            else
            {
                //_marbleCollectedFailedSoundInstance.Play();
            }
        }

        private void OnNewMarbleTouchedInSequence(object sender, FingerTouchSequenceGrownArgs fingerTouchSequenceGrownArgs)
        {
            if (!_settingsRepository.Settings.SoundsEffectsEnabled)
            {
                return;
            }

            var normalizedCount = 1 + (fingerTouchSequenceGrownArgs.NumberOfItemsTouchedSoFar * 0.05f);
            //_collectSequenceGrownInstance.Pitch = normalizedCount;
            _collectSequenceGrownInstance.Play();
        }

        public void Stop()
        {
            if (_touchSequenceSystem != null)
                _touchSequenceSystem.TouchSequenceGrown += OnNewMarbleTouchedInSequence;

            _burnersCount = 0;
        }

        public void LoadContent()
        {
            _collectSequenceGrownOkSound = _game.Content.Load<SoundEffect>(@"Sounds\blip9");
            _collectSequenceGrownInstance = _collectSequenceGrownOkSound.CreateInstance();
            _collectSequenceGrownInstance.Pitch = 1;
            _collectSequenceGrownInstance.Volume = 0.8f;

            _marbleCollectedCompletedSoundInstance =
                _game.Content.Load<SoundEffect>(@"Sounds\blip2").CreateInstance();
            _marbleCollectedCompletedSoundInstance.Volume = 0.8f;

            _goSoundInstnce = _game.Content.Load<SoundEffect>(@"Sounds\Go").CreateInstance();
            _goSoundInstnce.Volume = 0.6f;

            _keyboardClickSoundInstance  = _game.Content.Load<SoundEffect>(@"Sounds\click").CreateInstance();
            _keyboardClickSoundInstance.Volume = MiddleVolume;

            _burnInstance = _game.Content.Load<SoundEffect>(@"Sounds\burn2").CreateInstance();
            _burnInstance.Volume = MiddleVolume;

            _electricity1Instance  = _game.Content.Load<SoundEffect>(@"Sounds\Electricity1").CreateInstance();
            _electricity1Instance.Volume = MiddleVolume;
        }

        public void PlayLevelCompletedSound()
        {
            if (!_settingsRepository.Settings.SoundsEffectsEnabled)
            {
                return;
            }

            if (_levelCompletedInstance == null)
            {
                _levelCompletedInstance = _game.Content.Load<SoundEffect>(@"Sounds\LevelCompleted").CreateInstance();
                _levelCompletedInstance.Volume = FullVolume;
            }

            _levelCompletedInstance.Play();
        }

        public void StopPlayingMenuLoop()
        {
            if (_menuLoopInstance != null)
            {
                _menuLoopInstanceIsPlaying = false;
                _menuLoopInstance.Stop();
            }
        }

        public void StartPlayingMenuLoop()
        {
            if (!_settingsRepository.Settings.SoundsEffectsEnabled)
            {
                return;
            }

            if (_menuLoopInstance == null)
            {
                _menuLoopInstance = _game.Content.Load<SoundEffect>(@"Sounds\MenuLoop").CreateInstance();
            }

            _menuLoopInstance.Volume = FullVolume;
            _menuLoopInstance.IsLooped = true;

            if (!_menuLoopInstanceIsPlaying)
            {
                _menuLoopInstanceIsPlaying = true;
                _menuLoopInstance.Play();
            }
        }

        public void PlayLevelFailedSound()
        {
            if (!_settingsRepository.Settings.SoundsEffectsEnabled)
            {
                return;
            }

            if (_levelFailedInstance == null)
            {
                _levelFailedInstance = _game.Content.Load<SoundEffect>(@"Sounds\LevelFailed").CreateInstance();
                _levelFailedInstance.Volume = FullVolume;
            }

            _levelFailedInstance.Play();
        }

        public void PlayElectricitySound()
        {
            if (!_settingsRepository.Settings.SoundsEffectsEnabled)
            {
                return;
            }

            _electricity1Instance.Play();
        }

        public void PlayGameOverSound()
        {
            if (!_settingsRepository.Settings.SoundsEffectsEnabled)
            {
                return;
            }

            _gameOverSoundInstance.Play();
        }
    }
}