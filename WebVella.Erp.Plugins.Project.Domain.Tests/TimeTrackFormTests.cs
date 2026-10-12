using System;
using Xunit;

namespace WebVella.Erp.Plugins.Project.Domain.Tests
{
	public class TimeTrackFormTests
	{
		private const string TaskId = "6f1d7a39-5d4b-4a0e-9a54-7a9f8f3f0b11";
		private const string LoggedOn = "2026-10-12";

		[Fact]
		public void Parse_ValidForm()
		{
			var form = TimeTrackForm.Parse(TaskId, "45", LoggedOn, "false", "worked on it");

			Assert.Equal(new Guid(TaskId), form.TaskId);
			Assert.Equal(45, form.Minutes);
			Assert.Equal(new DateTime(2026, 10, 12), form.LoggedOn);
			Assert.False(form.IsBillable);
			Assert.Equal("worked on it", form.Body);
			Assert.True(form.ShouldLogMinutes);
		}

		[Theory]
		[InlineData(null)]
		[InlineData("")]
		[InlineData("   ")]
		public void Parse_MissingTaskId_Throws(string taskId)
		{
			var ex = Assert.Throws<Exception>(() => TimeTrackForm.Parse(taskId, "45", LoggedOn, "true", ""));
			Assert.Equal("Task Id is required", ex.Message);
		}

		[Theory]
		[InlineData(null)]
		[InlineData("")]
		[InlineData("   ")]
		public void Parse_MissingMinutes_Throws(string minutes)
		{
			var ex = Assert.Throws<Exception>(() => TimeTrackForm.Parse(TaskId, minutes, LoggedOn, "true", ""));
			Assert.Equal("Minutes is required", ex.Message);
		}

		[Theory]
		[InlineData(null)]
		[InlineData("")]
		[InlineData("   ")]
		public void Parse_MissingLoggedOn_Throws(string loggedOn)
		{
			var ex = Assert.Throws<Exception>(() => TimeTrackForm.Parse(TaskId, "45", loggedOn, "true", ""));
			Assert.Equal("Logged on is required", ex.Message);
		}

		[Fact]
		public void Parse_AllRequiredMissing_ReportsTaskIdFirst()
		{
			var ex = Assert.Throws<Exception>(() => TimeTrackForm.Parse("", "", "", "", ""));
			Assert.Equal("Task Id is required", ex.Message);
		}

		[Fact]
		public void Parse_MinutesAndLoggedOnMissing_ReportsMinutesFirst()
		{
			var ex = Assert.Throws<Exception>(() => TimeTrackForm.Parse(TaskId, "", "", "", ""));
			Assert.Equal("Minutes is required", ex.Message);
		}

		[Fact]
		public void Parse_UnparsableTaskId_FallsBackToEmptyGuid()
		{
			var form = TimeTrackForm.Parse("not-a-guid", "45", LoggedOn, "true", "");
			Assert.Equal(Guid.Empty, form.TaskId);
		}

		[Fact]
		public void Parse_UnparsableLoggedOn_FallsBackToMinValue()
		{
			var form = TimeTrackForm.Parse(TaskId, "45", "not-a-date", "true", "");
			Assert.Equal(DateTime.MinValue, form.LoggedOn);
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
		public void Parse_IsBillable_DefaultsToTrueWhenUnparsable(string isBillable, bool expected)
		{
			var form = TimeTrackForm.Parse(TaskId, "45", LoggedOn, isBillable, "");
			Assert.Equal(expected, form.IsBillable);
		}

		[Theory]
		[InlineData("abc")]
		[InlineData("1.5")]
		[InlineData("99999999999")]
		public void Parse_UnparsableMinutes_FallsBackToZeroButIsStillLogged(string minutes)
		{
			var form = TimeTrackForm.Parse(TaskId, minutes, LoggedOn, "true", "");
			Assert.Equal(0, form.Minutes);
			Assert.True(form.ShouldLogMinutes);
		}

		[Fact]
		public void Parse_ZeroMinutes_IsNotLogged()
		{
			var form = TimeTrackForm.Parse(TaskId, "0", LoggedOn, "true", "");
			Assert.Equal(0, form.Minutes);
			Assert.False(form.ShouldLogMinutes);
		}

		[Theory]
		[InlineData("00")]
		[InlineData(" 0")]
		[InlineData("-0")]
		[InlineData("+0")]
		public void Parse_ZeroMinutesWithOtherSpelling_IsStillLogged(string minutes)
		{
			var form = TimeTrackForm.Parse(TaskId, minutes, LoggedOn, "true", "");
			Assert.Equal(0, form.Minutes);
			Assert.True(form.ShouldLogMinutes);
		}

		[Fact]
		public void Parse_NegativeMinutes_AreKeptAndLogged()
		{
			var form = TimeTrackForm.Parse(TaskId, "-15", LoggedOn, "true", "");
			Assert.Equal(-15, form.Minutes);
			Assert.True(form.ShouldLogMinutes);
		}

		[Theory]
		[InlineData(null)]
		[InlineData("")]
		[InlineData("   ")]
		public void Parse_BlankBody_BecomesEmptyString(string body)
		{
			var form = TimeTrackForm.Parse(TaskId, "45", LoggedOn, "true", body);
			Assert.Equal("", form.Body);
		}

		[Fact]
		public void Parse_Body_IsKeptAsIs()
		{
			var form = TimeTrackForm.Parse(TaskId, "45", LoggedOn, "true", "  <p>notes</p> ");
			Assert.Equal("  <p>notes</p> ", form.Body);
		}
	}
}
