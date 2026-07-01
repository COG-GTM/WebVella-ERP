using System;
using System.Collections.Generic;
using System.Linq;
using WebVella.Erp.Api;
using WebVella.Erp.Api.Models;
using WebVella.Erp.Eql;
using WebVella.Erp.Plugins.Finance.Services;
using Xunit;

namespace WebVella.Erp.Tests
{
	[Collection("erp")]
	public class FinanceTests
	{
		private readonly ErpTestFixture _fx;
		public FinanceTests(ErpTestFixture fx) => _fx = fx;

		[Fact]
		public void Finance_entities_and_fields_are_created()
		{
			_fx.Run(() =>
			{
				var entMan = new EntityManager();
				var expected = new[]
				{
					"fin_account", "fin_journal_entry", "fin_journal_line",
					"fin_tax_code", "fin_ap_invoice", "fin_ar_invoice"
				};
				foreach (var name in expected)
				{
					var resp = entMan.ReadEntity(name);
					Assert.True(resp.Success && resp.Object != null, $"Entity '{name}' should exist. {resp.Message}");
				}

				var je = entMan.ReadEntity("fin_journal_entry").Object;
				var fieldNames = je.Fields.Select(f => f.Name).ToList();
				foreach (var f in new[] { "entry_date", "status", "total_debit", "total_credit", "is_balanced" })
					Assert.Contains(f, fieldNames);
			});
		}

		[Fact]
		public void Balanced_journal_posts_successfully()
		{
			_fx.Run(() =>
			{
				var svc = new FinanceService();
				svc.EnsureAccount("1000", "Cash", "asset", "debit");
				svc.EnsureAccount("4000", "Revenue", "revenue", "credit");

				var journalId = svc.PostSimpleEntry("finance-test", "REF-1", "1000", "4000", 250m, "test sale");
				Assert.NotEqual(Guid.Empty, journalId);

				var lines = svc.GetJournalLines(journalId);
				Assert.Equal(2, lines.Count);
				decimal debit = lines.Sum(l => Convert.ToDecimal(l["debit"]));
				decimal credit = lines.Sum(l => Convert.ToDecimal(l["credit"]));
				Assert.Equal(debit, credit);
				Assert.Equal(250m, debit);
			});
		}

		[Fact]
		public void Unbalanced_journal_is_rejected()
		{
			_fx.Run(() =>
			{
				var svc = new FinanceService();
				var lines = new List<JournalLine>
				{
					new JournalLine("1000", 100m, 0, "debit"),
					new JournalLine("4000", 0, 90m, "credit") // deliberately unbalanced
				};
				Assert.Throws<InvalidOperationException>(() =>
					svc.PostJournal(DateTime.UtcNow, "bad", "finance-test", "REF-2", lines));
			});
		}

		[Fact]
		public void Journal_hook_rejects_posted_but_unbalanced_entry()
		{
			_fx.Run(() =>
			{
				var recMan = new RecordManager();
				var rec = new EntityRecord();
				rec["id"] = Guid.NewGuid();
				rec["entry_number"] = "JE-HOOK-" + DateTime.UtcNow.Ticks;
				rec["entry_date"] = DateTime.UtcNow;
				rec["status"] = "posted";
				rec["total_debit"] = 100m;
				rec["total_credit"] = 50m; // imbalance -> hook must block
				rec["is_balanced"] = false;
				var resp = recMan.CreateRecord("fin_journal_entry", rec);
				Assert.False(resp.Success, "PreCreate hook should reject a posted, unbalanced journal entry.");
			});
		}
	}
}
