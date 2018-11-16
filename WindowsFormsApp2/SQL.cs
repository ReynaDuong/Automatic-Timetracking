using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using MySql.Data.MySqlClient;

namespace WindowsFormsApp2
{
    public class SQL
    {
        private static string server = "trackerdb.servebeer.com";
        private static string database = "mydb";
        private static string userID = "student";
        private static string password = "student";
        private static MySqlConnection dbConn;

        //initialize database parameters
        private static void IntializeDB()
        {
            MySqlConnectionStringBuilder builder = new MySqlConnectionStringBuilder();
            builder.Server = server;
            builder.Database = database;
            builder.UserID = userID;
            builder.Password = password;

            string connString = builder.ToString();

            builder = null;
            dbConn = new MySqlConnection(connString);
        }

        //load processes associations of a particular task
        public static List<string> loadProcesses(string taskID)
        {
            IntializeDB();
            string query = "SELECT * FROM Processes WHERE TaskID = " + "'" + taskID + "'";

            MySqlCommand cmd = new MySqlCommand(query, dbConn);

            dbConn.Open();                                      //opens connection
            MySqlDataReader reader = cmd.ExecuteReader();       //makes the query

            List<string> processes = new List<string>();
            while(reader.Read())
            {
                processes.Add(reader[0].ToString());
            }

            dbConn.Close();
            return processes;
        }

        //load URLs associations of a particular task
        public static List<string> loadUrls(string taskID)
        {
            IntializeDB();
            string query = "SELECT * FROM URLs WHERE TaskID = " + "'" + taskID + "'";

            MySqlCommand cmd = new MySqlCommand(query, dbConn);

            dbConn.Open();                                      //opens connection
            MySqlDataReader reader = cmd.ExecuteReader();       //makes the query

            List<string> urls = new List<string>();
            while (reader.Read())
            {
                urls.Add(reader[0].ToString());
            }

            dbConn.Close();
            return urls;
        }

        //load process(1), or URL(2) association rules
        public static List<Association> loadAssociations(int type)
        {
            IntializeDB();
            string query = string.Empty;

            if (type == 1)
                query = "SELECT * FROM Processes WHERE projectId = " + "'" + Global.projectId + "'";
            else if (type == 2)
                query = "SELECT * FROM URLs WHERE projectId = " + "'" + Global.projectId + "'";

            MySqlCommand cmd = new MySqlCommand(query, dbConn);

            dbConn.Open();                                      //opens connection
            MySqlDataReader reader = cmd.ExecuteReader();       //makes the query

            List<Association> rules = new List<Association>();

            //perform value->taskId association
            while (reader.Read())
            {
                Association r = new Association();
                r.value = reader[0].ToString();
                r.taskId = reader[1].ToString();
                r.taskName = Global.taskIdName[r.taskId];       //lookup task name in global dictionary, which is defined before this routine is called

                rules.Add(r);
            }

            dbConn.Close();
            return rules;
        }

        //insert process(1) or URL(2), also inserts workspaceId, projectId, or tasksID if they don't exist  
        public static void insertRule(int type, string value, string workspaceId, string projectId, string taskID, string workspaceName, string projectName, string taskName)
        {
            string query = string.Empty;

            if (value.Equals(""))
                return;

            //if task doesn't exist, check if parent ID exists and insert if necessary
            if (!ifExist(3, taskID, ""))                                
            {
                if (!ifExist(2, projectId, ""))
                {
                    if (!ifExist(1, workspaceId, ""))
                    {
                        insertClockifyInfo(1, workspaceName, workspaceId, "");
                    }
                    insertClockifyInfo(2, projectName, projectId, workspaceId);
                }
                insertClockifyInfo(3, taskName, taskID, projectId);
            }
            

            IntializeDB();

            if (type == 1)
                if (!ifExist(4, projectId, value))                 //insert rule only if it doesn't exist in current task
                    query = "INSERT INTO `Processes` (`Name`, `TaskID`, `projectId`) VALUES('" + value + "', '" + taskID + "', '" + projectId + "')";
                else
                {
                    MessageBox.Show("'" + value + "' already exist for this project!");
                    return;
                }
            else if (type == 2)
                if (!ifExist(5, projectId, value))
                    query = "INSERT INTO `URLs` (`URL`, `TaskID`, `projectId`) VALUES('" + value + "', '" + taskID + "', '" + projectId + "')";
                else
                {
                    MessageBox.Show("'" + value + "' already exist for this project!");
                    return;
                }
                    

            MySqlCommand cmd = new MySqlCommand(query, dbConn);
            dbConn.Open();                                      //opens connection
            
            cmd.ExecuteNonQuery();                              //makes the query
            dbConn.Close();
        }


        //check if entry already exists
        public static bool ifExist(int type, string id, string value)
        {
            IntializeDB();
            string table = string.Empty;
            string query = string.Empty;

            if (type == 1)
                table = "Workspaces";
            else if (type == 2)
                table = "Projects";
            else if (type == 3)
                table = "Tasks";
            else if (type == 4)
                table = "Processes";
            else if (type == 5)
                table = "URLss";

            if (type < 4)
                query = "SELECT EXISTS (SELECT * FROM " + table + " WHERE ID = '" + id + "') as `is-exists`";
            else if (type == 4)
                query = "SELECT EXISTS(SELECT * FROM Processes WHERE Name = '" + value + "' AND projectId = '" + id + "') as `is -exists`";
            else if (type == 5)
                query = "SELECT EXISTS(SELECT * FROM URLs WHERE URL = '" + value + "' AND projectId = '" + id + "') as `is -exists`";


            MySqlCommand cmd = new MySqlCommand(query, dbConn);
            dbConn.Open();                                      //opens connection

            MySqlDataReader reader = cmd.ExecuteReader();       //makes the query

            reader.Read();                                      //read next (only has one element for this query)

            if (reader[0].ToString().Equals("1"))
                return true;

            dbConn.Close();
            return false; ;
        }

        //inserts workspace, project, or task
        private static void insertClockifyInfo(int type, string name, string primary, string foreign)
        {
            IntializeDB();
            string query = string.Empty;

            if (type == 1)
                query = "INSERT INTO Workspaces (`Name`, `ID`) VALUES('" + name + "', '" + primary + "')";
            else if (type == 2)
                query = "INSERT INTO `Projects` (`Name`, `ID`, `workspaceId`) VALUES('" + name + "', '" + primary + "', '" + foreign + "')";
            else if (type == 3)
                query = "INSERT INTO `Tasks` (`Name`, `ID`, `projectId`) VALUES ('" + name + "', '" + primary + "', '" + foreign + "')";

            MySqlCommand cmd = new MySqlCommand(query, dbConn);
            dbConn.Open();                                      //opens connection

            cmd.ExecuteNonQuery();                              //makes the query
            dbConn.Close();
        }

        //delete a process or url
        public static void delete(int type, string value, string taskID)
        {
            IntializeDB();
            string table = string.Empty;
            string column = string.Empty;
            string query = string.Empty;

            if (type == 1)
            {
                table = "Processes";
                column = "Name";
            }
            else if (type == 2)
            {
                table = "URLs";
                column = "URL";
            }
                

            query = "DELETE FROM " + table + " WHERE " + column + " = '" + value + "' AND TaskID = '" + taskID + "'";

            MySqlCommand cmd = new MySqlCommand(query, dbConn);
            dbConn.Open();                                      //opens connection

            cmd.ExecuteNonQuery();                              //makes the query
            dbConn.Close();
        }




    }





        
    
}
