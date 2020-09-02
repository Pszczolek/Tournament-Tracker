using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrackerLibrary.Models
{
    public class Tournament
    {
        public event EventHandler<DateTime> OnTournamentComplete;

        public int Id { get; set; }
        public string TournamentName { get; set; }
        public decimal EntryFee { get; set; }
        public List<Team> EnteredTeams { get; set; } = new List<Team>();
        public List<Prize> Prizes { get; set; } = new List<Prize>();
        public List<List<Matchup>> Rounds { get; set; } = new List<List<Matchup>>();

        public decimal TotalIncome => EntryFee * EnteredTeams.Count;

        public void CompleteTournament()
        {
            OnTournamentComplete?.Invoke(this, DateTime.Now);
        }
    }
}
