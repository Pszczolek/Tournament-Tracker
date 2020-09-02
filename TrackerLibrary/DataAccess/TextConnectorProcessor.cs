using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TrackerLibrary.Models;

namespace TrackerLibrary.DataAccess.TextHelpers
{
    public static class TextConnectorProcessor
    {
        public static string FullFilePath(this string fileName)
        {
            return $"{ConfigurationManager.AppSettings["filePath"]}\\{fileName}";
        }

        public static List<string> LoadFile(this string file)
        {
            if (!File.Exists(file))
            {
                return new List<string>();
            }
            return File.ReadAllLines(file).ToList();
        }

        public static List<Person> ConvertToPeople(this List<string> lines)
        {
            //Id,FirstName,LastName,EmailAddress,CellphoneNumber
            List<Person> output = new List<Person>();

            foreach(string line in lines)
            {
                string[] cols = line.Split(',');

                Person p = new Person();
                p.Id = int.Parse(cols[0]);
                p.FirstName = cols[1];
                p.LastName = cols[2];
                p.EmailAddress = cols[3];
                p.CellphoneNumber = cols[4];

                output.Add(p);
            }

            return output;
        }

        public static List<Tournament> ConvertToTournaments(this List<string> lines)
        {
            //Id,TournamentName,EntryFee,(EnteredTeams - Id|Id|Id),(Prizes - Id|Id|Id),(Rounds Id^Id^Id^Id|Id^Id|Id)
            List<Tournament> output = new List<Tournament>();
            List<Team> teams = GlobalConfig.TeamsFile.FullFilePath().LoadFile().ConvertToTeams();
            List<Prize> prizes = GlobalConfig.PrizesFile.FullFilePath().LoadFile().ConvertToPrizes();
            List<Matchup> matchups = GlobalConfig.MatchupsFile.FullFilePath().LoadFile().ConvertToMatchups();

            foreach (string line in lines)
            {
                string[] cols = line.Split(',');

                Tournament t = new Tournament();
                t.Id = int.Parse(cols[0]);
                t.TournamentName = cols[1];
                t.EntryFee = decimal.Parse(cols[2]);

                string[] teamIds = cols[3].Split('|');

                foreach(string id in teamIds)
                {
                    t.EnteredTeams.Add(teams.Find(x => x.Id == int.Parse(id)));
                }


                if (cols[4].Length > 0)
                {
                    string[] prizeIds = cols[4].Split('|');

                    foreach (string id in prizeIds)
                    {
                        t.Prizes.Add(prizes.Find(p => p.Id == int.Parse(id)));
                    } 
                }

                if (cols[5].Length > 0)
                {
                    string[] rounds = cols[5].Split('|');

                    foreach (string round in rounds)
                    {
                        List<Matchup> matchupList = new List<Matchup>();
                        string[] matchupString = round.Split('^');

                        foreach (string matchupTextId in matchupString)
                        {
                            matchupList.Add(matchups.Find(m => m.Id == int.Parse(matchupTextId)));
                        }

                        t.Rounds.Add(matchupList);
                    } 
                }

                output.Add(t);

                // TO DO - Capture rounds information

            }
            return output;
        }

        public static List<Team> ConvertToTeams(this List<string> lines)
        {
            List<Team> output = new List<Team>();
            List<Person> people = GlobalConfig.PeopleFile.FullFilePath().LoadFile().ConvertToPeople();

            foreach (string line in lines)
            {
                string[] cols = line.Split(',');

                Team t = new Team();
                t.Id = int.Parse(cols[0]);
                t.TeamName = cols[1];

                string[] personIds = cols[2].Split('|');
                foreach(string id in personIds)
                {
                    t.TeamMembers.Add(people.Find(p => p.Id == int.Parse(id)));
                }

                output.Add(t);
            }

            return output;
        }

        public static List<Prize> ConvertToPrizes(this List<string> lines)
        {
            List<Prize> output = new List<Prize>();

            foreach(string line in lines)
            {
                string[] cols = line.Split(',');

                Prize p = new Prize();
                p.Id = int.Parse(cols[0]);
                p.PlaceNumber = int.Parse(cols[1]);
                p.PlaceName = cols[2];
                p.PrizeAmount = decimal.Parse(cols[3]);
                p.PrizePercentage = double.Parse(cols[4]);

                output.Add(p);
            }

            return output;
        }

        public static void SaveToPeopleFile(this List<Person> models)
        {
            List<string> lines = new List<string>();

            foreach(Person p in models)
            {
                lines.Add($"{ p.Id },{ p.FirstName },{ p.LastName },{ p.EmailAddress },{ p.CellphoneNumber}");
            }
            File.WriteAllLines(GlobalConfig.PeopleFile.FullFilePath(), lines);

        }

        public static void SaveRoundsToFile(this Tournament model)
        {
            foreach(List<Matchup> round in model.Rounds)
            {
                foreach(Matchup match in round)
                {
                    match.SaveMatchupToFile();
                }
            }
        }

        public static void SaveMatchupToFile(this Matchup matchup)
        {

            List<Matchup> matchups = GlobalConfig.MatchupsFile.FullFilePath().LoadFile().ConvertToMatchups();
            int currentId = 1;
            if(matchups.Count > 0)
            {
                currentId = matchups.OrderByDescending(m => m.Id).First().Id + 1;
            }

            matchup.Id = currentId;
            matchups.Add(matchup);

            foreach (MatchupEntry entry in matchup.Entries)
            {
                entry.SaveMatchupEntryToFile();
            }

            List<string> lines = new List<string>();

            foreach (Matchup m in matchups)
            {
                string entriesString = ConvertMatchupEntriesListToString(m.Entries);
                string winner = "";
                if(m.Winner != null)
                {
                    winner = m.Winner.Id.ToString();
                }

                lines.Add($"{ m.Id },{ entriesString },{ winner },{ m.MatchupRound }");
            }

            File.WriteAllLines(GlobalConfig.MatchupsFile.FullFilePath(), lines);
        }

        public static void UpdateMatchupToFile(this Matchup matchup)
        {

            List<Matchup> matchups = GlobalConfig.MatchupsFile.FullFilePath().LoadFile().ConvertToMatchups();

            foreach(Matchup m in matchups)
            {
                if(m.Id == matchup.Id)
                {
                    matchups.Remove(m);
                    break;
                }
            }

            matchups.Add(matchup);

            matchups = matchups.OrderBy(m => m.Id).ToList();

            foreach (MatchupEntry entry in matchup.Entries)
            {
                entry.UpdateMatchupEntryToFile();
            }

            List<string> lines = new List<string>();

            foreach (Matchup m in matchups)
            {
                string entriesString = ConvertMatchupEntriesListToString(m.Entries);
                string winner = "";
                if (m.Winner != null)
                {
                    winner = m.Winner.Id.ToString();
                }

                lines.Add($"{ m.Id },{ entriesString },{ winner },{ m.MatchupRound }");
            }

            File.WriteAllLines(GlobalConfig.MatchupsFile.FullFilePath(), lines);
        }

        public static void UpdateMatchupEntryToFile(this MatchupEntry entry)
        {
            List<MatchupEntry> matchupEntries = GlobalConfig.MatchupEntriesFile.FullFilePath().LoadFile().ConvertToMatchupEntries();

            foreach(MatchupEntry me in matchupEntries)
            {
                if(me.Id == entry.Id)
                {
                    matchupEntries.Remove(me);
                    break;
                }
            }

            matchupEntries.Add(entry);

            matchupEntries = matchupEntries.OrderBy(e => e.Id).ToList();

            List<string> lines = new List<string>();

            foreach (MatchupEntry m in matchupEntries)
            {
                string parent = "";
                if (m.ParentMatchup != null)
                {
                    parent = m.ParentMatchup.Id.ToString();
                }
                string teamCompeting = "";
                if (m.TeamCompeting != null)
                {
                    teamCompeting = m.TeamCompeting.Id.ToString();
                }
                lines.Add($"{ m.Id },{ teamCompeting },{ m.Score },{ parent }");
            }

            File.WriteAllLines(GlobalConfig.MatchupEntriesFile.FullFilePath(), lines);

        }

        private static Team LookupTeamById(int id)
        {
            List<string> teams = GlobalConfig.TeamsFile.FullFilePath().LoadFile();
            List<string> matchingTeams = new List<string>();

            foreach(string team in teams)
            {
                string[] cols = team.Split(',');
                if(cols[0] == id.ToString())
                {
                    matchingTeams.Add(team);
                    return matchingTeams.ConvertToTeams().First();
                }
            }

            return null;
        }

        private static Matchup LookupMatchupById(int id)
        {
            List<string> matchups = GlobalConfig.MatchupsFile.FullFilePath().LoadFile();
            List<string> matchingMatchups = new List<string>();

            foreach (string matchup in matchups)
            {
                string[] cols = matchup.Split(',');
                if (cols[0] == id.ToString())
                {
                    matchingMatchups.Add(matchup);
                    return matchingMatchups.ConvertToMatchups().First();
                }
            }

            return null;
        }

        public static List<MatchupEntry> ConvertToMatchupEntries(this List<string> lines)
        {
            List<MatchupEntry> output = new List<MatchupEntry>();

            foreach (string line in lines)
            {
                string[] cols = line.Split(',');

                MatchupEntry m = new MatchupEntry();
                m.Id = int.Parse(cols[0]);
                if(cols[1].Length > 0)
                {
                    m.TeamCompeting = LookupTeamById(int.Parse(cols[1]));
                }
                else
                {
                    m.TeamCompeting = null;
                }

                int score;
                if(int.TryParse(cols[2], out score))
                {
                    m.Score = score;
                }

                int parentId;
                if (int.TryParse(cols[3], out parentId))
                {
                    m.ParentMatchup = LookupMatchupById(parentId);
                }
                else
                {
                    m.ParentMatchup = null;
                }

                output.Add(m);
            }

            return output;
        }

        private static List<MatchupEntry> ConvertStringToMatchupEntry(string input)
        {
            string[] ids = input.Split('|');
            List<MatchupEntry> output = new List<MatchupEntry>();
            List<string> entries = GlobalConfig.MatchupEntriesFile.FullFilePath().LoadFile();
            List<string> matchingEntries = new List<string>();

            foreach (string id in ids)
            {
                foreach(string entry in entries)
                {
                    string[] cols = entry.Split(',');

                    if(cols[0] == id)
                    {
                        matchingEntries.Add(entry);
                    }
                }
            }

            output = matchingEntries.ConvertToMatchupEntries();

            return output;
        }

        public static List<Matchup> ConvertToMatchups(this List<string> lines)
        {
            //Id,Entries(id|id),Winner,MatchupRound
            List<Matchup> output = new List<Matchup>();

            foreach (string line in lines)
            {
                string[] cols = line.Split(',');

                Matchup m = new Matchup();
                m.Id = int.Parse(cols[0]);
                m.Entries = ConvertStringToMatchupEntry(cols[1]);

                if(cols[2].Length > 0)
                {
                    m.Winner = LookupTeamById(int.Parse(cols[2]));
                }

                m.MatchupRound = int.Parse(cols[3]);

                output.Add(m);
            }

            return output;
        }

        public static void SaveMatchupEntryToFile(this MatchupEntry entry)
        {
            List<MatchupEntry> matchupEntries = GlobalConfig.MatchupEntriesFile.FullFilePath().LoadFile().ConvertToMatchupEntries();
            int currentId = 1;
            if (matchupEntries.Count > 0)
            {
                currentId = matchupEntries.OrderByDescending(m => m.Id).First().Id + 1;
            }

            entry.Id = currentId;
            matchupEntries.Add(entry);

            List<string> lines = new List<string>();

            foreach (MatchupEntry m in matchupEntries)
            {
                string parent = "";
                if(m.ParentMatchup != null)
                {
                    parent = m.ParentMatchup.Id.ToString();
                }
                string teamCompeting = "";
                if (m.TeamCompeting != null)
                {
                    teamCompeting = m.TeamCompeting.Id.ToString();
                }
                lines.Add($"{ m.Id },{ teamCompeting },{ m.Score },{ parent }");
            }

            File.WriteAllLines(GlobalConfig.MatchupEntriesFile.FullFilePath(), lines);

        }

        public static void SaveToTournamentsFile(this List<Tournament> models)
        {
            List<string> lines = new List<string>();

            foreach(Tournament t in models)
            {
                lines.Add($"{ t.Id },"+
                    $"{ t.TournamentName },"+
                    $"{ t.EntryFee },"+
                    $"{ ConvertTeamsToString(t.EnteredTeams) },"+
                    $"{ ConvertPrizesToString(t.Prizes) },"+
                    $"{ ConvertRoundsToString(t.Rounds)}");
            }

            File.WriteAllLines(GlobalConfig.TournamentsFile.FullFilePath(), lines);
        }



        public static void SaveToTeamsFile(this List<Team> models)
        {
            List<string> lines = new List<string>();

            foreach(Team t in models)
            {
                lines.Add($"{ t.Id },{ t.TeamName },{ ConvertPeopleToString(t.TeamMembers) }");
            }
            File.WriteAllLines(GlobalConfig.TeamsFile.FullFilePath(), lines);
        }

        private static string ConvertRoundsToString(List<List<Matchup>> rounds)
        {
            string output = "";
            if (rounds.Count == 0)
                return "";
            foreach (List<Matchup> r in rounds)
            {
                output += $"{ ConvertMatchupListToString(r) }|";
            }

            output = output.Substring(0, output.Length - 1);

            return output;
        }

        private static string ConvertMatchupListToString(List<Matchup> matchups)
        {
            string output = "";
            if (matchups.Count == 0)
                return "";
            foreach (Matchup m in matchups)
            {
                output += $"{ m.Id }^";
            }

            output = output.Substring(0, output.Length - 1);

            return output;
        }

        private static string ConvertTeamsToString(List<Team> teams)
        {
            string output = "";
            if (teams.Count == 0)
                return "";
            foreach(Team t in teams)
            {
                output += $"{ t.Id }|";
            }

            output = output.Substring(0, output.Length - 1);

            return output;
        }

        private static object ConvertPrizesToString(List<Prize> prizes)
        {
            string output = "";
            if (prizes.Count == 0)
                return "";
            foreach (Prize p in prizes)
            {
                output += $"{ p.Id }|";
            }

            output = output.Substring(0, output.Length - 1);

            return output;
        }

        private static string ConvertPeopleToString(List<Person> people)
        {
            string output = "";

            if (people.Count == 0)
                return "";
            foreach (Person p in people)
            {
                output += $"{ p.Id }|";
            }

            output = output.Substring(0, output.Length - 1);

            return output;
        }

        private static string ConvertMatchupEntriesListToString(List<MatchupEntry> entries)
        {
            string output = "";

            if (entries.Count == 0)
                return "";
            foreach (MatchupEntry m in entries)
            {
                output += $"{ m.Id }|";
            }

            output = output.Substring(0, output.Length - 1);

            return output;
        }

        public static void SaveToPrizeFile(this List<Prize> models)
        {
            List<string> lines = new List<string>();

            foreach(Prize p in models)
            {
                lines.Add($"{ p.Id },{ p.PlaceNumber },{ p.PlaceName },{ p.PrizeAmount },{ p.PrizePercentage }");
            }
            File.WriteAllLines(GlobalConfig.PrizesFile.FullFilePath(), lines);
        }
    }
}
