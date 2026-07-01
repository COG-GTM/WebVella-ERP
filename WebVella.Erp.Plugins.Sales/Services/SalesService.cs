using System;
using System.Collections.Generic;
using System.Linq;
using WebVella.Erp.Api;
using WebVella.Erp.Api.Models;
using WebVella.Erp.Plugins.Finance.Services;
using WebVella.Erp.Plugins.Workflow.Services;

namespace WebVella.Erp.Plugins.Sales.Services
{
	/// <summary>
	/// Order-to-Cash domain logic. Encapsulates the sales business rules
	/// (order-total balancing, credit-limit / discount approval routing and the
	/// AR GL posting) and integrates with the Wave 1 Finance and Workflow
	/// contracts. GL posting itself is triggered additively from a hook on
	/// <c>sal_sales_order</c> (see <see cref="Hooks.SalesOrderHooks"/>).
	/// </summary>
	public class SalesService
	{
		public const string WorkflowName = "sales_order_approval";

		/// <summary>Orders with a discount above this percentage require approval.</summary>
		public const decimal DiscountApprovalThreshold = 20m;

		public const string SourceModule = "sales";
		public const string DefaultReceivableAccountCode = "1200";
		public const string DefaultRevenueAccountCode = "4000";

		private readonly RecordManager _recordManager;
		private readonly FinanceService _finance;
		private readonly WorkflowService _workflow;

		public SalesService()
		{
			_recordManager = new RecordManager(null, true, true);
			_finance = new FinanceService();
			_workflow = new WorkflowService();
		}

		#region << Reads >>

		public EntityRecord GetCustomer(Guid id)
		{
			var query = new EntityQuery("sal_customer", "*", EntityQuery.QueryEQ("id", id));
			var resp = _recordManager.Find(query);
			if (resp.Success && resp.Object != null && resp.Object.Data.Count > 0)
				return resp.Object.Data[0];
			return null;
		}

		public EntityRecord GetOrder(Guid id)
		{
			var query = new EntityQuery("sal_sales_order", "*", EntityQuery.QueryEQ("id", id));
			var resp = _recordManager.Find(query);
			if (resp.Success && resp.Object != null && resp.Object.Data.Count > 0)
				return resp.Object.Data[0];
			return null;
		}

		public List<EntityRecord> GetOrderLines(Guid orderId)
		{
			var query = new EntityQuery("sal_sales_order_line", "*", EntityQuery.QueryEQ("order_id", orderId));
			var resp = _recordManager.Find(query);
			if (resp.Success && resp.Object != null)
				return resp.Object.Data;
			return new List<EntityRecord>();
		}

		#endregion

		#region << Creates >>

		public Guid CreateCustomer(string code, string name, string receivableAccountCode, string revenueAccountCode, decimal creditLimit)
		{
			var id = Guid.NewGuid();
			var rec = new EntityRecord();
			rec["id"] = id;
			rec["code"] = code;
			rec["name"] = name;
			rec["default_receivable_account_code"] = receivableAccountCode ?? DefaultReceivableAccountCode;
			rec["revenue_account_code"] = revenueAccountCode ?? DefaultRevenueAccountCode;
			rec["credit_limit"] = creditLimit;
			var resp = _recordManager.CreateRecord("sal_customer", rec);
			if (!resp.Success)
				throw new InvalidOperationException("Sales: failed to create customer. " + resp.Message);
			return id;
		}

		public Guid CreateOrder(string number, Guid customerId, decimal totalAmount, decimal discountPct, string status = "draft")
		{
			var id = Guid.NewGuid();
			var rec = new EntityRecord();
			rec["id"] = id;
			rec["number"] = number;
			rec["customer_id"] = customerId;
			rec["order_date"] = DateTime.UtcNow;
			rec["status"] = status;
			rec["total_amount"] = totalAmount;
			rec["discount_pct"] = discountPct;
			var resp = _recordManager.CreateRecord("sal_sales_order", rec);
			if (!resp.Success)
				throw new InvalidOperationException("Sales: failed to create sales order. " + resp.Message);
			return id;
		}

		public Guid AddOrderLine(Guid orderId, string itemCode, string description, decimal quantity, decimal unitPrice)
		{
			var id = Guid.NewGuid();
			var rec = new EntityRecord();
			rec["id"] = id;
			rec["order_id"] = orderId;
			rec["item_code"] = itemCode;
			rec["description"] = description ?? string.Empty;
			rec["quantity"] = quantity;
			rec["unit_price"] = unitPrice;
			rec["line_total"] = quantity * unitPrice;
			var resp = _recordManager.CreateRecord("sal_sales_order_line", rec);
			if (!resp.Success)
				throw new InvalidOperationException("Sales: failed to create sales order line. " + resp.Message);
			return id;
		}

		#endregion

		#region << Business rules >>

		public decimal SumLineTotals(Guid orderId)
		{
			return GetOrderLines(orderId).Sum(l => GetDecimal(l, "line_total"));
		}

		/// <summary>
		/// The order total must equal the sum of its line totals.
		/// </summary>
		public bool IsOrderBalanced(Guid orderId)
		{
			var order = GetOrder(orderId);
			if (order == null)
				return false;
			return GetDecimal(order, "total_amount") == SumLineTotals(orderId);
		}

		/// <summary>
		/// An order needs workflow approval when its discount exceeds the
		/// threshold or its total exceeds the customer's credit limit.
		/// </summary>
		public bool RequiresApproval(EntityRecord order, EntityRecord customer)
		{
			if (order == null)
				return false;

			decimal discount = GetDecimal(order, "discount_pct");
			if (discount > DiscountApprovalThreshold)
				return true;

			if (customer != null)
			{
				decimal creditLimit = GetDecimal(customer, "credit_limit");
				decimal total = GetDecimal(order, "total_amount");
				if (creditLimit > 0 && total > creditLimit)
					return true;
			}

			return false;
		}

		public bool CanTransition(string fromState, string toState)
		{
			return _workflow.CanTransition(WorkflowName, fromState, toState);
		}

		/// <summary>
		/// Starts an approval flow for an order and stamps it back onto the order,
		/// moving it into the pending_approval state.
		/// </summary>
		public Guid SubmitForApproval(Guid orderId, IList<string> approverRoles)
		{
			var flowId = _workflow.StartApprovalFlow(WorkflowName, "sal_sales_order", orderId, approverRoles);
			var order = GetOrder(orderId);
			if (order == null)
				throw new InvalidOperationException("Sales: order not found for approval.");
			order["status"] = "pending_approval";
			order["approval_flow_id"] = flowId;
			var resp = _recordManager.UpdateRecord("sal_sales_order", order);
			if (!resp.Success)
				throw new InvalidOperationException("Sales: failed to submit order for approval. " + resp.Message);
			return flowId;
		}

		/// <summary>
		/// True when the order has an approval flow that has been fully approved.
		/// </summary>
		public bool IsOrderApproved(EntityRecord order)
		{
			if (order == null)
				return false;
			if (!order.Properties.ContainsKey("approval_flow_id") || order.Properties["approval_flow_id"] == null)
				return false;
			var flowId = (Guid)order.Properties["approval_flow_id"];
			return _workflow.IsApproved(flowId);
		}

		#endregion

		#region << Finance integration >>

		public EntityRecord GetJournalForOrder(Guid orderId)
		{
			var query = new EntityQuery("fin_journal_entry", "*",
				EntityQuery.QueryAND(
					EntityQuery.QueryEQ("source_module", SourceModule),
					EntityQuery.QueryEQ("source_reference", orderId.ToString())));
			var resp = _recordManager.Find(query);
			if (resp.Success && resp.Object != null && resp.Object.Data.Count > 0)
				return resp.Object.Data[0];
			return null;
		}

		public EntityRecord GetArInvoiceForOrder(string orderNumber)
		{
			var query = new EntityQuery("fin_ar_invoice", "*", EntityQuery.QueryEQ("invoice_number", orderNumber));
			var resp = _recordManager.Find(query);
			if (resp.Success && resp.Object != null && resp.Object.Data.Count > 0)
				return resp.Object.Data[0];
			return null;
		}

		/// <summary>
		/// Posts the AR GL entry (Dr Accounts Receivable, Cr Revenue) for a
		/// confirmed order and creates the matching AR invoice. Idempotent per
		/// order. Returns the created journal entry id (or the existing one).
		/// </summary>
		public Guid PostArForOrder(EntityRecord order, EntityRecord customer)
		{
			if (order == null)
				throw new InvalidOperationException("Sales: cannot post AR for a null order.");

			var orderId = (Guid)order["id"];
			var existing = GetJournalForOrder(orderId);
			if (existing != null)
				return (Guid)existing["id"];

			decimal amount = GetDecimal(order, "total_amount");
			if (amount <= 0)
				throw new InvalidOperationException("Sales: cannot post AR for an order with a non-positive total.");

			string receivableCode = GetString(customer, "default_receivable_account_code", DefaultReceivableAccountCode);
			string revenueCode = GetString(customer, "revenue_account_code", DefaultRevenueAccountCode);
			string customerName = GetString(customer, "name", "Customer");
			string orderNumber = GetString(order, "number", orderId.ToString());

			_finance.EnsureAccount(receivableCode, "Accounts Receivable", "asset", "debit");
			_finance.EnsureAccount(revenueCode, "Revenue", "revenue", "credit");

			var journalId = _finance.PostSimpleEntry(SourceModule, orderId.ToString(),
				receivableCode, revenueCode, amount, "Sales order " + orderNumber + " confirmed");

			_finance.CreateArInvoice(customerName, orderNumber, amount, null, journalId);

			return journalId;
		}

		#endregion

		#region << Helpers >>

		internal static decimal GetDecimal(EntityRecord record, string field)
		{
			if (record != null && record.Properties.ContainsKey(field) && record.Properties[field] != null)
			{
				try { return Convert.ToDecimal(record.Properties[field]); }
				catch { return 0; }
			}
			return 0;
		}

		internal static string GetString(EntityRecord record, string field, string fallback)
		{
			if (record != null && record.Properties.ContainsKey(field) && record.Properties[field] != null)
			{
				var v = record.Properties[field].ToString();
				if (!string.IsNullOrWhiteSpace(v))
					return v;
			}
			return fallback;
		}

		#endregion
	}
}
