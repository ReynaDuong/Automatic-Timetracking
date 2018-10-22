//Newtonsoft.Json package is required
//To download,simply go to reference to your right,find Nuget Package and browse Newtonsoft.json and install.
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Dynamic;
using Newtonsoft.Json;
using System.Web.Script.Serialization;
using System.IO;

namespace WindowsFormsApp2
{
    public partial class Form2 : Form
    {
        public Form2()
        {
            InitializeComponent();
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            String APIK = textBox1.Text.Trim();
            
            Rest Client = new Rest();
            Client.endpoint = "https://api.clockify.me/api/workspaces/5baa4d06b079875917c7d342/users";
            Client.APIKEY = APIK;
            String Response = String.Empty;
            Response = Client.MakeRequest();
            String name=String.Empty;


            if (Response.Contains("Error"))
            MessageBox.Show(Response);
            else
            {
                dynamic Array = JsonConvert.DeserializeObject(Response);
                
                var count1 = 0;
                var count2 = 0;
                foreach(Object i in Array)
                {
                    count1++;
                }
                dynamic[] UserInfo = new dynamic[count1];
                dynamic[] Username = new dynamic[count1];
              
                foreach (Object i in Array)
                {
                    UserInfo[count2] = JsonConvert.DeserializeObject(i.ToString());
                    Username[count2] = UserInfo[count2].name;
                    Username[count2] = Username[count2].ToString();
                    Username[count2] = Username[count2].ToLower();
                    count2++;

                }
              
                    MessageBox.Show("Login successfully, Welcome " + name);
                    Form1 obj = new Form1(Array,APIK);
                    this.Hide();
                    obj.Closed += (s, args) => this.Close();
                    obj.Closed += (s, args) => this.Dispose();
                    obj.Show();
                

                    


            }

            // 
        }

        private void button2_Click(object sender, EventArgs e)
        {
            this.Close();
        }


    }
}
