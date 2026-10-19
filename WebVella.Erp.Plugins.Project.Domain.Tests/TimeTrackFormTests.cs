using System;
using Xunit;

namespace WebVella.Erp.Plugins.Project.Domain.Tests
{
	public class TimeTrackFormTests
	{
		private const string TaskId = "3c1e8f0a-6b2d-4d6e-9f4a-0c7b5e2d1a90";
		private const string LoggedOn = "2026-10-19";

		[Fact]
		public void Parse_ValidForm()
		{
			var form = TimeTrackForm.Parse(TaskId, "45", LoggedOn, "false", "worked on it");

			Assert.Equal(new Guid(TaskId), form.TaskId);
			Assert.Equal(45, form.Minutes);
			Assert.Equal(new DateTime(2026, 10, 19), form.LoggedOn);
			Assert.False(form.IsBillable);
			Assert.Equal("worked on it", form.Body);
			Assert.True(form.ShouldLogTime);
		}

		#region << Required fields >>

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
		public void Parse_AllMissing_ReportsTaskIdFirst()
		{
			var ex = Assert.Throws<Exception>(() => TimeTrackForm.Parse("", "", "", "", ""));
			Assert.Equal("Task Id is required", ex.Message);
		}

		[Fact]
		public void Parse_MinutesAndLoggedOnMissing_ReportsMinutesBeforeLoggedOn()
		{
			var ex = Assert.Throws<Exception>(() => TimeTrackForm.Parse(TaskId, "", "", "", ""));
			Assert.Equal("Minutes is required", ex.Message);
		}

		#endregion

		#region << Lenient parsing >>

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
		[InlineData("on", true)]
		[InlineData("0", true)]
		[InlineData("true,false", true)]
		public void Parse_IsBillable_DefaultsToTrueWhenUnparsable(string isBillable, bool expected)
		{
			var form = TimeTrackForm.Parse(TaskId, "45", LoggedOn, isBillable, "");
			Assert.Equal(expected, form.IsBillable);
		}

		[Theory]
		[InlineData("abc", 0)]
		[InlineData("1.5", 0)]
		[InlineData("99999999999", 0)]
		[InlineData("-15", -15)]
		[InlineData(" 30 ", 30)]
		[InlineData("00", 0)]
		public void Parse_Minutes_FallsBackToZeroWhenUnparsable(string minutes, int expected)
		{
			var form = TimeTrackForm.Parse(TaskId, minutes, LoggedOn, "true", "");
			Assert.Equal(expected, form.Minutes);
		}

		[Theory]
		[InlineData(null, "")]
		[InlineData("", "")]
		[InlineData("   ", "")]
		[InlineData(" note ", " note ")]
		public void Parse_Body_BlankBecomesEmpty(string body, string expected)
		{
			var form = TimeTrackForm.Parse(TaskId, "45", LoggedOn, "true", body);
			Assert.Equal(expected, form.Body);
		}

		#endregion

		#region << Zero minutes >>

		[Fact]
		public void Parse_ZeroMinutes_IsNotLogged()
		{
			var form = TimeTrackForm.Parse(TaskId, "0", LoggedOn, "true", "");

			Assert.Equal(0, form.Minutes);
			Assert.False(form.ShouldLogTime);
		}

		[Theory]
		[InlineData("00")]
		[InlineData(" 0")]
		[InlineData("abc")]
		[InlineData("-0")]
		public void Parse_OnlyLiteralZeroSkipsLogging(string minutes)
		{
			var form = TimeTrackForm.Parse(TaskId, minutes, LoggedOn, "true", "");

			Assert.Equal(0, form.Minutes);
			Assert.True(form.ShouldLogTime);
		}

		[Theory]
		[InlineData("1")]
		[InlineData("-15")]
		public void Parse_NonZeroMinutes_AreLogged(string minutes)
		{
			var form = TimeTrackForm.Parse(TaskId, minutes, LoggedOn, "true", "");
			Assert.True(form.ShouldLogTime);
		}

		#endregion
	}
}
