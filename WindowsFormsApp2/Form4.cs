using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WindowsFormsApp2
{
    public partial class Form4 : Form
    {
        public string workspaceID = string.Empty;
        public string projectID = string.Empty;
        public string taskID = string.Empty;

        public string workspaceName = string.Empty;
        public string projectName = string.Empty;
        public string taskName = string.Empty;

        public string value = string.Empty;

        public Form4()
        {
            InitializeComponent();

            //format
            this.TopMost = true;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.MinimizeBox = true;
            this.Activate();

            buttonToggle("off");

            fetchClockify();
        }

        //fetch clockify for workspaces, projects and tasks
        public void fetchClockify()
        {
            treeView1.HideSelection = false;

            int i = 0;
            int j = 0;
            int k = 0;

            //iterate workspaces
            List<Dto.WorkspaceDto> workspaces = API.getWorkspaces();
            foreach (Dto.WorkspaceDto w in workspaces)
            {
                treeView1.Nodes.Add(w.name);                                //workspace name
                treeView1.Nodes[i].Tag = w.id;                              //workspace ID

                //iterate projects
                List<Dto.ProjectFullDto> projects = API.getProjectsByWorkspaceId(w.id);
                foreach (Dto.ProjectFullDto p in projects)
                {
                    treeView1.Nodes[i].Nodes.Add(p.name);                   //project name
                    treeView1.Nodes[i].Nodes[j].Tag = p.id;                 //project ID

                    //iterate tasks
                    List<Dto.TaskDto> tasks = API.getTasksByProjectId(w.id, p.id);
                    foreach (Dto.TaskDto t in tasks)
                    {
                        treeView1.Nodes[i].Nodes[j].Nodes.Add(t.name);      //task name
                        treeView1.Nodes[i].Nodes[j].Nodes[k].Tag = t.id;    //task ID

                        k++;
                    }

                    k = 0;
                    j++;
                }

                j = 0;
                i++;
            }
        }

        //when a tree node is selected
        private void treeView1_AfterSelect(object sender, TreeViewEventArgs e)
        {
            //check if child node (level 2 for tasks)
            if (treeView1.SelectedNode.Level == 2)
            {
                workspaceID = treeView1.SelectedNode.Parent.Parent.Tag.ToString();
                projectID = treeView1.SelectedNode.Parent.Tag.ToString();
                taskID = treeView1.SelectedNode.Tag.ToString();

                workspaceName = treeView1.SelectedNode.Parent.Parent.Text;
                projectName = treeView1.SelectedNode.Parent.Text;
                taskName = treeView1.SelectedNode.Text;


                loadListboxes();
                buttonToggle("on");
            }
            else
            {
                buttonToggle("off");
                listBox1.Items.Clear();
                listBox2.Items.Clear();
            }
                
            return;
        }

        //load processes and urls associations from database for selected task in the tree
        private void loadListboxes()
        {
            listBox1.BeginUpdate();
            listBox2.BeginUpdate();

            listBox1.Items.Clear();
            listBox2.Items.Clear();

            List<string> processes = SQL.loadProcesses(taskID);
            List<string> urls = SQL.loadUrls(taskID);

            foreach (string ps in processes)
            {
                listBox1.Items.Add(ps);
            }

            foreach (string url in urls)
            {
                listBox2.Items.Add(url);
            }

            listBox1.EndUpdate();
            listBox2.EndUpdate();
        }

        //add process
        private void button1_Click(object sender, EventArgs e)          
        {
            try
            {
                SQL.insertRule(1, textBox1.Text.Trim().ToLower(), workspaceID, projectID, taskID, workspaceName, projectName, taskName);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
            
            loadListboxes();    //reload associations
            textBox1.Clear();
            textBox1.Focus();
        }

        //add URL
        private void button3_Click(object sender, EventArgs e)          
        {
            try
            {
                SQL.insertRule(2, textBox2.Text.Trim().ToLower(), workspaceID, projectID, taskID, workspaceName, projectName, taskName);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }

            loadListboxes();
            textBox2.Clear();
            textBox2.Focus();
        }

        //remove process
        private void button2_Click(object sender, EventArgs e)
        {
            //in case of null string
            string value = string.Empty;
            try
            {
                value = listBox1.SelectedItem.ToString().ToLower();
            }
            catch
            {
                return;
            }
            
           SQL.delete(1, value, taskID);
            
            loadListboxes();
        }

        //remove url
        private void button4_Click(object sender, EventArgs e)
        {
            try
            {
                value = listBox2.SelectedItem.ToString().ToLower();
            }
            catch
            {
                return;
            }

            SQL.delete(2, value, taskID);

            loadListboxes();
        }

        //textbox for new process
        private void textBox1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                button1.PerformClick();
                e.SuppressKeyPress = true;
            }
        }

        //textbox for new URL
        private void textBox2_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                button3.PerformClick();
                e.SuppressKeyPress = true;
            }
        }

        //graying out buttons and textboxes
        public void buttonToggle(string pos)
        {
            if (pos.Equals("on"))
            {
                button1.Enabled = true;
                button2.Enabled = true;
                button3.Enabled = true;
                button4.Enabled = true;
                button5.Enabled = true;
                textBox1.Enabled = true;
                textBox2.Enabled = true;
            }
            else if (pos.Equals("off"))
            {
                button1.Enabled = false;
                button2.Enabled = false;
                button3.Enabled = false;
                button4.Enabled = false;
                button5.Enabled = false;
                textBox1.Enabled = false;
                textBox2.Enabled = false;
            }
        }

        private void Form4_Load(object sender, EventArgs e)
        {

        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void button5_Click(object sender, EventArgs e)
        {
            if (Global.projectId.Equals(string.Empty))
            {
                MessageBox.Show("Please choose a project to begin session.");

                this.Close();
                return;
            }

            Global.chosen = 1;
            this.Close();
        }

        private void button6_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }
    }
}
