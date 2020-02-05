using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace WindowsFormsApp2
{
    public class Api
    {
        //login using x-auth-token
        public static dynamic Login(string un, string pw)
        {
            var client = new Rest()
            {
                username = un,
                password = pw,
                httpMethod = HttpVerb.POST,
                endpoint = "https://api.clockify.me/api/auth/token"
            };

            var dto = new Dto.AuthenticationRequest()
            {
                email = un,
                password = pw
            };

            client.body = new JavaScriptSerializer().Serialize(dto);
            var response = client.MakeRequest();

            //MessageBox.Show(Response);

            return JsonConvert.DeserializeObject<Dto.AuthResponse>(response);
        }

        //add new time entry
        public static dynamic AddTimeEntry(DateTime start, DateTime end, string description, string workspaceId, string projectId, string taskId)
        {
            var client = new Rest()
            {
                httpMethod = HttpVerb.POST,
                token = Global.token,
                endpoint = "https://api.clockify.me/api/workspaces/" + workspaceId + "/timeEntries/"
            };

            var dto = new Dto.Jsontimeentry()
            {
                billable = "true",
                start = start.ToString("yyyy-MM-ddTHH:mm:ss.fff") + "Z",
                description = description,
                projectId = projectId,
                taskId = taskId,
                end = end.ToString("yyyy-MM-ddTHH:mm:ss.fff") + "Z"
            };

            client.body = new JavaScriptSerializer().Serialize(dto);
            var response = client.MakeRequest();


            try
            {
                return JsonConvert.DeserializeObject(response);                     //returns a deserialized response object

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
                MessageBox.Show(response);
                MessageBox.Show(Global.projectId + " " + taskId);
            }

            return null;
        }

        //update time entry by entryID
        public static void UpdateTimeEntry(DateTime start, DateTime end, string description, string entryId, string workspaceId, string projectId, string taskId)
        {
            var client = new Rest()
            {
                httpMethod = HttpVerb.PUT,
                token = Global.token,
                endpoint = "https://api.clockify.me/api/workspaces/" + workspaceId + "/timeEntries/" + entryId
            };

            var dto = new Dto.Jsontimeentry()
            {
                billable = "true",
                start = start.ToString("yyyy-MM-ddTHH:mm:ss.fff") + "Z",
                description = description,
                projectId = projectId,
                taskId = taskId,
                end = end.ToString("yyyy-MM-ddTHH:mm:ss.fff") + "Z"
            };

            client.body = new JavaScriptSerializer().Serialize(dto);
            var response = client.MakeRequest();

            return;
        }

        //Find all time entries on workspace
        public static dynamic FindTimeEntriesByWorkspace(string workspaceId)
        {
            var client = new Rest()
            {
                httpMethod = HttpVerb.GET,
                token = Global.token,
                endpoint = "https://api.clockify.me/api/workspaces/" + workspaceId + "/timeEntries/?page=0"
            };

            var response = client.MakeRequest();

            //MessageBox.Show(Response);

            return JsonConvert.DeserializeObject<List<Dto.TimeEntryFullDto>>(response);
        }

        //delete time entry within a workspace
        public static void DeleteTimeEntry(string workspaceId, string entryId)
        {
            var client = new Rest()
            {
                httpMethod = HttpVerb.DELETE,
                token = Global.token,
                endpoint = "https://api.clockify.me/api/workspaces/" + workspaceId + "/timeEntries/" + entryId
            };

            var response = client.MakeRequest();

            return;
        }

        //get all projects from workspace ID
        public static dynamic GetProjectsByWorkspaceId(string workspaceId)
        {
            var client = new Rest()
            {
                httpMethod = HttpVerb.GET,
                token = Global.token,
                endpoint = "https://api.clockify.me/api/workspaces/" + workspaceId + "/projects/"
            };

            var response = client.MakeRequest();

            return JsonConvert.DeserializeObject<List<Dto.ProjectFullDto>>(response);
        }

        //get all tasks by project ID within a workspace
        public static dynamic GetTasksByProjectId(string workspaceId, string projectId)
        {
            var client = new Rest()
            {
                httpMethod = HttpVerb.GET,
                token = Global.token,
                endpoint = "https://api.clockify.me/api/workspaces/" + workspaceId + "/projects/" + projectId + "/tasks/"
            };

            var response = client.MakeRequest();

            return JsonConvert.DeserializeObject<List<Dto.TaskDto>>(response);
        }

        //get all workspace
        public static dynamic GetWorkspaces()
        {
            var client = new Rest()
            {
                httpMethod = HttpVerb.GET,
                token = Global.token,
                endpoint = "https://api.clockify.me/api/workspaces/"
            };

            var response = client.MakeRequest();

            return JsonConvert.DeserializeObject<List<Dto.WorkspaceDto>>(response);
        }

        //add task to project
        public static dynamic AddTaskByProjectId(string workspaceId, string projectId, string taskName)
        {
            var client = new Rest()
            {
                httpMethod = HttpVerb.POST,
                token = Global.token,
                endpoint = "https://api.clockify.me/api/workspaces/" + workspaceId + "/projects/" + projectId + "/tasks/"
            };

            var dto = new Dto.TaskRequest()
            {
                name = taskName,
                projectId = projectId
            };

            client.body = new JavaScriptSerializer().Serialize(dto);

            var response = client.MakeRequest();

            return JsonConvert.DeserializeObject<Dto.TaskDto>(response);                     //returns a deserialized response object
        }


    }
}
