using System;
using System.Collections.Generic;
using System.Linq;
using WebVella.Erp.Api;
using WebVella.Erp.Api.Models;
using WebVella.Erp.Web.Models;
using WebVella.Erp.Plugins.Workflow.Services;

namespace WebVella.Erp.Plugins.Procurement
{
	public partial class ProcurementPlugin : ErpPlugin
	{
		// Stable entity ids (pur_ namespace) - freshly generated, unique to this module.
		internal static readonly Guid VendorEntityId = new Guid("2b4f94ee-0f64-4cbc-9cb3-a2ab75646a7c");
		internal static readonly Guid PurchaseOrderEntityId = new Guid("68027cf8-0f60-4611-8441-3cf6d4167055");
		internal static readonly Guid PurchaseOrderLineEntityId = new Guid("85094f9a-c8ca-4ef0-80ba-7782892d0c9c");
		internal static readonly Guid GoodsReceiptEntityId = new Guid("15c18655-4b9c-4fb0-96f2-46fdf1f891a6");

		private static void Patch20250101()
		{
			#region << Create Procurement Application >>
			{
				var id = new Guid("acf8d2ce-3fc9-47d9-9e32-35c11cf9cfde");
				var name = "procurement";
				var label = "Procurement";
				var description = "Procurement - vendors, purchase orders, goods receipts; GL posting via Finance and PO approvals via Workflow";
				var iconClass = "fa fa-shopping-cart";
				var author = "WebVella";
				var color = "#e65100";
				var weight = 22;
				var access = new List<Guid>();
				access.Add(new Guid("bdc56420-caf0-4030-8a0e-d264938e0cda"));

				new WebVella.Erp.Web.Services.AppService().CreateApplication(id, name, label, description, iconClass, author, color, weight, access, WebVella.Erp.Database.DbContext.Current.Transaction);
			}
			#endregion

			#region << Create Procurement Entities >>
			CreateVendorEntity();
			CreatePurchaseOrderEntity();
			CreatePurchaseOrderLineEntity();
			CreateGoodsReceiptEntity();
			#endregion

			#region << Create Relations >>
			// vendor 1--N purchase_order
			CreateOneToManyRelation(new Guid("c657bc1e-ae6b-4f07-8ef4-4a2b45b29fb4"), "pur_vendor_1n_purchase_order",
				"pur_vendor", "id", "pur_purchase_order", "vendor_id");
			// purchase_order 1--N purchase_order_line
			CreateOneToManyRelation(new Guid("f1376ba3-4f35-4af4-8c92-4cc0bd28f191"), "pur_purchase_order_1n_line",
				"pur_purchase_order", "id", "pur_purchase_order_line", "po_id");
			// purchase_order 1--N goods_receipt
			CreateOneToManyRelation(new Guid("b29be7e2-a91c-4920-9834-f405beb42ca1"), "pur_purchase_order_1n_goods_receipt",
				"pur_purchase_order", "id", "pur_goods_receipt", "po_id");
			#endregion

			#region << Define Approval Workflow (states + transitions) >>
			{
				var wf = new WorkflowService();
				wf.DefineState(PoApprovalWorkflow, "pur_purchase_order", "draft", "Draft", true, false);
				wf.DefineState(PoApprovalWorkflow, "pur_purchase_order", "pending_approval", "Pending Approval", false, false);
				wf.DefineState(PoApprovalWorkflow, "pur_purchase_order", "approved", "Approved", false, true);

				wf.DefineTransition(PoApprovalWorkflow, "draft", "pending_approval", false);
				wf.DefineTransition(PoApprovalWorkflow, "pending_approval", "approved", true);
			}
			#endregion

			#region << Create Sitemap Area + Nodes >>
			{
				var areaId = new Guid("abda0ca3-73b1-4844-afd9-12ef8270f032");
				var appId = new Guid("acf8d2ce-3fc9-47d9-9e32-35c11cf9cfde");
				new WebVella.Erp.Web.Services.AppService().CreateArea(areaId, appId, "buying", "Buying",
					new List<TranslationResource>(), "Buying", new List<TranslationResource>(),
					"fa fa-shopping-cart", "#e65100", 1, false, new List<Guid>(),
					WebVella.Erp.Database.DbContext.Current.Transaction);

				CreateListNode(new Guid("e686bada-b6e2-420b-932c-5038c45b39ba"), areaId, "vendors", "Vendors", "/procurement/buying/pur_vendor/list/r", "fa fa-list", 1);
				CreateListNode(new Guid("30dd0a64-aedb-41d9-a291-3d17eef16717"), areaId, "purchase-orders", "Purchase Orders", "/procurement/buying/pur_purchase_order/list/r", "fa fa-list", 2);
				CreateListNode(new Guid("ba2be295-828b-4eb5-89cd-b431e31a94b7"), areaId, "purchase-order-lines", "Purchase Order Lines", "/procurement/buying/pur_purchase_order_line/list/r", "fa fa-list", 3);
				CreateListNode(new Guid("1c00efbd-d64d-4b1b-a2fe-606ce5b8b2b4"), areaId, "goods-receipts", "Goods Receipts", "/procurement/buying/pur_goods_receipt/list/r", "fa fa-list", 4);
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
				throw new Exception("Procurement: failed to create entity '" + name + "'. Message: " + response.Message);

			foreach (var field in fields)
			{
				var fieldResponse = entMan.CreateField(entityId, field);
				if (!fieldResponse.Success)
					throw new Exception("Procurement: failed to create field '" + field.Name + "' on '" + name + "'. Message: " + fieldResponse.Message);
			}
		}

		private static void CreateOneToManyRelation(Guid id, string name, string originEntityName, string originFieldName, string targetEntityName, string targetFieldName)
		{
			var entMan = new EntityManager();
			var relMan = new EntityRelationManager();

			var originEntity = entMan.ReadEntity(originEntityName).Object;
			if (originEntity == null)
				throw new Exception("Procurement: relation '" + name + "' origin entity '" + originEntityName + "' not found.");
			var targetEntity = entMan.ReadEntity(targetEntityName).Object;
			if (targetEntity == null)
				throw new Exception("Procurement: relation '" + name + "' target entity '" + targetEntityName + "' not found.");

			var originField = originEntity.Fields.SingleOrDefault(x => x.Name == originFieldName);
			if (originField == null)
				throw new Exception("Procurement: relation '" + name + "' origin field '" + originFieldName + "' not found on '" + originEntityName + "'.");
			var targetField = targetEntity.Fields.SingleOrDefault(x => x.Name == targetFieldName);
			if (targetField == null)
				throw new Exception("Procurement: relation '" + name + "' target field '" + targetFieldName + "' not found on '" + targetEntityName + "'.");

			var relation = new EntityRelation
			{
				Id = id,
				Name = name,
				Label = name,
				Description = "",
				System = true,
				RelationType = EntityRelationType.OneToMany,
				OriginEntityId = originEntity.Id,
				OriginEntityName = originEntity.Name,
				OriginFieldId = originField.Id,
				OriginFieldName = originField.Name,
				TargetEntityId = targetEntity.Id,
				TargetEntityName = targetEntity.Name,
				TargetFieldId = targetField.Id,
				TargetFieldName = targetField.Name
			};

			var response = relMan.Create(relation);
			if (!response.Success)
				throw new Exception("Procurement: failed to create relation '" + name + "'. Message: " + response.Message);
		}

		private static InputTextField Text(string n, string l, int max = 255) =>
			new InputTextField { Id = Guid.NewGuid(), Name = n, Label = l, Required = false, Unique = false, MaxLength = max };

		private static InputGuidField GuidF(string n, string l) =>
			new InputGuidField { Id = Guid.NewGuid(), Name = n, Label = l, Required = false };

		private static InputCurrencyField Money(string n, string l) =>
			new InputCurrencyField { Id = Guid.NewGuid(), Name = n, Label = l, Required = false, DefaultValue = 0, Currency = new CurrencyType { Code = "USD" } };

		private static InputNumberField Num(string n, string l, byte dp = 0) =>
			new InputNumberField { Id = Guid.NewGuid(), Name = n, Label = l, Required = false, DefaultValue = 0, DecimalPlaces = dp };

		private static InputDateField DateF(string n, string l) =>
			new InputDateField { Id = Guid.NewGuid(), Name = n, Label = l, Required = false, Format = "yyyy-MM-dd", UseCurrentTimeAsDefaultValue = false };

		private static InputSelectField Select(string n, string l, string def, params (string v, string t)[] opts)
		{
			var options = new List<SelectOption>();
			foreach (var o in opts) options.Add(new SelectOption(o.v, o.t));
			return new InputSelectField { Id = Guid.NewGuid(), Name = n, Label = l, Required = false, DefaultValue = def, Options = options };
		}

		private static void CreateVendorEntity()
		{
			CreateEntityWithFields(VendorEntityId, "pur_vendor", "Vendor", "Vendors", new List<InputField>
			{
				Text("code", "Vendor Code", 50),
				Text("name", "Vendor Name", 255),
				Text("default_payable_account_code", "Default Payable Account Code", 50)
			});
		}

		private static void CreatePurchaseOrderEntity()
		{
			CreateEntityWithFields(PurchaseOrderEntityId, "pur_purchase_order", "Purchase Order", "Purchase Orders", new List<InputField>
			{
				Text("number", "Number", 50),
				GuidF("vendor_id", "Vendor"),
				DateF("order_date", "Order Date"),
				Select("status", "Status", "draft",
					("draft", "Draft"), ("pending_approval", "Pending Approval"), ("approved", "Approved"), ("received", "Received")),
				Money("total_amount", "Total Amount")
			});
		}

		private static void CreatePurchaseOrderLineEntity()
		{
			CreateEntityWithFields(PurchaseOrderLineEntityId, "pur_purchase_order_line", "Purchase Order Line", "Purchase Order Lines", new List<InputField>
			{
				GuidF("po_id", "Purchase Order"),
				Text("item_code", "Item Code", 100),
				Text("description", "Description", 500),
				Num("quantity", "Quantity", 2),
				Money("unit_price", "Unit Price"),
				Money("line_total", "Line Total")
			});
		}

		private static void CreateGoodsReceiptEntity()
		{
			CreateEntityWithFields(GoodsReceiptEntityId, "pur_goods_receipt", "Goods Receipt", "Goods Receipts", new List<InputField>
			{
				GuidF("po_id", "Purchase Order"),
				DateF("receipt_date", "Receipt Date"),
				Select("status", "Status", "draft", ("draft", "Draft"), ("received", "Received"), ("posted", "Posted"))
			});
		}
	}
}
