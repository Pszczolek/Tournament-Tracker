using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TrackerLibrary.Models;
using TrackerLibrary.DataAccess.TextHelpers;

namespace TrackerLibrary.DataAccess
{
    public class TextConnector : IDataConnection
    {
        public void CompleteTournament(Tournament model)
        {
            List<Tournament> tournaments = GlobalConfig.TournamentsFile
                .FullFilePath()
                .LoadFile()
                .ConvertToTournaments();

            tournaments.Remove(model);

            tournaments.SaveToTournamentsFile();

            TournamentLogic.UpdateTournamentResults(model);
        }

        public void CreatePerson(Person model)
        {
            List<Person> people = GetPerson_All();

            int currentId = 1;
            if(people.Count > 0)
            {
                currentId = people.Max(p => p.Id) + 1;
            }
            model.Id = currentId;
            people.Add(model);

            people.SaveToPeopleFile();
        }

        public void CreatePrize(Prize model)
        {
            // Load file and convert to List<Prize>
            List<Prize> prizes = GlobalConfig.PrizesFile.FullFilePath().LoadFile().ConvertToPrizes();

            // Find new max Id
            int currentId = 1;
            if (prizes.Count > 0)
            {
                currentId = prizes.Max(p => p.Id) + 1;
            }
            model.Id = currentId;
            prizes.Add(model);

            prizes.SaveToPrizeFile();
        }

        public void CreateTeam(Team model)
        {
            List<Team> teams = GetTeam_All();

            int currentId = 1;
            if (teams.Count > 0)
            {
                currentId = teams.Max(p => p.Id) + 1;
            }
            model.Id = currentId;
            teams.Add(model);

            teams.SaveToTeamsFile();
        }

        public void CreateTournament(Tournament model)
        {
            List<Tournament> tournaments = GlobalConfig.TournamentsFile
                .FullFilePath()
                .LoadFile()
                .ConvertToTournaments();

            int currentId = 1;
            if(tournaments.Count > 0)
            {
                currentId = tournaments.Max(t => t.Id) + 1;
            }
            model.Id = currentId;

            model.SaveRoundsToFile();

            tournaments.Add(model);

            tournaments.SaveToTournamentsFile();

            TournamentLogic.UpdateTournamentResults(model);
        }

        public List<Person> GetPerson_All()
        {
            return GlobalConfig.PeopleFile.FullFilePath().LoadFile().ConvertToPeople();
        }

        public List<Team> GetTeam_All()
        {
            return GlobalConfig.TeamsFile.FullFilePath().LoadFile().ConvertToTeams();
        }

        public List<Tournament> GetTournament_All()
        {
            return GlobalConfig.TournamentsFile
                .FullFilePath()
                .LoadFile()
                .ConvertToTournaments();
        }

        public void UpdateMatchup(Matchup model)
        {
            model.UpdateMatchupToFile();
        }
    }
}
