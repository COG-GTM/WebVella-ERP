using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using WebVella.Erp.Api;
using WebVella.Erp.Api.Models;
using WebVella.Erp.Eql;
using WebVella.Erp.Plugins.Project.Services;
using WebVella.Erp.Web.Models;

namespace WebVella.Erp.Plugins.Project.DataSource
{
	public class TrackTimeTasks : CodeDataSource
	{
		public TrackTimeTasks() : base()
		{
			Id = new Guid("473EE9B6-2131-4164-B5FE-D9B3073E9178");
			Name = "WvProjectTrackTimeTasks";
			Description = "Current User's open tasks and track time info";
			ResultModel = "List<EntityRecord>";

			Parameters.Add(new DataSourceParameter { Name = "search_query", Type = "text", Value = "" });
		}

		public override object Execute(Dictionary<string, object> arguments)
		{
			var currentUser = SecurityContext.CurrentUser;
			if (currentUser == null)
				return null;

			var userOpenTasks = new TaskService().GetTaskQueue(null,currentUser.Id,Model.TasksDueType.StartTimeDue, includeProjectData: true);

			var userTimelogs = new List<EntityRecord>();

			{
				var eqlCommand = "SELECT * from timelog WHERE created_by = @userId";
				var eqlParams = new List<EqlParameter>() { new EqlParameter("userId", currentUser.Id) };
				userTimelogs = new EqlCommand(eqlCommand, eqlParams).Execute();
			}

			var timelogIndexesByRelatedId = new Dictionary<string, List<int>>(StringComparer.Ordinal);
			var unindexedTimelogIndexes = new List<int>();
			for (int i = 0; i < userTimelogs.Count; i++)
			{
				var relatedIds = ParseRelatedIds(userTimelogs[i]["l_related_records"] == null ? "" : (string)userTimelogs[i]["l_related_records"]);
				if (relatedIds == null)
				{
					unindexedTimelogIndexes.Add(i);
					continue;
				}
				foreach (var relatedId in relatedIds)
				{
					if (!timelogIndexesByRelatedId.TryGetValue(relatedId, out var indexes))
						timelogIndexesByRelatedId[relatedId] = indexes = new List<int>();
					indexes.Add(i);
				}
			}

			foreach (var task in userOpenTasks)
			{
				task.Properties.Add("logged_minutes", (int)0);
				task.Properties.Add("last_logged_on", (DateTime)DateTime.MinValue);
				var taskId = task["id"].ToString();
				var taskTimelogIndexes = timelogIndexesByRelatedId.TryGetValue(taskId, out var indexedTimelogs) ? indexedTimelogs : new List<int>();
				if (unindexedTimelogIndexes.Count > 0)
				{
					taskTimelogIndexes = taskTimelogIndexes
						.Concat(unindexedTimelogIndexes.Where(i => (userTimelogs[i]["l_related_records"] == null ? "" : (string)userTimelogs[i]["l_related_records"]).Contains(taskId)))
						.OrderBy(i => i).ToList();
				}
				var taskTimelogs = taskTimelogIndexes.Select(i => userTimelogs[i]).ToList();
				if (taskTimelogs.Count > 0)
				{
					task["logged_minutes"] = (int)(taskTimelogs.Select(x => (decimal)x["minutes"]).Aggregate(func: (aggrResult, x) => aggrResult + x));
					var lastLog = taskTimelogs.OrderByDescending(x => (DateTime)x["created_on"]).FirstOrDefault();
					if (lastLog != null)
						task["last_logged_on"] = (DateTime)lastLog["created_on"];
				}
				task["is_billable"] = true;
				if (task.Properties.ContainsKey("$project_nn_task") && task["$project_nn_task"] != null && task["$project_nn_task"] is List<EntityRecord>
					&& ((List<EntityRecord>)task["$project_nn_task"]).Any()) {

					var firstRecord = ((List<EntityRecord>)task["$project_nn_task"])[0];
					if (firstRecord.Properties.ContainsKey("is_billable") && firstRecord["is_billable"] != null && firstRecord["is_billable"] is Boolean) {
						task["is_billable"] = (bool)firstRecord["is_billable"];
					}
				}
			}

			return userOpenTasks.OrderByDescending(x => (DateTime?)x["created_on"]).ToList();
		}

		//Distinct ids of a JSON array of 36-char GUID strings with no escapes or comments, where a substring match on the raw text
		//is the same as an exact match on an element; null for anything else, which keeps the substring match
		private static HashSet<string> ParseRelatedIds(string relatedRecords)
		{
			if (relatedRecords.Length == 0)
				return new HashSet<string>(StringComparer.Ordinal);
			if (!relatedRecords.All(c => Uri.IsHexDigit(c) || c == '-' || c == '"' || c == '\'' || c == '[' || c == ']' || c == ',' || char.IsWhiteSpace(c)))
				return null;

			List<string> ids;
			try
			{
				ids = JsonConvert.DeserializeObject<List<string>>(relatedRecords);
			}
			catch (JsonException)
			{
				return null;
			}
			if (ids == null || ids.Any(id => id == null || id.Length != 36 || !id.All(c => c == '-' || Uri.IsHexDigit(c))))
				return null;

			return new HashSet<string>(ids, StringComparer.Ordinal);
		}
	}
}
