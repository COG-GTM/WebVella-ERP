using System;
using System.Collections.Generic;
using WebVella.Erp.Api;
using WebVella.Erp.Api.Models;
using WebVella.Erp.Hooks;
using WebVella.Erp.Plugins.Finance.Services;
using WebVella.Erp.Plugins.Inventory.Services;
using WebVella.Erp.Plugins.Workflow.Services;

namespace WebVella.Erp.Plugins.Inventory.Hooks
{
	/// <summary>
	/// Enforces the inventory business rules additively on posting a stock movement:
	///   * stock can never go negative (issues / decreasing adjustments are rejected);
	///   * adjustments above <see cref="InventoryService.AdjustmentApprovalThreshold"/>
	///     must be routed through an approved Workflow flow;
	///   * status transitions are validated against the WorkflowService state machine.
	/// On successful posting it applies the on-hand delta and posts a balanced GL
	/// journal through FinanceService. Auto-discovered via the assembly scan.
	/// </summary>
	[HookAttachment("inv_stock_movement")]
	public class StockMovementHooks :
		IErpPreCreateRecordHook, IErpPreUpdateRecordHook,
		IErpPostCreateRecordHook, IErpPostUpdateRecordHook
	{
		public void OnPreCreateRecord(string entityName, EntityRecord record, List<ErrorModel> errors)
		{
			ValidatePosting(record, errors);
		}

		public void OnPreUpdateRecord(string entityName, EntityRecord record, List<ErrorModel> errors)
		{
			ValidateTransition(record, errors);
			ValidatePosting(record, errors);
		}

		public void OnPostCreateRecord(string entityName, EntityRecord record)
		{
			ApplyPosting(record);
		}

		public void OnPostUpdateRecord(string entityName, EntityRecord record)
		{
			ApplyPosting(record);
		}

		#region << Validation (Pre) >>

		private static void ValidateTransition(EntityRecord record, List<ErrorModel> errors)
		{
			var newStatus = GetString(record, "status", "draft");
			var id = GetGuid(record, "id");
			if (id == null)
				return;

			var svc = new InventoryService();
			var persisted = svc.GetMovement(id.Value);
			if (persisted == null)
				return;

			var currentStatus = GetString(persisted, "status", "draft");
			if (string.Equals(currentStatus, newStatus, StringComparison.Ordinal))
				return;

			var wf = new WorkflowService();
			if (!wf.CanTransition(InventoryPlugin.MovementWorkflowName, currentStatus, newStatus))
			{
				errors.Add(new ErrorModel
				{
					Key = "status",
					Message = $"Invalid stock movement transition '{currentStatus}' -> '{newStatus}'."
				});
			}
		}

		private static void ValidatePosting(EntityRecord record, List<ErrorModel> errors)
		{
			if (GetString(record, "status", "draft") != "posted")
				return;

			var itemId = GetGuid(record, "item_id");
			var warehouseId = GetGuid(record, "warehouse_id");
			if (itemId == null || warehouseId == null)
			{
				errors.Add(new ErrorModel { Key = "item_id", Message = "Stock movement requires an item and a warehouse." });
				return;
			}

			var type = GetString(record, "movement_type", "receipt");
			var quantity = GetDecimal(record, "quantity");
			var svc = new InventoryService();

			// Stock can never go negative.
			decimal delta = InventoryService.SignedDelta(type, quantity);
			decimal onHand = svc.GetOnHand(itemId.Value, warehouseId.Value);
			if (onHand + delta < 0m)
			{
				errors.Add(new ErrorModel
				{
					Key = "quantity",
					Message = $"Stock cannot go negative: on-hand {onHand}, requested delta {delta}."
				});
				return;
			}

			// Adjustments above the threshold must be approved.
			if (type == "adjustment")
			{
				decimal amount = Math.Abs(quantity) * EffectiveUnitCost(svc, itemId.Value, record);
				if (amount > InventoryService.AdjustmentApprovalThreshold)
				{
					var flowId = GetGuid(record, "approval_flow_id");
					var wf = new WorkflowService();
					if (flowId == null || !wf.IsApproved(flowId.Value))
					{
						errors.Add(new ErrorModel
						{
							Key = "approval_flow_id",
							Message = $"Adjustment of {amount:0.##} exceeds the approval threshold " +
								$"({InventoryService.AdjustmentApprovalThreshold}) and requires an approved workflow flow."
						});
					}
				}
			}
		}

		#endregion

		#region << Side effects (Post) >>

		private static void ApplyPosting(EntityRecord record)
		{
			if (GetString(record, "status", "draft") != "posted")
				return;

			var movementId = GetGuid(record, "id");
			var itemId = GetGuid(record, "item_id");
			var warehouseId = GetGuid(record, "warehouse_id");
			if (movementId == null || itemId == null || warehouseId == null)
				return;

			// Idempotency: skip if this movement has already been posted to the GL.
			if (JournalExistsForMovement(movementId.Value))
				return;

			var type = GetString(record, "movement_type", "receipt");
			var quantity = GetDecimal(record, "quantity");
			var svc = new InventoryService();

			decimal delta = InventoryService.SignedDelta(type, quantity);
			svc.ApplyStockDelta(itemId.Value, warehouseId.Value, delta);

			PostGl(svc, itemId.Value, movementId.Value, type, quantity, record);
		}

		private static void PostGl(InventoryService svc, Guid itemId, Guid movementId, string type,
			decimal quantity, EntityRecord record)
		{
			var item = svc.GetItem(itemId);
			if (item == null)
				return;

			string inventoryAccount = GetString(item, "gl_inventory_account_code", null);
			string cogsAccount = GetString(item, "gl_cogs_account_code", null);
			decimal unitCost = EffectiveUnitCost(svc, itemId, record);
			decimal amount = Math.Abs(quantity) * unitCost;
			if (amount <= 0m || string.IsNullOrWhiteSpace(inventoryAccount))
				return;

			var fin = new FinanceService();
			// Ensure the accounts this posting touches exist (idempotent by code).
			fin.EnsureAccount(inventoryAccount, "Inventory", "asset", "debit");
			fin.EnsureAccount(InventoryService.GrniClearingAccountCode, "Goods Received Not Invoiced", "liability", "credit");
			fin.EnsureAccount(InventoryService.AdjustmentAccountCode, "Inventory Adjustment", "expense", "debit");
			if (!string.IsNullOrWhiteSpace(cogsAccount))
				fin.EnsureAccount(cogsAccount, "Cost of Goods Sold", "expense", "debit");

			string reference = movementId.ToString();
			string description = $"Inventory {type} {movementId:N}";

			string debitAccount;
			string creditAccount;
			switch (type)
			{
				case "receipt":
					debitAccount = inventoryAccount;
					creditAccount = InventoryService.GrniClearingAccountCode;
					break;
				case "issue":
					debitAccount = string.IsNullOrWhiteSpace(cogsAccount) ? InventoryService.AdjustmentAccountCode : cogsAccount;
					creditAccount = inventoryAccount;
					break;
				default: // adjustment
					if (quantity >= 0m)
					{
						debitAccount = inventoryAccount;
						creditAccount = InventoryService.AdjustmentAccountCode;
					}
					else
					{
						debitAccount = InventoryService.AdjustmentAccountCode;
						creditAccount = inventoryAccount;
					}
					break;
			}

			fin.PostSimpleEntry("inventory", reference, debitAccount, creditAccount, amount, description);
		}

		private static decimal EffectiveUnitCost(InventoryService svc, Guid itemId, EntityRecord record)
		{
			decimal unitCost = GetDecimal(record, "unit_cost");
			if (unitCost > 0m)
				return unitCost;
			var item = svc.GetItem(itemId);
			return item == null ? 0m : GetDecimal(item, "standard_cost");
		}

		private static bool JournalExistsForMovement(Guid movementId)
		{
			var recMan = new RecordManager(null, true, true);
			var query = new EntityQuery("fin_journal_entry", "*",
				EntityQuery.QueryAND(
					EntityQuery.QueryEQ("source_module", "inventory"),
					EntityQuery.QueryEQ("source_reference", movementId.ToString())));
			var resp = recMan.Find(query);
			return resp.Success && resp.Object != null && resp.Object.Data.Count > 0;
		}

		#endregion

		#region << Field helpers >>

		private static string GetString(EntityRecord record, string field, string fallback)
		{
			if (record != null && record.Properties.ContainsKey(field) && record.Properties[field] != null)
				return record.Properties[field].ToString();
			return fallback;
		}

		private static Guid? GetGuid(EntityRecord record, string field)
		{
			if (record == null || !record.Properties.ContainsKey(field) || record.Properties[field] == null)
				return null;
			var value = record.Properties[field];
			if (value is Guid g) return g;
			return Guid.TryParse(value.ToString(), out var parsed) ? parsed : (Guid?)null;
		}

		private static decimal GetDecimal(EntityRecord record, string field)
		{
			if (record != null && record.Properties.ContainsKey(field) && record.Properties[field] != null)
			{
				try { return Convert.ToDecimal(record.Properties[field]); }
				catch { return 0m; }
			}
			return 0m;
		}

		#endregion
	}
}
