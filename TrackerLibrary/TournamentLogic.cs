using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TrackerLibrary.Models;

namespace TrackerLibrary
{
    public static class TournamentLogic
    {
        public static void CreateRounds(Tournament model)
        {
            List<Team> randomizedTeams = RandomizeTeamOrder(model.EnteredTeams);
            int rounds = FindNumberOfRounds(randomizedTeams.Count);
            int byes = NumberOfByes(rounds, randomizedTeams.Count);

            model.Rounds.Add(CreateFirstRound(randomizedTeams, byes));
            CreateOtherRounds(model, rounds);

        }

        public static void UpdateTournamentResults(Tournament model)
        {
            int startingRound = model.CheckCurrentRound();
            List<Matchup> matchupsToScore = new List<Matchup>();

            foreach(List<Matchup> round in model.Rounds)
            {
                foreach(Matchup match in round)
                {
                    if(match.Winner == null && (match.Entries.Any(x => x.Score != 0) || match.Entries.Count == 1))
                    {
                        matchupsToScore.Add(match);
                    }
                }
            }

            MarkWinnersInMatchups(matchupsToScore);

            AdvanceWinners(matchupsToScore, model);

            matchupsToScore.ForEach(m => GlobalConfig.Connection.UpdateMatchup(m));

            int endingRound = model.CheckCurrentRound();

            if(endingRound > startingRound)
            {
                model.AlertUsersToNewRound();
            }

        }

        public static void AlertUsersToNewRound(this Tournament model)
        {
            int currentRoundIndex = model.CheckCurrentRound();
            List<Matchup> currentRound = model.Rounds.Where(r => r.First().MatchupRound == currentRoundIndex).First();

            foreach (Matchup match in currentRound)
            {
                foreach(MatchupEntry me in match.Entries)
                {
                    foreach(Person p in me.TeamCompeting.TeamMembers)
                    {
                        AlertPersonToNewRound(p, me.TeamCompeting.TeamName,
                            match.Entries.Where(x => x.TeamCompeting != me.TeamCompeting).FirstOrDefault());
                    }
                }
            }
        }

        private static void AlertPersonToNewRound(Person p, string teamName, MatchupEntry matchupEntry)
        {
            if (p.EmailAddress.Length == 0)
            {
                return;
            }

            string to = "";
            string subject = "";
            StringBuilder body = new StringBuilder();

            if(matchupEntry != null)
            {
                subject = $"You have matchup with { matchupEntry.TeamCompeting.TeamName }.";
                body.AppendLine("<h1>You have new matchup!</h1>");
                body.AppendLine("<strong>Competitor:");
                body.AppendLine(matchupEntry.TeamCompeting.TeamName);
                body.AppendLine();
                body.AppendLine();
                body.AppendLine("See you soon!");
                body.AppendLine("~Tournament Tracker");
            }
            else
            {
                subject = $"You have buy in this round.";
                body.AppendLine("<h1>You have a buy round this time!</h1>");
                body.AppendLine();
                body.AppendLine();
                body.AppendLine("See you in next round!");
                body.AppendLine("~Tournament Tracker");
            }
            to = p.EmailAddress;


            EmailLogic.SendEmail(to, subject, body.ToString());
        }

        private static int CheckCurrentRound(this Tournament model)
        {
            int output = 1;
            foreach(List<Matchup> round in model.Rounds)
            {
                if (round.All(r => r.Winner != null))
                {
                    output++;
                }
                else
                    return output;
            }
            CompleteTournament(model);
            return output-1;
        }

        private static void CompleteTournament(Tournament model)
        {
            GlobalConfig.Connection.CompleteTournament(model);
            Team winners = model.Rounds.Last().First().Winner;
            Team runnerUp = model.Rounds.Last().First().Entries.Where(t => t.TeamCompeting != winners).First().TeamCompeting;
            List<Team> teamsSortedByPlace = GetTeamsByPlace(model);
            decimal winnerPrize = 0;
            decimal runnerUpPrize = 0;

            if(model.Prizes.Count > 0)
            {

                Prize firstPlacePrize = model.Prizes.Where(p => p.PlaceNumber == 1).FirstOrDefault();
                Prize secondPlacePrize = model.Prizes.Where(p => p.PlaceNumber == 2).FirstOrDefault();

                if (firstPlacePrize != null)
                {
                    winnerPrize = firstPlacePrize.CalculatePrizePayout(model.TotalIncome);
                }


                if(secondPlacePrize != null)
                {
                    runnerUpPrize = secondPlacePrize.CalculatePrizePayout(model.TotalIncome);
                }
            }

            string subject = "";
            StringBuilder body = new StringBuilder();


            subject = $"In {model.TournamentName} the winner is {winners.TeamName}!";
            body.AppendLine("<h1>We have a winner!</h1>");
            body.AppendLine("Congratulations to tournament champion!<br>");
            body.AppendLine("Prizes were assigned as followed:<br>");
            body.AppendLine(GeneratePrizesTable(model));
            body.AppendLine(GeneratePlacesTable(teamsSortedByPlace));
            body.AppendLine($"Thanks to all {model.TournamentName} competitors.");
            body.AppendLine("~Tournament Tracker");

            List<string> bcc = new List<string>();
            foreach(Team t in model.EnteredTeams)
            {
                foreach(Person p in t.TeamMembers)
                {
                    if(p.EmailAddress.Length > 0)
                    {
                        bcc.Add(p.EmailAddress);
                    }
                }
            }

            EmailLogic.SendEmail(new List<string>(), bcc, subject, body.ToString());

            model.CompleteTournament();
        }


        private static string GeneratePrizesTable(Tournament model)
        {
            string output = "";
            List<Prize> prizesListOrdered = model.Prizes.OrderBy(p => p.PlaceNumber).ToList();
            StringBuilder table = new StringBuilder();

            foreach(Prize p in prizesListOrdered)
            {
                table.AppendLine($"{p.PlaceName} | {p.CalculatePrizePayout(model.TotalIncome)}");
            }
            output = table.ToString();

            return output;
        }

        private static string GeneratePlacesTable(List<Team> teamsSortedByPlace)
        {
            string output = "";
            StringBuilder table = new StringBuilder();

            table.AppendLine($"<ol>");
            for(int i = 0; i < teamsSortedByPlace.Count; i++)
            {
                table.AppendLine($"<li>{teamsSortedByPlace[i].TeamName}</li>");
            }
            table.AppendLine($"</ol>");
            output = table.ToString();

            return output;
        }

        private static List<Team> GetTeamsByPlace(Tournament model)
        {
            List<Team> output = new List<Team>();
            Dictionary<Team, int> teamsBonusPointsDict = new Dictionary<Team, int>();

            foreach(Team t in model.EnteredTeams)
            {
                teamsBonusPointsDict.Add(t, 0);
            }

            Matchup finalMatch = model.Rounds.Last().First();
            output.Add(finalMatch.Winner);

            for(int roundIndex = model.Rounds.Count-1; roundIndex >= 0; roundIndex--)
            {
                int counter = model.Rounds.Count - roundIndex - 1;
                int bonusScore = (int)Math.Pow(2, counter);
                List<Team> teamsLost = new List<Team>();
                foreach (Matchup match in model.Rounds[roundIndex])
                {
                    Team matchLoser = match.Entries.Find(me => me.TeamCompeting != match.Winner)?.TeamCompeting;
                    if(matchLoser != null)
                    {
                        teamsLost.Add(matchLoser);
                    }
                    MatchupEntry winnerEntry = match.Entries.Find(m => m.TeamCompeting == match.Winner);
                    ScoreBonusPointsToPreviousMatchups(teamsBonusPointsDict, winnerEntry, bonusScore);
                }

                Dictionary<Team, int> teamsPointsInRound = new Dictionary<Team, int>();
                foreach (Team t in teamsLost)
                {
                    teamsPointsInRound.Add(t, teamsBonusPointsDict[t]);
                }
                teamsPointsInRound.OrderByDescending(t => t.Value);

                while (teamsPointsInRound.Count > 0)
                {
                    Team bestOfLostTeams = teamsPointsInRound.First().Key;
                    output.Add(bestOfLostTeams);
                    teamsPointsInRound.Remove(bestOfLostTeams);
                }
            }
            return output;
        }

        private static void ScoreBonusPointsToPreviousMatchups(Dictionary<Team, int> teamsBonusPoints, MatchupEntry entry, int score)
        {
            int teamScore = 0;
            bool success = teamsBonusPoints.TryGetValue(entry.TeamCompeting, out teamScore);
            if (success)
            {
                teamsBonusPoints[entry.TeamCompeting] += score;
            }

            if(entry.ParentMatchup != null)
            {
                foreach(MatchupEntry me in entry.ParentMatchup.Entries)
                {
                    ScoreBonusPointsToPreviousMatchups(teamsBonusPoints, me, score);
                }
            }
        }

        private static Dictionary<int,decimal> GetPrizesPayoutPerPlace(Tournament model)
        {
            Dictionary<int, decimal> output = new Dictionary<int, decimal>();


            return output;
        }

        private static decimal CalculatePrizePayout(this Prize prize, decimal totalIncome)
        {
            decimal output = 0;
            if(prize.PrizePercentage > 0)
            {
                output = Decimal.Multiply(totalIncome, Convert.ToDecimal(prize.PrizePercentage / 100));
            }
            else if(prize.PrizeAmount > 0){
                output = prize.PrizeAmount;
            }
            return output;
        }

        private static void AdvanceWinners(List<Matchup> matchups, Tournament tournament)
        {
            foreach (Matchup match in matchups)
            {
                foreach (List<Matchup> round in tournament.Rounds)
                {
                    foreach (Matchup m in round)
                    {
                        foreach (MatchupEntry me in m.Entries)
                        {
                            if (me.ParentMatchup != null)
                            {
                                if (me.ParentMatchup.Id == match.Id)
                                {
                                    me.TeamCompeting = match.Winner;
                                    GlobalConfig.Connection.UpdateMatchup(m);
                                }
                            }
                        }
                    }
                }
                GlobalConfig.Connection.UpdateMatchup(match);
            }
        }

        private static void MarkWinnersInMatchups(List<Matchup> matchups)
        {
            //greater or lesser
            string greaterWins = ConfigurationManager.AppSettings["greaterWins"];

            foreach (Matchup m in matchups)
            {
                if(m.Entries.Count == 1)
                {
                    m.Winner = m.Entries[0].TeamCompeting;
                    continue;
                }

                if (greaterWins == "0")
                {
                    if(m.Entries[0].Score < m.Entries[1].Score)
                    {
                        m.Winner = m.Entries[0].TeamCompeting;
                    }
                    else if(m.Entries[1].Score < m.Entries[0].Score)
                    {
                        m.Winner = m.Entries[1].TeamCompeting;
                    }
                    else
                    {
                        throw new Exception("We do not allow ties in this application");
                    }

                }
                else
                {
                    if (m.Entries[0].Score > m.Entries[1].Score)
                    {
                        m.Winner = m.Entries[0].TeamCompeting;
                    }
                    else if (m.Entries[1].Score > m.Entries[0].Score)
                    {
                        m.Winner = m.Entries[1].TeamCompeting;
                    }
                    else
                    {
                        throw new Exception("We do not allow ties in this application");
                    }
                } 
            }
        }

        private static void CreateOtherRounds(Tournament model, int totalRounds)
        {
            int currentRoundIndex = 2;
            List<Matchup> previousRound = model.Rounds[0];
            List<Matchup> currentRound = new List<Matchup>();
            Matchup currentMatchup = new Matchup();
            
            while(currentRoundIndex <= totalRounds)
            {
                foreach(Matchup match in previousRound)
                {
                    currentMatchup.Entries.Add(new MatchupEntry { ParentMatchup = match });
                    if(currentMatchup.Entries.Count > 1)
                    {
                        currentMatchup.MatchupRound = currentRoundIndex;
                        currentRound.Add(currentMatchup);
                        currentMatchup = new Matchup();
                    }
                }

                model.Rounds.Add(currentRound);
                previousRound = currentRound;
                currentRound = new List<Matchup>();
                currentRoundIndex++;
            }
        }

        private static List<Matchup> CreateFirstRound(List<Team> teams, int byes)
        {
            List<Matchup> output = new List<Matchup>();
            Matchup currentMatchup = new Matchup();

            foreach(Team t in teams)
            {
                currentMatchup.Entries.Add(new MatchupEntry { TeamCompeting = t });
                if(byes > 0 || currentMatchup.Entries.Count > 1)
                {
                    currentMatchup.MatchupRound = 1;
                    output.Add(currentMatchup);
                    currentMatchup = new Matchup();

                    if (byes > 0)
                        byes--;
                }
            }
            return output;
        }

        private static int NumberOfByes(int rounds, int numberOfTeams)
        {
            int output = 0;
            int totalTeams = 1;

            for(int i = 1; i <= rounds; i++)
            {
                totalTeams *= 2;
            }

            output = totalTeams - numberOfTeams;
            return output;
        }

        private static int FindNumberOfRounds(int teamCount)
        {
            int output = 1;
            int val = 2;

            while (val < teamCount)
            {
                output++;
                val *= 2;
            }

            return output;
        }

        private static List<Team> RandomizeTeamOrder(List<Team> teams)
        {
            return teams.OrderBy(x => Guid.NewGuid()).ToList();
        }
    }
}
