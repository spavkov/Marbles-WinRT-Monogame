using System;
using System.Threading.Tasks;
using Marbles.Core.Model;
using Microsoft.Xna.Framework;
using Roboblob.Core.WinRT.IO.Serialization;
using Windows.Storage;

namespace Marbles.Core.Repositories
{
    public class GameSettingsRepository
    {
        private ISerializer _serializer;
        private MarblesGameSettings _settings;
        private const string _settingsFileName = "gameSettings.json";

        public GameSettingsRepository(Game game)
        {
            _serializer = game.Services.GetService(typeof (ISerializer)) as ISerializer;            
        }

        public event EventHandler Loaded;

        private void OnLoaded()
        {
            var h = Loaded;
            if (h != null)
            {
                h(this, EventArgs.Empty);
            }

        }
        public MarblesGameSettings Settings
        {
            get
            {
                return _settings;
            }
        }

        public async Task<MarblesGameSettings> Load()
        {
            if (_settings == null)
            {
                try
                {

                   _settings = await _serializer.Deserialize<MarblesGameSettings>(_settingsFileName,
                                                                             ApplicationData.Current.LocalFolder);
                   IsLoaded = true;
                }
                catch( Exception)
                {
                    _settings = new MarblesGameSettings();
                    IsLoaded = true;
                    return _settings;
                }           
            }

            OnLoaded();

            return _settings;
        }

        public bool IsLoaded { get; private set; }

        public async Task<bool> Save()
        {
            try
            {
                await _serializer.Serialize(_settings, _settingsFileName, ApplicationData.Current.LocalFolder, CreationCollisionOption.ReplaceExisting);
                return true;
            }
            catch (Exception e)
            {
                return false;
            }           
        }
    }
}