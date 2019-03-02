using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Roboblob.Core.WinRT.IO.Serialization;
using Roboblob.Core.WinRT.Threading;

using Windows.Storage;

namespace Marbles.Core.Helpers
{
    public class PlayersLocalDataMaintainer
    {
        private readonly ISerializer _serializer;
        private readonly UiThreadDispatcher _dispatcher;
        private PlayersData _data;
        private const string HighestScoresPerUserFileName = "playersData";

        public PlayersLocalDataMaintainer(ISerializer serializer, UiThreadDispatcher dispatcher)
        {
            _serializer = serializer;
            _dispatcher = dispatcher;
        }

        public async Task<bool> Initialize()
        {
            await ReadData();

            return true;
        }

        private async Task<bool> ReadData()
        {
            try
            {
                _data = await _serializer.Deserialize<PlayersData>(HighestScoresPerUserFileName,
                                                                   ApplicationData.Current.LocalFolder);
            }
            catch (Exception)
            {
                _data = new PlayersData();

                return WriteData().Result;
            }

            return true;
        }

        private async Task<bool> WriteData()
        {
            try
            {
                await _serializer.Serialize(_data, HighestScoresPerUserFileName, ApplicationData.Current.LocalFolder,
                                             CreationCollisionOption.ReplaceExisting);
                return true;

            }
            catch (Exception)
            {
                return false;
            }
        }

        public bool IsCurrentUsersNewHighScore(int score)
        {
            if (_data.HighestScoresForPlayers.ContainsKey(_data.CurrentPlayerName))
            {
                return _data.HighestScoresForPlayers[_data.CurrentPlayerName] < score;
            }

            return true;
        }

        public Task<bool> SetNewHighestScoreForCurrentPlayer(int score)
        {
            if (!_data.HighestScoresForPlayers.ContainsKey(_data.CurrentPlayerName))
            {
                _data.HighestScoresForPlayers.Add(_data.CurrentPlayerName, score);
            }

            _data.HighestScoresForPlayers[_data.CurrentPlayerName] = _data.HighestScoresForPlayers[_data.CurrentPlayerName] < score ? score : _data.HighestScoresForPlayers[_data.CurrentPlayerName];

            return WriteData();
        }

        public string CurrentPlayerName
        {
            get { return _data.CurrentPlayerName; }
        }

        public async Task<bool> SetCurrentPlayerName(string name)
        {
            _data.CurrentPlayerName = name;
            return await WriteData();
        }

        public class PlayersData
        {
            public PlayersData()
            {
                _currentPlayerName = "Player_" + new Random().Next(1, 9999);
            }

            public string CurrentPlayerName
            {
                get { return _currentPlayerName; }
                set
                {
                    _currentPlayerName = value;
                }
            }

            public Dictionary<string, int> HighestScoresForPlayers = new Dictionary<string, int>();
            private string _currentPlayerName;
        }

        public int GetCurrentPlayerHighScore()
        {
            if (!_data.HighestScoresForPlayers.ContainsKey(_data.CurrentPlayerName))
            {
                return 0;
            }

            return _data.HighestScoresForPlayers[_data.CurrentPlayerName];
        }
    }
}