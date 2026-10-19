using System;
using System.Collections.Generic;
using System.Linq;

namespace WebVella.Erp.Plugins.Project.Domain.Comments
{
	public static class CommentWatcherRules
	{
		public const string WatchRelationName = "user_nn_task_watchers";
		public const string WatchRelationNotFoundMessage = "Watch relation not found";

		// Only the comment author is ever added as a watcher, and only to tasks whose watcher list was loaded
		// and does not already contain the author.
		public static bool ShouldAddCreatorAsWatcher(Guid commentCreator, bool hasWatcherList, IEnumerable<Guid> currentWatcherIds)
		{
			if (!hasWatcherList)
				return false;
			return !currentWatcherIds.ToList().Contains(commentCreator);
		}
	}
}
