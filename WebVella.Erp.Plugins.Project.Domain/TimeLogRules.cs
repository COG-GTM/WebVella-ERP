using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace WebVella.Erp.Plugins.Project.Domain
{
	public static class TimeLogRules
	{
		public const string ProjectsScope = "projects";
		public const string BillableMinutesField = "x_billable_minutes";
		public const string NonBillableMinutesField = "x_nonbillable_minutes";
		public const string TimelogStartedOnField = "timelog_started_on";

		public const string IdFieldNotFoundMessage = "Hook exception: timelog id field not found in record";
		public const string TimeLogNotFoundMessage = "Hook exception: timelog with this id was not found";
		public const string RelatedTaskNotFoundMessage = "Hook exception: This timelog does not have an existing taskId";
		public const string RecordNotFoundMessage = "RecordId not found";
		public const string OnlyAuthorCanDeleteMessage = "Only the author can delete its comment";

		public static void RequireIdField(bool hasIdField)
		{
			if (!hasIdField)
				throw new Exception(IdFieldNotFoundMessage);
		}

		public static void RequireTimeLogFound(bool found)
		{
			if (!found)
				throw new Exception(TimeLogNotFoundMessage);
		}

		public static void RequireRelatedTask(bool found)
		{
			if (!found)
				throw new Exception(RelatedTaskNotFoundMessage);
		}

		public static void RequireRecordFound(bool found)
		{
			if (!found)
				throw new Exception(RecordNotFoundMessage);
		}

		public static void RequireAuthor(Guid createdBy, Guid currentUserId)
		{
			if (createdBy != currentUserId)
				throw new Exception(OnlyAuthorCanDeleteMessage);
		}

		public static bool IsProjectTimeLog(string scope)
		{
			return scope != null && scope.Contains(ProjectsScope);
		}

		public static bool HasRelatedRecords(string relatedRecords)
		{
			return relatedRecords != null && relatedRecords != "";
		}

		public static List<Guid> ParseRelatedRecordIds(string relatedRecords)
		{
			return JsonConvert.DeserializeObject<List<Guid>>(relatedRecords);
		}

		public static string GetMinutesField(bool isBillable)
		{
			return isBillable ? BillableMinutesField : NonBillableMinutesField;
		}

		public static string GetLoggedTypeLabel(bool isBillable)
		{
			return isBillable ? "billable" : "non-billable";
		}

		public static Dictionary<string, object> BuildTaskPatchOnCreate(Guid taskId, bool isBillable, decimal taskMinutes, int loggedMinutes)
		{
			return new Dictionary<string, object>
			{
				["id"] = taskId,
				[GetMinutesField(isBillable)] = taskMinutes + loggedMinutes,
				[TimelogStartedOnField] = null
			};
		}

		public static Dictionary<string, object> BuildTaskPatchOnDelete(Guid taskId, bool isBillable, decimal taskMinutes, decimal loggedMinutes)
		{
			var result = Math.Round(taskMinutes - loggedMinutes);
			return new Dictionary<string, object>
			{
				["id"] = taskId,
				[GetMinutesField(isBillable)] = result > 0 ? (object)result : 0
			};
		}
	}
}
