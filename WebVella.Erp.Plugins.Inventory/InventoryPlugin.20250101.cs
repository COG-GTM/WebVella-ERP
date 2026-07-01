using System;
using System.Collections.Generic;
using System.Linq;
using WebVella.Erp.Api;
using WebVella.Erp.Api.Models;
using WebVella.Erp.Web.Models;
using WebVella.Erp.Plugins.Workflow.Services;

namespace WebVella.Erp.Plugins.Inventory
{
	public partial class InventoryPlugin : ErpPlugin
	{
		// Stable entity ids (inv_ namespace: 1a5e0000-...)
		internal static readonly Guid ItemEntityId = new Guid("1a5e0000-0000-0000-0000-000000000001");
		internal static readonly Guid WarehouseEntityId = new Guid("1a5e0000-0000-0000-0000-000000000002");
		internal static readonly Guid StockLevelEntityId = new Guid("1a5e0000-0000-0000-0000-000000000003");
		internal static readonly Guid StockMovementEntityId = new Guid("1a5e0000-0000-0000-0000-000000000004");

		// Workflow name used for stock-movement state transitions.
		internal const string MovementWorkflowName = "inv_movement";

		private static void Patch20250101()
		{
			#region << Create Inventory Application >>
			{
				var id = new Guid("1a5ea000-0000-0000-0000-000000000001");
				var name = "inventory";
				var label = "Inventory";
				var description = "Inventory management - items/SKUs, warehouses, stock levels and stock movements with GL posting and approval routing";
				var iconClass = "fa fa-boxes";
				var author = "WebVella";
				var color = "#e65100";
				var weight = 22;
				var access = new List<Guid>();
				access.Add(new Guid("bdc56420-caf0-4030-8a0e-d264938e0cda"));

				new WebVella.Erp.Web.Services.AppService().CreateApplication(id, name, label, description, iconClass, author, color, weight, access, WebVella.Erp.Database.DbContext.Current.Transaction);
			}
			#endregion

			#region << Create Inventory Entities >>
			CreateItemEntity();
			CreateWarehouseEntity();
			CreateStockLevelEntity();
			CreateStockMovementEntity();
			#endregion

			#region << Create Inventory Relations >>
			CreateRelations();
			#endregion

			#region << Define Movement Workflow (states + transitions) >>
			{
				var wf = new WorkflowService();
				wf.DefineState(MovementWorkflowName, "inv_stock_movement", "draft", "Draft", true, false);
				wf.DefineState(MovementWorkflowName, "inv_stock_movement", "posted", "Posted", false, true);
				wf.DefineTransition(MovementWorkflowName, "draft", "posted", false);
			}
			#endregion

			#region << Create Sitemap Area + Nodes >>
			{
				var areaId = new Guid("1a5eb000-0000-0000-0000-000000000001");
				var appId = new Guid("1a5ea000-0000-0000-0000-000000000001");
				new WebVella.Erp.Web.Services.AppService().CreateArea(areaId, appId, "stock", "Stock",
					new List<TranslationResource>(), "Stock", new List<TranslationResource>(),
					"fa fa-boxes", "#e65100", 1, false, new List<Guid>(),
					WebVella.Erp.Database.DbContext.Current.Transaction);

				CreateListNode(new Guid("1a5ec000-0000-0000-0000-000000000001"), areaId, "items", "Items", "/inventory/stock/inv_item/list/r", "fa fa-list", 1);
				CreateListNode(new Guid("1a5ec000-0000-0000-0000-000000000002"), areaId, "warehouses", "Warehouses", "/inventory/stock/inv_warehouse/list/r", "fa fa-list", 2);
				CreateListNode(new Guid("1a5ec000-0000-0000-0000-000000000003"), areaId, "stock-levels", "Stock Levels", "/inventory/stock/inv_stock_level/list/r", "fa fa-list", 3);
				CreateListNode(new Guid("1a5ec000-0000-0000-0000-000000000004"), areaId, "stock-movements", "Stock Movements", "/inventory/stock/inv_stock_movement/list/r", "fa fa-list", 4);
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
				throw new Exception("Inventory: failed to create entity '" + name + "'. Message: " + response.Message);

			foreach (var field in fields)
			{
				var fieldResponse = entMan.CreateField(entityId, field);
				if (!fieldResponse.Success)
					throw new Exception("Inventory: failed to create field '" + field.Name + "' on '" + name + "'. Message: " + fieldResponse.Message);
			}
		}

		private static InputTextField Text(string n, string l, int max = 255) =>
			new InputTextField { Id = Guid.NewGuid(), Name = n, Label = l, Required = false, Unique = false, MaxLength = max };

		private static InputGuidField GuidF(string n, string l) =>
			new InputGuidField { Id = Guid.NewGuid(), Name = n, Label = l, Required = false };

		private static InputCurrencyField Money(string n, string l) =>
			new InputCurrencyField { Id = Guid.NewGuid(), Name = n, Label = l, Required = false, DefaultValue = 0, Currency = new CurrencyType { Code = "USD" } };

		private static InputNumberField Num(string n, string l, byte dp = 2) =>
			new InputNumberField { Id = Guid.NewGuid(), Name = n, Label = l, Required = false, DefaultValue = 0, DecimalPlaces = dp };

		private static InputSelectField Select(string n, string l, string def, params (string v, string t)[] opts)
		{
			var options = new List<SelectOption>();
			foreach (var o in opts) options.Add(new SelectOption(o.v, o.t));
			return new InputSelectField { Id = Guid.NewGuid(), Name = n, Label = l, Required = false, DefaultValue = def, Options = options };
		}

		private static void CreateItemEntity()
		{
			CreateEntityWithFields(ItemEntityId, "inv_item", "Item", "Items", new List<InputField>
			{
				Text("code", "Item Code", 50),
				Text("name", "Item Name", 255),
				Text("unit_of_measure", "Unit of Measure", 50),
				Money("standard_cost", "Standard Cost"),
				Text("gl_inventory_account_code", "GL Inventory Account Code", 50),
				Text("gl_cogs_account_code", "GL COGS Account Code", 50)
			});
		}

		private static void CreateWarehouseEntity()
		{
			CreateEntityWithFields(WarehouseEntityId, "inv_warehouse", "Warehouse", "Warehouses", new List<InputField>
			{
				Text("code", "Warehouse Code", 50),
				Text("name", "Warehouse Name", 255)
			});
		}

		private static void CreateStockLevelEntity()
		{
			CreateEntityWithFields(StockLevelEntityId, "inv_stock_level", "Stock Level", "Stock Levels", new List<InputField>
			{
				GuidF("item_id", "Item"),
				GuidF("warehouse_id", "Warehouse"),
				Num("quantity_on_hand", "Quantity On Hand", 2)
			});
		}

		private static void CreateStockMovementEntity()
		{
			CreateEntityWithFields(StockMovementEntityId, "inv_stock_movement", "Stock Movement", "Stock Movements", new List<InputField>
			{
				GuidF("item_id", "Item"),
				GuidF("warehouse_id", "Warehouse"),
				Select("movement_type", "Movement Type", "receipt",
					("receipt", "Receipt"), ("issue", "Issue"), ("adjustment", "Adjustment")),
				Num("quantity", "Quantity", 2),
				Money("unit_cost", "Unit Cost"),
				Text("reference", "Reference", 255),
				Select("status", "Status", "draft", ("draft", "Draft"), ("posted", "Posted")),
				GuidF("approval_flow_id", "Approval Flow"),
				GuidF("journal_entry_id", "Journal Entry")
			});
		}

		#region << Relations >>

		// Relation ids (inv_ namespace: 1a5ed000-...)
		private static readonly Guid ItemStockLevelRelationId = new Guid("1a5ed000-0000-0000-0000-000000000001");
		private static readonly Guid WarehouseStockLevelRelationId = new Guid("1a5ed000-0000-0000-0000-000000000002");
		private static readonly Guid ItemStockMovementRelationId = new Guid("1a5ed000-0000-0000-0000-000000000003");
		private static readonly Guid WarehouseStockMovementRelationId = new Guid("1a5ed000-0000-0000-0000-000000000004");

		private static void CreateRelations()
		{
			CreateOneToManyRelation(ItemStockLevelRelationId, "inv_item_stock_level", "Item -> Stock Levels",
				ItemEntityId, "id", StockLevelEntityId, "item_id");
			CreateOneToManyRelation(WarehouseStockLevelRelationId, "inv_warehouse_stock_level", "Warehouse -> Stock Levels",
				WarehouseEntityId, "id", StockLevelEntityId, "warehouse_id");
			CreateOneToManyRelation(ItemStockMovementRelationId, "inv_item_stock_movement", "Item -> Stock Movements",
				ItemEntityId, "id", StockMovementEntityId, "item_id");
			CreateOneToManyRelation(WarehouseStockMovementRelationId, "inv_warehouse_stock_movement", "Warehouse -> Stock Movements",
				WarehouseEntityId, "id", StockMovementEntityId, "warehouse_id");
		}

		private static void CreateOneToManyRelation(Guid relationId, string name, string label,
			Guid originEntityId, string originFieldName, Guid targetEntityId, string targetFieldName)
		{
			var entMan = new EntityManager();
			var relMan = new EntityRelationManager();

			var originEntity = entMan.ReadEntity(originEntityId).Object;
			var originField = originEntity.Fields.SingleOrDefault(x => x.Name == originFieldName);
			var targetEntity = entMan.ReadEntity(targetEntityId).Object;
			var targetField = targetEntity.Fields.SingleOrDefault(x => x.Name == targetFieldName);

			var relation = new EntityRelation();
			relation.Id = relationId;
			relation.Name = name;
			relation.Label = label;
			relation.Description = null;
			relation.System = false;
			relation.RelationType = EntityRelationType.OneToMany;
			relation.OriginEntityId = originEntity.Id;
			relation.OriginEntityName = originEntity.Name;
			relation.OriginFieldId = originField.Id;
			relation.OriginFieldName = originField.Name;
			relation.TargetEntityId = targetEntity.Id;
			relation.TargetEntityName = targetEntity.Name;
			relation.TargetFieldId = targetField.Id;
			relation.TargetFieldName = targetField.Name;

			var response = relMan.Create(relation);
			if (!response.Success)
				throw new Exception("Inventory: failed to create relation '" + name + "'. Message: " + response.Message);
		}

		#endregion
	}
}
