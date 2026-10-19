using System;
using Xunit;

namespace WebVella.Erp.Plugins.Project.Domain.Tests
{
	public class TimeTrackFormInputTests
	{
		private const string TaskId = "3c1e8f0a-6b2d-4d6e-9f4a-0c7b5e2d1a90";
		private const string LoggedOn = "2026-10-19";

		[Fact]
		public void Parse_ValidForm()
		{
			var input = TimeTrackFormInput.Parse(TaskId, "45", LoggedOn, "false", "worked on it");

			Assert.Equal(new Guid(TaskId), input.TaskId);
			Assert.Equal(45, input.Minutes);
			Assert.Equal(new DateTime(2026, 10, 19), input.LoggedOn);
			Assert.False(input.IsBillable);
			Assert.Equal("worked on it", input.Body);
			Assert.True(input.ShouldLogMinutes);
		}

		[Theory]
		[InlineData(null)]
		[InlineData("")]
		[InlineData("   ")]
		public void Parse_MissingTaskId_Throws(string taskId)
		{
			var ex = Assert.Throws<Exception>(() => TimeTrackFormInput.Parse(taskId, "45", LoggedOn, "true", ""));
			Assert.Equal("Task Id is required", ex.Message);
		}

		[Theory]
		[InlineData(null)]
		[InlineData("")]
		[InlineData("   ")]
		public void Parse_MissingMinutes_Throws(string minutes)
		{
			var ex = Assert.Throws<Exception>(() => TimeTrackFormInput.Parse(TaskId, minutes, LoggedOn, "true", ""));
			Assert.Equal("Minutes is required", ex.Message);
		}

		[Theory]
		[InlineData(null)]
		[InlineData("")]
		[InlineData("   ")]
		public void Parse_MissingLoggedOn_Throws(string loggedOn)
		{
			var ex = Assert.Throws<Exception>(() => TimeTrackFormInput.Parse(TaskId, "45", loggedOn, "true", ""));
			Assert.Equal("Logged on is required", ex.Message);
		}

		[Fact]
		public void Parse_AllRequiredMissing_ReportsTaskIdFirst()
		{
			var ex = Assert.Throws<Exception>(() => TimeTrackFormInput.Parse("", "", "", "", ""));
			Assert.Equal("Task Id is required", ex.Message);
		}

		[Fact]
		public void Parse_MinutesAndLoggedOnMissing_ReportsMinutesFirst()
		{
			var ex = Assert.Throws<Exception>(() => TimeTrackFormInput.Parse(TaskId, "", "", "", ""));
			Assert.Equal("Minutes is required", ex.Message);
		}

		[Fact]
		public void Parse_UnparsableTaskId_FallsBackToEmptyGuid()
		{
			var input = TimeTrackFormInput.Parse("not-a-guid", "45", LoggedOn, "true", "");
			Assert.Equal(Guid.Empty, input.TaskId);
		}

		[Fact]
		public void Parse_UnparsableLoggedOn_FallsBackToMinValue()
		{
			var input = TimeTrackFormInput.Parse(TaskId, "45", "not-a-date", "true", "");
			Assert.Equal(DateTime.MinValue, input.LoggedOn);
		}

		[Theory]
		[InlineData("true", true)]
		[InlineData("True", true)]
		[InlineData("false", false)]
		[InlineData("FALSE", false)]
		[InlineData("", true)]
		[InlineData(null, true)]
		[InlineData("yes", true)]
		[InlineData("0", true)]
		[InlineData("on", true)]
		[InlineData("true,false", true)]
		public void Parse_IsBillable_DefaultsToTrueWhenUnparsable(string isBillable, bool expected)
		{
			var input = TimeTrackFormInput.Parse(TaskId, "45", LoggedOn, isBillable, "");
			Assert.Equal(expected, input.IsBillable);
		}

		[Theory]
		[InlineData("abc")]
		[InlineData("1.5")]
		[InlineData("99999999999")]
		public void Parse_UnparsableMinutes_FallsBackToZeroButIsStillLogged(string minutes)
		{
			var input = TimeTrackFormInput.Parse(TaskId, minutes, LoggedOn, "true", "");
			Assert.Equal(0, input.Minutes);
			Assert.True(input.ShouldLogMinutes);
		}

		[Fact]
		public void Parse_ZeroMinutes_IsNotLogged()
		{
			var input = TimeTrackFormInput.Parse(TaskId, "0", LoggedOn, "true", "");
			Assert.Equal(0, input.Minutes);
			Assert.False(input.ShouldLogMinutes);
		}

		[Theory]
		[InlineData("00")]
		[InlineData(" 0")]
		[InlineData("-0")]
		[InlineData("+0")]
		public void Parse_ZeroMinutesSpelledDifferently_IsStillLogged(string minutes)
		{
			var input = TimeTrackFormInput.Parse(TaskId, minutes, LoggedOn, "true", "");
			Assert.Equal(0, input.Minutes);
			Assert.True(input.ShouldLogMinutes);
		}

		[Fact]
		public void Parse_NegativeMinutes_AreKeptAndLogged()
		{
			var input = TimeTrackFormInput.Parse(TaskId, "-15", LoggedOn, "true", "");
			Assert.Equal(-15, input.Minutes);
			Assert.True(input.ShouldLogMinutes);
		}

		[Theory]
		[InlineData(null)]
		[InlineData("")]
		[InlineData("   ")]
		public void Parse_BlankBody_BecomesEmptyString(string body)
		{
			var input = TimeTrackFormInput.Parse(TaskId, "45", LoggedOn, "true", body);
			Assert.Equal("", input.Body);
		}

		[Fact]
		public void Parse_Body_IsKeptVerbatim()
		{
			var input = TimeTrackFormInput.Parse(TaskId, "45", LoggedOn, "true", "  <p>notes</p> ");
			Assert.Equal("  <p>notes</p> ", input.Body);
		}
	}
}
