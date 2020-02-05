using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace WindowsFormsApp2
{
    public partial class Form4 : Form
    {
        public string workspaceId = string.Empty;
        public string projectId = string.Empty;
        public string taskId = string.Empty;

        public string workspaceName = string.Empty;
        public string projectName = string.Empty;
        public string taskName = string.Empty;

        public string value = string.Empty;

        public Form4()
        {
            InitializeComponent();

            //format
            TopMost = true;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            MinimizeBox = true;
            Activate();

            ButtonToggle("off");

            FetchClockify();
        }

        //fetch clockify for workspaces, projects and tasks
        public void FetchClockify()
        {
            treeView1.HideSelection = false;

            var i = 0;
            var j = 0;
            var k = 0;

            //iterate workspaces
            List<Dto.WorkspaceDto> workspaces = Api.GetWorkspaces();
            foreach (var w in workspaces)
            {
                treeView1.Nodes.Add(w.name);                                //workspace name
                treeView1.Nodes[i].Tag = w.id;                              //workspace ID

                //iterate projects
                List<Dto.ProjectFullDto> projects = Api.GetProjectsByWorkspaceId(w.id);
                foreach (var p in projects)
                {
                    treeView1.Nodes[i].Nodes.Add(p.name);                   //project name
                    treeView1.Nodes[i].Nodes[j].Tag = p.id;                 //project ID

                    //iterate tasks
                    List<Dto.TaskDto> tasks = Api.GetTasksByProjectId(w.id, p.id);
                    foreach (var t in tasks)
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
                workspaceId = treeView1.SelectedNode.Parent.Parent.Tag.ToString();
                projectId = treeView1.SelectedNode.Parent.Tag.ToString();
                taskId = treeView1.SelectedNode.Tag.ToString();

                workspaceName = treeView1.SelectedNode.Parent.Parent.Text;
                projectName = treeView1.SelectedNode.Parent.Text;
                taskName = treeView1.SelectedNode.Text;


                LoadListboxes();
                ButtonToggle("on");
            }
            else
            {
                ButtonToggle("off");
                listBox1.Items.Clear();
                listBox2.Items.Clear();
            }
                
            return;
        }

        //load processes and urls associations from database for selected task in the tree
        private void LoadListboxes()
        {
            listBox1.BeginUpdate();
            listBox2.BeginUpdate();

            listBox1.Items.Clear();
            listBox2.Items.Clear();

            var processes = Sql.LoadProcesses(taskId);
            var urls = Sql.LoadUrls(taskId);

            foreach (var ps in processes)
            {
                listBox1.Items.Add(ps);
            }

            foreach (var url in urls)
            {
                listBox2.Items.Add(url);
            }

            listBox1.EndUpdate();
            listBox2.EndUpdate();
        }

        //add process
        private void AddNewProcessButton_Click(object sender, EventArgs e)          
        {
            try
            {
                Sql.InsertRule(1, textBox1.Text.Trim().ToLower(), workspaceId, projectId, taskId, workspaceName, projectName, taskName);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
            
            LoadListboxes();    //reload associations
            textBox1.Clear();
            textBox1.Focus();
        }

        //add URL
        private void button3_Click(object sender, EventArgs e)          
        {
            try
            {
                Sql.InsertRule(2, textBox2.Text.Trim().ToLower(), workspaceId, projectId, taskId, workspaceName, projectName, taskName);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }

            LoadListboxes();
            textBox2.Clear();
            textBox2.Focus();
        }

        //remove process
        private void button2_Click(object sender, EventArgs e)
        {
            //in case of null string
            var value = string.Empty;
            try
            {
                value = listBox1.SelectedItem.ToString().ToLower();
            }
            catch
            {
                return;
            }
            
           Sql.Delete(1, value, taskId);
            
            LoadListboxes();
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

            Sql.Delete(2, value, taskId);

            LoadListboxes();
        }

        //textbox for new process
        private void textBox1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                AddNewProcessButton.PerformClick();
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
        public void ButtonToggle(string pos)
        {
            if (pos.Equals("on"))
            {
                AddNewProcessButton.Enabled = true;
                button2.Enabled = true;
                button3.Enabled = true;
                button4.Enabled = true;
                button5.Enabled = true;
                textBox1.Enabled = true;
                textBox2.Enabled = true;
            }
            else if (pos.Equals("off"))
            {
                AddNewProcessButton.Enabled = false;
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

                Close();
                return;
            }

            Global.chosen = 1;
            Close();
        }

        private void button6_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }
    }
}
