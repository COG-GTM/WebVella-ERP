using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace WebVella.Erp.Plugins.Project.Domain
{
	public static class TimeLogRules
	{
		public const string ProjectsScope = "projects";

		public static void EnsureIdFieldPresent(bool hasIdField)
		{
			if (!hasIdField)
				throw new Exception("Hook exception: timelog id field not found in record");
		}

		public static void EnsureTimeLogFound(bool timeLogFound)
		{
			if (!timeLogFound)
				throw new Exception("Hook exception: timelog with this id was not found");
		}

		public static void EnsureRelatedTaskFound(bool relatedTaskFound)
		{
			if (!relatedTaskFound)
				throw new Exception("Hook exception: This timelog does not have an existing taskId");
		}

		public static void EnsureRecordFound(bool recordFound)
		{
			if (!recordFound)
				throw new Exception("RecordId not found");
		}

		public static void EnsureIsAuthor(Guid authorId, Guid currentUserId)
		{
			if (authorId != currentUserId)
				throw new Exception("Only the author can delete its comment");
		}

		//Plain substring match on the serialized l_scope value
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
	}
}
