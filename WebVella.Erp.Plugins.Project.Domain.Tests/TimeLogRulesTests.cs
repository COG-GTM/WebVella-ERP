using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using Xunit;

namespace WebVella.Erp.Plugins.Project.Domain.Tests
{
	public class TimeLogRulesTests
	{
		private static readonly Guid TaskId = new Guid("3c1e8f0a-6b2d-4d6e-9f4a-0c7b5e2d1a90");
		private static readonly Guid ProjectId = new Guid("8a4f2b1c-9d3e-4f5a-b6c7-d8e9f0a1b2c3");

		#region << Required records / fields >>

		[Fact]
		public void RequireIdField_Present_DoesNotThrow()
		{
			TimeLogRules.RequireIdField(true);
		}

		[Fact]
		public void RequireIdField_Missing_ThrowsSameMessage()
		{
			var ex = Assert.Throws<Exception>(() => TimeLogRules.RequireIdField(false));
			Assert.Equal("Hook exception: timelog id field not found in record", ex.Message);
		}

		[Fact]
		public void RequireTimeLogFound_Found_DoesNotThrow()
		{
			TimeLogRules.RequireTimeLogFound(true);
		}

		[Fact]
		public void RequireTimeLogFound_NotFound_ThrowsSameMessage()
		{
			var ex = Assert.Throws<Exception>(() => TimeLogRules.RequireTimeLogFound(false));
			Assert.Equal("Hook exception: timelog with this id was not found", ex.Message);
		}

		[Fact]
		public void RequireRelatedTask_Found_DoesNotThrow()
		{
			TimeLogRules.RequireRelatedTask(true);
		}

		[Fact]
		public void RequireRelatedTask_NotFound_ThrowsSameMessage()
		{
			var ex = Assert.Throws<Exception>(() => TimeLogRules.RequireRelatedTask(false));
			Assert.Equal("Hook exception: This timelog does not have an existing taskId", ex.Message);
		}

		[Fact]
		public void RequireRecordFound_Found_DoesNotThrow()
		{
			TimeLogRules.RequireRecordFound(true);
		}

		[Fact]
		public void RequireRecordFound_NotFound_ThrowsSameMessage()
		{
			var ex = Assert.Throws<Exception>(() => TimeLogRules.RequireRecordFound(false));
			Assert.Equal("RecordId not found", ex.Message);
		}

		[Fact]
		public void RequireAuthor_SameUser_DoesNotThrow()
		{
			var userId = Guid.NewGuid();
			TimeLogRules.RequireAuthor(userId, userId);
		}

		[Fact]
		public void RequireAuthor_OtherUser_ThrowsSameMessage()
		{
			var ex = Assert.Throws<Exception>(() => TimeLogRules.RequireAuthor(Guid.NewGuid(), Guid.NewGuid()));
			Assert.Equal("Only the author can delete its comment", ex.Message);
		}

		#endregion

		#region << Scope >>

		[Theory]
		[InlineData("[\"projects\"]", true)]
		[InlineData("[\"crm\",\"projects\"]", true)]
		[InlineData("projects", true)]
		[InlineData("[\"my-projects-scope\"]", true)]
		[InlineData("[\"crm\"]", false)]
		[InlineData("[\"Projects\"]", false)]
		[InlineData("[\"project\"]", false)]
		[InlineData("[]", false)]
		[InlineData("", false)]
		[InlineData(null, false)]
		public void IsProjectTimeLog_IsCaseSensitiveSubstringMatch(string scope, bool expected)
		{
			Assert.Equal(expected, TimeLogRules.IsProjectTimeLog(scope));
		}

		#endregion

		#region << l_related_records >>

		[Theory]
		[InlineData(null, false)]
		[InlineData("", false)]
		[InlineData(" ", true)]
		[InlineData("[]", true)]
		[InlineData("null", true)]
		[InlineData("not-json", true)]
		public void HasRelatedRecords_OnlyNullOrEmptyCountsAsMissing(string relatedRecords, bool expected)
		{
			Assert.Equal(expected, TimeLogRules.HasRelatedRecords(relatedRecords));
		}

		[Fact]
		public void ParseRelatedRecordIds_SingleId()
		{
			Assert.Equal(new List<Guid> { TaskId }, TimeLogRules.ParseRelatedRecordIds($"[\"{TaskId}\"]"));
		}

		[Fact]
		public void ParseRelatedRecordIds_MultipleIds_KeepsOrder()
		{
			var json = JsonConvert.SerializeObject(new List<Guid> { TaskId, ProjectId });
			Assert.Equal(new List<Guid> { TaskId, ProjectId }, TimeLogRules.ParseRelatedRecordIds(json));
		}

		[Fact]
		public void ParseRelatedRecordIds_EmptyArray_ReturnsEmptyList()
		{
			var result = TimeLogRules.ParseRelatedRecordIds("[]");
			Assert.NotNull(result);
			Assert.Empty(result);
		}

		[Theory]
		[InlineData("null")]
		[InlineData("")]
		[InlineData("   ")]
		public void ParseRelatedRecordIds_NullOrBlankJson_ReturnsNull(string json)
		{
			Assert.Null(TimeLogRules.ParseRelatedRecordIds(json));
		}

		[Fact]
		public void ParseRelatedRecordIds_Null_Throws()
		{
			Assert.Throws<ArgumentNullException>(() => TimeLogRules.ParseRelatedRecordIds(null));
		}

		[Theory]
		[InlineData("not-json")]
		[InlineData("[\"3c1e8f0a-6b2d-4d6e-9f4a-0c7b5e2d1a90\"")]
		[InlineData("[\"not-a-guid\"]")]
		[InlineData("\"3c1e8f0a-6b2d-4d6e-9f4a-0c7b5e2d1a90\"")]
		[InlineData("{\"id\":\"3c1e8f0a-6b2d-4d6e-9f4a-0c7b5e2d1a90\"}")]
		public void ParseRelatedRecordIds_InvalidJson_ThrowsJsonException(string json)
		{
			Assert.ThrowsAny<JsonException>(() => TimeLogRules.ParseRelatedRecordIds(json));
		}

		#endregion

		#region << Billable / non-billable >>

		[Theory]
		[InlineData(true, "x_billable_minutes")]
		[InlineData(false, "x_nonbillable_minutes")]
		public void GetMinutesField_PicksBillableOrNonBillable(bool isBillable, string expected)
		{
			Assert.Equal(expected, TimeLogRules.GetMinutesField(isBillable));
		}

		[Theory]
		[InlineData(true, "billable")]
		[InlineData(false, "non-billable")]
		public void GetLoggedTypeLabel_ReturnsFeedLabel(bool isBillable, string expected)
		{
			Assert.Equal(expected, TimeLogRules.GetLoggedTypeLabel(isBillable));
		}

		#endregion

		#region << Roll-up on create >>

		[Fact]
		public void BuildTaskPatchOnCreate_Billable_AddsToBillableAndClearsTimelogStartedOn()
		{
			var patch = TimeLogRules.BuildTaskPatchOnCreate(TaskId, true, 100m, 30);

			Assert.Equal(new[] { "id", "x_billable_minutes", "timelog_started_on" }, patch.Keys);
			Assert.Equal(TaskId, patch["id"]);
			Assert.Equal(130m, patch["x_billable_minutes"]);
			Assert.Null(patch["timelog_started_on"]);
		}

		[Fact]
		public void BuildTaskPatchOnCreate_NonBillable_AddsToNonBillableAndClearsTimelogStartedOn()
		{
			var patch = TimeLogRules.BuildTaskPatchOnCreate(TaskId, false, 15m, 45);

			Assert.Equal(new[] { "id", "x_nonbillable_minutes", "timelog_started_on" }, patch.Keys);
			Assert.Equal(TaskId, patch["id"]);
			Assert.Equal(60m, patch["x_nonbillable_minutes"]);
			Assert.Null(patch["timelog_started_on"]);
		}

		[Theory]
		[InlineData(true, "x_billable_minutes")]
		[InlineData(false, "x_nonbillable_minutes")]
		public void BuildTaskPatchOnCreate_ZeroMinutes_StillPatchesTask(bool isBillable, string field)
		{
			var patch = TimeLogRules.BuildTaskPatchOnCreate(TaskId, isBillable, 100m, 0);

			Assert.Equal(100m, patch[field]);
			Assert.True(patch.ContainsKey("timelog_started_on"));
		}

		[Fact]
		public void BuildTaskPatchOnCreate_NegativeMinutes_AreNotClamped()
		{
			var patch = TimeLogRules.BuildTaskPatchOnCreate(TaskId, true, 10m, -30);
			Assert.Equal(-20m, patch["x_billable_minutes"]);
		}

		[Fact]
		public void BuildTaskPatchOnCreate_FractionalTaskMinutes_AreNotRounded()
		{
			var patch = TimeLogRules.BuildTaskPatchOnCreate(TaskId, true, 2.5m, 10);
			Assert.Equal(12.5m, patch["x_billable_minutes"]);
		}

		[Fact]
		public void BuildTaskPatchOnCreate_WritesDecimal()
		{
			var patch = TimeLogRules.BuildTaskPatchOnCreate(TaskId, true, 0m, 5);
			Assert.IsType<decimal>(patch["x_billable_minutes"]);
		}

		#endregion

		#region << Roll-up on delete >>

		[Fact]
		public void BuildTaskPatchOnDelete_Billable_SubtractsFromBillableOnly()
		{
			var patch = TimeLogRules.BuildTaskPatchOnDelete(TaskId, true, 130m, 30m);

			Assert.Equal(new[] { "id", "x_billable_minutes" }, patch.Keys);
			Assert.Equal(TaskId, patch["id"]);
			Assert.Equal(100m, patch["x_billable_minutes"]);
		}

		[Fact]
		public void BuildTaskPatchOnDelete_NonBillable_SubtractsFromNonBillableOnly()
		{
			var patch = TimeLogRules.BuildTaskPatchOnDelete(TaskId, false, 60m, 45m);

			Assert.Equal(new[] { "id", "x_nonbillable_minutes" }, patch.Keys);
			Assert.Equal(15m, patch["x_nonbillable_minutes"]);
		}

		[Fact]
		public void BuildTaskPatchOnDelete_DoesNotTouchTimelogStartedOn()
		{
			var patch = TimeLogRules.BuildTaskPatchOnDelete(TaskId, true, 130m, 30m);
			Assert.False(patch.ContainsKey("timelog_started_on"));
		}

		[Theory]
		[InlineData(true, "x_billable_minutes", 10, 30)]
		[InlineData(false, "x_nonbillable_minutes", 10, 30)]
		[InlineData(true, "x_billable_minutes", 30, 30)]
		[InlineData(true, "x_billable_minutes", 0, 15)]
		[InlineData(true, "x_billable_minutes", -20, 0)]
		public void BuildTaskPatchOnDelete_ClampsAtZero_WritingInt0(bool isBillable, string field, int taskMinutes, int loggedMinutes)
		{
			var patch = TimeLogRules.BuildTaskPatchOnDelete(TaskId, isBillable, taskMinutes, loggedMinutes);

			Assert.IsType<int>(patch[field]);
			Assert.Equal(0, patch[field]);
		}

		[Theory]
		[InlineData("10.4", "0", "10")]
		[InlineData("10.6", "0", "11")]
		[InlineData("2.5", "0", "2")]
		[InlineData("3.5", "0", "4")]
		[InlineData("20.5", "10", "10")]
		[InlineData("10", "9.4", "1")]
		[InlineData("20.6", "10", "11")]
		public void BuildTaskPatchOnDelete_RoundsHalfToEven(string taskMinutes, string loggedMinutes, string expected)
		{
			var patch = TimeLogRules.BuildTaskPatchOnDelete(TaskId, true, Dec(taskMinutes), Dec(loggedMinutes));

			Assert.IsType<decimal>(patch["x_billable_minutes"]);
			Assert.Equal(Dec(expected), patch["x_billable_minutes"]);
		}

		[Theory]
		[InlineData("10", "9.5")]
		[InlineData("10", "9.6")]
		[InlineData("0.4", "0")]
		public void BuildTaskPatchOnDelete_RoundsBeforeClamping(string taskMinutes, string loggedMinutes)
		{
			var patch = TimeLogRules.BuildTaskPatchOnDelete(TaskId, true, Dec(taskMinutes), Dec(loggedMinutes));
			Assert.Equal(0, patch["x_billable_minutes"]);
		}

		[Fact]
		public void BuildTaskPatchOnDelete_NegativeMinutes_IncreaseTotal()
		{
			var patch = TimeLogRules.BuildTaskPatchOnDelete(TaskId, false, 10m, -5m);
			Assert.Equal(15m, patch["x_nonbillable_minutes"]);
		}

		#endregion

		private static decimal Dec(string value)
		{
			return decimal.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
		}
	}
}
