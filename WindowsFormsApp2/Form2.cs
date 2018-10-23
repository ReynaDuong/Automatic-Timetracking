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
            String UN = textBox1.Text.Trim();
            String PW = textBox2.Text.Trim();
            Rest Client = new Rest
            {
                endpoint = "https://api.clockify.me/api/auth/token/",
                Username = UN,
                Password = PW,
                httpMethod= httpVerb.POST
            };
            var auth = "{  \"email\": "+"\""+UN+"\""+",\"password\": "+"\""+PW+"\""+" }";
            var jsonauth = new JavaScriptSerializer().Serialize(auth);
            Client.postJSON = auth;
            String Response = String.Empty;
            Response = Client.MakeRequest();
            String name=String.Empty;


            if (Response.Contains("Error"))
            {
                MessageBox.Show(Response);
            }
            else
            {
                dynamic postR = JsonConvert.DeserializeObject(Response);
                    MessageBox.Show("Login successfully, Welcome " + postR.name);
                    Form1 obj = new Form1(postR.token, postR.name);
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
