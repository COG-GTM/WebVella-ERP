using System;
using System.Collections.Generic;
using WebVella.Erp.Api;
using WebVella.Erp.Api.Models;
using WebVella.Erp.Web.Models;

namespace WebVella.Erp.Plugins.Finance
{
	public partial class FinancePlugin : ErpPlugin
	{
		// Stable entity ids (fin_ namespace: f1a00000-...)
		internal static readonly Guid AccountEntityId = new Guid("f1a00000-0000-0000-0000-000000000001");
		internal static readonly Guid JournalEntryEntityId = new Guid("f1a00000-0000-0000-0000-000000000002");
		internal static readonly Guid JournalLineEntityId = new Guid("f1a00000-0000-0000-0000-000000000003");
		internal static readonly Guid TaxCodeEntityId = new Guid("f1a00000-0000-0000-0000-000000000004");
		internal static readonly Guid ApInvoiceEntityId = new Guid("f1a00000-0000-0000-0000-000000000005");
		internal static readonly Guid ArInvoiceEntityId = new Guid("f1a00000-0000-0000-0000-000000000006");

		private static void Patch20250101()
		{
			#region << Create Finance Application >>
			{
				var id = new Guid("fa100000-0000-0000-0000-000000000001");
				var name = "finance";
				var label = "Finance";
				var description = "Finance / General Ledger - chart of accounts, double-entry journals, AP/AR subledgers, tax codes";
				var iconClass = "fa fa-balance-scale";
				var author = "WebVella";
				var color = "#1b5e20";
				var weight = 20;
				var access = new List<Guid>();
				access.Add(new Guid("bdc56420-caf0-4030-8a0e-d264938e0cda"));

				new WebVella.Erp.Web.Services.AppService().CreateApplication(id, name, label, description, iconClass, author, color, weight, access, WebVella.Erp.Database.DbContext.Current.Transaction);
			}
			#endregion

			#region << Create Finance Entities >>
			CreateAccountEntity();
			CreateJournalEntryEntity();
			CreateJournalLineEntity();
			CreateTaxCodeEntity();
			CreateApInvoiceEntity();
			CreateArInvoiceEntity();
			#endregion

			#region << Create Sitemap Area + Nodes >>
			{
				var areaId = new Guid("f1b00000-0000-0000-0000-000000000001");
				var appId = new Guid("fa100000-0000-0000-0000-000000000001");
				new WebVella.Erp.Web.Services.AppService().CreateArea(areaId, appId, "gl", "General Ledger",
					new List<TranslationResource>(), "General Ledger", new List<TranslationResource>(),
					"fa fa-book", "#1b5e20", 1, false, new List<Guid>(),
					WebVella.Erp.Database.DbContext.Current.Transaction);

				CreateListNode(new Guid("f1c00000-0000-0000-0000-000000000001"), areaId, "accounts", "Chart of Accounts", "/finance/gl/fin_account/list/r", "fa fa-list", 1);
				CreateListNode(new Guid("f1c00000-0000-0000-0000-000000000002"), areaId, "journal-entries", "Journal Entries", "/finance/gl/fin_journal_entry/list/r", "fa fa-list", 2);
				CreateListNode(new Guid("f1c00000-0000-0000-0000-000000000003"), areaId, "tax-codes", "Tax Codes", "/finance/gl/fin_tax_code/list/r", "fa fa-list", 3);
				CreateListNode(new Guid("f1c00000-0000-0000-0000-000000000004"), areaId, "ap-invoices", "AP Invoices", "/finance/gl/fin_ap_invoice/list/r", "fa fa-list", 4);
				CreateListNode(new Guid("f1c00000-0000-0000-0000-000000000005"), areaId, "ar-invoices", "AR Invoices", "/finance/gl/fin_ar_invoice/list/r", "fa fa-list", 5);
			}
			#endregion
		}

		private static void CreateListNode(Guid id, Guid areaId, string name, string label, string url, string iconClass, int weight)
		{
			new WebVella.Erp.Web.Services.AppService().CreateAreaNode(id, areaId, name, label,
				new List<TranslationResource>(), iconClass, url, ((int)1), null, weight,
				new List<Guid>(), new List<Guid>(), new List<Guid>(), new List<Guid>(), new List<Guid>(),
				WebVella.Erp.Database.DbContext.Current.Transaction);
		}

		private static void CreateEntityWithFields(Guid entityId, string name, string label, string labelPlural, List<InputField> fields)
		{
			var entMan = new EntityManager();
			var response = entMan.CreateEntity(entityId, name, label, labelPlural, createOnlyIdField: false);
			if (!response.Success)
				throw new Exception("Finance: failed to create entity '" + name + "'. Message: " + response.Message);

			foreach (var field in fields)
			{
				var fieldResponse = entMan.CreateField(entityId, field);
				if (!fieldResponse.Success)
					throw new Exception("Finance: failed to create field '" + field.Name + "' on '" + name + "'. Message: " + fieldResponse.Message);
			}
		}

		private static InputTextField Text(string n, string l, int max = 255) =>
			new InputTextField { Id = Guid.NewGuid(), Name = n, Label = l, Required = false, Unique = false, MaxLength = max };

		private static InputGuidField GuidF(string n, string l) =>
			new InputGuidField { Id = Guid.NewGuid(), Name = n, Label = l, Required = false };

		private static InputCurrencyField Money(string n, string l) =>
			new InputCurrencyField { Id = Guid.NewGuid(), Name = n, Label = l, Required = false, DefaultValue = 0, Currency = new CurrencyType { Code = "USD" } };

		private static InputNumberField Num(string n, string l, byte dp = 0) =>
			new InputNumberField { Id = Guid.NewGuid(), Name = n, Label = l, Required = false, DefaultValue = 0, DecimalPlaces = dp };

		private static InputDateField DateF(string n, string l) =>
			new InputDateField { Id = Guid.NewGuid(), Name = n, Label = l, Required = false, Format = "yyyy-MM-dd", UseCurrentTimeAsDefaultValue = false };

		private static InputCheckboxField Bool(string n, string l, bool def) =>
			new InputCheckboxField { Id = Guid.NewGuid(), Name = n, Label = l, Required = false, DefaultValue = def };

		private static InputSelectField Select(string n, string l, string def, params (string v, string t)[] opts)
		{
			var options = new List<SelectOption>();
			foreach (var o in opts) options.Add(new SelectOption(o.v, o.t));
			return new InputSelectField { Id = Guid.NewGuid(), Name = n, Label = l, Required = false, DefaultValue = def, Options = options };
		}

		private static void CreateAccountEntity()
		{
			CreateEntityWithFields(AccountEntityId, "fin_account", "Account", "Chart of Accounts", new List<InputField>
			{
				Text("code", "Account Code", 50),
				Text("name", "Account Name", 255),
				Select("account_type", "Account Type", "asset",
					("asset", "Asset"), ("liability", "Liability"), ("equity", "Equity"), ("revenue", "Revenue"), ("expense", "Expense")),
				Select("normal_balance", "Normal Balance", "debit", ("debit", "Debit"), ("credit", "Credit")),
				Bool("is_active", "Is Active", true)
			});
		}

		private static void CreateJournalEntryEntity()
		{
			CreateEntityWithFields(JournalEntryEntityId, "fin_journal_entry", "Journal Entry", "Journal Entries", new List<InputField>
			{
				Text("entry_number", "Entry Number", 50),
				DateF("entry_date", "Entry Date"),
				Text("description", "Description", 500),
				Select("status", "Status", "draft", ("draft", "Draft"), ("posted", "Posted"), ("void", "Void")),
				Text("source_module", "Source Module", 100),
				Text("source_reference", "Source Reference", 255),
				Money("total_debit", "Total Debit"),
				Money("total_credit", "Total Credit"),
				Bool("is_balanced", "Is Balanced", false)
			});
		}

		private static void CreateJournalLineEntity()
		{
			CreateEntityWithFields(JournalLineEntityId, "fin_journal_line", "Journal Line", "Journal Lines", new List<InputField>
			{
				GuidF("journal_entry_id", "Journal Entry"),
				GuidF("account_id", "Account"),
				Text("account_code", "Account Code", 50),
				Text("description", "Description", 255),
				Money("debit", "Debit"),
				Money("credit", "Credit"),
				Num("line_number", "Line Number")
			});
		}

		private static void CreateTaxCodeEntity()
		{
			CreateEntityWithFields(TaxCodeEntityId, "fin_tax_code", "Tax Code", "Tax Codes", new List<InputField>
			{
				Text("code", "Code", 50),
				Text("name", "Name", 255),
				Num("rate", "Rate (%)", 4),
				Bool("is_active", "Is Active", true)
			});
		}

		private static void CreateApInvoiceEntity()
		{
			CreateEntityWithFields(ApInvoiceEntityId, "fin_ap_invoice", "AP Invoice", "AP Invoices", new List<InputField>
			{
				GuidF("journal_entry_id", "Journal Entry"),
				Text("vendor_name", "Vendor Name", 255),
				Text("invoice_number", "Invoice Number", 100),
				Money("amount", "Amount"),
				Select("status", "Status", "open", ("open", "Open"), ("paid", "Paid"), ("void", "Void")),
				DateF("due_date", "Due Date")
			});
		}

		private static void CreateArInvoiceEntity()
		{
			CreateEntityWithFields(ArInvoiceEntityId, "fin_ar_invoice", "AR Invoice", "AR Invoices", new List<InputField>
			{
				GuidF("journal_entry_id", "Journal Entry"),
				Text("customer_name", "Customer Name", 255),
				Text("invoice_number", "Invoice Number", 100),
				Money("amount", "Amount"),
				Select("status", "Status", "open", ("open", "Open"), ("paid", "Paid"), ("void", "Void")),
				DateF("due_date", "Due Date")
			});
		}
	}
}
