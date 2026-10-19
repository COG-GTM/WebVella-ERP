using System.Collections.Generic;

namespace WebVella.Erp.Plugins.Project.Domain.Comments
{
	public sealed class ProjectCommentFeedItem
	{
		public ProjectCommentFeedItem(string subject, List<string> relatedRecords, List<string> scope, string type)
		{
			Subject = subject;
			RelatedRecords = relatedRecords;
			Scope = scope;
			Type = type;
		}

		public string Subject { get; }

		public List<string> RelatedRecords { get; }

		public List<string> Scope { get; }

		public string Type { get; }
	}

	public static class ProjectCommentRules
	{
		public const string MissingRelatedTaskMessage = "Hook exception: This comment is a project comment but does not have an existing taskId related";
		public const string FeedItemType = "comment";

		public static bool HasExistingRelatedTask(int relatedTaskCount)
		{
			return relatedTaskCount > 0;
		}

		// Related records order: task id, comment id, project id (when known), then task watchers (no de-duplication).
		public static ProjectCommentFeedItem BuildFeedItem(string taskId, string taskKey, string taskSubject, string commentId,
			string projectId, IEnumerable<string> taskWatcherIds)
		{
			var subject = $"commented on <a href=\"/projects/tasks/tasks/r/{taskId}/details\">[{taskKey}] {taskSubject}</a>";
			var relatedRecords = new List<string>() { taskId, commentId };
			if (projectId != null)
			{
				relatedRecords.Add(projectId);
			}
			relatedRecords.AddRange(taskWatcherIds);
			var scope = new List<string>() { CommentScopeRules.ProjectsScope };
			return new ProjectCommentFeedItem(subject, relatedRecords, scope, FeedItemType);
		}
	}
}
