using System.Collections.Generic;
using System.Linq;
using WebVella.Erp.Eql;
using Xunit;

namespace WebVella.Erp.Eql.Tests
{
	public class EqlPagingOrderTests
	{
		private const string TaskListEql = "SELECT * FROM task WHERE x_search CONTAINS @searchQuery ORDER BY @sortBy @sortOrder PAGE @page PAGESIZE @pageSize";

		private static List<string> OrderBy(string eql, params EqlParameter[] parameters)
		{
			var tree = new EqlBuilder(eql).Build(parameters.ToList()).Tree;
			var orderBy = ((EqlSelectNode)tree.RootNode).OrderBy;
			return orderBy == null ? new List<string>() : orderBy.Fields.Select(x => $"{x.FieldName} {x.Direction}").ToList();
		}

		[Fact]
		public void PagedTaskList_BreaksEndTimeTiesById()
		{
			var order = OrderBy(TaskListEql,
				new EqlParameter("searchQuery", ""),
				new EqlParameter("sortBy", "end_time"),
				new EqlParameter("sortOrder", "asc"),
				new EqlParameter("page", 2),
				new EqlParameter("pageSize", 10));

			Assert.Equal(new List<string> { "end_time ASC", "id ASC" }, order);
		}

		[Fact]
		public void PagedQuery_AlreadyOrderedById_IsUnchanged()
		{
			Assert.Equal(new List<string> { "id DESC" }, OrderBy("SELECT * FROM task ORDER BY id DESC PAGE 1 PAGESIZE 10"));
		}

		[Fact]
		public void UnpagedQuery_IsUnchanged()
		{
			Assert.Equal(new List<string> { "end_time ASC" }, OrderBy("SELECT * FROM task ORDER BY end_time"));
			Assert.Empty(OrderBy("SELECT * FROM task PAGE 1 PAGESIZE 10"));
		}
	}
}
