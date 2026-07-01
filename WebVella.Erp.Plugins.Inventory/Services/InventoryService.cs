using System;
using System.Collections.Generic;
using System.Linq;
using WebVella.Erp.Api;
using WebVella.Erp.Api.Models;

namespace WebVella.Erp.Plugins.Inventory.Services
{
	/// <summary>
	/// Inventory domain helper: manages items, warehouses, stock levels and stock
	/// movements. The heavy business rules (negative-stock rejection, GL posting,
	/// approval routing) are enforced additively by <c>StockMovementHooks</c> when a
	/// movement is posted; this service provides the read/write helpers those hooks
	/// and tests rely on.
	/// </summary>
	public class InventoryService
	{
		private readonly RecordManager _rec;

		/// <summary>
		/// Adjustments whose absolute value (quantity * unit cost) exceeds this
		/// threshold must be routed through the Workflow approval engine before they
		/// can be posted.
		/// </summary>
		public const decimal AdjustmentApprovalThreshold = 1000m;

		/// <summary>Goods-Received-Not-Invoiced clearing account credited on receipts.</summary>
		public const string GrniClearingAccountCode = "2150";

		/// <summary>Inventory adjustment gain/loss account used for adjustment postings.</summary>
		public const string AdjustmentAccountCode = "5900";

		public InventoryService()
		{
			_rec = new RecordManager(null, true, true);
		}

		#region << Items & Warehouses >>

		public Guid EnsureItem(string code, string name, string unitOfMeasure, decimal standardCost,
			string glInventoryAccountCode, string glCogsAccountCode)
		{
			var existing = GetItemByCode(code);
			if (existing != null)
				return (Guid)existing["id"];

			var id = Guid.NewGuid();
			var rec = new EntityRecord();
			rec["id"] = id;
			rec["code"] = code;
			rec["name"] = name;
			rec["unit_of_measure"] = unitOfMeasure;
			rec["standard_cost"] = standardCost;
			rec["gl_inventory_account_code"] = glInventoryAccountCode;
			rec["gl_cogs_account_code"] = glCogsAccountCode;
			var resp = _rec.CreateRecord("inv_item", rec);
			if (!resp.Success)
				throw new InvalidOperationException("Inventory: failed to create item '" + code + "'. " + resp.Message);
			return id;
		}

		public Guid EnsureWarehouse(string code, string name)
		{
			var existing = GetWarehouseByCode(code);
			if (existing != null)
				return (Guid)existing["id"];

			var id = Guid.NewGuid();
			var rec = new EntityRecord();
			rec["id"] = id;
			rec["code"] = code;
			rec["name"] = name;
			var resp = _rec.CreateRecord("inv_warehouse", rec);
			if (!resp.Success)
				throw new InvalidOperationException("Inventory: failed to create warehouse '" + code + "'. " + resp.Message);
			return id;
		}

		public EntityRecord GetItemByCode(string code) => FindOne("inv_item", "code", code);
		public EntityRecord GetItem(Guid id) => FindOne("inv_item", "id", id);
		public EntityRecord GetWarehouseByCode(string code) => FindOne("inv_warehouse", "code", code);
		public EntityRecord GetWarehouse(Guid id) => FindOne("inv_warehouse", "id", id);

		#endregion

		#region << Stock levels >>

		public EntityRecord GetStockLevel(Guid itemId, Guid warehouseId)
		{
			var query = new EntityQuery("inv_stock_level", "*",
				EntityQuery.QueryAND(
					EntityQuery.QueryEQ("item_id", itemId),
					EntityQuery.QueryEQ("warehouse_id", warehouseId)));
			var resp = _rec.Find(query);
			if (resp.Success && resp.Object != null && resp.Object.Data.Count > 0)
				return resp.Object.Data[0];
			return null;
		}

		public decimal GetOnHand(Guid itemId, Guid warehouseId)
		{
			var level = GetStockLevel(itemId, warehouseId);
			if (level == null)
				return 0m;
			return ToDecimal(level["quantity_on_hand"]);
		}

		/// <summary>Ensures a stock-level row exists for the item/warehouse pair (qty 0).</summary>
		public Guid EnsureStockLevel(Guid itemId, Guid warehouseId)
		{
			var level = GetStockLevel(itemId, warehouseId);
			if (level != null)
				return (Guid)level["id"];

			var id = Guid.NewGuid();
			var rec = new EntityRecord();
			rec["id"] = id;
			rec["item_id"] = itemId;
			rec["warehouse_id"] = warehouseId;
			rec["quantity_on_hand"] = 0m;
			var resp = _rec.CreateRecord("inv_stock_level", rec);
			if (!resp.Success)
				throw new InvalidOperationException("Inventory: failed to create stock level. " + resp.Message);
			return id;
		}

		/// <summary>
		/// Applies a signed delta to on-hand quantity, creating the stock-level row
		/// if needed. Rejects a delta that would drive stock below zero.
		/// </summary>
		public void ApplyStockDelta(Guid itemId, Guid warehouseId, decimal delta)
		{
			EnsureStockLevel(itemId, warehouseId);
			var level = GetStockLevel(itemId, warehouseId);
			decimal current = ToDecimal(level["quantity_on_hand"]);
			decimal updated = current + delta;
			if (updated < 0m)
				throw new InvalidOperationException(
					$"Inventory: stock cannot go negative (on-hand {current}, delta {delta}).");

			level["quantity_on_hand"] = updated;
			var resp = _rec.UpdateRecord("inv_stock_level", level);
			if (!resp.Success)
				throw new InvalidOperationException("Inventory: failed to update stock level. " + resp.Message);
		}

		#endregion

		#region << Stock movements >>

		/// <summary>
		/// The signed on-hand delta a movement produces: receipts add, issues subtract,
		/// adjustments use the (possibly negative) quantity as-is.
		/// </summary>
		public static decimal SignedDelta(string movementType, decimal quantity)
		{
			switch (movementType)
			{
				case "receipt": return Math.Abs(quantity);
				case "issue": return -Math.Abs(quantity);
				default: return quantity; // adjustment (signed)
			}
		}

		public Guid CreateDraftMovement(Guid itemId, Guid warehouseId, string movementType,
			decimal quantity, decimal unitCost, string reference)
		{
			var id = Guid.NewGuid();
			var rec = new EntityRecord();
			rec["id"] = id;
			rec["item_id"] = itemId;
			rec["warehouse_id"] = warehouseId;
			rec["movement_type"] = movementType;
			rec["quantity"] = quantity;
			rec["unit_cost"] = unitCost;
			rec["reference"] = reference ?? string.Empty;
			rec["status"] = "draft";
			var resp = _rec.CreateRecord("inv_stock_movement", rec);
			if (!resp.Success)
				throw new InvalidOperationException("Inventory: failed to create draft movement. " + resp.Message);
			return id;
		}

		/// <summary>
		/// Posts a draft movement (sets status = posted). The StockMovementHooks enforce
		/// the negative-stock, approval and GL-posting rules; the returned QueryResponse
		/// reflects whether posting was allowed.
		/// </summary>
		public QueryResponse PostMovement(Guid movementId, Guid? approvalFlowId = null)
		{
			var movement = GetMovement(movementId);
			if (movement == null)
				throw new InvalidOperationException("Inventory: movement not found.");
			if (approvalFlowId.HasValue)
				movement["approval_flow_id"] = approvalFlowId.Value;
			movement["status"] = "posted";
			return _rec.UpdateRecord("inv_stock_movement", movement);
		}

		/// <summary>Creates a movement directly in the posted state (single step).</summary>
		public QueryResponse CreateAndPostMovement(Guid itemId, Guid warehouseId, string movementType,
			decimal quantity, decimal unitCost, string reference, Guid? approvalFlowId = null)
		{
			var rec = new EntityRecord();
			rec["id"] = Guid.NewGuid();
			rec["item_id"] = itemId;
			rec["warehouse_id"] = warehouseId;
			rec["movement_type"] = movementType;
			rec["quantity"] = quantity;
			rec["unit_cost"] = unitCost;
			rec["reference"] = reference ?? string.Empty;
			rec["status"] = "posted";
			if (approvalFlowId.HasValue)
				rec["approval_flow_id"] = approvalFlowId.Value;
			return _rec.CreateRecord("inv_stock_movement", rec);
		}

		public EntityRecord GetMovement(Guid id) => FindOne("inv_stock_movement", "id", id);

		#endregion

		#region << Helpers >>

		private EntityRecord FindOne(string entity, string field, object value)
		{
			var query = new EntityQuery(entity, "*", EntityQuery.QueryEQ(field, value));
			var resp = _rec.Find(query);
			if (resp.Success && resp.Object != null && resp.Object.Data.Count > 0)
				return resp.Object.Data[0];
			return null;
		}

		internal static decimal ToDecimal(object value)
		{
			if (value == null) return 0m;
			try { return Convert.ToDecimal(value); }
			catch { return 0m; }
		}

		#endregion
	}
}
