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

		public static void EnsureIdFieldPresent(bool hasIdField)
		{
			if (!hasIdField)
				throw new Exception("Hook exception: timelog id field not found in record");
		}

		public static void EnsureTimelogFound(bool timelogFound)
		{
			if (!timelogFound)
				throw new Exception("Hook exception: timelog with this id was not found");
		}

		public static void EnsureRelatedTaskFound(bool taskFound)
		{
			if (!taskFound)
				throw new Exception("Hook exception: This timelog does not have an existing taskId");
		}

		public static void EnsureRecordFound(bool recordFound)
		{
			if (!recordFound)
				throw new Exception("RecordId not found");
		}

		public static void EnsureIsAuthor(Guid createdBy, Guid currentUserId)
		{
			if (createdBy != currentUserId)
				throw new Exception("Only the author can delete its comment");
		}

		public static bool IsProjectTimeLog(string scope)
		{
			return scope != null && scope.Contains(ProjectsScope);
		}

		public static bool HasRelatedRecords(string relatedRecords)
		{
			return relatedRecords != null && relatedRecords != "";
		}

		public static List<Guid> ParseRelatedRecords(string relatedRecords)
		{
			return JsonConvert.DeserializeObject<List<Guid>>(relatedRecords);
		}

		public static string GetMinutesField(bool isBillable)
		{
			return isBillable ? BillableMinutesField : NonBillableMinutesField;
		}

		public static string GetLoggedType(bool isBillable)
		{
			return isBillable ? "billable" : "non-billable";
		}

		public static decimal AddLoggedMinutes(decimal taskMinutes, int loggedMinutes)
		{
			return taskMinutes + loggedMinutes;
		}

		public static decimal RemoveLoggedMinutes(decimal taskMinutes, decimal loggedMinutes)
		{
			var result = Math.Round(taskMinutes - loggedMinutes);
			if (result > 0)
				return result;
			return 0;
		}
	}
}
