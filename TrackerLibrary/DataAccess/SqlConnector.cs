using Dapper;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TrackerLibrary.Models;

namespace TrackerLibrary.DataAccess
{
    public class SqlConnector : IDataConnection
    {
        private const string db = "Tournaments";
        public void CreatePerson(Person model)
        {
            using (IDbConnection connection = new System.Data.SqlClient.SqlConnection(GlobalConfig.CnnString(db)))
            {
                var p = new DynamicParameters();
                p.Add("@FirstName", model.FirstName);
                p.Add("@LastName", model.LastName);
                p.Add("@EmailAddress", model.EmailAddress);
                p.Add("@CellphoneNumber", model.CellphoneNumber);
                p.Add("@Id", 0, dbType: DbType.Int32, direction: ParameterDirection.Output);

                connection.Execute("dbo.spPeople_Insert", p, commandType: CommandType.StoredProcedure);

                model.Id = p.Get<int>("@Id");
            }
        }

        public void CreatePrize(Prize model)
        {
            using (IDbConnection connection = new System.Data.SqlClient.SqlConnection(GlobalConfig.CnnString(db)))
            {
                var p = new DynamicParameters();
                p.Add("@PlaceNumber", model.PlaceNumber);
                p.Add("@PlaceName", model.PlaceName);
                p.Add("@PrizeAmount", model.PrizeAmount);
                p.Add("@PrizePercentage", model.PrizePercentage);
                p.Add("@Id", 0, dbType: DbType.Int32, direction: ParameterDirection.Output);

                connection.Execute("dbo.spPrizes_Insert", p, commandType: CommandType.StoredProcedure);

                model.Id = p.Get<int>("@Id");
            }
        }

        public void CreateTeam(Team model)
        {
            using (IDbConnection connection = new System.Data.SqlClient.SqlConnection(GlobalConfig.CnnString(db)))
            {
                var p = new DynamicParameters();
                p.Add("@TeamName", model.TeamName);
                p.Add("@Id", 0, dbType: DbType.Int32, direction: ParameterDirection.Output);

                connection.Execute("dbo.spTeams_Insert", p, commandType: CommandType.StoredProcedure);

                model.Id = p.Get<int>("@Id");

                foreach(Person tm in model.TeamMembers)
                {
                    p = new DynamicParameters();
                    p.Add("@TeamId", model.Id);
                    p.Add("@PersonId", tm.Id);
                    p.Add("@Id", 0, dbType: DbType.Int32, direction: ParameterDirection.Output);

                    connection.Execute("dbo.spTeamMembers_Insert", p, commandType: CommandType.StoredProcedure);
                }
            }
        }

        public void CreateTournament(Tournament model)
        {
            using (IDbConnection connection = new System.Data.SqlClient.SqlConnection(GlobalConfig.CnnString(db)))
            {
                SaveTournament(connection, model);
                SaveTournamentPrizes(connection, model);
                SaveTournamentEntries(connection, model);
                SaveTournamentRounds(connection, model);
                TournamentLogic.UpdateTournamentResults(model);
            }
        }

        private void SaveTournamentRounds(IDbConnection connection, Tournament model)
        {
            foreach (List<Matchup> round in model.Rounds)
            {
                foreach(Matchup match in round)
                {
                    var p = new DynamicParameters();
                    p.Add("@TournamentId", model.Id);
                    p.Add("@MatchupRound", match.MatchupRound);
                    p.Add("@Id", 0, dbType: DbType.Int32, direction: ParameterDirection.Output);

                    connection.Execute("dbo.spMatchups_Insert", p, commandType: CommandType.StoredProcedure);
                    match.Id = p.Get<int>("@Id");

                    foreach(MatchupEntry entry in match.Entries)
                    {
                        p = new DynamicParameters();
                        p.Add("@MatchupId", match.Id);
                        p.Add("@ParentMatchupId", entry.ParentMatchup?.Id);
                        p.Add("@TeamCompetingId", entry.TeamCompeting?.Id);
                        p.Add("@Id", 0, dbType: DbType.Int32, direction: ParameterDirection.Output);

                        connection.Execute("dbo.spMatchupEntries_Insert", p, commandType: CommandType.StoredProcedure);
                        entry.Id = p.Get<int>("@Id");
                    }
                }
            }
        }

        private void SaveTournamentEntries(IDbConnection connection, Tournament model)
        {
            foreach (Team tm in model.EnteredTeams)
            {
                var p = new DynamicParameters();
                p.Add("@TournamentId", model.Id);
                p.Add("@TeamId", tm.Id);
                p.Add("@Id", 0, dbType: DbType.Int32, direction: ParameterDirection.Output);

                connection.Execute("dbo.spTournamentEntries_Insert", p, commandType: CommandType.StoredProcedure);
            }
        }

        private void SaveTournamentPrizes(IDbConnection connection, Tournament model)
        {
            foreach (Prize pz in model.Prizes)
            {
                var p = new DynamicParameters();
                p.Add("@TournamentId", model.Id);
                p.Add("@PrizeId", pz.Id);
                p.Add("@Id", 0, dbType: DbType.Int32, direction: ParameterDirection.Output);

                connection.Execute("dbo.spTournamentPrizes_Insert", p, commandType: CommandType.StoredProcedure);
            }
        }

        private void SaveTournament(IDbConnection connection, Tournament model)
        {
            var p = new DynamicParameters();
            p.Add("@TournamentName", model.TournamentName);
            p.Add("@EntryFee", model.EntryFee);
            p.Add("@Id", 0, dbType: DbType.Int32, direction: ParameterDirection.Output);
            connection.Execute("dbo.spTournaments_Insert", p, commandType: CommandType.StoredProcedure);

            model.Id = p.Get<int>("@Id");
        }

        public List<Person> GetPerson_All()
        {
            List<Person> output;

            using(IDbConnection connection = new System.Data.SqlClient.SqlConnection(GlobalConfig.CnnString(db)))
            {
                output = connection.Query<Person>("dbo.spPeople_GetAll").ToList();
            }
            return output;
        }

        public List<Team> GetTeam_All()
        {
            List<Team> output;

            using (IDbConnection connection = new System.Data.SqlClient.SqlConnection(GlobalConfig.CnnString(db)))
            {
                output = connection.Query<Team>("dbo.spTeams_GetAll").ToList();
                foreach(Team t in output)
                {
                    var p = new DynamicParameters();
                    p.Add("@TeamId", t.Id);
                    t.TeamMembers = connection.Query<Person>("dbo.spTeamMembers_GetByTeam", p, commandType: CommandType.StoredProcedure).ToList();
                }
            }
            return output;
        }

        public List<Tournament> GetTournament_All()
        {
            List<Tournament> output;

            using (IDbConnection connection = new System.Data.SqlClient.SqlConnection(GlobalConfig.CnnString(db)))
            {
                output = connection.Query<Tournament>("dbo.spTournaments_GetAll").ToList();
                foreach (Tournament t in output)
                {
                    var p = new DynamicParameters();
                    p.Add("@TournamentId", t.Id);

                    t.Prizes = connection.Query<Prize>("dbo.spPrizes_GetByTournament", p, commandType: CommandType.StoredProcedure).ToList();
                    t.EnteredTeams = connection.Query<Team>("dbo.spTeams_GetByTournament", p, commandType: CommandType.StoredProcedure).ToList();
                    foreach(Team tm in t.EnteredTeams)
                    {
                        p = new DynamicParameters();
                        p.Add("@TeamId", tm.Id);
                        tm.TeamMembers = connection.Query<Person>("dbo.spTeamMembers_GetByTeam", p, commandType: CommandType.StoredProcedure).ToList();

                    }
                    p = new DynamicParameters();
                    p.Add("@TournamentId", t.Id);
                    List<Matchup> matchups = connection.Query<Matchup>("dbo.spMatchups_GetByTournament", p, commandType: CommandType.StoredProcedure).ToList();

                    foreach(Matchup m in matchups)
                    {
                        p = new DynamicParameters();
                        p.Add("@MatchupId", m.Id);
                        m.Entries = connection.Query<MatchupEntry>("dbo.spMatchupEntries_GetByMatchup", p, commandType: CommandType.StoredProcedure).ToList();

                        List<Team> allTeams = GetTeam_All();

                        if (m.WinnerId > 0)
                        {
                            m.Winner = allTeams.Find(tm => tm.Id == m.WinnerId);
                        }

                        foreach (MatchupEntry e in m.Entries)
                        {
                            if(e.TeamCompetingId > 0)
                            {
                                e.TeamCompeting = allTeams.Find(tm => tm.Id == e.TeamCompetingId);
                            }
                            if(e.ParentMatchupId > 0)
                            {
                                e.ParentMatchup = matchups.Find(ma => ma.Id == e.ParentMatchupId);
                            }
                        }

                    }

                    List<Matchup> currentRound = new List<Matchup>();
                    int roundIndex = 1;

                    foreach(Matchup m in matchups)
                    {
                        if(m.MatchupRound > roundIndex)
                        {
                            t.Rounds.Add(currentRound);
                            currentRound = new List<Matchup>();
                            roundIndex++;
                        }
                        currentRound.Add(m);
                    }
                    t.Rounds.Add(currentRound);

                }
            }
            return output;
        }

        public void UpdateMatchup( Matchup model)
        {
            using (IDbConnection connection = new System.Data.SqlClient.SqlConnection(GlobalConfig.CnnString(db)))
            {
                var p = new DynamicParameters();
                if (model.Winner != null)
                {
                    p.Add("@Id", model.Id);
                    p.Add("@WinnerId", model.Winner.Id);
                    connection.Execute("dbo.spMatchups_Update", p, commandType: CommandType.StoredProcedure); 
                }

                foreach (MatchupEntry entry in model.Entries)
                {
                    if (entry.TeamCompeting != null)
                    {
                        p = new DynamicParameters();
                        p.Add("@Id", entry.Id);
                        p.Add("@TeamCompetingId", entry.TeamCompeting.Id);
                        p.Add("@Score", entry.Score);
                        connection.Execute("dbo.spMatchupEntries_Update", p, commandType: CommandType.StoredProcedure); 
                    }
                }
            }

        }

        public void CompleteTournament(Tournament model)
        {
            using (IDbConnection connection = new System.Data.SqlClient.SqlConnection(GlobalConfig.CnnString(db)))
            {
                var p = new DynamicParameters();
                p.Add("@id", model.Id);
                connection.Execute("dbo.spTournaments_Complete", p, commandType: CommandType.StoredProcedure);
            }
        }
    }
}
