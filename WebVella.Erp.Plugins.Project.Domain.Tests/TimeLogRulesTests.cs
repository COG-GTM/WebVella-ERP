using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using Xunit;

namespace WebVella.Erp.Plugins.Project.Domain.Tests
{
	public class TimeLogRulesTests
	{
		private static readonly Guid TaskId = new Guid("6f1d7a39-5d4b-4a0e-9a54-7a9f8f3f0b11");
		private static readonly Guid ProjectId = new Guid("0b7f4c5e-2a71-4f0a-8f0e-1c3c9a0e7d22");

		[Fact]
		public void EnsureIdFieldPresent_WhenPresent_DoesNotThrow()
		{
			TimeLogRules.EnsureIdFieldPresent(true);
		}

		[Fact]
		public void EnsureIdFieldPresent_WhenMissing_Throws()
		{
			var ex = Assert.Throws<Exception>(() => TimeLogRules.EnsureIdFieldPresent(false));
			Assert.Equal("Hook exception: timelog id field not found in record", ex.Message);
		}

		[Fact]
		public void EnsureTimelogFound_WhenFound_DoesNotThrow()
		{
			TimeLogRules.EnsureTimelogFound(true);
		}

		[Fact]
		public void EnsureTimelogFound_WhenNotFound_Throws()
		{
			var ex = Assert.Throws<Exception>(() => TimeLogRules.EnsureTimelogFound(false));
			Assert.Equal("Hook exception: timelog with this id was not found", ex.Message);
		}

		[Fact]
		public void EnsureRelatedTaskFound_WhenFound_DoesNotThrow()
		{
			TimeLogRules.EnsureRelatedTaskFound(true);
		}

		[Fact]
		public void EnsureRelatedTaskFound_WhenNotFound_Throws()
		{
			var ex = Assert.Throws<Exception>(() => TimeLogRules.EnsureRelatedTaskFound(false));
			Assert.Equal("Hook exception: This timelog does not have an existing taskId", ex.Message);
		}

		[Fact]
		public void EnsureRecordFound_WhenFound_DoesNotThrow()
		{
			TimeLogRules.EnsureRecordFound(true);
		}

		[Fact]
		public void EnsureRecordFound_WhenNotFound_Throws()
		{
			var ex = Assert.Throws<Exception>(() => TimeLogRules.EnsureRecordFound(false));
			Assert.Equal("RecordId not found", ex.Message);
		}

		[Fact]
		public void EnsureIsAuthor_WhenCurrentUserIsAuthor_DoesNotThrow()
		{
			var userId = Guid.NewGuid();
			TimeLogRules.EnsureIsAuthor(userId, userId);
		}

		[Fact]
		public void EnsureIsAuthor_WhenCurrentUserIsNotAuthor_Throws()
		{
			var ex = Assert.Throws<Exception>(() => TimeLogRules.EnsureIsAuthor(Guid.NewGuid(), Guid.NewGuid()));
			Assert.Equal("Only the author can delete its comment", ex.Message);
		}

		[Theory]
		[InlineData("[\"projects\"]", true)]
		[InlineData("[\"crm\",\"projects\"]", true)]
		[InlineData("projects", true)]
		[InlineData("[\"subprojects\"]", true)]
		[InlineData("[\"crm\"]", false)]
		[InlineData("[\"Projects\"]", false)]
		[InlineData("[]", false)]
		[InlineData("", false)]
		[InlineData(null, false)]
		public void IsProjectTimeLog_UsesCaseSensitiveContains(string scope, bool expected)
		{
			Assert.Equal(expected, TimeLogRules.IsProjectTimeLog(scope));
		}

		[Theory]
		[InlineData(null, false)]
		[InlineData("", false)]
		[InlineData(" ", true)]
		[InlineData("[]", true)]
		[InlineData("not-json", true)]
		public void HasRelatedRecords_OnlyNullAndEmptyAreMissing(string relatedRecords, bool expected)
		{
			Assert.Equal(expected, TimeLogRules.HasRelatedRecords(relatedRecords));
		}

		[Fact]
		public void ParseRelatedRecords_SingleId()
		{
			var result = TimeLogRules.ParseRelatedRecords($"[\"{TaskId}\"]");
			Assert.Equal(new List<Guid> { TaskId }, result);
		}

		[Fact]
		public void ParseRelatedRecords_MultipleIds_PreservesOrder()
		{
			var json = JsonConvert.SerializeObject(new List<Guid> { TaskId, ProjectId });
			var result = TimeLogRules.ParseRelatedRecords(json);
			Assert.Equal(new List<Guid> { TaskId, ProjectId }, result);
		}

		[Fact]
		public void ParseRelatedRecords_EmptyArray_ReturnsEmptyList()
		{
			var result = TimeLogRules.ParseRelatedRecords("[]");
			Assert.NotNull(result);
			Assert.Empty(result);
		}

		[Fact]
		public void ParseRelatedRecords_JsonNull_ReturnsNull()
		{
			Assert.Null(TimeLogRules.ParseRelatedRecords("null"));
		}

		[Fact]
		public void ParseRelatedRecords_Whitespace_ReturnsNull()
		{
			Assert.Null(TimeLogRules.ParseRelatedRecords(" "));
		}

		[Fact]
		public void ParseRelatedRecords_InvalidJson_Throws()
		{
			Assert.Throws<JsonReaderException>(() => TimeLogRules.ParseRelatedRecords("not-json"));
		}

		[Fact]
		public void ParseRelatedRecords_InvalidGuid_Throws()
		{
			Assert.ThrowsAny<JsonException>(() => TimeLogRules.ParseRelatedRecords("[\"not-a-guid\"]"));
		}

		[Fact]
		public void ParseRelatedRecords_NotAnArray_Throws()
		{
			Assert.ThrowsAny<JsonException>(() => TimeLogRules.ParseRelatedRecords($"\"{TaskId}\""));
		}

		[Fact]
		public void ParseRelatedRecords_Null_Throws()
		{
			Assert.Throws<ArgumentNullException>(() => TimeLogRules.ParseRelatedRecords(null));
		}

		[Theory]
		[InlineData(true, "x_billable_minutes")]
		[InlineData(false, "x_nonbillable_minutes")]
		public void GetMinutesField_PicksBillableOrNonBillableField(bool isBillable, string expected)
		{
			Assert.Equal(expected, TimeLogRules.GetMinutesField(isBillable));
		}

		[Theory]
		[InlineData(true, "billable")]
		[InlineData(false, "non-billable")]
		public void GetLoggedType_ReturnsFeedLabel(bool isBillable, string expected)
		{
			Assert.Equal(expected, TimeLogRules.GetLoggedType(isBillable));
		}

		[Theory]
		[InlineData(100, 30, 130)]
		[InlineData(0, 45, 45)]
		[InlineData(100, 0, 100)]
		[InlineData(100, -30, 70)]
		[InlineData(10, -30, -20)]
		public void AddLoggedMinutes_AddsWithoutValidation(int taskMinutes, int loggedMinutes, int expected)
		{
			Assert.Equal((decimal)expected, TimeLogRules.AddLoggedMinutes(taskMinutes, loggedMinutes));
		}

		[Fact]
		public void AddLoggedMinutes_KeepsFractionalTaskMinutes()
		{
			Assert.Equal(12.5m, TimeLogRules.AddLoggedMinutes(2.5m, 10));
		}

		[Theory]
		[InlineData("130", "30", "100")]
		[InlineData("30", "30", "0")]
		[InlineData("10", "30", "0")]
		[InlineData("0", "15", "0")]
		[InlineData("100", "0", "100")]
		[InlineData("100", "-30", "130")]
		public void RemoveLoggedMinutes_SubtractsAndClampsAtZero(string taskMinutes, string loggedMinutes, string expected)
		{
			Assert.Equal(decimal.Parse(expected), TimeLogRules.RemoveLoggedMinutes(decimal.Parse(taskMinutes), decimal.Parse(loggedMinutes)));
		}

		[Theory]
		[InlineData("10.4", "0", "10")]
		[InlineData("10.6", "0", "11")]
		[InlineData("2.5", "0", "2")]
		[InlineData("3.5", "0", "4")]
		[InlineData("20.5", "10", "10")]
		[InlineData("10", "9.6", "0")]
		[InlineData("10", "9.4", "1")]
		[InlineData("10", "9.5", "0")]
		public void RemoveLoggedMinutes_RoundsToEvenBeforeClamping(string taskMinutes, string loggedMinutes, string expected)
		{
			Assert.Equal(decimal.Parse(expected), TimeLogRules.RemoveLoggedMinutes(decimal.Parse(taskMinutes), decimal.Parse(loggedMinutes)));
		}
	}
}
