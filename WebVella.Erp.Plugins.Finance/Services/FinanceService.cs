using System;
using System.Collections.Generic;
using System.Linq;
using WebVella.Erp.Api;
using WebVella.Erp.Api.Models;

namespace WebVella.Erp.Plugins.Finance.Services
{
	/// <summary>
	/// A single line of a double-entry journal. Exactly one of Debit / Credit
	/// should be non-zero.
	/// </summary>
	public class JournalLine
	{
		public Guid? AccountId { get; set; }
		public string AccountCode { get; set; }
		public decimal Debit { get; set; }
		public decimal Credit { get; set; }
		public string Description { get; set; }

		public JournalLine() { }

		public JournalLine(string accountCode, decimal debit, decimal credit, string description = null)
		{
			AccountCode = accountCode;
			Debit = debit;
			Credit = credit;
			Description = description;
		}
	}

	/// <summary>
	/// Cross-module contract other modules (Inventory, Procurement, Sales, HR)
	/// use — via their own <c>[HookAttachment]</c> hooks — to post GL journals.
	/// New modules depend on this interface only, never on another module's internals.
	/// </summary>
	public interface IFinancePostingService
	{
		Guid PostJournal(DateTime entryDate, string description, string sourceModule, string sourceReference, IList<JournalLine> lines);
		Guid PostSimpleEntry(string sourceModule, string sourceReference, string debitAccountCode, string creditAccountCode, decimal amount, string description);
	}

	public class FinanceService : IFinancePostingService
	{
		private readonly RecordManager _recordManager;

		public FinanceService(bool useTransaction = true)
		{
			_recordManager = new RecordManager(null, true, true);
		}

		/// <summary>
		/// Posts a balanced double-entry journal. Throws if the journal does not
		/// balance (total debits must equal total credits and be greater than zero).
		/// Creates one fin_journal_entry and one fin_journal_line per line.
		/// </summary>
		public Guid PostJournal(DateTime entryDate, string description, string sourceModule, string sourceReference, IList<JournalLine> lines)
		{
			if (lines == null || lines.Count == 0)
				throw new InvalidOperationException("Finance: a journal entry must have at least one line.");

			decimal totalDebit = lines.Sum(l => l.Debit);
			decimal totalCredit = lines.Sum(l => l.Credit);

			if (totalDebit <= 0 && totalCredit <= 0)
				throw new InvalidOperationException("Finance: a journal entry must have non-zero debit/credit amounts.");

			if (totalDebit != totalCredit)
				throw new InvalidOperationException(
					$"Finance: journal does not balance. Total debit {totalDebit} != total credit {totalCredit}.");

			var journalId = Guid.NewGuid();
			var journal = new EntityRecord();
			journal["id"] = journalId;
			journal["entry_number"] = "JE-" + DateTime.UtcNow.Ticks;
			journal["entry_date"] = entryDate;
			journal["description"] = description ?? string.Empty;
			journal["status"] = "posted";
			journal["source_module"] = sourceModule ?? string.Empty;
			journal["source_reference"] = sourceReference ?? string.Empty;
			journal["total_debit"] = totalDebit;
			journal["total_credit"] = totalCredit;
			journal["is_balanced"] = true;

			var jResp = _recordManager.CreateRecord("fin_journal_entry", journal);
			if (!jResp.Success)
				throw new InvalidOperationException("Finance: failed to create journal entry. " + jResp.Message);

			int lineNo = 1;
			foreach (var line in lines)
			{
				var lineRec = new EntityRecord();
				lineRec["id"] = Guid.NewGuid();
				lineRec["journal_entry_id"] = journalId;
				if (line.AccountId.HasValue)
					lineRec["account_id"] = line.AccountId.Value;
				lineRec["account_code"] = line.AccountCode ?? string.Empty;
				lineRec["description"] = line.Description ?? string.Empty;
				lineRec["debit"] = line.Debit;
				lineRec["credit"] = line.Credit;
				lineRec["line_number"] = (decimal)lineNo++;

				var lResp = _recordManager.CreateRecord("fin_journal_line", lineRec);
				if (!lResp.Success)
					throw new InvalidOperationException("Finance: failed to create journal line. " + lResp.Message);
			}

			return journalId;
		}

		/// <summary>
		/// Convenience helper used by other modules' hooks: posts a two-line balanced
		/// journal debiting one account and crediting another for the same amount.
		/// </summary>
		public Guid PostSimpleEntry(string sourceModule, string sourceReference, string debitAccountCode, string creditAccountCode, decimal amount, string description)
		{
			if (amount <= 0)
				throw new InvalidOperationException("Finance: posting amount must be positive.");

			var lines = new List<JournalLine>
			{
				new JournalLine(debitAccountCode, amount, 0, description),
				new JournalLine(creditAccountCode, 0, amount, description)
			};
			return PostJournal(DateTime.UtcNow, description, sourceModule, sourceReference, lines);
		}

		/// <summary>
		/// Ensures a chart-of-accounts account exists (idempotent by code).
		/// </summary>
		public Guid EnsureAccount(string code, string name, string accountType, string normalBalance)
		{
			var existing = GetAccountByCode(code);
			if (existing != null)
				return (Guid)existing["id"];

			var id = Guid.NewGuid();
			var rec = new EntityRecord();
			rec["id"] = id;
			rec["code"] = code;
			rec["name"] = name;
			rec["account_type"] = accountType;
			rec["normal_balance"] = normalBalance;
			rec["is_active"] = true;
			var resp = _recordManager.CreateRecord("fin_account", rec);
			if (!resp.Success)
				throw new InvalidOperationException("Finance: failed to create account '" + code + "'. " + resp.Message);
			return id;
		}

		public EntityRecord GetAccountByCode(string code)
		{
			var query = new EntityQuery("fin_account", "*", EntityQuery.QueryEQ("code", code));
			var resp = _recordManager.Find(query);
			if (resp.Success && resp.Object != null && resp.Object.Data.Count > 0)
				return resp.Object.Data[0];
			return null;
		}

		public EntityRecord GetJournalEntry(Guid journalId)
		{
			var query = new EntityQuery("fin_journal_entry", "*", EntityQuery.QueryEQ("id", journalId));
			var resp = _recordManager.Find(query);
			if (resp.Success && resp.Object != null && resp.Object.Data.Count > 0)
				return resp.Object.Data[0];
			return null;
		}

		public List<EntityRecord> GetJournalLines(Guid journalId)
		{
			var query = new EntityQuery("fin_journal_line", "*", EntityQuery.QueryEQ("journal_entry_id", journalId));
			var resp = _recordManager.Find(query);
			if (resp.Success && resp.Object != null)
				return resp.Object.Data;
			return new List<EntityRecord>();
		}

		public Guid CreateApInvoice(string vendorName, string invoiceNumber, decimal amount, DateTime? dueDate, Guid? journalId)
		{
			var id = Guid.NewGuid();
			var rec = new EntityRecord();
			rec["id"] = id;
			rec["vendor_name"] = vendorName;
			rec["invoice_number"] = invoiceNumber;
			rec["amount"] = amount;
			rec["status"] = "open";
			if (dueDate.HasValue) rec["due_date"] = dueDate.Value;
			if (journalId.HasValue) rec["journal_entry_id"] = journalId.Value;
			var resp = _recordManager.CreateRecord("fin_ap_invoice", rec);
			if (!resp.Success)
				throw new InvalidOperationException("Finance: failed to create AP invoice. " + resp.Message);
			return id;
		}

		public Guid CreateArInvoice(string customerName, string invoiceNumber, decimal amount, DateTime? dueDate, Guid? journalId)
		{
			var id = Guid.NewGuid();
			var rec = new EntityRecord();
			rec["id"] = id;
			rec["customer_name"] = customerName;
			rec["invoice_number"] = invoiceNumber;
			rec["amount"] = amount;
			rec["status"] = "open";
			if (dueDate.HasValue) rec["due_date"] = dueDate.Value;
			if (journalId.HasValue) rec["journal_entry_id"] = journalId.Value;
			var resp = _recordManager.CreateRecord("fin_ar_invoice", rec);
			if (!resp.Success)
				throw new InvalidOperationException("Finance: failed to create AR invoice. " + resp.Message);
			return id;
		}
	}
}
