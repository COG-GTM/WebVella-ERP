using System;
using System.Collections.Generic;
using System.Linq;
using WebVella.Erp.Api;
using WebVella.Erp.Api.Models;
using WebVella.Erp.Plugins.Finance.Services;
using WebVella.Erp.Plugins.Workflow.Services;

namespace WebVella.Erp.Plugins.Procurement.Services
{
	/// <summary>
	/// Procurement business logic: purchase-order total validation, workflow-driven
	/// approval routing (via <see cref="WorkflowService"/>) and goods-receipt GL
	/// posting (via <see cref="FinanceService"/>). Cross-module calls go only
	/// through the Finance / Workflow public contracts.
	/// </summary>
	public class ProcurementService
	{
		private readonly RecordManager _recordManager;
		private readonly WorkflowService _workflow;
		private readonly FinanceService _finance;

		// GL account used for the debit side of a goods receipt (inventory / expense).
		public const string InventoryAccountCode = "1400";
		// Fallback credit account (GRNI / AP) when a vendor has no payable account configured.
		public const string DefaultPayableAccountCode = "2000";

		public ProcurementService()
		{
			_recordManager = new RecordManager(null, true, true);
			_workflow = new WorkflowService();
			_finance = new FinanceService();
		}

		#region << Purchase order totals >>

		public decimal SumLineTotals(Guid purchaseOrderId)
		{
			var query = new EntityQuery("pur_purchase_order_line", "*", EntityQuery.QueryEQ("po_id", purchaseOrderId));
			var resp = _recordManager.Find(query);
			if (!resp.Success || resp.Object == null)
				return 0m;
			return resp.Object.Data.Sum(l => l.Properties.ContainsKey("line_total") && l["line_total"] != null
				? Convert.ToDecimal(l["line_total"]) : 0m);
		}

		public EntityRecord GetPurchaseOrder(Guid purchaseOrderId)
		{
			var query = new EntityQuery("pur_purchase_order", "*", EntityQuery.QueryEQ("id", purchaseOrderId));
			var resp = _recordManager.Find(query);
			if (resp.Success && resp.Object != null && resp.Object.Data.Count > 0)
				return resp.Object.Data[0];
			return null;
		}

		/// <summary>
		/// True when the purchase order's total_amount equals the sum of its line totals.
		/// </summary>
		public bool PurchaseOrderIsBalanced(Guid purchaseOrderId)
		{
			var po = GetPurchaseOrder(purchaseOrderId);
			if (po == null)
				return false;
			decimal total = po.Properties.ContainsKey("total_amount") && po["total_amount"] != null
				? Convert.ToDecimal(po["total_amount"]) : 0m;
			return total == SumLineTotals(purchaseOrderId);
		}

		#endregion

		#region << Workflow approval routing >>

		/// <summary>
		/// Submits a draft PO for approval: validates it balances, checks the
		/// draft->pending_approval transition is configured, moves the PO to
		/// pending_approval and starts an approval flow. Returns the flow id.
		/// </summary>
		public Guid SubmitForApproval(Guid purchaseOrderId, IList<string> approverRoles)
		{
			if (!PurchaseOrderIsBalanced(purchaseOrderId))
				throw new InvalidOperationException("Procurement: purchase order total does not equal the sum of its line totals.");

			if (!_workflow.CanTransition(ProcurementPlugin.PoApprovalWorkflow, "draft", "pending_approval"))
				throw new InvalidOperationException("Procurement: draft->pending_approval transition is not configured.");

			var po = GetPurchaseOrder(purchaseOrderId);
			if (po == null)
				throw new InvalidOperationException("Procurement: purchase order not found.");
			po["status"] = "pending_approval";
			var upd = _recordManager.UpdateRecord("pur_purchase_order", po);
			if (!upd.Success)
				throw new InvalidOperationException("Procurement: failed to move purchase order to pending_approval. " + upd.Message);

			return _workflow.StartApprovalFlow(ProcurementPlugin.PoApprovalWorkflow, "pur_purchase_order", purchaseOrderId, approverRoles);
		}

		/// <summary>
		/// Approves a PO only when the approval flow is fully approved and the
		/// pending_approval->approved transition is configured. Returns false
		/// (leaving the PO in pending_approval) when the flow is not yet approved.
		/// </summary>
		public bool Approve(Guid purchaseOrderId, Guid flowId)
		{
			if (!_workflow.IsApproved(flowId))
				return false;

			if (!_workflow.CanTransition(ProcurementPlugin.PoApprovalWorkflow, "pending_approval", "approved"))
				throw new InvalidOperationException("Procurement: pending_approval->approved transition is not configured.");

			var po = GetPurchaseOrder(purchaseOrderId);
			if (po == null)
				throw new InvalidOperationException("Procurement: purchase order not found.");
			po["status"] = "approved";
			var upd = _recordManager.UpdateRecord("pur_purchase_order", po);
			if (!upd.Success)
				throw new InvalidOperationException("Procurement: failed to approve purchase order. " + upd.Message);
			return true;
		}

		#endregion

		#region << Goods receipt GL posting >>

		public EntityRecord GetVendor(Guid vendorId)
		{
			var query = new EntityQuery("pur_vendor", "*", EntityQuery.QueryEQ("id", vendorId));
			var resp = _recordManager.Find(query);
			if (resp.Success && resp.Object != null && resp.Object.Data.Count > 0)
				return resp.Object.Data[0];
			return null;
		}

		/// <summary>
		/// Posts the GL side of a goods receipt: a balanced double entry
		/// (Dr Inventory/Expense, Cr AP/GRNI) via <see cref="FinanceService.PostSimpleEntry"/>
		/// plus an AP invoice via <see cref="FinanceService.CreateApInvoice"/>.
		/// Returns the created journal-entry and AP-invoice ids.
		/// </summary>
		public (Guid journalEntryId, Guid apInvoiceId) PostGoodsReceipt(EntityRecord goodsReceipt)
		{
			if (goodsReceipt == null)
				throw new InvalidOperationException("Procurement: goods receipt is required.");

			var receiptId = (Guid)goodsReceipt["id"];
			var poId = goodsReceipt.Properties.ContainsKey("po_id") && goodsReceipt["po_id"] != null
				? (Guid)goodsReceipt["po_id"] : Guid.Empty;

			var po = poId != Guid.Empty ? GetPurchaseOrder(poId) : null;
			if (po == null)
				throw new InvalidOperationException("Procurement: goods receipt is not linked to a purchase order.");

			decimal amount = po.Properties.ContainsKey("total_amount") && po["total_amount"] != null
				? Convert.ToDecimal(po["total_amount"]) : 0m;
			if (amount <= 0)
				throw new InvalidOperationException("Procurement: purchase order total must be positive to post a goods receipt.");

			string vendorName = "Unknown Vendor";
			string payableAccount = DefaultPayableAccountCode;
			if (po.Properties.ContainsKey("vendor_id") && po["vendor_id"] != null)
			{
				var vendor = GetVendor((Guid)po["vendor_id"]);
				if (vendor != null)
				{
					if (vendor.Properties.ContainsKey("name") && vendor["name"] != null)
						vendorName = vendor["name"].ToString();
					if (vendor.Properties.ContainsKey("default_payable_account_code") && vendor["default_payable_account_code"] != null
						&& !string.IsNullOrWhiteSpace(vendor["default_payable_account_code"].ToString()))
						payableAccount = vendor["default_payable_account_code"].ToString();
				}
			}

			// Ensure both GL accounts exist so posting never fails on a missing account.
			_finance.EnsureAccount(InventoryAccountCode, "Inventory", "asset", "debit");
			_finance.EnsureAccount(payableAccount, "Accounts Payable", "liability", "credit");

			var reference = "GR-" + receiptId.ToString("N").Substring(0, 8);
			var description = "Goods receipt for PO " + (po.Properties.ContainsKey("number") ? po["number"] : poId.ToString());

			var journalId = _finance.PostSimpleEntry("procurement", reference, InventoryAccountCode, payableAccount, amount, description);
			var apInvoiceId = _finance.CreateApInvoice(vendorName, reference, amount, DateTime.UtcNow.AddDays(30), journalId);

			return (journalId, apInvoiceId);
		}

		#endregion
	}
}
