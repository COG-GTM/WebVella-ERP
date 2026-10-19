using System;
using System.Collections.Generic;
using System.Linq;
using WebVella.Erp.Plugins.Project.Domain.Comments;
using Xunit;

namespace WebVella.Erp.Plugins.Project.Domain.Tests.Comments
{
	public class CommentWatcherRulesTests
	{
		private static readonly Guid Creator = new Guid("11111111-1111-1111-1111-111111111111");
		private static readonly Guid Other1 = new Guid("22222222-2222-2222-2222-222222222222");
		private static readonly Guid Other2 = new Guid("33333333-3333-3333-3333-333333333333");

		[Fact]
		public void Constants_AreUnchanged()
		{
			Assert.Equal("user_nn_task_watchers", CommentWatcherRules.WatchRelationName);
			Assert.Equal("Watch relation not found", CommentWatcherRules.WatchRelationNotFoundMessage);
		}

		[Fact]
		public void CreatorNotWatching_IsAdded()
		{
			Assert.True(CommentWatcherRules.ShouldAddCreatorAsWatcher(Creator, true, new[] { Other1, Other2 }));
		}

		[Fact]
		public void CreatorAlreadyWatching_IsNotAddedAgain()
		{
			Assert.False(CommentWatcherRules.ShouldAddCreatorAsWatcher(Creator, true, new[] { Other1, Creator }));
		}

		[Fact]
		public void EmptyWatcherList_CreatorIsAdded()
		{
			Assert.True(CommentWatcherRules.ShouldAddCreatorAsWatcher(Creator, true, new Guid[0]));
		}

		[Fact]
		public void WatcherListNotLoaded_CreatorIsNotAdded_IntentionallyPreservedQuirk()
		{
			// Intentionally preserved quirk: tasks without a loaded $user_nn_task_watchers list are silently skipped
			// instead of being treated as having no watchers.
			Assert.False(CommentWatcherRules.ShouldAddCreatorAsWatcher(Creator, false, new Guid[0]));
		}

		[Fact]
		public void WatcherListNotLoaded_WatcherIdsNotEnumerated()
		{
			var enumerated = false;
			IEnumerable<Guid> Ids() { enumerated = true; yield return Other1; }
			CommentWatcherRules.ShouldAddCreatorAsWatcher(Creator, false, Ids());
			Assert.False(enumerated);
		}

		[Fact]
		public void EmptyGuidCreator_IsTreatedLikeAnyOtherId()
		{
			Assert.True(CommentWatcherRules.ShouldAddCreatorAsWatcher(Guid.Empty, true, new[] { Other1 }));
			Assert.False(CommentWatcherRules.ShouldAddCreatorAsWatcher(Guid.Empty, true, new[] { Guid.Empty }));
		}

		[Fact]
		public void OnlyCreatorIsConsidered_TaskOwnerOrOtherUsersNeverSelected()
		{
			// The rule only ever answers whether the comment author should be added; no owner/assignee logic exists.
			var tasks = new[]
			{
				new[] { Other1 },
				new[] { Creator },
				new Guid[0],
			};
			var decisions = tasks.Select(w => CommentWatcherRules.ShouldAddCreatorAsWatcher(Creator, true, w)).ToList();
			Assert.Equal(new[] { true, false, true }, decisions);
		}

		[Fact]
		public void WatcherIdsEnumerationFailure_Propagates()
		{
			IEnumerable<Guid> Ids() { throw new InvalidCastException(); }
			Assert.Throws<InvalidCastException>(() => CommentWatcherRules.ShouldAddCreatorAsWatcher(Creator, true, Ids()));
		}
	}
}
