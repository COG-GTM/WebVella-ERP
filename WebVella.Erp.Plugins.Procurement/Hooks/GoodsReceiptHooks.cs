using System.Collections.Generic;
using WebVella.Erp.Api.Models;
using WebVella.Erp.Hooks;
using WebVella.Erp.Plugins.Procurement.Services;

namespace WebVella.Erp.Plugins.Procurement.Hooks
{
	/// <summary>
	/// Posts the Finance side of a goods receipt once it is marked as posted:
	/// a balanced GL entry (Dr Inventory/Expense, Cr AP/GRNI) via
	/// <see cref="FinanceService.PostSimpleEntry"/> and an AP invoice via
	/// <see cref="FinanceService.CreateApInvoice"/>. This keeps the cross-module
	/// integration additive — Finance is never edited, only called.
	/// Auto-discovered by the assembly scan via <see cref="HookAttachment"/>.
	/// </summary>
	[HookAttachment("pur_goods_receipt")]
	public class GoodsReceiptHooks : IErpPostCreateRecordHook, IErpPostUpdateRecordHook
	{
		public void OnPostCreateRecord(string entityName, EntityRecord record)
		{
			PostIfReceived(record);
		}

		public void OnPostUpdateRecord(string entityName, EntityRecord record)
		{
			PostIfReceived(record);
		}

		private static void PostIfReceived(EntityRecord record)
		{
			if (record == null || !record.Properties.ContainsKey("status") || record["status"] == null)
				return;

			if (record["status"].ToString() != "posted")
				return;

			new ProcurementService().PostGoodsReceipt(record);
		}
	}
}
