using System;
using System.Threading.Tasks;
using Roboblob.Core.WinRT.Threading;
using Roboblob.XNA.WinRT.Scoreoid;
using Windows.UI.Core;
using Windows.UI.Xaml;

namespace Marbles.Core.Helpers
{
    public class PlayersHighScoresSynchronizer
    {
        private readonly ScoreoidClient _scoreoidClient;
        private readonly PlayersLocalDataMaintainer _playersLocalDataMaintainer;
        private readonly UiThreadDispatcher _dispatcher;
        private const string HighestScoreKey = "highestscore";

        public PlayersHighScoresSynchronizer(ScoreoidClient scoreoidClient, PlayersLocalDataMaintainer playersLocalDataMaintainer, UiThreadDispatcher dispatcher)
        {
            _scoreoidClient = scoreoidClient;
            _playersLocalDataMaintainer = playersLocalDataMaintainer;
            _dispatcher = dispatcher;
            _scoreoidClient.GameId = GameConstants.ScoreoidGameId;
            _scoreoidClient.ApiKey = GameConstants.ScoreoidApiKey;
        }

        public async Task<bool> SetCurrentPlayerName(string name)
        {
            var res = await _playersLocalDataMaintainer.SetCurrentPlayerName(name);
            OnCurrentUserHighscoresDataChanged();
            return res;
        }

        public string CurrentPlayerName
        {
            get { return _playersLocalDataMaintainer.CurrentPlayerName; }
        }

        public async Task<bool> SynchronizeCurrentPlayerHighestScore()
        {
            var weNeedToNotify = false;
            var res = false;
            var thisPlayerIsCompletelyNew = false;

            try
            {
                var serverHighestScore = await _scoreoidClient.GetPlayerData(_playersLocalDataMaintainer.CurrentPlayerName, HighestScoreKey);

                int convertedScoreFromServer = 0;
                var thereIsHighestScoreOnServer = !string.IsNullOrWhiteSpace(serverHighestScore) && int.TryParse(serverHighestScore, out convertedScoreFromServer);
                var weNeedToUpdateHighestScoreOnServer = false;
                var weNeedToUpdateHighestScoreLocally = false;

                if (thereIsHighestScoreOnServer)
                {
                    if (convertedScoreFromServer < _playersLocalDataMaintainer.GetCurrentPlayerHighScore())
                    {
                        weNeedToUpdateHighestScoreOnServer = true;
                    }
                    else if (convertedScoreFromServer > 0 && convertedScoreFromServer > _playersLocalDataMaintainer.GetCurrentPlayerHighScore())
                    {
                        weNeedToUpdateHighestScoreLocally = true;
                    }
                }
                else
                {
                    weNeedToUpdateHighestScoreOnServer = true;
                }

                if (weNeedToUpdateHighestScoreOnServer)
                {
                    // we set the highest score and add the actual score
                    await OnServerCreateNewScoreForCurrentPlayerAndSetItsHighscoreData();
                }

                if (weNeedToUpdateHighestScoreLocally)
                {
                    await _playersLocalDataMaintainer.SetNewHighestScoreForCurrentPlayer(convertedScoreFromServer);
                }

                weNeedToNotify = weNeedToUpdateHighestScoreOnServer || weNeedToUpdateHighestScoreLocally;

                res = true;
            }
            catch (PlayerNotFoundScoreoidException)
            {
                thisPlayerIsCompletelyNew = true;
            }
            catch (Exception e)
            {
                res = false;
            }

            if (thisPlayerIsCompletelyNew)
            {
                var r2 = await OnServerCreateNewScoreForCurrentPlayerAndSetItsHighscoreData();
                res = r2;
                weNeedToNotify = r2;
            }

            if (weNeedToNotify)
            {
                OnCurrentUserHighscoresDataChanged();
            }

            return res;
        }

        private async Task<bool> OnServerCreateNewScoreForCurrentPlayerAndSetItsHighscoreData()
        {
            try
            {
                if (_playersLocalDataMaintainer.GetCurrentPlayerHighScore() > 0)
                {
                    await _scoreoidClient.CreateScoreAsync(_playersLocalDataMaintainer.CurrentPlayerName,
                                                         _playersLocalDataMaintainer.GetCurrentPlayerHighScore());
                }
                else
                {
                    await _scoreoidClient.CreatePlayerAsync(new player() { username = _playersLocalDataMaintainer.CurrentPlayerName });
                }

                await _scoreoidClient.SetPlayerData(_playersLocalDataMaintainer.CurrentPlayerName, HighestScoreKey,
                                                    _playersLocalDataMaintainer.GetCurrentPlayerHighScore().ToString());

                return true;
            }
            catch (Exception e)
            {
                return false;
            }
        }

        public int GetCurrentPlayerHighScore()
        {
            return _playersLocalDataMaintainer.GetCurrentPlayerHighScore();
        }

        private void OnCurrentUserHighscoresDataChanged()
        {
            _dispatcher.InvokeOnUiThread(() =>
                                                                       {
                                                                           var h = CurrentUserHighscoresDataChanged;
                                                                           if (h != null)
                                                                           {
                                                                               h(this, EventArgs.Empty);
                                                                           }  
                                                                       } );
         

        }

        public async Task<bool> Initialize()
        {
            try
            {
                await _playersLocalDataMaintainer.Initialize();
                OnCurrentUserHighscoresDataChanged();
                return await SynchronizeCurrentPlayerHighestScore();
            }
            catch (Exception e)
            {
                return false;
            }
        }

        public bool IsCurrentUsersNewHighScore(int currentScore)
        {
            return _playersLocalDataMaintainer.IsCurrentUsersNewHighScore(currentScore);
        }

        public async Task<bool> SetNewHighestScoreForCurrentPlayer(int highScore)
        {
            var localHighestScore = _playersLocalDataMaintainer.GetCurrentPlayerHighScore();

            if (localHighestScore > highScore)
            {
                return true;
            }

            await _playersLocalDataMaintainer.SetNewHighestScoreForCurrentPlayer(highScore);
            OnCurrentUserHighscoresDataChanged();

            return await SynchronizeCurrentPlayerHighestScore();
        }

        public event EventHandler CurrentUserHighscoresDataChanged;
    }
}