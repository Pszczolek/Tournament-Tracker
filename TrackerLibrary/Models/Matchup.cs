using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrackerLibrary.Models
{
    public class Matchup
    {
        public int Id { get; set; }
        public List<MatchupEntry> Entries { get; set; } = new List<MatchupEntry>();
        public int WinnerId { get; set; }
        public Team Winner { get; set; }
        public int MatchupRound { get; set; }

        public string DisplayName {
            get {
                string output = "";
                foreach (MatchupEntry entry in Entries) {
                    if (entry.TeamCompeting != null)
                    {
                        if (output.Length == 0)
                        {
                            output = entry.TeamCompeting.TeamName;
                        }
                        else
                        {
                            output += $" vs. {entry.TeamCompeting.TeamName} ";
                        } 
                    }
                    else
                    {
                        if (output.Length == 0)
                        {
                            output = "<not yet determined>";
                        }
                        else
                        {
                            output += " vs. <not yet determined";
                        }
                    }
                }
                return output;
            }
        }
    }
}
