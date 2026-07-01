using System;
using System.Collections.Generic;
using WebVella.Erp.Api.Models;
using WebVella.Erp.Hooks;
using WebVella.Erp.Plugins.Sales.Services;

namespace WebVella.Erp.Plugins.Sales.Hooks
{
	/// <summary>
	/// Enforces the Order-to-Cash rules on <c>sal_sales_order</c> and drives the
	/// cross-module integration additively:
	///  - a confirm transition must be allowed by the Workflow state machine
	///    (<see cref="Services.SalesService.CanTransition"/> -> WorkflowService.CanTransition);
	///  - the order total must equal the sum of its line totals;
	///  - orders that require approval (discount above threshold or over the
	///    customer credit limit) cannot be confirmed until their approval flow
	///    is approved (WorkflowService.IsApproved);
	///  - once confirmed, an AR GL entry is posted and an AR invoice is created
	///    through the Finance contract (FinanceService.PostSimpleEntry / CreateArInvoice).
	/// Auto-discovered by HookManager.RegisterHooks via the assembly scan.
	/// </summary>
	[HookAttachment("sal_sales_order")]
	public class SalesOrderHooks : IErpPreUpdateRecordHook, IErpPostUpdateRecordHook
	{
		public void OnPreUpdateRecord(string entityName, EntityRecord record, List<ErrorModel> errors)
		{
			if (record == null || !IsConfirming(record))
				return;

			var id = GetId(record);
			if (id == null)
				return;

			var svc = new SalesService();
			var current = svc.GetOrder(id.Value);
			if (current == null)
				return;

			var fromState = SalesService.GetString(current, "status", "draft");

			// The transition must be defined in the Workflow state machine.
			if (!svc.CanTransition(fromState, "confirmed"))
			{
				errors.Add(new ErrorModel
				{
					Key = "status",
					Message = $"Transition '{fromState}' -> 'confirmed' is not allowed for a sales order."
				});
				return;
			}

			// Order total must equal the sum of the line totals.
			if (!svc.IsOrderBalanced(id.Value))
			{
				errors.Add(new ErrorModel
				{
					Key = "total_amount",
					Message = "Sales order total does not match the sum of its line totals."
				});
			}

			// Over-threshold / over-credit-limit orders need an approved flow.
			var customer = GetCustomer(svc, current);
			if (svc.RequiresApproval(current, customer) && !svc.IsOrderApproved(current))
			{
				errors.Add(new ErrorModel
				{
					Key = "status",
					Message = "This sales order requires approval before it can be confirmed."
				});
			}
		}

		public void OnPostUpdateRecord(string entityName, EntityRecord record)
		{
			var id = GetId(record);
			if (id == null)
				return;

			var svc = new SalesService();
			var order = svc.GetOrder(id.Value);
			if (order == null)
				return;

			var status = SalesService.GetString(order, "status", "draft");
			if (status != "confirmed")
				return;

			var customer = GetCustomer(svc, order);
			svc.PostArForOrder(order, customer);
		}

		private static bool IsConfirming(EntityRecord record)
		{
			return record.Properties.ContainsKey("status")
				&& record.Properties["status"] != null
				&& record.Properties["status"].ToString() == "confirmed";
		}

		private static Guid? GetId(EntityRecord record)
		{
			if (record != null && record.Properties.ContainsKey("id") && record.Properties["id"] != null)
			{
				try { return (Guid)record.Properties["id"]; }
				catch { return null; }
			}
			return null;
		}

		private static EntityRecord GetCustomer(SalesService svc, EntityRecord order)
		{
			if (order != null && order.Properties.ContainsKey("customer_id") && order.Properties["customer_id"] != null)
			{
				try { return svc.GetCustomer((Guid)order.Properties["customer_id"]); }
				catch { return null; }
			}
			return null;
		}
	}
}
