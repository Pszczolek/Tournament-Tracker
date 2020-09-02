using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TrackerLibrary.Models;

namespace TrackerLibrary.DataAccess
{
    public interface IDataConnection
    {
        void CreatePrize(Prize model);
        void CreatePerson(Person model);
        void CreateTeam(Team model);
        void CreateTournament(Tournament model);

        void UpdateMatchup(Matchup model);

        void CompleteTournament(Tournament model);

        List<Team> GetTeam_All();
        List<Person> GetPerson_All();
        List<Tournament> GetTournament_All();
    }
}
