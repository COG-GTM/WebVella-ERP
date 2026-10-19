using System;
using System.Collections.Generic;
using WebVella.Erp.Plugins.Project.Domain.Comments;
using Xunit;

namespace WebVella.Erp.Plugins.Project.Domain.Tests.Comments
{
	public class CommentDeletionRulesTests
	{
		private static readonly Guid Author = new Guid("11111111-1111-1111-1111-111111111111");
		private static readonly Guid Other = new Guid("22222222-2222-2222-2222-222222222222");

		[Fact]
		public void Messages_AreUnchanged()
		{
			Assert.Equal("RecordId not found", CommentDeletionRules.RecordNotFoundMessage);
			Assert.Equal("Only the author can delete its comment", CommentDeletionRules.NotAuthorMessage);
		}

		[Fact]
		public void AuthorizeDelete_AuthorIsCurrentUser_IsAllowed()
		{
			Assert.Equal(CommentDeleteAuthorization.Allowed, CommentDeletionRules.AuthorizeDelete(true, () => Author, () => Author));
		}

		[Fact]
		public void AuthorizeDelete_DifferentUser_IsNotAuthor()
		{
			Assert.Equal(CommentDeleteAuthorization.NotAuthor, CommentDeletionRules.AuthorizeDelete(true, () => Author, () => Other));
		}

		[Fact]
		public void AuthorizeDelete_EmptyGuidAuthorAndEmptyGuidUser_IsAllowed()
		{
			Assert.Equal(CommentDeleteAuthorization.Allowed, CommentDeletionRules.AuthorizeDelete(true, () => Guid.Empty, () => Guid.Empty));
		}

		[Theory]
		[InlineData(true)]
		[InlineData(false)]
		public void AuthorizeDelete_MissingRecord_IsRecordNotFound_RegardlessOfUser(bool sameUser)
		{
			var current = sameUser ? Author : Other;
			Assert.Equal(CommentDeleteAuthorization.RecordNotFound, CommentDeletionRules.AuthorizeDelete(false, () => Author, () => current));
		}

		[Fact]
		public void AuthorizeDelete_MissingRecord_DoesNotResolveAuthorOrCurrentUser()
		{
			var resolved = new List<string>();
			var result = CommentDeletionRules.AuthorizeDelete(false,
				() => { resolved.Add("author"); return Author; },
				() => { resolved.Add("user"); return Author; });

			Assert.Equal(CommentDeleteAuthorization.RecordNotFound, result);
			Assert.Empty(resolved);
		}

		[Fact]
		public void AuthorizeDelete_ExistenceCheckedBeforeAuthorship_FailingLookupsNotInvoked()
		{
			// Mirrors the service: eqlResult[0] / SecurityContext.CurrentUser would throw if touched for a missing record.
			var result = CommentDeletionRules.AuthorizeDelete(false,
				() => throw new ArgumentOutOfRangeException(),
				() => throw new NullReferenceException());
			Assert.Equal(CommentDeleteAuthorization.RecordNotFound, result);
		}

		[Fact]
		public void AuthorizeDelete_ResolvesAuthorBeforeCurrentUser()
		{
			var resolved = new List<string>();
			CommentDeletionRules.AuthorizeDelete(true,
				() => { resolved.Add("author"); return Author; },
				() => { resolved.Add("user"); return Other; });
			Assert.Equal(new[] { "author", "user" }, resolved);
		}

		[Fact]
		public void AuthorizeDelete_AuthorLookupThrows_ExceptionPropagatesAndCurrentUserNotResolved()
		{
			// Intentionally preserved quirk: a null created_by is unboxed with (Guid) and throws NullReferenceException
			// instead of producing a domain error.
			var userResolved = false;
			Assert.Throws<NullReferenceException>(() => CommentDeletionRules.AuthorizeDelete(true,
				() => throw new NullReferenceException(),
				() => { userResolved = true; return Author; }));
			Assert.False(userResolved);
		}

		[Fact]
		public void AuthorizeDelete_CurrentUserLookupThrows_ExceptionPropagates()
		{
			// Intentionally preserved quirk: no current user (SecurityContext.CurrentUser == null) surfaces as NullReferenceException.
			Assert.Throws<NullReferenceException>(() => CommentDeletionRules.AuthorizeDelete(true,
				() => Author,
				() => throw new NullReferenceException()));
		}

		[Fact]
		public void BuildDeletionList_NoChildren_ContainsOnlyRequested()
		{
			var id = Guid.NewGuid();
			Assert.Equal(new[] { id }, CommentDeletionRules.BuildDeletionList(id, new Guid[0]));
		}

		[Fact]
		public void BuildDeletionList_RequestedFirstThenChildrenInQueryOrder()
		{
			var id = Guid.NewGuid();
			var c1 = Guid.NewGuid();
			var c2 = Guid.NewGuid();
			var c3 = Guid.NewGuid();
			Assert.Equal(new[] { id, c1, c2, c3 }, CommentDeletionRules.BuildDeletionList(id, new[] { c1, c2, c3 }));
		}

		[Fact]
		public void BuildDeletionList_DoesNotDeduplicate_IntentionallyPreservedQuirk()
		{
			// Intentionally preserved quirk: ids are appended as returned, duplicates (including the requested id) are kept.
			var id = Guid.NewGuid();
			var c1 = Guid.NewGuid();
			Assert.Equal(new[] { id, c1, c1, id }, CommentDeletionRules.BuildDeletionList(id, new[] { c1, c1, id }));
		}

		[Fact]
		public void BuildDeletionList_NullChildren_ThrowsNullReference()
		{
			Assert.Throws<NullReferenceException>(() => CommentDeletionRules.BuildDeletionList(Guid.NewGuid(), null));
		}
	}
}
