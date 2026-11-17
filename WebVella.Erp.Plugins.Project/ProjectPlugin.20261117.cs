using System;
using System.Collections.Generic;
using System.Linq;
using WebVella.Erp.Api;

namespace WebVella.Erp.Plugins.Project
{
	public partial class ProjectPlugin : ErpPlugin
	{
		private static readonly Guid ProjectTasksTargetDateNodeId = new Guid("e83f6542-f9f8-4fec-aeb8-48731951f182");

		//the "target date" column of these task lists was bound to created_on or the removed target_date field
		public static readonly IReadOnlyList<Guid> TaskListTargetDateNodeIds = new[]
		{
			new Guid("ad9c357f-e620-4ed1-9593-d76c97019677"), //all
			new Guid("bd05d5ef-0ab4-48b0-a40e-5959875d071b"), //open
			new Guid("8d244aa4-ad6b-464f-87e9-37a55fd18d19"), //no-owner
			ProjectTasksTargetDateNodeId,
		};

		public static string TaskListTargetDateOptions(Guid nodeId)
		{
			var emptyValue = nodeId == ProjectTasksTargetDateNodeId ? "n/a" : "";
			return @"{
  ""is_visible"": """",
  ""label_mode"": ""3"",
  ""label_text"": """",
  ""mode"": ""4"",
  ""value"": ""{\""type\"":\""0\"",\""string\"":\""RowRecord.end_time\"",\""default\"":\""" + emptyValue + @"\""}"",
  ""name"": ""end_time"",
  ""class"": """",
  ""show_icon"": ""false"",
  ""connected_entity_id"": """",
  ""connected_record_id_ds"": """",
  ""access_override_ds"": """",
  ""required_override_ds"": """",
  ""ajax_api_url_ds"": """"
}";
		}

		//an admin may have deleted a cell; skip it instead of failing startup
		public static Dictionary<Guid, string> TaskListTargetDateUpdates(IEnumerable<Guid> existingNodeIds)
		{
			var existing = new HashSet<Guid>(existingNodeIds);
			return TaskListTargetDateNodeIds.Where(existing.Contains).ToDictionary(id => id, TaskListTargetDateOptions);
		}

		private static void Patch20261117(EntityManager entMan, EntityRelationManager relMan, RecordManager recMan)
		{
			var pageService = new WebVella.Erp.Web.Services.PageService();
			var existingNodeIds = pageService.GetAllBodyNodes().Select(x => x.Id);
			foreach (var update in TaskListTargetDateUpdates(existingNodeIds))
				pageService.UpdatePageBodyNodeOptions(update.Key, update.Value, WebVella.Erp.Database.DbContext.Current.Transaction);
		}
	}
}
