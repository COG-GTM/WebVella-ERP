using System.Text;
using WebVella.Erp.Database;
using Xunit;

namespace WebVella.Erp.Tests
{
	public class DbRecordRepositoryTests
	{
		[Theory]
		[InlineData("\n")]
		[InlineData("\r\n")]
		public void RemoveTrailingComma_KeepsClosingQuote_ForAnyNewline(string newline)
		{
			var sql = new StringBuilder();
			sql.Append(" rec_task.\"id\" AS \"id\",").Append(newline);
			sql.Append("( SELECT 1 )::jsonb AS \"$project_nn_task\",").Append(newline);

			DbRecordRepository.RemoveTrailingComma(sql);

			Assert.Equal(" rec_task.\"id\" AS \"id\"," + newline + "( SELECT 1 )::jsonb AS \"$project_nn_task\"", sql.ToString());
		}

		[Fact]
		public void RemoveTrailingComma_LeavesSqlWithoutTrailingCommaUnchanged()
		{
			var sql = new StringBuilder(" rec_task.\"id\" AS \"id\"");

			DbRecordRepository.RemoveTrailingComma(sql);

			Assert.Equal(" rec_task.\"id\" AS \"id\"", sql.ToString());
		}
	}
}
