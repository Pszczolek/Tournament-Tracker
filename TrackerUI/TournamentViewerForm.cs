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
    public partial class TournamentViewerForm : Form
    {
        private Tournament tournament;
        List<int> rounds;
        List<Matchup> selectedMatchups = new List<Matchup>();

        public TournamentViewerForm(Tournament model)
        {
            InitializeComponent();
            tournament = model;
            tournament.OnTournamentComplete += Tournament_OnTournamentComplete;
            LoadFormData();
        }

        private void Tournament_OnTournamentComplete(object sender, DateTime e)
        {
            this.Close();
        }

        private void LoadFormData()
        {
            tournamentName.Text = tournament.TournamentName;
            LoadRounds();
        }

        private void LoadRounds()
        {
            int currentRound = 1;
            rounds = new List<int>();
            rounds.Add(1);
            foreach(List<Matchup> matchups in tournament.Rounds)
            {
                if(matchups.First().MatchupRound > currentRound)
                {
                    currentRound = matchups.First().MatchupRound;
                    rounds.Add(currentRound);
                }
            }

            WireUpRoundList();
            WireUpMatchupList();
        }

        private void WireUpRoundList()
        {
            roundDropDown.DataSource = null;
            roundDropDown.DataSource = rounds;
        }

        private void WireUpMatchupList()
        {
            matchupListBox.DataSource = null;
            matchupListBox.DataSource = selectedMatchups;
            matchupListBox.DisplayMember = "DisplayName";
        }

        private void roundDropDown_SelectedIndexChanged(object sender, EventArgs e)
        {
            LoadMatchups((int)roundDropDown.SelectedItem);
        }

        private void LoadMatchups(int round)
        {
            selectedMatchups = new List<Matchup>();

            foreach (List<Matchup> matchups in tournament.Rounds)
            {
                if (matchups.First().MatchupRound == round)
                {
                    selectedMatchups.Clear();
                    foreach(Matchup m in matchups)
                    {
                        if (m.Winner == null || !unplayedOnlyCheckbox.Checked)
                        {
                            selectedMatchups.Add(m);
                        }
                    }

                }
            }
            WireUpMatchupList();
            DisplayMatchupInfo();
        }

        private void DisplayMatchupInfo()
        {
            bool isVisible = (selectedMatchups.Count > 0);
            teamOneName.Visible = isVisible;
            teamOneScoreLabel.Visible = isVisible;
            teamOneScoreValue.Visible = isVisible;
            teamTwoName.Visible = isVisible;
            teamTwoScoreLabel.Visible = isVisible;
            teamTwoScoreValue.Visible = isVisible;
            vsLabel.Visible = isVisible;
            scoreButton.Visible = isVisible;
        }

        private void matchupListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            LoadMatchup((Matchup)matchupListBox.SelectedItem);
        }

        private void LoadMatchup(Matchup match)
        {
            if(match == null)
            {
                return;
            }
            for(int i = 0; i < match.Entries.Count; i++)
            {
                if(i == 0)
                {
                    if(match.Entries[0].TeamCompeting != null)
                    {
                        teamOneName.Text = match.Entries[0].TeamCompeting.TeamName;
                        teamOneScoreValue.Text = match.Entries[0].Score.ToString();
                        teamTwoName.Text = "<bye>";
                        teamTwoScoreValue.Text = "0";
                    }
                    else
                    {
                        teamOneName.Text = "<undefined team>";
                        teamOneScoreValue.Text = "";
                    }
                }

                if (i == 1)
                {
                    if (match.Entries[1].TeamCompeting != null)
                    {
                        teamTwoName.Text = match.Entries[1].TeamCompeting.TeamName;
                        teamTwoScoreValue.Text = match.Entries[1].Score.ToString();
                    }
                    else
                    {
                        teamTwoName.Text = "<undefined team>";
                        teamTwoScoreValue.Text = "";
                    }
                }
            }
        }

        private void unplayedOnlyCheckbox_CheckedChanged(object sender, EventArgs e)
        {
            LoadMatchups((int)roundDropDown.SelectedItem);
        }

        private string ValidateData()
        {
            string output = "";
            double teamOneScore = 0;
            double teamTwoScore = 0;
            bool scoreOneValid = double.TryParse(teamOneScoreValue.Text, out teamOneScore);
            bool scoreTwoValid = double.TryParse(teamTwoScoreValue.Text, out teamTwoScore);

            if(!scoreOneValid)
            {
                output = "First team's score is not a valid number.";
            }
            else if (!scoreTwoValid)
            {
                output = "Second team's score is not a valid number.";
            }
            else if (teamOneScore == 0 && teamTwoScore == 0)
            {
                output = "Both scores cannot be 0.";
            }
            else if(teamOneScore == teamTwoScore)
            {
                output = "Tied scores are not allowed.";
            }

            return output;
        }

        private void scoreButton_Click(object sender, EventArgs e)
        {
            string errorMessage = ValidateData();
            if (errorMessage.Length > 0)
            {
                MessageBox.Show($"Input error: {errorMessage}", "Input Error");
                return;
            }

            Matchup match = (Matchup)matchupListBox.SelectedItem;
            double teamOneScore = 0;
            double teamTwoScore = 0;

            for (int i = 0; i < match.Entries.Count; i++)
            {
                if (i == 0)
                {
                    if (match.Entries[0].TeamCompeting != null)
                    {
                        bool scoreValid = double.TryParse(teamOneScoreValue.Text, out teamOneScore);
                        if (scoreValid)
                        {
                            match.Entries[0].Score = teamOneScore;
                        }
                        else
                        {
                            MessageBox.Show("Please enter valid score");
                            return;
                        }
                    }

                }

                if (i == 1)
                {
                    if (match.Entries[0].TeamCompeting != null)
                    {
                        bool scoreValid = double.TryParse(teamTwoScoreValue.Text, out teamTwoScore);
                        if (scoreValid)
                        {
                            match.Entries[1].Score = teamTwoScore;
                        }
                        else
                        {
                            MessageBox.Show("Please enter valid score");
                            return;
                        }
                    }

                }
            }

            //try
            //{
            TournamentLogic.UpdateTournamentResults(tournament);
            //}
            //catch (Exception ex)
            //{
            //    MessageBox.Show($"The application had the following error: { ex.Message }");
            //}

            LoadMatchups((int)roundDropDown.SelectedItem);


        }
    }
}
