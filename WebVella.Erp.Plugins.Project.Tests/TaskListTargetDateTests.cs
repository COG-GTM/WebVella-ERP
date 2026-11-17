using System;
using System.Linq;
using Newtonsoft.Json.Linq;
using Xunit;

namespace WebVella.Erp.Plugins.Project.Tests
{
	public class TaskListTargetDateTests
	{
		private static readonly Guid AllTasks = new Guid("ad9c357f-e620-4ed1-9593-d76c97019677");
		private static readonly Guid OpenTasks = new Guid("bd05d5ef-0ab4-48b0-a40e-5959875d071b");
		private static readonly Guid NoOwnerTasks = new Guid("8d244aa4-ad6b-464f-87e9-37a55fd18d19");
		private static readonly Guid ProjectTasks = new Guid("e83f6542-f9f8-4fec-aeb8-48731951f182");

		private static JObject Value(string options) => JObject.Parse((string)JObject.Parse(options)["value"]);

		[Fact]
		public void EveryTaskList_ShowsTheTaskEndDate()
		{
			var updates = ProjectPlugin.TaskListTargetDateUpdates(new[] { AllTasks, OpenTasks, NoOwnerTasks, ProjectTasks });

			Assert.Equal(4, updates.Count);
			foreach (var options in updates.Values)
			{
				Assert.Equal("RowRecord.end_time", (string)Value(options)["string"]);
				Assert.Equal("end_time", (string)JObject.Parse(options)["name"]);
			}
		}

		[Fact]
		public void ProjectTasksTab_KeepsNaForTasksWithNoEndDate()
		{
			Assert.Equal("n/a", (string)Value(ProjectPlugin.TaskListTargetDateOptions(ProjectTasks))["default"]);
			Assert.Equal("", (string)Value(ProjectPlugin.TaskListTargetDateOptions(AllTasks))["default"]);
		}

		[Fact]
		public void MissingCell_IsSkipped()
		{
			var updates = ProjectPlugin.TaskListTargetDateUpdates(new[] { AllTasks, ProjectTasks, Guid.NewGuid() });

			Assert.Equal(new[] { AllTasks, ProjectTasks }.OrderBy(x => x), updates.Keys.OrderBy(x => x));
		}
	}
}
