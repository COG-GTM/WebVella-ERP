using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using WebVella.Erp.Plugins.Project.Services;
using Xunit;

namespace WebVella.Erp.Tests
{
	public class TaskServiceTests
	{
		private static readonly Guid ProjectId = new Guid("2d9b2d1d-e32b-45e1-a013-91d92a9ce792");

		public static IEnumerable<object[]> ProjectRelationValues()
		{
			yield return new object[] { new List<Guid> { ProjectId } };
			yield return new object[] { JArray.Parse($"[\"{ProjectId}\"]") };
			yield return new object[] { new List<string> { ProjectId.ToString() } };
			yield return new object[] { ProjectId.ToString() };
			yield return new object[] { new JValue(ProjectId.ToString()) };
			yield return new object[] { ProjectId };
		}

		[Theory]
		[MemberData(nameof(ProjectRelationValues))]
		public void ToGuidList_ReadsProjectRelationFromApiJson(object projectValue)
		{
			Assert.Equal(new List<Guid> { ProjectId }, TaskService.ToGuidList(projectValue));
		}

		[Fact]
		public void ToGuidList_KeepsEmptyAndMultipleCounts()
		{
			Assert.Empty(TaskService.ToGuidList(new JArray()));
			Assert.Empty(TaskService.ToGuidList(null));
			Assert.Equal(2, TaskService.ToGuidList(new JArray(ProjectId.ToString(), Guid.NewGuid().ToString())).Count);
		}
	}
}
