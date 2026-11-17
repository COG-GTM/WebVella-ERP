using System;
using Newtonsoft.Json.Linq;
using Xunit;

namespace WebVella.Erp.Plugins.Project.Tests
{
	public class TaskListTargetDateTests
	{
		[Fact]
		public void TargetDateColumn_ShowsTheTaskEndDate()
		{
			var options = JObject.Parse(ProjectPlugin.TaskListTargetDateOptions);
			var value = JObject.Parse((string)options["value"]);

			Assert.Equal("RowRecord.end_time", (string)value["string"]);
			Assert.Equal("end_time", (string)options["name"]);
		}

		[Theory]
		[InlineData("ad9c357f-e620-4ed1-9593-d76c97019677")] //all
		[InlineData("bd05d5ef-0ab4-48b0-a40e-5959875d071b")] //open
		[InlineData("8d244aa4-ad6b-464f-87e9-37a55fd18d19")] //no-owner
		[InlineData("e83f6542-f9f8-4fec-aeb8-48731951f182")] //project tasks
		public void EveryTaskList_GetsTheEndDateColumn(string nodeId)
		{
			Assert.Contains(new Guid(nodeId), ProjectPlugin.TaskListTargetDateNodeIds);
		}
	}
}
