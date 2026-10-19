using System;
using System.Collections.Generic;

namespace WebVella.Erp.Plugins.Project.Domain.Comments
{
	public enum CommentDeleteAuthorization
	{
		Allowed,
		RecordNotFound,
		NotAuthor
	}

	public static class CommentDeletionRules
	{
		public const string RecordNotFoundMessage = "RecordId not found";
		public const string NotAuthorMessage = "Only the author can delete its comment";

		// Lookups are deferred so the author/current user are only resolved after the existence check,
		// in the same order (author first, then current user) as the original inline comparison.
		public static CommentDeleteAuthorization AuthorizeDelete(bool recordExists, Func<Guid> getAuthorId, Func<Guid> getCurrentUserId)
		{
			if (!recordExists)
				return CommentDeleteAuthorization.RecordNotFound;

			if (getAuthorId() != getCurrentUserId())
				return CommentDeleteAuthorization.NotAuthor;

			return CommentDeleteAuthorization.Allowed;
		}

		// Requested comment first, then its direct children (only one nesting level is supported).
		public static List<Guid> BuildDeletionList(Guid recordId, IEnumerable<Guid> childCommentIds)
		{
			var commentIdListForDeletion = new List<Guid>();
			commentIdListForDeletion.Add(recordId);
			foreach (var childId in childCommentIds)
				commentIdListForDeletion.Add(childId);
			return commentIdListForDeletion;
		}
	}
}
