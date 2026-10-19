using System;
using System.Collections.Generic;
using System.Linq;
using WebVella.Erp.Plugins.Project.Domain.Comments;
using Xunit;

namespace WebVella.Erp.Plugins.Project.Domain.Tests.Comments
{
	public class RelatedTaskFilterTests
	{
		[Fact]
		public void Build_SingleId()
		{
			var id = Guid.NewGuid();
			var filter = RelatedTaskFilter.Build(new[] { id });
			Assert.Equal(" id = @taskId1 ", filter.WhereClause);
			Assert.Single(filter.Parameters);
			Assert.Equal("taskId1", filter.Parameters[0].Key);
			Assert.Equal(id, filter.Parameters[0].Value);
		}

		[Theory]
		[InlineData(2, " id = @taskId1  OR  id = @taskId2 ")]
		[InlineData(3, " id = @taskId1  OR  id = @taskId2  OR  id = @taskId3 ")]
		public void Build_MultipleIds_AllInspectedAndOrJoined(int count, string expectedWhere)
		{
			var ids = Enumerable.Range(0, count).Select(_ => Guid.NewGuid()).ToList();
			var filter = RelatedTaskFilter.Build(ids);
			Assert.Equal(expectedWhere, filter.WhereClause);
			Assert.Equal(ids, filter.Parameters.Select(p => p.Value));
			Assert.Equal(Enumerable.Range(1, count).Select(i => "taskId" + i), filter.Parameters.Select(p => p.Key));
		}

		[Fact]
		public void Build_TenIds_ParameterNamesAreOneBased()
		{
			var ids = Enumerable.Range(0, 10).Select(_ => Guid.NewGuid()).ToList();
			var filter = RelatedTaskFilter.Build(ids);
			Assert.Equal("taskId10", filter.Parameters.Last().Key);
			Assert.EndsWith(" OR  id = @taskId10 ", filter.WhereClause);
		}

		[Fact]
		public void Build_EmptyList_ProducesEmptyWhereClause_IntentionallyPreservedQuirk()
		{
			// Intentionally preserved quirk: "[]" yields "... WHERE " with no condition (the EQL query then fails).
			var filter = RelatedTaskFilter.Build(new List<Guid>());
			Assert.Equal("", filter.WhereClause);
			Assert.Empty(filter.Parameters);
		}

		[Fact]
		public void Build_DuplicateIds_AreNotDeduplicated_IntentionallyPreservedQuirk()
		{
			var id = Guid.NewGuid();
			var filter = RelatedTaskFilter.Build(new[] { id, id });
			Assert.Equal(" id = @taskId1  OR  id = @taskId2 ", filter.WhereClause);
			Assert.All(filter.Parameters, p => Assert.Equal(id, p.Value));
		}

		[Fact]
		public void Build_NonTaskIdsAreInspectedToo()
		{
			// All related record ids (e.g. project or user ids) are queried as task ids; non-tasks simply do not match.
			var taskId = Guid.NewGuid();
			var projectId = Guid.NewGuid();
			var filter = RelatedTaskFilter.Build(new[] { projectId, taskId });
			Assert.Equal(new[] { projectId, taskId }, filter.Parameters.Select(p => p.Value));
		}

		[Fact]
		public void Build_NullList_ThrowsNullReference_IntentionallyPreservedQuirk()
		{
			// Intentionally preserved quirk: "null"/whitespace JSON deserializes to null and the foreach throws NullReferenceException.
			Assert.Throws<NullReferenceException>(() => RelatedTaskFilter.Build(null));
		}
	}
}
