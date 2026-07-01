using System;
using WebVella.Erp.Api;
using WebVella.Erp.Api.Models;
using WebVella.Erp.Hooks;
using WebVella.Erp.Plugins.Finance.Services;

namespace WebVella.Erp.Plugins.HR.Hooks
{
	/// <summary>
	/// Cross-module integration with Finance: when a payroll run is posted, post a
	/// balanced GL expense journal (Dr Salary Expense, Cr Wages Payable) through
	/// FinanceService.PostSimpleEntry. This lives entirely in an HR hook so it is
	/// additive and never edits the Finance module.
	/// Auto-discovered by HookManager.RegisterHooks via the assembly scan.
	/// </summary>
	[HookAttachment("hr_payroll_run")]
	public class PayrollRunHooks : IErpPostCreateRecordHook, IErpPostUpdateRecordHook
	{
		private const string SourceModule = "hr-payroll";
		private const string SalaryExpenseAccount = "6000";
		private const string WagesPayableAccount = "2100";

		public void OnPostCreateRecord(string entityName, EntityRecord record) => PostIfPosted(record);

		public void OnPostUpdateRecord(string entityName, EntityRecord record) => PostIfPosted(record);

		private static void PostIfPosted(EntityRecord record)
		{
			if (record == null || !record.Properties.ContainsKey("status") || record.Properties["status"] == null)
				return;
			if (record.Properties["status"].ToString() != "posted")
				return;

			var runId = record.Properties.ContainsKey("id") && record.Properties["id"] != null
				? record.Properties["id"].ToString()
				: null;
			if (runId == null)
				return;

			// idempotent: don't post twice for the same payroll run
			if (JournalExists(runId))
				return;

			decimal amount = GetDecimal(record, "total_amount");
			if (amount <= 0)
				return;

			var finance = new FinanceService();
			finance.EnsureAccount(SalaryExpenseAccount, "Salary Expense", "expense", "debit");
			finance.EnsureAccount(WagesPayableAccount, "Wages Payable", "liability", "credit");

			var period = record.Properties.ContainsKey("period") && record.Properties["period"] != null
				? record.Properties["period"].ToString()
				: string.Empty;

			finance.PostSimpleEntry(SourceModule, runId, SalaryExpenseAccount, WagesPayableAccount, amount,
				"Payroll run " + period);
		}

		private static bool JournalExists(string runId)
		{
			var recMan = new RecordManager(null, true, true);
			var query = new EntityQuery("fin_journal_entry", "*",
				EntityQuery.QueryAND(
					EntityQuery.QueryEQ("source_module", SourceModule),
					EntityQuery.QueryEQ("source_reference", runId)));
			var resp = recMan.Find(query);
			return resp.Success && resp.Object != null && resp.Object.Data.Count > 0;
		}

		private static decimal GetDecimal(EntityRecord record, string field)
		{
			if (record.Properties.ContainsKey(field) && record.Properties[field] != null)
			{
				try { return Convert.ToDecimal(record.Properties[field]); }
				catch { return 0; }
			}
			return 0;
		}
	}
}
