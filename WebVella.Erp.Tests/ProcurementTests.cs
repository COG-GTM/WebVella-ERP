using System;
using System.Collections.Generic;
using System.Linq;
using WebVella.Erp.Api;
using WebVella.Erp.Api.Models;
using WebVella.Erp.Plugins.Procurement;
using WebVella.Erp.Plugins.Procurement.Services;
using WebVella.Erp.Plugins.Workflow.Services;
using Xunit;

namespace WebVella.Erp.Tests
{
	[Collection("erp")]
	public class ProcurementTests
	{
		private readonly ErpTestFixture _fx;
		public ProcurementTests(ErpTestFixture fx) => _fx = fx;

		#region << helpers >>

		private static Guid CreateVendor(RecordManager rm, string code, string name, string payableAccount)
		{
			var id = Guid.NewGuid();
			var rec = new EntityRecord();
			rec["id"] = id;
			rec["code"] = code;
			rec["name"] = name;
			rec["default_payable_account_code"] = payableAccount;
			var resp = rm.CreateRecord("pur_vendor", rec);
			Assert.True(resp.Success, "create vendor: " + resp.Message);
			return id;
		}

		private static Guid CreatePurchaseOrder(RecordManager rm, string number, Guid? vendorId, decimal total, string status = "draft")
		{
			var id = Guid.NewGuid();
			var rec = new EntityRecord();
			rec["id"] = id;
			rec["number"] = number;
			if (vendorId.HasValue) rec["vendor_id"] = vendorId.Value;
			rec["order_date"] = DateTime.UtcNow;
			rec["status"] = status;
			rec["total_amount"] = total;
			var resp = rm.CreateRecord("pur_purchase_order", rec);
			Assert.True(resp.Success, "create purchase order: " + resp.Message);
			return id;
		}

		private static Guid CreateLine(RecordManager rm, Guid poId, string itemCode, decimal qty, decimal unitPrice)
		{
			var id = Guid.NewGuid();
			var rec = new EntityRecord();
			rec["id"] = id;
			rec["po_id"] = poId;
			rec["item_code"] = itemCode;
			rec["description"] = itemCode;
			rec["quantity"] = qty;
			rec["unit_price"] = unitPrice;
			rec["line_total"] = qty * unitPrice;
			var resp = rm.CreateRecord("pur_purchase_order_line", rec);
			Assert.True(resp.Success, "create line: " + resp.Message);
			return id;
		}

		#endregion

		[Fact]
		public void Procurement_entities_fields_and_relations_are_created()
		{
			_fx.Run(() =>
			{
				var entMan = new EntityManager();
				foreach (var name in new[] { "pur_vendor", "pur_purchase_order", "pur_purchase_order_line", "pur_goods_receipt" })
				{
					var resp = entMan.ReadEntity(name);
					Assert.True(resp.Success && resp.Object != null, $"Entity '{name}' should exist. {resp.Message}");
				}

				var po = entMan.ReadEntity("pur_purchase_order").Object;
				var poFields = po.Fields.Select(f => f.Name).ToList();
				foreach (var f in new[] { "number", "vendor_id", "order_date", "status", "total_amount" })
					Assert.Contains(f, poFields);

				var line = entMan.ReadEntity("pur_purchase_order_line").Object;
				var lineFields = line.Fields.Select(f => f.Name).ToList();
				foreach (var f in new[] { "po_id", "item_code", "description", "quantity", "unit_price", "line_total" })
					Assert.Contains(f, lineFields);

				var relMan = new EntityRelationManager();
				foreach (var rel in new[] { "pur_vendor_1n_purchase_order", "pur_purchase_order_1n_line", "pur_purchase_order_1n_goods_receipt" })
				{
					var resp = relMan.Read(rel);
					Assert.True(resp.Success && resp.Object != null, $"Relation '{rel}' should exist. {resp.Message}");
				}
			});
		}

		[Fact]
		public void Purchase_order_total_must_equal_sum_of_line_totals()
		{
			_fx.Run(() =>
			{
				var rm = new RecordManager();
				var svc = new ProcurementService();

				// balanced: 2*100 + 1*100 = 300 == total
				var balanced = CreatePurchaseOrder(rm, "PO-BAL-" + Guid.NewGuid().ToString("N").Substring(0, 6), null, 300m);
				CreateLine(rm, balanced, "A", 2m, 100m);
				CreateLine(rm, balanced, "B", 1m, 100m);
				Assert.True(svc.PurchaseOrderIsBalanced(balanced), "PO with matching totals should balance.");

				// mismatched: line sum 100 != total 300
				var mismatched = CreatePurchaseOrder(rm, "PO-MIS-" + Guid.NewGuid().ToString("N").Substring(0, 6), null, 300m);
				CreateLine(rm, mismatched, "A", 1m, 100m);
				Assert.False(svc.PurchaseOrderIsBalanced(mismatched), "PO whose total != sum of lines should not balance.");
			});
		}

		[Fact]
		public void Workflow_defines_po_approval_transitions()
		{
			_fx.Run(() =>
			{
				var wf = new WorkflowService();
				Assert.True(wf.CanTransition(ProcurementPlugin.PoApprovalWorkflow, "draft", "pending_approval"));
				Assert.True(wf.CanTransition(ProcurementPlugin.PoApprovalWorkflow, "pending_approval", "approved"));
				Assert.False(wf.CanTransition(ProcurementPlugin.PoApprovalWorkflow, "draft", "approved")); // must route through pending_approval
			});
		}

		[Fact]
		public void Submitting_an_unbalanced_po_is_blocked()
		{
			_fx.Run(() =>
			{
				var rm = new RecordManager();
				var svc = new ProcurementService();

				var poId = CreatePurchaseOrder(rm, "PO-UNBAL-" + Guid.NewGuid().ToString("N").Substring(0, 6), null, 500m);
				CreateLine(rm, poId, "A", 1m, 100m); // 100 != 500

				// service refuses to submit an unbalanced PO
				Assert.Throws<InvalidOperationException>(() => svc.SubmitForApproval(poId, new List<string> { "manager" }));

				// and the hook blocks a direct status change to pending_approval too
				var po = svc.GetPurchaseOrder(poId);
				po["status"] = "pending_approval";
				var resp = rm.UpdateRecord("pur_purchase_order", po);
				Assert.False(resp.Success, "Hook should block submitting an unbalanced PO for approval.");
			});
		}

		[Fact]
		public void Po_reaches_approved_only_after_approval_flow_approves()
		{
			_fx.Run(() =>
			{
				var rm = new RecordManager();
				var svc = new ProcurementService();
				var wf = new WorkflowService();

				var poId = CreatePurchaseOrder(rm, "PO-APR-" + Guid.NewGuid().ToString("N").Substring(0, 6), null, 200m);
				CreateLine(rm, poId, "A", 2m, 100m);

				var flowId = svc.SubmitForApproval(poId, new List<string> { "manager" });
				Assert.Equal("pending_approval", svc.GetPurchaseOrder(poId)["status"].ToString());

				// not yet approved -> Approve is a no-op and PO stays pending_approval
				Assert.False(svc.Approve(poId, flowId));
				Assert.Equal("pending_approval", svc.GetPurchaseOrder(poId)["status"].ToString());

				// record the approval decision, then the PO can move to approved
				Assert.True(wf.RecordDecision(flowId, 1, Guid.NewGuid(), true, "ok"));
				Assert.True(wf.IsApproved(flowId));
				Assert.True(svc.Approve(poId, flowId));
				Assert.Equal("approved", svc.GetPurchaseOrder(poId)["status"].ToString());
			});
		}

		[Fact]
		public void Goods_receipt_posts_gl_entry_and_ap_invoice_via_finance()
		{
			_fx.Run(() =>
			{
				var rm = new RecordManager();

				var vendorId = CreateVendor(rm, "V-" + Guid.NewGuid().ToString("N").Substring(0, 6), "Acme Supplies", "2100");
				var poId = CreatePurchaseOrder(rm, "PO-GR-" + Guid.NewGuid().ToString("N").Substring(0, 6), vendorId, 750m);
				CreateLine(rm, poId, "WIDGET", 3m, 250m);

				// creating a posted goods receipt triggers GoodsReceiptHooks -> Finance posting
				var grId = Guid.NewGuid();
				// ProcurementService builds the GL/AP reference from the goods-receipt id
				var reference = "GR-" + grId.ToString("N").Substring(0, 8);
				var gr = new EntityRecord();
				gr["id"] = grId;
				gr["po_id"] = poId;
				gr["receipt_date"] = DateTime.UtcNow;
				gr["status"] = "posted";
				var resp = rm.CreateRecord("pur_goods_receipt", gr);
				Assert.True(resp.Success, "create goods receipt: " + resp.Message);

				// a balanced fin_journal_entry sourced from procurement must exist
				var jeQuery = new EntityQuery("fin_journal_entry", "*",
					EntityQuery.QueryAND(
						EntityQuery.QueryEQ("source_module", "procurement"),
						EntityQuery.QueryEQ("source_reference", reference)));
				var jeResp = rm.Find(jeQuery);
				Assert.True(jeResp.Success && jeResp.Object != null && jeResp.Object.Data.Count > 0,
					"A fin_journal_entry should be posted for the goods receipt.");
				var je = jeResp.Object.Data[0];
				Assert.Equal(750m, Convert.ToDecimal(je["total_debit"]));
				Assert.Equal(Convert.ToDecimal(je["total_debit"]), Convert.ToDecimal(je["total_credit"]));

				// and an AP invoice for the vendor must exist
				var apQuery = new EntityQuery("fin_ap_invoice", "*", EntityQuery.QueryEQ("invoice_number", reference));
				var apResp = rm.Find(apQuery);
				Assert.True(apResp.Success && apResp.Object != null && apResp.Object.Data.Count > 0,
					"An AP invoice should be created for the goods receipt.");
				Assert.Equal(750m, Convert.ToDecimal(apResp.Object.Data[0]["amount"]));
			});
		}
	}
}
