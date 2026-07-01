using System;
using System.Collections.Generic;
using System.Linq;
using WebVella.Erp.Api;
using WebVella.Erp.Api.Models;
using WebVella.Erp.Plugins.Sales.Services;
using WebVella.Erp.Plugins.Workflow.Services;
using Xunit;

namespace WebVella.Erp.Tests
{
	[Collection("erp")]
	public class SalesTests
	{
		private readonly ErpTestFixture _fx;
		public SalesTests(ErpTestFixture fx) => _fx = fx;

		private static string Uniq(string prefix) => prefix + "-" + Guid.NewGuid().ToString("N").Substring(0, 8);

		private static void Confirm(Guid orderId, out QueryResponse resp)
		{
			var svc = new SalesService();
			var order = svc.GetOrder(orderId);
			order["status"] = "confirmed";
			resp = new RecordManager().UpdateRecord("sal_sales_order", order);
		}

		[Fact]
		public void Sales_entities_fields_and_relations_are_created()
		{
			_fx.Run(() =>
			{
				var entMan = new EntityManager();
				foreach (var name in new[] { "sal_customer", "sal_sales_order", "sal_sales_order_line" })
				{
					var resp = entMan.ReadEntity(name);
					Assert.True(resp.Success && resp.Object != null, $"Entity '{name}' should exist. {resp.Message}");
				}

				var order = entMan.ReadEntity("sal_sales_order").Object;
				var fieldNames = order.Fields.Select(f => f.Name).ToList();
				foreach (var f in new[] { "number", "customer_id", "order_date", "status", "total_amount", "discount_pct" })
					Assert.Contains(f, fieldNames);

				var customer = entMan.ReadEntity("sal_customer").Object;
				var custFields = customer.Fields.Select(f => f.Name).ToList();
				foreach (var f in new[] { "code", "name", "default_receivable_account_code", "revenue_account_code", "credit_limit" })
					Assert.Contains(f, custFields);

				var line = entMan.ReadEntity("sal_sales_order_line").Object;
				var lineFields = line.Fields.Select(f => f.Name).ToList();
				foreach (var f in new[] { "order_id", "item_code", "description", "quantity", "unit_price", "line_total" })
					Assert.Contains(f, lineFields);

				var relMan = new EntityRelationManager();
				Assert.NotNull(relMan.Read("sal_customer_1_n_sales_order").Object);
				Assert.NotNull(relMan.Read("sal_sales_order_1_n_order_line").Object);
			});
		}

		[Fact]
		public void Balanced_order_confirms_and_posts_ar_gl_entry_and_invoice()
		{
			_fx.Run(() =>
			{
				var svc = new SalesService();
				var custId = svc.CreateCustomer(Uniq("CUST"), "Acme Corp", "1200", "4000", 100000m);
				var number = Uniq("SO");
				var orderId = svc.CreateOrder(number, custId, 300m, 0m);
				svc.AddOrderLine(orderId, "ITEM-1", "Widget", 2m, 100m); // 200
				svc.AddOrderLine(orderId, "ITEM-2", "Gadget", 1m, 100m); // 100

				Assert.True(svc.IsOrderBalanced(orderId), "Order total should equal the sum of line totals.");

				Confirm(orderId, out var resp);
				Assert.True(resp.Success, "Balanced order should confirm. " + resp.Message);

				// AR GL entry posted via Finance
				var journal = svc.GetJournalForOrder(orderId);
				Assert.NotNull(journal);
				Assert.Equal("posted", journal["status"].ToString());
				Assert.Equal(300m, Convert.ToDecimal(journal["total_debit"]));
				Assert.Equal(300m, Convert.ToDecimal(journal["total_credit"]));

				// double-entry: Dr AR (1200), Cr Revenue (4000)
				var lines = new WebVella.Erp.Plugins.Finance.Services.FinanceService().GetJournalLines((Guid)journal["id"]);
				Assert.Equal(2, lines.Count);
				var debit = lines.Single(l => Convert.ToDecimal(l["debit"]) > 0);
				var credit = lines.Single(l => Convert.ToDecimal(l["credit"]) > 0);
				Assert.Equal("1200", debit["account_code"].ToString());
				Assert.Equal("4000", credit["account_code"].ToString());

				// AR invoice created
				var invoice = svc.GetArInvoiceForOrder(number);
				Assert.NotNull(invoice);
				Assert.Equal(300m, Convert.ToDecimal(invoice["amount"]));
			});
		}

		[Fact]
		public void Mismatched_order_total_cannot_be_confirmed()
		{
			_fx.Run(() =>
			{
				var svc = new SalesService();
				var custId = svc.CreateCustomer(Uniq("CUST"), "Beta LLC", "1200", "4000", 100000m);
				var orderId = svc.CreateOrder(Uniq("SO"), custId, 100m, 0m);
				svc.AddOrderLine(orderId, "ITEM-1", "Widget", 1m, 90m); // sum 90 != 100

				Assert.False(svc.IsOrderBalanced(orderId), "Order should be detected as unbalanced.");

				Confirm(orderId, out var resp);
				Assert.False(resp.Success, "Unbalanced order must not confirm.");
				Assert.Null(svc.GetJournalForOrder(orderId));
			});
		}

		[Fact]
		public void Over_threshold_discount_order_requires_approval_before_confirm()
		{
			_fx.Run(() =>
			{
				var svc = new SalesService();
				var custId = svc.CreateCustomer(Uniq("CUST"), "Gamma Inc", "1200", "4000", 100000m);
				var number = Uniq("SO");
				var orderId = svc.CreateOrder(number, custId, 500m, 50m); // 50% discount > 20% threshold
				svc.AddOrderLine(orderId, "ITEM-1", "Bulk", 5m, 100m); // 500

				var order = svc.GetOrder(orderId);
				var customer = svc.GetCustomer(custId);
				Assert.True(svc.RequiresApproval(order, customer), "High-discount order should require approval.");

				// Cannot confirm before approval
				Confirm(orderId, out var blocked);
				Assert.False(blocked.Success, "Over-threshold order must not confirm before approval.");
				Assert.Null(svc.GetJournalForOrder(orderId));

				// Route through Workflow approval
				var flowId = svc.SubmitForApproval(orderId, new List<string> { "sales_manager" });
				Assert.False(svc.IsOrderApproved(svc.GetOrder(orderId)));

				var wf = new WorkflowService();
				Assert.True(wf.RecordDecision(flowId, 1, Guid.NewGuid(), true, "approved"));
				Assert.True(wf.IsApproved(flowId));

				// Now confirmation succeeds and posts the GL entry
				Confirm(orderId, out var confirmed);
				Assert.True(confirmed.Success, "Approved order should confirm. " + confirmed.Message);
				Assert.NotNull(svc.GetJournalForOrder(orderId));
				Assert.NotNull(svc.GetArInvoiceForOrder(number));
			});
		}

		[Fact]
		public void Sales_workflow_states_and_transitions_are_defined()
		{
			_fx.Run(() =>
			{
				var wf = new WorkflowService();
				Assert.True(wf.CanTransition(SalesService.WorkflowName, "draft", "confirmed"));
				Assert.True(wf.CanTransition(SalesService.WorkflowName, "pending_approval", "confirmed"));
				Assert.True(wf.CanTransition(SalesService.WorkflowName, "confirmed", "fulfilled"));
				Assert.False(wf.CanTransition(SalesService.WorkflowName, "draft", "fulfilled"));
			});
		}
	}
}
