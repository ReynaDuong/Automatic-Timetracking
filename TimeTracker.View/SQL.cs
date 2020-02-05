using System.Collections.Generic;
using System.Windows.Forms;
using MySql.Data.MySqlClient;

namespace TimeTracker.View
{
    public class Sql
    {
        private static string _server = "trackerdb.servebeer.com";
        private static string _database = "mydb";
        private static string _userId = "student";
        private static string _password = "student";
        private static MySqlConnection _dbConn;

        //initialize database parameters
        private static void IntializeDb()
        {
            var builder = new MySqlConnectionStringBuilder();
            builder.Server = _server;
            builder.Database = _database;
            builder.UserID = _userId;
            builder.Password = _password;

            var connString = builder.ToString();

            builder = null;
            _dbConn = new MySqlConnection(connString);
        }

        //load processes associations of a particular task
        public static List<string> LoadProcesses(string taskId)
        {
            IntializeDb();
            var query = "SELECT * FROM Processes WHERE TaskID = " + "'" + taskId + "'";

            var cmd = new MySqlCommand(query, _dbConn);

            _dbConn.Open();                                      //opens connection
            var reader = cmd.ExecuteReader();       //makes the query

            var processes = new List<string>();
            while(reader.Read())
            {
                processes.Add(reader[0].ToString());
            }

            _dbConn.Close();
            return processes;
        }

        //load URLs associations of a particular task
        public static List<string> LoadUrls(string taskId)
        {
            IntializeDb();
            var query = "SELECT * FROM URLs WHERE TaskID = " + "'" + taskId + "'";

            var cmd = new MySqlCommand(query, _dbConn);

            _dbConn.Open();                                      //opens connection
            var reader = cmd.ExecuteReader();       //makes the query

            var urls = new List<string>();
            while (reader.Read())
            {
                urls.Add(reader[0].ToString());
            }

            _dbConn.Close();
            return urls;
        }

        //load process(1), or URL(2) association rules
        public static List<Association> LoadAssociations(int type, string projectId)
        {
            IntializeDb();
            var query = string.Empty;

            if (type == 1)
            {
	            query = "SELECT * FROM mydb.Processes WHERE projectId = " + "'" + projectId + "'";
            }
            else if (type == 2)
            {
	            query = "SELECT * FROM mydb.URLs WHERE projectId = " + "'" + projectId + "'";
            }


            var cmd = new MySqlCommand(query, _dbConn);

            _dbConn.Open();                                      //opens connection
            var reader = cmd.ExecuteReader();       //makes the query

            var rules = new List<Association>();

            //perform value->taskId association
            try
            {
                while (reader.Read())
                {
                    //MessageBox.Show(reader[0].ToString());
                    var r = new Association();
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


            _dbConn.Close();
            return rules;
        }

        //insert process(1) or URL(2), also inserts workspaceId, projectId, or tasksID if they don't exist  
        public static void InsertRule(int type, string value, string workspaceId, string projectId, string taskId, string workspaceName, string projectName, string taskName)
        {
            var query = string.Empty;
            var existedTaskName = string.Empty;
            var existedTaskId = string.Empty;

            if (value.Equals(""))
            {
	            return;
            }

            //if task doesn't exist, check if parent ID exists and insert if necessary
            if (!IfExist(3, taskId, ""))                                
            {
                if (!IfExist(2, projectId, ""))
                {
                    if (!IfExist(1, workspaceId, ""))
                    {
                        InsertClockifyInfo(1, workspaceName, workspaceId, "");
                    }
                    InsertClockifyInfo(2, projectName, projectId, workspaceId);
                }
                InsertClockifyInfo(3, taskName, taskId, projectId);
            }
            

            IntializeDb();

            if (type == 1)
            {
	            if (!IfExist(4, projectId, value))                 //insert rule only if it doesn't exist in current task
	            {
		            query = "INSERT INTO `Processes` (`Name`, `TaskID`, `projectId`) VALUES('" + value + "', '" + taskId + "', '" + projectId + "')";
	            }
	            else
	            {
		            existedTaskId = QueryTaskId(1, value, projectId);
		            existedTaskName = QueryTaskName(existedTaskId, projectId);
		            MessageBox.Show("'" + value + "' already exist in task '" + existedTaskName + "'");
		            return;
	            }
            }
            else if (type == 2)
            {
	            if (!IfExist(5, projectId, value))
	            {
		            query = "INSERT INTO `URLs` (`URL`, `TaskID`, `projectId`) VALUES('" + value + "', '" + taskId + "', '" + projectId + "')";
	            }
	            else
	            {
		            existedTaskId = QueryTaskId(2, value, projectId);
		            existedTaskName = QueryTaskName(existedTaskId, projectId);
		            MessageBox.Show("'" + value + "' already exist in task '" + existedTaskName + "'");
		            return;
	            }
            }


            var cmd = new MySqlCommand(query, _dbConn);
            _dbConn.Open();                                      //opens connection
            
            cmd.ExecuteNonQuery();                              //makes the query
            _dbConn.Close();
        }

        //queries a taskId from table 'processes' or 'url'
        public static string QueryTaskId(int type, string value, string projectId)
        {
            IntializeDb();
            var taskId = string.Empty;
            var query = string.Empty;

            if (type == 1)
            {
	            query = "SELECT Processes.TaskID FROM Processes WHERE Processes.Name = '" + value + "' AND Processes.ProjectID = '" + projectId + "'";
            }
            else if (type == 2)
            {
	            query = "SELECT URLs.TaskID FROM URLs WHERE URLs.URL = '" + value + "' AND URLs.ProjectID = '" + projectId + "'";
            }


            var cmd = new MySqlCommand(query, _dbConn);

            _dbConn.Open();                                      //opens connection

            var reader = cmd.ExecuteReader();       //makes the query

            reader.Read();
            taskId = reader[0].ToString();

            _dbConn.Close();
            return taskId;
        }


        //queries a task name
        public static string QueryTaskName(string taskId, string projectId)
        {
            IntializeDb();
            var taskName = string.Empty;
            var query = "SELECT Tasks.Name FROM Tasks WHERE Tasks.ID = '" + taskId + "' AND Tasks.ProjectID = '" + projectId + "'";

            var cmd = new MySqlCommand(query, _dbConn);

            _dbConn.Open();                                      //opens connection

            var reader = cmd.ExecuteReader();       //makes the query

            reader.Read();
            taskName = reader[0].ToString();
            
            _dbConn.Close();
            return taskName;
        }

        //check if entry already exists
        public static bool IfExist(int type, string projectId, string value)
        {
            IntializeDb();
            var table = string.Empty;
            var query = string.Empty;

            if (type == 1)
            {
	            table = "Workspaces";
            }
            else if (type == 2)
            {
	            table = "Projects";
            }
            else if (type == 3)
            {
	            table = "Tasks";
            }
            else if (type == 4)
            {
	            table = "Processes";
            }
            else if (type == 5)
            {
	            table = "URLss";
            }

            if (type < 4)
            {
	            query = "SELECT EXISTS (SELECT * FROM " + table + " WHERE ID = '" + projectId + "') as `is-exists`";
            }
            else if (type == 4)
            {
	            query = "SELECT EXISTS(SELECT * FROM Processes WHERE Name = '" + value + "' AND projectId = '" + projectId + "') as `is -exists`";
            }
            else if (type == 5)
            {
	            query = "SELECT EXISTS(SELECT * FROM URLs WHERE URL = '" + value + "' AND projectId = '" + projectId + "') as `is -exists`";
            }


            var cmd = new MySqlCommand(query, _dbConn);
            _dbConn.Open();                                      //opens connection

            var reader = cmd.ExecuteReader();       //makes the query

            reader.Read();                                      //read next (only has one element for this query)

            if (reader[0].ToString().Equals("1"))
            {
                _dbConn.Close();
                return true;
            }
                

            _dbConn.Close();
            return false; ;
        }

        //inserts workspace, project, or task
        private static void InsertClockifyInfo(int type, string name, string primary, string foreign)
        {
            IntializeDb();
            var query = string.Empty;

            // workspace
            if (type == 1)
            {
	            query = "INSERT INTO Workspaces (`Name`, `ID`) VALUES('" + name + "', '" + primary + "')";
            }
            else if (type == 2)
            {
                // project
	            query = "INSERT INTO `Projects` (`Name`, `ID`, `workspaceId`) VALUES('" + name + "', '" + primary +
	                    "', '" + foreign + "')";
            }
            else if (type == 3)
            {
                // tasks
	            query = "INSERT INTO `Tasks` (`Name`, `ID`, `projectId`) VALUES ('" + name + "', '" + primary + "', '" +
	                    foreign + "')";
            }

            var cmd = new MySqlCommand(query, _dbConn);
            _dbConn.Open();                                      //opens connection

            cmd.ExecuteNonQuery();                              //makes the query
            _dbConn.Close();
        }

        //delete a process or url
        public static void Delete(int type, string value, string taskId)
        {
            IntializeDb();
            var table = string.Empty;
            var column = string.Empty;
            var query = string.Empty;

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
                

            query = "DELETE FROM " + table + " WHERE " + column + " = '" + value + "' AND TaskID = '" + taskId + "'";

            var cmd = new MySqlCommand(query, _dbConn);
            _dbConn.Open();                                      //opens connection

            cmd.ExecuteNonQuery();                              //makes the query
            _dbConn.Close();
        }
    }





        
    
}
