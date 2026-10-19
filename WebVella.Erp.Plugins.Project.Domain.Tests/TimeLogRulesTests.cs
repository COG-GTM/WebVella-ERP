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

		[Fact]
		public void EnsureIdFieldPresent_Present_DoesNotThrow()
		{
			TimeLogRules.EnsureIdFieldPresent(true);
		}

		[Fact]
		public void EnsureIdFieldPresent_Missing_Throws()
		{
			var ex = Assert.Throws<Exception>(() => TimeLogRules.EnsureIdFieldPresent(false));
			Assert.Equal("Hook exception: timelog id field not found in record", ex.Message);
		}

		[Fact]
		public void EnsureTimeLogFound_Found_DoesNotThrow()
		{
			TimeLogRules.EnsureTimeLogFound(true);
		}

		[Fact]
		public void EnsureTimeLogFound_NotFound_Throws()
		{
			var ex = Assert.Throws<Exception>(() => TimeLogRules.EnsureTimeLogFound(false));
			Assert.Equal("Hook exception: timelog with this id was not found", ex.Message);
		}

		[Fact]
		public void EnsureRelatedTaskFound_Found_DoesNotThrow()
		{
			TimeLogRules.EnsureRelatedTaskFound(true);
		}

		[Fact]
		public void EnsureRelatedTaskFound_NotFound_Throws()
		{
			var ex = Assert.Throws<Exception>(() => TimeLogRules.EnsureRelatedTaskFound(false));
			Assert.Equal("Hook exception: This timelog does not have an existing taskId", ex.Message);
		}

		[Fact]
		public void EnsureRecordFound_Found_DoesNotThrow()
		{
			TimeLogRules.EnsureRecordFound(true);
		}

		[Fact]
		public void EnsureRecordFound_NotFound_Throws()
		{
			var ex = Assert.Throws<Exception>(() => TimeLogRules.EnsureRecordFound(false));
			Assert.Equal("RecordId not found", ex.Message);
		}

		[Fact]
		public void EnsureIsAuthor_SameUser_DoesNotThrow()
		{
			var userId = Guid.NewGuid();
			TimeLogRules.EnsureIsAuthor(userId, userId);
		}

		[Fact]
		public void EnsureIsAuthor_OtherUser_Throws()
		{
			var ex = Assert.Throws<Exception>(() => TimeLogRules.EnsureIsAuthor(Guid.NewGuid(), Guid.NewGuid()));
			Assert.Equal("Only the author can delete its comment", ex.Message);
		}

		[Fact]
		public void EnsureIsAuthor_EmptyAuthorAndUser_DoesNotThrow()
		{
			TimeLogRules.EnsureIsAuthor(Guid.Empty, Guid.Empty);
		}

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
		public void IsProjectTimeLog_CaseSensitiveSubstringMatch(string scope, bool expected)
		{
			Assert.Equal(expected, TimeLogRules.IsProjectTimeLog(scope));
		}

		[Theory]
		[InlineData(null, false)]
		[InlineData("", false)]
		[InlineData(" ", true)]
		[InlineData("[]", true)]
		[InlineData("null", true)]
		[InlineData("not-json", true)]
		public void HasRelatedRecords_OnlyNullOrEmptyIsMissing(string relatedRecords, bool expected)
		{
			Assert.Equal(expected, TimeLogRules.HasRelatedRecords(relatedRecords));
		}

		[Fact]
		public void ParseRelatedRecords_SingleId()
		{
			Assert.Equal(new List<Guid> { TaskId }, TimeLogRules.ParseRelatedRecords($"[\"{TaskId}\"]"));
		}

		[Fact]
		public void ParseRelatedRecords_MultipleIds_KeepsOrder()
		{
			var json = JsonConvert.SerializeObject(new List<Guid> { TaskId, ProjectId });
			Assert.Equal(new List<Guid> { TaskId, ProjectId }, TimeLogRules.ParseRelatedRecords(json));
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
		public void ParseRelatedRecords_EmptyString_ReturnsNull()
		{
			Assert.Null(TimeLogRules.ParseRelatedRecords(""));
		}

		[Fact]
		public void ParseRelatedRecords_Whitespace_ReturnsNull()
		{
			Assert.Null(TimeLogRules.ParseRelatedRecords("   "));
		}

		[Fact]
		public void ParseRelatedRecords_Null_Throws()
		{
			Assert.Throws<ArgumentNullException>(() => TimeLogRules.ParseRelatedRecords(null));
		}

		[Fact]
		public void ParseRelatedRecords_InvalidJson_Throws()
		{
			Assert.Throws<JsonReaderException>(() => TimeLogRules.ParseRelatedRecords("not-json"));
		}

		[Fact]
		public void ParseRelatedRecords_TruncatedJson_Throws()
		{
			Assert.ThrowsAny<JsonException>(() => TimeLogRules.ParseRelatedRecords($"[\"{TaskId}\""));
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
	}
}
