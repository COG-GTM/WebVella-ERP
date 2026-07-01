using System;
using System.Collections.Generic;
using System.Linq;
using WebVella.Erp.Api;
using WebVella.Erp.Api.Models;
using WebVella.Erp.Web.Models;
using WebVella.Erp.Plugins.Sales.Services;
using WebVella.Erp.Plugins.Workflow.Services;

namespace WebVella.Erp.Plugins.Sales
{
	public partial class SalesPlugin : ErpPlugin
	{
		// Stable entity ids (sal_ namespace: 5a1e0000-...)
		internal static readonly Guid CustomerEntityId = new Guid("5a1e0000-0000-0000-0000-000000000001");
		internal static readonly Guid SalesOrderEntityId = new Guid("5a1e0000-0000-0000-0000-000000000002");
		internal static readonly Guid SalesOrderLineEntityId = new Guid("5a1e0000-0000-0000-0000-000000000003");

		private static void Patch20250101()
		{
			#region << Create Sales Application >>
			{
				var id = new Guid("5aa00000-0000-0000-0000-000000000001");
				var name = "sales";
				var label = "Sales";
				var description = "Sales / Order-to-Cash - customers, sales orders and order lines with AR posting and approval workflow";
				var iconClass = "fa fa-shopping-cart";
				var author = "WebVella";
				var color = "#e65100";
				var weight = 22;
				var access = new List<Guid>();
				access.Add(new Guid("bdc56420-caf0-4030-8a0e-d264938e0cda"));

				new WebVella.Erp.Web.Services.AppService().CreateApplication(id, name, label, description, iconClass, author, color, weight, access, WebVella.Erp.Database.DbContext.Current.Transaction);
			}
			#endregion

			#region << Create Sales Entities >>
			CreateCustomerEntity();
			CreateSalesOrderEntity();
			CreateSalesOrderLineEntity();
			#endregion

			#region << Create Relations >>
			CreateRelations();
			#endregion

			#region << Define Approval Workflow >>
			DefineSalesWorkflow();
			#endregion

			#region << Create Sitemap Area + Nodes >>
			{
				var areaId = new Guid("5ab00000-0000-0000-0000-000000000001");
				var appId = new Guid("5aa00000-0000-0000-0000-000000000001");
				new WebVella.Erp.Web.Services.AppService().CreateArea(areaId, appId, "o2c", "Order to Cash",
					new List<TranslationResource>(), "Order to Cash", new List<TranslationResource>(),
					"fa fa-shopping-cart", "#e65100", 1, false, new List<Guid>(),
					WebVella.Erp.Database.DbContext.Current.Transaction);

				CreateListNode(new Guid("5ac00000-0000-0000-0000-000000000001"), areaId, "customers", "Customers", "/sales/o2c/sal_customer/list/r", "fa fa-list", 1);
				CreateListNode(new Guid("5ac00000-0000-0000-0000-000000000002"), areaId, "sales-orders", "Sales Orders", "/sales/o2c/sal_sales_order/list/r", "fa fa-list", 2);
				CreateListNode(new Guid("5ac00000-0000-0000-0000-000000000003"), areaId, "sales-order-lines", "Sales Order Lines", "/sales/o2c/sal_sales_order_line/list/r", "fa fa-list", 3);
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
				throw new Exception("Sales: failed to create entity '" + name + "'. Message: " + response.Message);

			foreach (var field in fields)
			{
				var fieldResponse = entMan.CreateField(entityId, field);
				if (!fieldResponse.Success)
					throw new Exception("Sales: failed to create field '" + field.Name + "' on '" + name + "'. Message: " + fieldResponse.Message);
			}
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

		private static void CreateCustomerEntity()
		{
			CreateEntityWithFields(CustomerEntityId, "sal_customer", "Customer", "Customers", new List<InputField>
			{
				Text("code", "Customer Code", 50),
				Text("name", "Customer Name", 255),
				Text("default_receivable_account_code", "Default Receivable Account Code", 50),
				Text("revenue_account_code", "Revenue Account Code", 50),
				Money("credit_limit", "Credit Limit")
			});
		}

		private static void CreateSalesOrderEntity()
		{
			CreateEntityWithFields(SalesOrderEntityId, "sal_sales_order", "Sales Order", "Sales Orders", new List<InputField>
			{
				Text("number", "Order Number", 50),
				GuidF("customer_id", "Customer"),
				DateF("order_date", "Order Date"),
				Select("status", "Status", "draft",
					("draft", "Draft"), ("pending_approval", "Pending Approval"), ("confirmed", "Confirmed"), ("fulfilled", "Fulfilled")),
				Money("total_amount", "Total Amount"),
				Num("discount_pct", "Discount (%)", 2),
				GuidF("approval_flow_id", "Approval Flow")
			});
		}

		private static void CreateSalesOrderLineEntity()
		{
			CreateEntityWithFields(SalesOrderLineEntityId, "sal_sales_order_line", "Sales Order Line", "Sales Order Lines", new List<InputField>
			{
				GuidF("order_id", "Sales Order"),
				Text("item_code", "Item Code", 100),
				Text("description", "Description", 255),
				Num("quantity", "Quantity", 2),
				Money("unit_price", "Unit Price"),
				Money("line_total", "Line Total")
			});
		}

		private static void CreateRelations()
		{
			var entMan = new EntityManager();
			var relMan = new EntityRelationManager();

			var customer = entMan.ReadEntity(CustomerEntityId).Object;
			var order = entMan.ReadEntity(SalesOrderEntityId).Object;
			var line = entMan.ReadEntity(SalesOrderLineEntityId).Object;

			Guid Field(Entity e, string fieldName) => e.Fields.Single(f => f.Name == fieldName).Id;

			// 1:N sal_customer -> sal_sales_order (origin = customer.id, target = order.customer_id)
			CreateRelation(relMan, new Guid("5ad00000-0000-0000-0000-000000000001"),
				"sal_customer_1_n_sales_order", "Customer Sales Orders",
				customer.Id, Field(customer, "id"), order.Id, Field(order, "customer_id"));

			// 1:N sal_sales_order -> sal_sales_order_line (origin = order.id, target = line.order_id)
			CreateRelation(relMan, new Guid("5ad00000-0000-0000-0000-000000000002"),
				"sal_sales_order_1_n_order_line", "Sales Order Lines",
				order.Id, Field(order, "id"), line.Id, Field(line, "order_id"));
		}

		private static void CreateRelation(EntityRelationManager relMan, Guid id, string name, string label,
			Guid originEntityId, Guid originFieldId, Guid targetEntityId, Guid targetFieldId)
		{
			var relation = new EntityRelation
			{
				Id = id,
				Name = name,
				Label = label,
				Description = "",
				System = false,
				RelationType = EntityRelationType.OneToMany,
				OriginEntityId = originEntityId,
				OriginFieldId = originFieldId,
				TargetEntityId = targetEntityId,
				TargetFieldId = targetFieldId
			};
			var response = relMan.Create(relation);
			if (!response.Success)
				throw new Exception("Sales: failed to create relation '" + name + "'. Message: " + response.Message);
		}

		private static void DefineSalesWorkflow()
		{
			var wf = new WorkflowService();
			var workflowName = SalesService.WorkflowName;
			var entityName = "sal_sales_order";

			wf.DefineState(workflowName, entityName, "draft", "Draft", true, false);
			wf.DefineState(workflowName, entityName, "pending_approval", "Pending Approval", false, false);
			wf.DefineState(workflowName, entityName, "confirmed", "Confirmed", false, false);
			wf.DefineState(workflowName, entityName, "fulfilled", "Fulfilled", false, true);

			wf.DefineTransition(workflowName, "draft", "confirmed", false);
			wf.DefineTransition(workflowName, "draft", "pending_approval", true);
			wf.DefineTransition(workflowName, "pending_approval", "confirmed", true);
			wf.DefineTransition(workflowName, "confirmed", "fulfilled", false);
		}
	}
}
