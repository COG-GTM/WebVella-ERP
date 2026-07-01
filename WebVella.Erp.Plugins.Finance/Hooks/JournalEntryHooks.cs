using System;
using System.Collections.Generic;
using WebVella.Erp.Api.Models;
using WebVella.Erp.Hooks;

namespace WebVella.Erp.Plugins.Finance.Hooks
{
	/// <summary>
	/// Enforces the double-entry balancing rule at the record level: a journal
	/// entry that is being posted must have total debit == total credit.
	/// Auto-discovered by HookManager.RegisterHooks via the assembly scan.
	/// </summary>
	[HookAttachment("fin_journal_entry")]
	public class JournalEntryHooks : IErpPreCreateRecordHook, IErpPreUpdateRecordHook
	{
		public void OnPreCreateRecord(string entityName, EntityRecord record, List<ErrorModel> errors)
		{
			Validate(record, errors);
		}

		public void OnPreUpdateRecord(string entityName, EntityRecord record, List<ErrorModel> errors)
		{
			Validate(record, errors);
		}

		private static void Validate(EntityRecord record, List<ErrorModel> errors)
		{
			if (record == null)
				return;

			var status = record.Properties.ContainsKey("status") && record.Properties["status"] != null
				? record.Properties["status"].ToString()
				: "draft";

			// Only enforce balancing once the entry is being posted.
			if (status != "posted")
				return;

			decimal totalDebit = GetDecimal(record, "total_debit");
			decimal totalCredit = GetDecimal(record, "total_credit");

			if (totalDebit != totalCredit)
			{
				errors.Add(new ErrorModel
				{
					Key = "total_debit",
					Message = $"Journal entry does not balance: total debit ({totalDebit}) must equal total credit ({totalCredit})."
				});
			}
			else if (totalDebit <= 0)
			{
				errors.Add(new ErrorModel
				{
					Key = "total_debit",
					Message = "A posted journal entry must have non-zero amounts."
				});
			}
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
