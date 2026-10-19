using System;
using System.Collections.Generic;
using WebVella.Erp.Plugins.Project.Domain.Comments;
using Xunit;

namespace WebVella.Erp.Plugins.Project.Domain.Tests.Comments
{
	public class ProjectCommentRulesTests
	{
		private const string TaskId = "aaaaaaaa-0000-0000-0000-000000000001";
		private const string CommentId = "cccccccc-0000-0000-0000-000000000002";
		private const string ProjectId = "bbbbbbbb-0000-0000-0000-000000000003";
		private const string W1 = "dddddddd-0000-0000-0000-000000000004";
		private const string W2 = "eeeeeeee-0000-0000-0000-000000000005";

		[Fact]
		public void MissingRelatedTaskMessage_IsUnchanged()
		{
			Assert.Equal("Hook exception: This comment is a project comment but does not have an existing taskId related",
				ProjectCommentRules.MissingRelatedTaskMessage);
		}

		[Theory]
		[InlineData(0, false)]
		[InlineData(1, true)]
		[InlineData(2, true)]
		public void HasExistingRelatedTask(int count, bool expected)
		{
			Assert.Equal(expected, ProjectCommentRules.HasExistingRelatedTask(count));
		}

		[Fact]
		public void BuildFeedItem_Subject()
		{
			var item = ProjectCommentRules.BuildFeedItem(TaskId, "PRJ-7", "Fix bug", CommentId, null, new string[0]);
			Assert.Equal($"commented on <a href=\"/projects/tasks/tasks/r/{TaskId}/details\">[PRJ-7] Fix bug</a>", item.Subject);
		}

		[Fact]
		public void BuildFeedItem_SubjectIsNotHtmlEncoded_IntentionallyPreservedQuirk()
		{
			// Intentionally preserved quirk: key/subject are interpolated raw into the HTML anchor.
			var item = ProjectCommentRules.BuildFeedItem(TaskId, "<k>", "a & <b>", CommentId, null, new string[0]);
			Assert.Equal($"commented on <a href=\"/projects/tasks/tasks/r/{TaskId}/details\">[<k>] a & <b></a>", item.Subject);
		}

		[Fact]
		public void BuildFeedItem_NullKeyAndSubject_RenderEmpty()
		{
			var item = ProjectCommentRules.BuildFeedItem(TaskId, null, null, CommentId, null, new string[0]);
			Assert.Equal($"commented on <a href=\"/projects/tasks/tasks/r/{TaskId}/details\">[] </a>", item.Subject);
		}

		[Fact]
		public void BuildFeedItem_RelatedRecordsOrder_WithProject()
		{
			var item = ProjectCommentRules.BuildFeedItem(TaskId, "K", "S", CommentId, ProjectId, new[] { W1, W2 });
			Assert.Equal(new[] { TaskId, CommentId, ProjectId, W1, W2 }, item.RelatedRecords);
		}

		[Fact]
		public void BuildFeedItem_RelatedRecordsOrder_WithoutProject()
		{
			var item = ProjectCommentRules.BuildFeedItem(TaskId, "K", "S", CommentId, null, new[] { W1, W2 });
			Assert.Equal(new[] { TaskId, CommentId, W1, W2 }, item.RelatedRecords);
		}

		[Fact]
		public void BuildFeedItem_NoWatchers()
		{
			var item = ProjectCommentRules.BuildFeedItem(TaskId, "K", "S", CommentId, ProjectId, new List<string>());
			Assert.Equal(new[] { TaskId, CommentId, ProjectId }, item.RelatedRecords);
		}

		[Fact]
		public void BuildFeedItem_WatchersNotDeduplicated_IntentionallyPreservedQuirk()
		{
			// Intentionally preserved quirk: repeated watchers (or a watcher equal to another related id) are kept as-is.
			var item = ProjectCommentRules.BuildFeedItem(TaskId, "K", "S", CommentId, ProjectId, new[] { W1, W1, TaskId });
			Assert.Equal(new[] { TaskId, CommentId, ProjectId, W1, W1, TaskId }, item.RelatedRecords);
		}

		[Fact]
		public void BuildFeedItem_ScopeAndType()
		{
			var item = ProjectCommentRules.BuildFeedItem(TaskId, "K", "S", CommentId, null, new string[0]);
			Assert.Equal(new[] { "projects" }, item.Scope);
			Assert.Equal("comment", item.Type);
		}

		[Fact]
		public void BuildFeedItem_NullWatchers_Throws()
		{
			Assert.Throws<ArgumentNullException>(() => ProjectCommentRules.BuildFeedItem(TaskId, "K", "S", CommentId, null, null));
		}
	}
}
