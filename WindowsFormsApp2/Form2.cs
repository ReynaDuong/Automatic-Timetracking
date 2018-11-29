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
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            string userName = textBox1.Text.Trim();
            string passWord = textBox2.Text.Trim();
            //userName = "lyw81718@gmail.com";
            //passWord = "123456";

            try
            {
               Dto.AuthResponse Response = API.login(userName, passWord);

               Global.token = Response.token;
               Global.name = Response.name;

            }
            catch (Exception ex)
            {
                //MessageBox.Show("Most likely wrong username/password, exception will be handled soon.");
                MessageBox.Show(ex.ToString());
                return;
            }



            Form1 obj = new Form1();
            this.Hide();
            obj.Closed += (s, args) => this.Close();
            obj.Closed += (s, args) => this.Dispose();
            obj.Show();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void Form2_Load(object sender, EventArgs e)
        {
            this.ActiveControl = textBox1;
            this.CenterToScreen();
        }

        private void textBox1_KeyDown(object sender, KeyEventArgs e)
        {

        }
    }
}
