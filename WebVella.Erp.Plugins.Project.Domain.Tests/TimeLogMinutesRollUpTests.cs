using System;
using System.Collections.Generic;
using Xunit;

namespace WebVella.Erp.Plugins.Project.Domain.Tests
{
	public class TimeLogMinutesRollUpTests
	{
		private static readonly Guid TaskId = new Guid("3c1e8f0a-6b2d-4d6e-9f4a-0c7b5e2d1a90");

		[Theory]
		[InlineData(true, "x_billable_minutes")]
		[InlineData(false, "x_nonbillable_minutes")]
		public void GetMinutesField_PicksBillableOrNonBillable(bool isBillable, string expected)
		{
			Assert.Equal(expected, TimeLogMinutesRollUp.GetMinutesField(isBillable));
		}

		[Theory]
		[InlineData(true, "billable")]
		[InlineData(false, "non-billable")]
		public void GetLoggedTypeLabel_ReturnsFeedLabel(bool isBillable, string expected)
		{
			Assert.Equal(expected, TimeLogMinutesRollUp.GetLoggedTypeLabel(isBillable));
		}

		[Theory]
		[InlineData(100, 30, 130)]
		[InlineData(0, 45, 45)]
		[InlineData(100, 0, 100)]
		[InlineData(100, -30, 70)]
		[InlineData(10, -30, -20)]
		public void AddMinutes_AddsWithoutRoundingOrClamping(int taskMinutes, int loggedMinutes, int expected)
		{
			Assert.Equal((decimal)expected, TimeLogMinutesRollUp.AddMinutes(taskMinutes, loggedMinutes));
		}

		[Fact]
		public void AddMinutes_KeepsFractionalTaskMinutes()
		{
			Assert.Equal(12.5m, TimeLogMinutesRollUp.AddMinutes(2.5m, 10));
		}

		[Theory]
		[InlineData("130", "30", "100")]
		[InlineData("100", "0", "100")]
		[InlineData("100", "-30", "130")]
		[InlineData("30", "30", "0")]
		[InlineData("10", "30", "0")]
		[InlineData("0", "15", "0")]
		[InlineData("-20", "0", "0")]
		public void RemoveMinutes_SubtractsAndClampsAtZero(string taskMinutes, string loggedMinutes, string expected)
		{
			Assert.Equal(decimal.Parse(expected), TimeLogMinutesRollUp.RemoveMinutes(decimal.Parse(taskMinutes), decimal.Parse(loggedMinutes)));
		}

		[Theory]
		[InlineData("10.4", "0", "10")]
		[InlineData("10.6", "0", "11")]
		[InlineData("2.5", "0", "2")]
		[InlineData("3.5", "0", "4")]
		[InlineData("20.5", "10", "10")]
		[InlineData("10", "9.4", "1")]
		[InlineData("10", "9.5", "0")]
		[InlineData("10", "9.6", "0")]
		[InlineData("0.4", "0", "0")]
		public void RemoveMinutes_RoundsHalfToEvenBeforeClamping(string taskMinutes, string loggedMinutes, string expected)
		{
			Assert.Equal(decimal.Parse(expected), TimeLogMinutesRollUp.RemoveMinutes(decimal.Parse(taskMinutes), decimal.Parse(loggedMinutes)));
		}

		[Fact]
		public void BuildCreatePatch_Billable_AddsToBillableAndClearsTimerStart()
		{
			var patch = TimeLogMinutesRollUp.BuildCreatePatch(TaskId, true, 100m, 30);

			Assert.Equal(new[] { "id", "x_billable_minutes", "timelog_started_on" }, patch.Keys);
			Assert.Equal(TaskId, patch["id"]);
			Assert.Equal(130m, patch["x_billable_minutes"]);
			Assert.Null(patch["timelog_started_on"]);
		}

		[Fact]
		public void BuildCreatePatch_NonBillable_AddsToNonBillableAndClearsTimerStart()
		{
			var patch = TimeLogMinutesRollUp.BuildCreatePatch(TaskId, false, 15m, 45);

			Assert.Equal(new[] { "id", "x_nonbillable_minutes", "timelog_started_on" }, patch.Keys);
			Assert.Equal(TaskId, patch["id"]);
			Assert.Equal(60m, patch["x_nonbillable_minutes"]);
			Assert.Null(patch["timelog_started_on"]);
		}

		[Fact]
		public void BuildCreatePatch_ZeroMinutes_StillPatchesTask()
		{
			var patch = TimeLogMinutesRollUp.BuildCreatePatch(TaskId, true, 100m, 0);
			Assert.Equal(100m, patch["x_billable_minutes"]);
			Assert.True(patch.ContainsKey("timelog_started_on"));
		}

		[Fact]
		public void BuildCreatePatch_NegativeMinutes_AreNotClamped()
		{
			var patch = TimeLogMinutesRollUp.BuildCreatePatch(TaskId, true, 10m, -30);
			Assert.Equal(-20m, patch["x_billable_minutes"]);
		}

		[Fact]
		public void BuildDeletePatch_Billable_SubtractsFromBillableOnly()
		{
			var patch = TimeLogMinutesRollUp.BuildDeletePatch(TaskId, true, 130m, 30m);

			Assert.Equal(new[] { "id", "x_billable_minutes" }, patch.Keys);
			Assert.Equal(TaskId, patch["id"]);
			Assert.Equal(100m, patch["x_billable_minutes"]);
		}

		[Fact]
		public void BuildDeletePatch_NonBillable_SubtractsFromNonBillableOnly()
		{
			var patch = TimeLogMinutesRollUp.BuildDeletePatch(TaskId, false, 60m, 45m);

			Assert.Equal(new[] { "id", "x_nonbillable_minutes" }, patch.Keys);
			Assert.Equal(15m, patch["x_nonbillable_minutes"]);
		}

		[Fact]
		public void BuildDeletePatch_DoesNotTouchTimerStart()
		{
			var patch = TimeLogMinutesRollUp.BuildDeletePatch(TaskId, true, 130m, 30m);
			Assert.False(patch.ContainsKey("timelog_started_on"));
		}

		[Theory]
		[InlineData(true, "x_billable_minutes")]
		[InlineData(false, "x_nonbillable_minutes")]
		public void BuildDeletePatch_ClampsAtZero(bool isBillable, string field)
		{
			var patch = TimeLogMinutesRollUp.BuildDeletePatch(TaskId, isBillable, 10m, 30m);
			Assert.Equal(0m, patch[field]);
		}

		[Fact]
		public void BuildDeletePatch_Rounds()
		{
			var patch = TimeLogMinutesRollUp.BuildDeletePatch(TaskId, true, 20.6m, 10m);
			Assert.Equal(11m, patch["x_billable_minutes"]);
		}

		[Fact]
		public void BuildDeletePatch_NegativeMinutes_IncreaseTotal()
		{
			var patch = TimeLogMinutesRollUp.BuildDeletePatch(TaskId, false, 10m, -5m);
			Assert.Equal(15m, patch["x_nonbillable_minutes"]);
		}
	}
}
