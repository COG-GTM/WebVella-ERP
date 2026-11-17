using System;
using System.Collections.Generic;
using System.Linq;
using Npgsql;
using WebVella.Erp.Api;

namespace WebVella.Erp.Plugins.Project
{
	public partial class ProjectPlugin : ErpPlugin
	{
		//the "target date" column of these task lists was bound to created_on or the removed target_date field
		public static readonly IReadOnlyList<Guid> TaskListTargetDateNodeIds = new[]
		{
			new Guid("ad9c357f-e620-4ed1-9593-d76c97019677"), //all
			new Guid("bd05d5ef-0ab4-48b0-a40e-5959875d071b"), //open
			new Guid("8d244aa4-ad6b-464f-87e9-37a55fd18d19"), //no-owner
			new Guid("e83f6542-f9f8-4fec-aeb8-48731951f182"), //project tasks
		};

		public const string TaskListTargetDateOptions = @"{
  ""is_visible"": """",
  ""label_mode"": ""3"",
  ""label_text"": """",
  ""mode"": ""4"",
  ""value"": ""{\""type\"":\""0\"",\""string\"":\""RowRecord.end_time\"",\""default\"":\""\""}"",
  ""name"": ""end_time"",
  ""class"": """",
  ""show_icon"": ""false"",
  ""connected_entity_id"": """",
  ""connected_record_id_ds"": """",
  ""access_override_ds"": """",
  ""required_override_ds"": """",
  ""ajax_api_url_ds"": """"
}";

		//runs inside the startup transaction, so on a fresh install it sees the cells earlier patches created;
		//cells an admin deleted are skipped instead of failing startup
		public static int UpdateTaskListTargetDateColumns(NpgsqlTransaction transaction)
		{
			using var command = new NpgsqlCommand("UPDATE public.app_page_body_node SET options = @options WHERE id = ANY(@ids)", transaction.Connection, transaction);
			command.Parameters.AddWithValue("@options", TaskListTargetDateOptions);
			command.Parameters.AddWithValue("@ids", TaskListTargetDateNodeIds.ToArray());
			return command.ExecuteNonQuery();
		}

		private static void Patch20261117(EntityManager entMan, EntityRelationManager relMan, RecordManager recMan)
		{
			UpdateTaskListTargetDateColumns(WebVella.Erp.Database.DbContext.Current.Transaction);
			new WebVella.Erp.Web.Services.PageService().ClearPagesCache();
		}
	}
}
