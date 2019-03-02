using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Roboblob.XNA.WinRT.Scoreoid;

namespace Marbles.Core.Helpers
{
    public class HighScoresRetriever
    {
        private readonly ScoreoidClient _client;
        private int MAXITEMS = 20;
        private DateTime _startOfWeek;
        private DateTime _endOfWeek;
        private DateTime _startOfMonth;
        private DateTime _endOfMonth;
        private List<ScoreoidScore> _todaysScores = new List<ScoreoidScore>();
        private List<ScoreoidScore> _weekScores = new List<ScoreoidScore>();
        private List<ScoreoidScore> _monthScores = new List<ScoreoidScore>();
        private List<ScoreoidScore> _allScores = new List<ScoreoidScore>();

        public HighScoresRetriever(ScoreoidClient client)
        {
            _client = client;
            InitPeriods();
        }

        private void InitPeriods()
        {
            DayOfWeek day = DateTime.Now.DayOfWeek;
            _startOfWeek = DateTime.Now;
            while (day != DayOfWeek.Monday)
            {
                _startOfWeek = _startOfWeek.AddDays(-1);
                day = _startOfWeek.DayOfWeek;
            }

            _endOfWeek = _startOfWeek.AddDays(6);

            _startOfMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            _endOfMonth = _startOfMonth.AddMonths(1).AddDays(-1);
        }

        public async Task<List<ScoreoidScore>> GetTodays()
        {
            try
            {
                _todaysScores = await _client.GetBestScoresAsync(ScoreoidOrderBy.Score, ScoreoidOrder.Desc, MAXITEMS , DateTime.Now, DateTime.Now);
                return _todaysScores;
            }
            catch (Exception e)
            {
                return _todaysScores;
            }
        }

        public async Task<List<ScoreoidScore>> GetThisWeek()
        {
            try
            {
                _weekScores = await _client.GetBestScoresAsync(ScoreoidOrderBy.Score, ScoreoidOrder.Desc, MAXITEMS, _startOfWeek, _endOfWeek);
                return _weekScores;
            }
            catch (Exception e)
            {
                return _weekScores;
            }
        }

        public async Task<List<ScoreoidScore>> GetThisMonth()
        {
            try
            {
                _monthScores = await _client.GetBestScoresAsync(ScoreoidOrderBy.Score, ScoreoidOrder.Desc, MAXITEMS, _startOfMonth, _endOfMonth);
                return _monthScores;
            }
            catch (Exception e)
            {
                return _monthScores;
            }
        }

        public async Task<List<ScoreoidScore>> GetAll()
        {
            try
            {
                _allScores = await _client.GetBestScoresAsync(ScoreoidOrderBy.Score, ScoreoidOrder.Desc, MAXITEMS, DateTime.MinValue, DateTime.Now.AddDays(1));
                return _allScores;
            }
            catch (Exception e)
            {
                return _allScores;
            }
        }
    }
}