//Newtonsoft.Json package is required
//To download,simply go to reference to your right,find Nuget Package and browse Newtonsoft.json and install.
using System;
using System.Windows.Forms;

namespace WindowsFormsApp2
{
    public partial class Form2 : Form
    {
        public Form2()
        {
            InitializeComponent();
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            var userName = textBox1.Text.Trim();
            var passWord = textBox2.Text.Trim();
            //userName = "lyw81718@gmail.com";
            //passWord = "123456";

            try
            {
               Dto.AuthResponse response = Api.Login(userName, passWord);

               Global.token = response.token;
               Global.name = response.name;

            }
            catch (Exception ex)
            {
                //MessageBox.Show("Most likely wrong username/password, exception will be handled soon.");
                MessageBox.Show(ex.ToString());
                return;
            }



            var obj = new Form1();
            Hide();
            obj.Closed += (s, args) => Close();
            obj.Closed += (s, args) => Dispose();
            obj.Show();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void Form2_Load(object sender, EventArgs e)
        {
            ActiveControl = textBox1;
            CenterToScreen();
        }

        private void textBox1_KeyDown(object sender, KeyEventArgs e)
        {

        }
    }
}
