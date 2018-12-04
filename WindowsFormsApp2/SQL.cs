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
        public static List<Association> loadAssociations(int type, string projectId)
        {
            IntializeDB();
            string query = string.Empty;

            if (type == 1)
                query = "SELECT * FROM mydb.Processes WHERE projectId = " + "'" + projectId + "'";
            else if (type == 2)
                query = "SELECT * FROM mydb.URLs WHERE projectId = " + "'" + projectId + "'";


            MySqlCommand cmd = new MySqlCommand(query, dbConn);

            dbConn.Open();                                      //opens connection
            MySqlDataReader reader = cmd.ExecuteReader();       //makes the query

            List<Association> rules = new List<Association>();

            //perform value->taskId association
            try
            {
                while (reader.Read())
                {
                    //MessageBox.Show(reader[0].ToString());
                    Association r = new Association();
                    r.value = reader[0].ToString();
                    r.taskId = reader[1].ToString();

                    r.taskName = Global.allTaskIdName[r.taskId];       //lookup task name in global dictionary, which is defined before this routine is called

                    rules.Add(r);
                }
            }
            catch
            {
                MessageBox.Show("Error occurred in fetching task associations from database.");
            }


            dbConn.Close();
            return rules;
        }

        //insert process(1) or URL(2), also inserts workspaceId, projectId, or tasksID if they don't exist  
        public static void insertRule(int type, string value, string workspaceId, string projectId, string taskId, string workspaceName, string projectName, string taskName)
        {
            string query = string.Empty;
            string existedTaskName = string.Empty;
            string existedTaskId = string.Empty;

            if (value.Equals(""))
                return;

            //if task doesn't exist, check if parent ID exists and insert if necessary
            if (!ifExist(3, taskId, ""))                                
            {
                if (!ifExist(2, projectId, ""))
                {
                    if (!ifExist(1, workspaceId, ""))
                    {
                        insertClockifyInfo(1, workspaceName, workspaceId, "");
                    }
                    insertClockifyInfo(2, projectName, projectId, workspaceId);
                }
                insertClockifyInfo(3, taskName, taskId, projectId);
            }
            

            IntializeDB();

            if (type == 1)
                if (!ifExist(4, projectId, value))                 //insert rule only if it doesn't exist in current task
                    query = "INSERT INTO `Processes` (`Name`, `TaskID`, `projectId`) VALUES('" + value + "', '" + taskId + "', '" + projectId + "')";
                else
                {
                    existedTaskId = queryTaskId(1, value, projectId);
                    existedTaskName = queryTaskName(existedTaskId, projectId);
                    MessageBox.Show("'" + value + "' already exist in task '" + existedTaskName + "'");
                    return;
                }
            else if (type == 2)
                if (!ifExist(5, projectId, value))
                    query = "INSERT INTO `URLs` (`URL`, `TaskID`, `projectId`) VALUES('" + value + "', '" + taskId + "', '" + projectId + "')";
                else
                {
                    existedTaskId = queryTaskId(2, value, projectId);
                    existedTaskName = queryTaskName(existedTaskId, projectId);
                    MessageBox.Show("'" + value + "' already exist in task '" + existedTaskName + "'");
                    return;
                }
                    

            MySqlCommand cmd = new MySqlCommand(query, dbConn);
            dbConn.Open();                                      //opens connection
            
            cmd.ExecuteNonQuery();                              //makes the query
            dbConn.Close();
        }

        //queries a taskId from table 'processes' or 'url'
        public static string queryTaskId(int type, string value, string projectId)
        {
            IntializeDB();
            string taskId = string.Empty;
            string query = string.Empty;

            if (type == 1)
                query = "SELECT Processes.TaskID FROM Processes WHERE Processes.Name = '" + value + "' AND Processes.ProjectID = '" + projectId + "'";
            else if (type == 2)
                query = "SELECT URLs.TaskID FROM URLs WHERE URLs.URL = '" + value + "' AND URLs.ProjectID = '" + projectId + "'";


            MySqlCommand cmd = new MySqlCommand(query, dbConn);

            dbConn.Open();                                      //opens connection

            MySqlDataReader reader = cmd.ExecuteReader();       //makes the query

            reader.Read();
            taskId = reader[0].ToString();

            dbConn.Close();
            return taskId;
        }


        //queries a task name
        public static string queryTaskName(string taskId, string projectId)
        {
            IntializeDB();
            string taskName = string.Empty;
            string query = "SELECT Tasks.Name FROM Tasks WHERE Tasks.ID = '" + taskId + "' AND Tasks.ProjectID = '" + projectId + "'";

            MySqlCommand cmd = new MySqlCommand(query, dbConn);

            dbConn.Open();                                      //opens connection

            MySqlDataReader reader = cmd.ExecuteReader();       //makes the query

            reader.Read();
            taskName = reader[0].ToString();
            
            dbConn.Close();
            return taskName;
        }

        //check if entry already exists
        public static bool ifExist(int type, string projectId, string value)
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
                query = "SELECT EXISTS (SELECT * FROM " + table + " WHERE ID = '" + projectId + "') as `is-exists`";
            else if (type == 4)
                query = "SELECT EXISTS(SELECT * FROM Processes WHERE Name = '" + value + "' AND projectId = '" + projectId + "') as `is -exists`";
            else if (type == 5)
                query = "SELECT EXISTS(SELECT * FROM URLs WHERE URL = '" + value + "' AND projectId = '" + projectId + "') as `is -exists`";


            MySqlCommand cmd = new MySqlCommand(query, dbConn);
            dbConn.Open();                                      //opens connection

            MySqlDataReader reader = cmd.ExecuteReader();       //makes the query

            reader.Read();                                      //read next (only has one element for this query)

            if (reader[0].ToString().Equals("1"))
            {
                dbConn.Close();
                return true;
            }
                

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
