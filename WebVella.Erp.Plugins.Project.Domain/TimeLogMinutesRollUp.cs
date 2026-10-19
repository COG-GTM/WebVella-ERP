using System;
using System.Collections.Generic;

namespace WebVella.Erp.Plugins.Project.Domain
{
	public static class TimeLogMinutesRollUp
	{
		public const string BillableMinutesField = "x_billable_minutes";
		public const string NonBillableMinutesField = "x_nonbillable_minutes";
		public const string TimeLogStartedOnField = "timelog_started_on";

		public static string GetMinutesField(bool isBillable)
		{
			return isBillable ? BillableMinutesField : NonBillableMinutesField;
		}

		public static string GetLoggedTypeLabel(bool isBillable)
		{
			return isBillable ? "billable" : "non-billable";
		}

		public static decimal AddMinutes(decimal taskMinutes, int loggedMinutes)
		{
			return taskMinutes + loggedMinutes;
		}

		public static decimal RemoveMinutes(decimal taskMinutes, decimal loggedMinutes)
		{
			var result = Math.Round(taskMinutes - loggedMinutes);
			if (result > 0)
				return result;
			return 0;
		}

		//Task patch applied when a project timelog is created
		public static Dictionary<string, object> BuildCreatePatch(Guid taskId, bool isBillable, decimal taskMinutes, int loggedMinutes)
		{
			var patch = new Dictionary<string, object>();
			patch["id"] = taskId;
			patch[GetMinutesField(isBillable)] = AddMinutes(taskMinutes, loggedMinutes);
			patch[TimeLogStartedOnField] = null;
			return patch;
		}

		//Task patch applied when a project timelog is deleted
		public static Dictionary<string, object> BuildDeletePatch(Guid taskId, bool isBillable, decimal taskMinutes, decimal loggedMinutes)
		{
			var patch = new Dictionary<string, object>();
			patch["id"] = taskId;
			patch[GetMinutesField(isBillable)] = RemoveMinutes(taskMinutes, loggedMinutes);
			return patch;
		}
	}
}
