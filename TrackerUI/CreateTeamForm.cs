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
    public partial class CreateTeamForm : Form
    {
        private List<Person> availableTeamMembers = GlobalConfig.Connection.GetPerson_All();
        private List<Person> selectedTeamMembers = new List<Person>();

        private ITeamRequester callingForm;

        public CreateTeamForm(ITeamRequester caller)
        {
            InitializeComponent();

            callingForm = caller;
            //CreateSampleData();
            WireUpLists();
        }

        private void CreateSampleData()
        {
            availableTeamMembers.Add(new Person { FirstName = "Mik", LastName = "Pszcz" });
            availableTeamMembers.Add(new Person { FirstName = "Ewa", LastName = "Pszcz" });

            selectedTeamMembers.Add(new Person { FirstName = "Marcin", LastName = "Og" });
            selectedTeamMembers.Add(new Person { FirstName = "Krystian", LastName = "Kosz" });
        }

        private void WireUpLists()
        {
            selectTeamMemberDropDown.DataSource = null;

            selectTeamMemberDropDown.DataSource = availableTeamMembers;
            selectTeamMemberDropDown.DisplayMember = "FullName";

            teamMembersListBox.DataSource = null;

            teamMembersListBox.DataSource = selectedTeamMembers;
            teamMembersListBox.DisplayMember = "FullName";
        }

        private void createMemberButton_Click(object sender, EventArgs e)
        {
            if (ValidateForm())
            {
                Person p = new Person();
                p.FirstName = firstNameValue.Text;
                p.LastName = lastNameValue.Text;
                p.EmailAddress = emailValue.Text;
                p.CellphoneNumber = phoneValue.Text;

                GlobalConfig.Connection.CreatePerson(p);

                selectedTeamMembers.Add(p);
                WireUpLists();

                firstNameValue.Text = "";
                lastNameValue.Text = "";
                emailValue.Text = "";
                phoneValue.Text = "";
            }
            else
            {
                MessageBox.Show("This form has invalid information. Please fill valid data");
            }
        }

        private void addTeamMemberButton_Click(object sender, EventArgs e)
        {
            Person p = (Person)selectTeamMemberDropDown.SelectedItem;

            if (p != null)
            {
                availableTeamMembers.Remove(p);
                selectedTeamMembers.Add(p);

                WireUpLists(); 
            }
        }

        private bool ValidateForm()
        {
            bool output = true;
            if(firstNameValue.Text.Length == 0)
            {
                output = false;
            }
            if(lastNameValue.Text.Length == 0)
            {
                output = false;
            }
            if(emailValue.Text.Length == 0)
            {
                output = false;
            }
            if(phoneValue.Text.Length == 0)
            {
                output = false;
            }
            return output;
        }

        private void removeSelectedPlayerButton_Click(object sender, EventArgs e)
        {
            Person p = (Person)teamMembersListBox.SelectedItem;

            if (p != null)
            {
                selectedTeamMembers.Remove(p);
                availableTeamMembers.Add(p);

                WireUpLists(); 
            }
        }

        private void createTeamButton_Click(object sender, EventArgs e)
        {
            Team t = new Team();
            t.TeamName = teamNameValue.Text;
            t.TeamMembers = selectedTeamMembers;

            GlobalConfig.Connection.CreateTeam(t);

            //foreach(Person p in selectedTeamMembers)
            //{
            //    availableTeamMembers.Add(p);
            //}
            //selectedTeamMembers.Clear();
            //WireUpLists();
            //teamNameValue.Text = "";

            callingForm.TeamComplete(t);
            this.Close();
        }
    }
}
