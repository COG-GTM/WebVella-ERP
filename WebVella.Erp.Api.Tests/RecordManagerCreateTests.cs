using System.Collections.Generic;
using WebVella.Erp.Api;
using WebVella.Erp.Api.Models;
using Xunit;

namespace WebVella.Erp.Api.Tests
{
	public class RecordManagerCreateTests
	{
		private static QueryResponse TaskResponse(string key, string search, bool success = true)
		{
			var record = new EntityRecord();
			record["id"] = System.Guid.Parse("9a0f1d1e-55a2-4b1c-8f3e-2b1d6a7c4e10");
			record["key"] = key;
			record["x_search"] = search;
			return new QueryResponse { Success = success, Object = new QueryResult { Data = new List<EntityRecord> { record } } };
		}

		[Fact]
		public void CreateResponse_ShowsKeyAndSearchSetByPostCreateHooks()
		{
			var created = TaskResponse("placeholder-guid", "");
			created.Message = "Record was created successfully";

			var response = RecordManager.ReloadAfterPostCreateHooks(created, () => TaskResponse("SMP-6", "SMP-6 new task"));

			Assert.Equal("SMP-6", response.Object.Data[0]["key"]);
			Assert.Equal("SMP-6 new task", response.Object.Data[0]["x_search"]);
			Assert.Equal("Record was created successfully", response.Message);
		}

		[Fact]
		public void FailedReload_KeepsCreatedRecord()
		{
			var created = TaskResponse("placeholder-guid", "");

			Assert.Same(created, RecordManager.ReloadAfterPostCreateHooks(created, () => TaskResponse("SMP-6", "x", success: false)));
			Assert.Same(created, RecordManager.ReloadAfterPostCreateHooks(created, () => new QueryResponse { Success = true }));
		}
	}
}
