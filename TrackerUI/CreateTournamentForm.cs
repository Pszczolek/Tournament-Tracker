using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using TrackerLibrary;
using TrackerLibrary.Models;

namespace TrackerUI
{
    public partial class CreateTournamentForm : Form, IPrizeRequester, ITeamRequester
    {
        private List<Team> availableTeams = GlobalConfig.Connection.GetTeam_All();
        private List<Team> selectedTeams = new List<Team>();
        private List<Prize> selectedPrizes = new List<Prize>();

        public CreateTournamentForm()
        {
            InitializeComponent();
            WireUpLists();
        }

        private void WireUpLists()
        {
            selectTeamDropDown.DataSource = null;
            selectTeamDropDown.DataSource = availableTeams;
            selectTeamDropDown.DisplayMember = "TeamName";

            tournamentTeamsListBox.DataSource = null;
            tournamentTeamsListBox.DataSource = selectedTeams;
            tournamentTeamsListBox.DisplayMember = "TeamName";

            prizesListBox.DataSource = null;
            prizesListBox.DataSource = selectedPrizes;
            prizesListBox.DisplayMember = "PlaceName";
        }

        private void addTeamButton_Click(object sender, EventArgs e)
        {
            //Person p = (Person)selectTeamMemberDropDown.SelectedItem;

            //if (p != null)
            //{
            //    availableTeamMembers.Remove(p);
            //    selectedTeamMembers.Add(p);

            //    WireUpLists();
            //}
            Team t = (Team)selectTeamDropDown.SelectedItem;
            if(t != null)
            {
                availableTeams.Remove(t);
                selectedTeams.Add(t);
                WireUpLists();
            }
        }

        private void createPrizeButton_Click(object sender, EventArgs e)
        {
            CreatePrizeForm prizeForm = new CreatePrizeForm(this);
            prizeForm.Show();
        }

        private void createNewTeamLink_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            CreateTeamForm teamFrom = new CreateTeamForm(this);
            teamFrom.Show();
        }

        public void PrizeComplete(Prize model)
        {
            selectedPrizes.Add(model);
            WireUpLists();
        }

        public void TeamComplete(Team model)
        {
            selectedTeams.Add(model);
            WireUpLists();
        }

        private void removeSelectedTeamButton_Click(object sender, EventArgs e)
        {
            Team t = (Team)tournamentTeamsListBox.SelectedItem;
            if (t != null)
            {
                selectedTeams.Remove(t);
                availableTeams.Add(t);
                WireUpLists();
            }
        }

        private void removeSelectedPrizeButton_Click(object sender, EventArgs e)
        {
            Prize p = (Prize)prizesListBox.SelectedItem;
            if (p != null)
            {
                selectedPrizes.Remove(p);
                WireUpLists();
            }
        }

        private void createTournamentButton_Click(object sender, EventArgs e)
        {
            decimal fee = 0;
            bool feeAcceptable = decimal.TryParse(entryFeeValue.Text, out fee);

            if (!feeAcceptable)
            {
                MessageBox.Show("You need to enter valid entry fee",
                    "Invalid entry fee value", 
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

            Tournament tm = new Tournament();
            tm.TournamentName = tournamentNameValue.Text;
            tm.EntryFee = fee;
            tm.Prizes = selectedPrizes;
            tm.EnteredTeams = selectedTeams;

            TournamentLogic.CreateRounds(tm);

            GlobalConfig.Connection.CreateTournament(tm);
            tm.AlertUsersToNewRound();

            TournamentViewerForm form = new TournamentViewerForm(tm);
            form.Show();
            this.Close();
        }

    }
}
