using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace WindowsFormsApp2
{
    public class API
    {
        //login using x-auth-token
        public static dynamic login(string UN, string PW)
        {
            Rest client = new Rest()
            {
                Username = UN,
                Password = PW,
                httpMethod = httpVerb.POST,
                endpoint = "https://api.clockify.me/api/auth/token"
            };

            Dto.AuthenticationRequest dto = new Dto.AuthenticationRequest()
            {
                email = UN,
                password = PW
            };

            client.body = new JavaScriptSerializer().Serialize(dto);
            string Response = client.MakeRequest();

            //MessageBox.Show(Response);

            return JsonConvert.DeserializeObject<Dto.AuthResponse>(Response);
        }

        //add new time entry
        public static dynamic AddTimeEntry(DateTime start, DateTime end, string description, string workspaceId, string projectId, string taskId)
        {
            Rest client = new Rest()
            {
                httpMethod = httpVerb.POST,
                Token = Global.token,
                endpoint = "https://api.clockify.me/api/workspaces/" + workspaceId + "/timeEntries/"
            };

            Dto.JSONTIMEENTRY dto = new Dto.JSONTIMEENTRY()
            {
                billable = "true",
                start = start.ToString("yyyy-MM-ddTHH:mm:ss.fff") + "Z",
                description = description,
                projectId = projectId,
                taskId = taskId,
                end = end.ToString("yyyy-MM-ddTHH:mm:ss.fff") + "Z"
            };

            client.body = new JavaScriptSerializer().Serialize(dto);
            string Response = client.MakeRequest();


            try
            {
                return JsonConvert.DeserializeObject(Response);                     //returns a deserialized response object

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
                MessageBox.Show(Response);
                MessageBox.Show(Global.projectId + " " + taskId);
            }

            return null;
        }

        //update time entry by entryID
        public static void UpdateTimeEntry(DateTime start, DateTime end, string description, string entryId, string workspaceId, string projectId, string taskId)
        {
            Rest client = new Rest()
            {
                httpMethod = httpVerb.PUT,
                Token = Global.token,
                endpoint = "https://api.clockify.me/api/workspaces/" + workspaceId + "/timeEntries/" + entryId
            };

            Dto.JSONTIMEENTRY dto = new Dto.JSONTIMEENTRY()
            {
                billable = "true",
                start = start.ToString("yyyy-MM-ddTHH:mm:ss.fff") + "Z",
                description = description,
                projectId = projectId,
                taskId = taskId,
                end = end.ToString("yyyy-MM-ddTHH:mm:ss.fff") + "Z"
            };

            client.body = new JavaScriptSerializer().Serialize(dto);
            string Response = client.MakeRequest();

            return;
        }

        //Find all time entries on workspace
        public static dynamic FindTimeEntriesByWorkspace(string workspaceId)
        {
            Rest client = new Rest()
            {
                httpMethod = httpVerb.GET,
                Token = Global.token,
                endpoint = "https://api.clockify.me/api/workspaces/" + workspaceId + "/timeEntries/?page=0"
            };

            string Response = client.MakeRequest();

            //MessageBox.Show(Response);

            return JsonConvert.DeserializeObject<List<Dto.TimeEntryFullDto>>(Response);
        }

        //delete time entry within a workspace
        public static void DeleteTimeEntry(string workspaceId, string entryId)
        {
            Rest client = new Rest()
            {
                httpMethod = httpVerb.DELETE,
                Token = Global.token,
                endpoint = "https://api.clockify.me/api/workspaces/" + workspaceId + "/timeEntries/" + entryId
            };

            string Response = client.MakeRequest();

            return;
        }

        //get all projects from workspace ID
        public static dynamic getProjectsByWorkspaceId(string workspaceId)
        {
            Rest client = new Rest()
            {
                httpMethod = httpVerb.GET,
                Token = Global.token,
                endpoint = "https://api.clockify.me/api/workspaces/" + workspaceId + "/projects/"
            };

            string Response = client.MakeRequest();

            return JsonConvert.DeserializeObject<List<Dto.ProjectFullDto>>(Response);
        }

        //get all tasks by project ID within a workspace
        public static dynamic getTasksByProjectId(string workspaceId, string projectId)
        {
            Rest client = new Rest()
            {
                httpMethod = httpVerb.GET,
                Token = Global.token,
                endpoint = "https://api.clockify.me/api/workspaces/" + workspaceId + "/projects/" + projectId + "/tasks/"
            };

            string Response = client.MakeRequest();

            return JsonConvert.DeserializeObject<List<Dto.TaskDto>>(Response);
        }

        //get all workspace
        public static dynamic getWorkspaces()
        {
            Rest client = new Rest()
            {
                httpMethod = httpVerb.GET,
                Token = Global.token,
                endpoint = "https://api.clockify.me/api/workspaces/"
            };

            string Response = client.MakeRequest();

            return JsonConvert.DeserializeObject<List<Dto.WorkspaceDto>>(Response);
        }

        //add task to project
        public static dynamic addTaskByProjectId(string workspaceId, string projectId, string taskName)
        {
            Rest client = new Rest()
            {
                httpMethod = httpVerb.POST,
                Token = Global.token,
                endpoint = "https://api.clockify.me/api/workspaces/" + workspaceId + "/projects/" + projectId + "/tasks/"
            };

            Dto.TaskRequest dto = new Dto.TaskRequest()
            {
                name = taskName,
                projectId = projectId
            };

            client.body = new JavaScriptSerializer().Serialize(dto);

            string Response = client.MakeRequest();

            return JsonConvert.DeserializeObject<Dto.TaskDto>(Response);                     //returns a deserialized response object
        }


    }
}
