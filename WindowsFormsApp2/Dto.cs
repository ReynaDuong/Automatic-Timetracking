using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WindowsFormsApp2
{
    public class Dto
    {
        public class ProjectFullDto
        {
            public bool archived { get; set; }
            public bool billable { get; set; }
            public ClientDto client { get; set; }
            public string clientId { get; set; }
            public string color { get; set; }
            public EstimateDto estimate { get; set; }
            public HourlyRateDto hourlyRate { get; set; }
            public string id { get; set; }
            public MembershipDto[] memberships { get; set; }
            public string name { get; set; }
            public bool @public { get; set; }
            public TaskDto[] tasks { get; set; }
            public string workspaceId { get; set; }
        }

        public class TaskDto
        {
            public string assigneeId { get; set; }
            public string estimate { get; set; }
            public string id { get; set; }
            public string name { get; set; }
            public string projectId { get; set; }
            public string status { get; set; }
        }

        public class HourlyRateDto
        {
            public int amount { get; set; }
            public string currency { get; set; }
        }

        public class ClientDto
        {
            public string id { get; set; }
            public string name { get; set; }
            public string workspaceId { get; set; }
        }

        public class MembershipDto
        {
            public HourlyRateDto hourlyRate { get; set; }
            public string membershipStatus { get; set; }
            public string membershipType { get; set; }
            public string target { get; set; }
            public string userId { get; set; }
        }

        public class EstimateDto
        {
            public string estimate { get; set; }
            public typeEnum type { get; set; }
        }

        public class JSONTIMEENTRY
        {
            public string start { get; set; }
            public string billable { get; set; }
            public string description { get; set; }
            public string projectId { get; set; }
            public string taskId { get; set; }
            public string end { get; set; }
            public string[] tagIds { get; set; }
        }

        public class TaskRequest
        {
            public string id;
            public string name;
            public string projectId;
            public string assigneeId;
            public string estimate;
            public string status;
        }

        public class TimeEntryFullDto
        {
            //public bool billable { get; set; }
            public string description { get; set; }
            //public HourlyRateDto hourlyRate { get; set; }
            public string id { get; set; }
            //public bool isLocked { get; set; }
            //public ProjectDtoImpl project { get; set; }
            //public string projectId { get; set; }
            //public TagDto[] tags { get; set; }
            //public TaskDto tasks { get; set; }
            //public TimeInvervalDto timeInterval { get; set; }
            //public uint totalBillable { get; set; }
            //public UserDto user { get; set; }
            //public string workspaceId { get; set; }
        }

        public class ProjectDtoImpl
        {
            public bool archived { get; set; }
            public bool billable { get; set; }
            public string clientId { get; set; }
            public string color { get; set; }
            public EstimateDto estimate { get; set; }
            public HourlyRateDto hourlyRate { get; set; }
            public string id { get; set; }
            public MembershipDto memberships { get; set; }
            public string name { get; set; }
            public bool @public { get; set; }
            public string workspaceId { get; set; }
        }

        public class UserDto
        {
            public string activeWorkspace { get; set; }
            public string defaultWorkspace { get; set; }
            public string email { get; set; }
            public string id { get; set; }
            public MembershipDto[] memberships { get; set; }
            public string name { get; set; }
            public string profilePicture { get; set; }
            public UserSettingsDto settings { get; set; }
            public statusEnum status {get; set;}
        }

        public class SummaryReportSettingsDto
        {
            public string group { get; set; }
            public string subgroup { get; set; }
        }

        public class UserSettingsDto
        {
            public string dateFormat { get; set; }
            public string isCompactViewOn { get; set; }
            public string longRunning { get; set; }
            public string sendNewsletter { get; set; }
            public SummaryReportSettingsDto summaryReportSettings { get; set; }
            public string timeFormat { get; set; }
            public string timeTrackingManual { get; set; }
            public string timeZone { get; set; }
            public weekEnum weekStart { get; set; }
            public string weeklyUpdates { get; set; }
        }


        public class TagDto
        {
            public string id { get; set; }
            public string name { get; set; }
            public string workspaceId { get; set; }
        }

        public class TimeInvervalDto
        {
            public string duration { get; set; }
            public string end { get; set; }
            public string start { get; set; }
        }

        public class WorkspaceDto
        {
            public HourlyRateDto hourlyRate { get; set; }
            public string id { get; set; }
            public string imageUrl { get; set; }
            public MembershipDto[] memberships { get; set; }
            public string name;
            public WorkspaceSettingsDto workspaceSettings { get; set; }
        }

        public class Round
        {
            public string minutes { get; set; }
            public string round { get; set; }
        }

        public class WorkspaceSettingsDto
        {
            public string canSeeTimeSheet { get; set; }
            public string defaultBillableProjects { get; set; }
            public string forceDescription { get; set; }
            public string forceProjects { get; set; }
            public string forceTags { get; set; }
            public string forceTasks { get; set; }
            public string lockTimeEntries { get; set; }
            public string onlyAdminsCreateProject { get; set; }
            public string onlyAdminsSeeAllTimeEntries { get; set; }
            public string onlyAdminsSeeBillableRates { get; set; }
            public string onlyAdminsSeeDashboard { get; set; }
            public string onlyAdminsSeePublicProjectsEntries { get; set; }
            public string projectFavorites { get; set; }
            public string projectPickerSpecialFilter { get; set; }
            public Round round { get; set; }
            public string timeRoundingInReports { get; set; }
        }

        public class AuthenticationRequest
        {
            public string email { get; set; }
            public string password { get; set; }
        }

        public class AuthResponse
        {
            public string email { get; set; }
            public string id { get; set; }
            public bool isNew { get; set; }
            public MembershipDto[] membership { get; set; }
            public string name { get; set; }
            public Boolean @new { get; set; }
            public string refreshToken { get; set; }
            public statusEnum status { get; set; }
            public string token { get; set; }
        }

        public enum taskDtoEnum
        {
            ACTIVE,
            DONE
        }

        public enum statusEnum
        {
            ACTIVE,
            PENDING_EMAIL_VERIFICATION,
            DELETED
        }

        public enum weekEnum
        {
            MONDAY,
            TUESDAY,
            WEDNESDAY,
            THURSDAY,
            FRIDAY,
            SATURDAY,
            SUNDAY
        }

        public enum typeEnum
        {
            AUTO,
            MANUAL
        }

        public enum membershipEnum
        {
            PENDING,
            ACTIVE,
            DECLINED,
            INACTIVE
        }

    }
}







