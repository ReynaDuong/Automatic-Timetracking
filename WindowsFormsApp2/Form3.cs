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
    public partial class Form3 : Form
    {
        public Form3()
        {
            InitializeComponent();

            //format
            this.TopMost = true;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.MinimizeBox = true;
            this.Activate();

            button2.Enabled = false;

            fetch();
        }
        
        //fetch projects from all workspaces
        public void fetch()
        {
            int i = 0;
            int j = 0;

            List<Dto.WorkspaceDto> workspaces = API.getWorkspaces();
            foreach (Dto.WorkspaceDto w in workspaces)
            {
                treeView1.Nodes.Add(w.name);                    //workspace name
                treeView1.Nodes[i].Tag = w.id;                  //workspace ID

                List<Dto.ProjectFullDto> projects = API.getProjectsByWorkspaceId(w.id);
                foreach(Dto.ProjectFullDto p in projects)
                {
                    treeView1.Nodes[i].Nodes.Add(p.name);       //project name
                    treeView1.Nodes[i].Nodes[j].Tag = p.id;     //project ID

                    j++;
                }

                j = 0;
                i++;
            }
        }

        //load association rules when a project is being selected
        private void button2_Click(object sender, EventArgs e)
        {

            Global.workspaceId = treeView1.SelectedNode.Parent.Tag.ToString();
            Global.workspaceName = treeView1.SelectedNode.Parent.Text;
            Global.projectId = treeView1.SelectedNode.Tag.ToString();
            Global.projectName = treeView1.SelectedNode.Text;

            Global.chosen = 1;
            this.Close();
        }


        private void button1_Click(object sender, EventArgs e)
        {
            treeView1.Nodes.Clear();
            fetch();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void Form3_Load(object sender, EventArgs e)
        {

        }

        private void treeView1_AfterSelect(object sender, TreeViewEventArgs e)
        {
            //check if child node (level 1 for project)
            if (treeView1.SelectedNode.Level == 1)
                button2.Enabled = true;
            else
                button2.Enabled = false;
        }
    }
}
