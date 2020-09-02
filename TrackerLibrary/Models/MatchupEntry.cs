using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrackerLibrary.Models
{
    public class MatchupEntry
    {
        public int Id { get; set; }

        public int TeamCompetingId { get; set; }

        /// <summary>
        /// Represents one team in the matchup.
        /// </summary>
        public Team TeamCompeting { get; set; }
        
        /// <summary>
        /// Repre the score for this particular team.
        /// </summary>
        public double Score { get; set; }

        public int ParentMatchupId { get; set; }

        /// <summary>
        /// Represents the matchup that this team 
        /// came from as a winner.
        /// </summary>
        public Matchup ParentMatchup { get; set; }

    }
}
