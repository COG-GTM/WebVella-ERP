using System;
using WebVella.Erp.Plugins.Project.Domain.Comments;
using Xunit;

namespace WebVella.Erp.Plugins.Project.Domain.Tests.Comments
{
	public class CommentScopeRulesTests
	{
		[Theory]
		[InlineData("[\"projects\"]", true)]
		[InlineData("[\"crm\",\"projects\"]", true)]
		[InlineData("projects", true)]
		[InlineData("[\"crm\"]", false)]
		[InlineData("[]", false)]
		[InlineData("null", false)]
		[InlineData("", false)]
		[InlineData(null, false)]
		public void IsProjectScope(string scope, bool expected)
		{
			Assert.Equal(expected, CommentScopeRules.IsProjectScope(scope));
		}

		[Theory]
		[InlineData("[\"myprojectsx\"]")]
		[InlineData("[\"subprojects\"]")]
		[InlineData("projectsfoo")]
		public void IsProjectScope_SubstringMatch_IntentionallyPreservedQuirk(string scope)
		{
			// Intentionally preserved quirk: the serialized scope is matched with string.Contains, not per item.
			Assert.True(CommentScopeRules.IsProjectScope(scope));
		}

		[Theory]
		[InlineData("[\"Projects\"]")]
		[InlineData("[\"PROJECTS\"]")]
		[InlineData("[\"project\"]")]
		public void IsProjectScope_IsCaseSensitiveAndExact_IntentionallyPreservedQuirk(string scope)
		{
			// Intentionally preserved quirk: ordinal, case-sensitive match.
			Assert.False(CommentScopeRules.IsProjectScope(scope));
		}

		[Fact]
		public void ProjectsScope_Constant()
		{
			Assert.Equal("projects", CommentScopeRules.ProjectsScope);
		}

		[Theory]
		[InlineData(null, false)]
		[InlineData("", false)]
		[InlineData("[]", true)]
		[InlineData("null", true)]
		[InlineData("[\"6f1c4c1e-8b9f-4bd5-9a41-6a0c2a3f6b11\"]", true)]
		public void HasRelatedRecords(string related, bool expected)
		{
			Assert.Equal(expected, CommentScopeRules.HasRelatedRecords(related));
		}

		[Theory]
		[InlineData(" ")]
		[InlineData("\t")]
		[InlineData("\r\n")]
		public void HasRelatedRecords_WhitespaceCountsAsPresent_IntentionallyPreservedQuirk(string related)
		{
			// Intentionally preserved quirk: only null and "" are treated as empty; whitespace proceeds to deserialization.
			Assert.True(CommentScopeRules.HasRelatedRecords(related));
		}

		[Fact]
		public void ResolveCommentCreator_Guid_ReturnsGuid()
		{
			var id = Guid.NewGuid();
			Assert.Equal(id, CommentScopeRules.ResolveCommentCreator(id));
		}

		[Fact]
		public void ResolveCommentCreator_EmptyGuid_ReturnsEmptyGuid()
		{
			Assert.Equal(Guid.Empty, CommentScopeRules.ResolveCommentCreator(Guid.Empty));
		}

		[Fact]
		public void ResolveCommentCreator_Null_ReturnsNull()
		{
			Assert.Null(CommentScopeRules.ResolveCommentCreator(null));
		}

		[Fact]
		public void ResolveCommentCreator_NullableGuidWithValue_ReturnsGuid()
		{
			Guid? id = Guid.NewGuid();
			Assert.Equal(id, CommentScopeRules.ResolveCommentCreator(id));
		}

		[Fact]
		public void ResolveCommentCreator_GuidString_ReturnsNull_IntentionallyPreservedQuirk()
		{
			// Intentionally preserved quirk: a created_by stored as a string Guid is not parsed, so no watcher is added.
			Assert.Null(CommentScopeRules.ResolveCommentCreator(Guid.NewGuid().ToString()));
		}

		[Fact]
		public void ResolveCommentCreator_OtherType_ReturnsNull()
		{
			Assert.Null(CommentScopeRules.ResolveCommentCreator(42));
		}
	}
}
