using System;
using System.Collections.Generic;
using System.Linq;
using WebVella.Erp.Api;
using Xunit;

namespace WebVella.Erp.Tests
{
	/// <summary>
	/// Proves the new Wave 1 modules boot together with the existing
	/// Project / Crm / Mail / TravelERP plugins in one database with no metadata
	/// GUID or name collisions, and that the existing plugins still applied cleanly.
	/// </summary>
	[Collection("erp")]
	public class RegressionTests
	{
		private readonly ErpTestFixture _fx;
		public RegressionTests(ErpTestFixture fx) => _fx = fx;

		[Fact]
		public void All_plugins_boot_without_entity_name_collisions()
		{
			_fx.Run(() =>
			{
				var entMan = new EntityManager();
				var entities = entMan.ReadEntities().Object;

				var dupNames = entities.GroupBy(e => e.Name)
					.Where(g => g.Count() > 1)
					.Select(g => g.Key)
					.ToList();
				Assert.True(dupNames.Count == 0, "Duplicate entity names: " + string.Join(", ", dupNames));

				var dupIds = entities.GroupBy(e => e.Id)
					.Where(g => g.Count() > 1)
					.Select(g => g.Key.ToString())
					.ToList();
				Assert.True(dupIds.Count == 0, "Duplicate entity ids: " + string.Join(", ", dupIds));
			});
		}

		[Fact]
		public void Existing_and_new_entities_coexist()
		{
			_fx.Run(() =>
			{
				var entMan = new EntityManager();
				var names = entMan.ReadEntities().Object.Select(e => e.Name).ToHashSet();

				// core system entities
				Assert.Contains("user", names);
				Assert.Contains("role", names);
				// existing plugins (Next / Project / Crm) still apply cleanly
				Assert.Contains("account", names);
				Assert.Contains("project", names);
				Assert.Contains("contact", names);
				// new modules
				Assert.Contains("fin_journal_entry", names);
				Assert.Contains("wf_approval_flow", names);
			});
		}

		[Fact]
		public void No_field_name_collisions_within_any_entity()
		{
			_fx.Run(() =>
			{
				var entMan = new EntityManager();
				var entities = entMan.ReadEntities().Object;
				var offenders = new List<string>();
				foreach (var e in entities)
				{
					var dup = e.Fields.GroupBy(f => f.Name).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
					if (dup.Count > 0)
						offenders.Add($"{e.Name}: {string.Join(",", dup)}");
				}
				Assert.True(offenders.Count == 0, "Entities with duplicate field names: " + string.Join(" | ", offenders));
			});
		}
	}
}
