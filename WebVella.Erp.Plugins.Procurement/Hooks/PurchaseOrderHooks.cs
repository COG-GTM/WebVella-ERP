using System;
using System.Collections.Generic;
using WebVella.Erp.Api;
using WebVella.Erp.Api.Models;
using WebVella.Erp.Hooks;
using WebVella.Erp.Plugins.Procurement.Services;
using WebVella.Erp.Plugins.Workflow.Services;

namespace WebVella.Erp.Plugins.Procurement.Hooks
{
	/// <summary>
	/// Enforces the purchase-order approval state machine at the record level.
	/// A status change into an approval state (pending_approval / approved) is only
	/// allowed when the po_approval workflow defines that transition, and a PO must
	/// balance (total_amount == sum of line totals) before it can be submitted.
	/// Auto-discovered by the assembly scan via <see cref="HookAttachment"/>.
	/// </summary>
	[HookAttachment("pur_purchase_order")]
	public class PurchaseOrderHooks : IErpPreUpdateRecordHook
	{
		public void OnPreUpdateRecord(string entityName, EntityRecord record, List<ErrorModel> errors)
		{
			if (record == null || !record.Properties.ContainsKey("status") || record["status"] == null)
				return;

			var newStatus = record["status"].ToString();

			// Only the approval states are governed by the workflow engine.
			if (newStatus != "pending_approval" && newStatus != "approved")
				return;

			if (!record.Properties.ContainsKey("id") || record["id"] == null)
				return;
			var poId = (Guid)record["id"];

			var service = new ProcurementService();
			var current = service.GetPurchaseOrder(poId);
			var oldStatus = current != null && current.Properties.ContainsKey("status") && current["status"] != null
				? current["status"].ToString()
				: "draft";

			if (oldStatus == newStatus)
				return;

			var workflow = new WorkflowService();
			if (!workflow.CanTransition(ProcurementPlugin.PoApprovalWorkflow, oldStatus, newStatus))
			{
				errors.Add(new ErrorModel
				{
					Key = "status",
					Message = $"Purchase order transition '{oldStatus}' -> '{newStatus}' is not allowed by the {ProcurementPlugin.PoApprovalWorkflow} workflow."
				});
				return;
			}

			if (newStatus == "pending_approval" && !service.PurchaseOrderIsBalanced(poId))
			{
				errors.Add(new ErrorModel
				{
					Key = "total_amount",
					Message = "Purchase order total must equal the sum of its line totals before it can be submitted for approval."
				});
			}
		}
	}
}
