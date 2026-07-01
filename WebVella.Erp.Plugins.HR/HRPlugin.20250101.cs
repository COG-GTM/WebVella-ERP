using System;
using System.Collections.Generic;
using System.Linq;
using WebVella.Erp.Api;
using WebVella.Erp.Api.Models;
using WebVella.Erp.Web.Models;
using WebVella.Erp.Plugins.Workflow.Services;

namespace WebVella.Erp.Plugins.HR
{
	public partial class HRPlugin : ErpPlugin
	{
		// Stable entity ids (hr_ namespace: aa100000-...)
		internal static readonly Guid DepartmentEntityId = new Guid("aa100000-0000-0000-0000-000000000001");
		internal static readonly Guid EmployeeEntityId = new Guid("aa100000-0000-0000-0000-000000000002");
		internal static readonly Guid LeaveRequestEntityId = new Guid("aa100000-0000-0000-0000-000000000003");
		internal static readonly Guid PayrollRunEntityId = new Guid("aa100000-0000-0000-0000-000000000004");

		// Workflow used to route leave requests through the approval engine.
		public const string LeaveWorkflowName = "hr_leave_approval";

		private static void Patch20250101()
		{
			#region << Create HR Application >>
			{
				var id = new Guid("aaa00000-0000-0000-0000-000000000001");
				var name = "hr";
				var label = "HR";
				var description = "Human Resources - departments, employees, leave requests and payroll runs";
				var iconClass = "fa fa-users";
				var author = "WebVella";
				var color = "#00695c";
				var weight = 22;
				var access = new List<Guid>();
				access.Add(new Guid("bdc56420-caf0-4030-8a0e-d264938e0cda"));

				new WebVella.Erp.Web.Services.AppService().CreateApplication(id, name, label, description, iconClass, author, color, weight, access, WebVella.Erp.Database.DbContext.Current.Transaction);
			}
			#endregion

			#region << Create HR Entities >>
			CreateDepartmentEntity();
			CreateEmployeeEntity();
			CreateLeaveRequestEntity();
			CreatePayrollRunEntity();
			#endregion

			#region << Create HR Relations >>
			// Department (1) -> Employee (N)
			CreateOneToManyRelation(
				new Guid("aad00000-0000-0000-0000-000000000001"), "hr_department_1n_employee",
				"hr_department", "id", "hr_employee", "department_id");
			// Employee (1) -> Leave Request (N)
			CreateOneToManyRelation(
				new Guid("aad00000-0000-0000-0000-000000000002"), "hr_employee_1n_leave_request",
				"hr_employee", "id", "hr_leave_request", "employee_id");
			// Employee (1) -> Payroll Run (N)
			CreateOneToManyRelation(
				new Guid("aad00000-0000-0000-0000-000000000003"), "hr_employee_1n_payroll_run",
				"hr_employee", "id", "hr_payroll_run", "employee_id");
			#endregion

			#region << Define Leave Approval Workflow >>
			{
				var wf = new WorkflowService();
				wf.DefineState(LeaveWorkflowName, "hr_leave_request", "draft", "Draft", true, false);
				wf.DefineState(LeaveWorkflowName, "hr_leave_request", "pending_approval", "Pending Approval", false, false);
				wf.DefineState(LeaveWorkflowName, "hr_leave_request", "approved", "Approved", false, true);
				wf.DefineState(LeaveWorkflowName, "hr_leave_request", "rejected", "Rejected", false, true);

				wf.DefineTransition(LeaveWorkflowName, "draft", "pending_approval", false);
				wf.DefineTransition(LeaveWorkflowName, "pending_approval", "approved", true);
				wf.DefineTransition(LeaveWorkflowName, "pending_approval", "rejected", true);
			}
			#endregion

			#region << Create Sitemap Area + Nodes >>
			{
				var areaId = new Guid("aab00000-0000-0000-0000-000000000001");
				var appId = new Guid("aaa00000-0000-0000-0000-000000000001");
				new WebVella.Erp.Web.Services.AppService().CreateArea(areaId, appId, "people", "People",
					new List<TranslationResource>(), "People", new List<TranslationResource>(),
					"fa fa-users", "#00695c", 1, false, new List<Guid>(),
					WebVella.Erp.Database.DbContext.Current.Transaction);

				CreateListNode(new Guid("aac00000-0000-0000-0000-000000000001"), areaId, "departments", "Departments", "/hr/people/hr_department/list/r", "fa fa-list", 1);
				CreateListNode(new Guid("aac00000-0000-0000-0000-000000000002"), areaId, "employees", "Employees", "/hr/people/hr_employee/list/r", "fa fa-list", 2);
				CreateListNode(new Guid("aac00000-0000-0000-0000-000000000003"), areaId, "leave-requests", "Leave Requests", "/hr/people/hr_leave_request/list/r", "fa fa-list", 3);
				CreateListNode(new Guid("aac00000-0000-0000-0000-000000000004"), areaId, "payroll-runs", "Payroll Runs", "/hr/people/hr_payroll_run/list/r", "fa fa-list", 4);
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

		private static void CreateOneToManyRelation(Guid relationId, string relationName, string originEntityName, string originFieldName, string targetEntityName, string targetFieldName)
		{
			var entMan = new EntityManager();
			var relMan = new EntityRelationManager();

			var originEntity = entMan.ReadEntity(originEntityName).Object;
			var targetEntity = entMan.ReadEntity(targetEntityName).Object;
			var originField = originEntity.Fields.SingleOrDefault(x => x.Name == originFieldName);
			var targetField = targetEntity.Fields.SingleOrDefault(x => x.Name == targetFieldName);

			var relation = new EntityRelation
			{
				Id = relationId,
				Name = relationName,
				Label = relationName,
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
				throw new Exception("HR: failed to create relation '" + relationName + "'. Message: " + response.Message);
		}

		private static void CreateEntityWithFields(Guid entityId, string name, string label, string labelPlural, List<InputField> fields)
		{
			var entMan = new EntityManager();
			var response = entMan.CreateEntity(entityId, name, label, labelPlural, createOnlyIdField: false);
			if (!response.Success)
				throw new Exception("HR: failed to create entity '" + name + "'. Message: " + response.Message);

			foreach (var field in fields)
			{
				var fieldResponse = entMan.CreateField(entityId, field);
				if (!fieldResponse.Success)
					throw new Exception("HR: failed to create field '" + field.Name + "' on '" + name + "'. Message: " + fieldResponse.Message);
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

		private static void CreateDepartmentEntity()
		{
			CreateEntityWithFields(DepartmentEntityId, "hr_department", "Department", "Departments", new List<InputField>
			{
				Text("code", "Department Code", 50),
				Text("name", "Department Name", 255),
				Text("gl_expense_account_code", "GL Expense Account Code", 50)
			});
		}

		private static void CreateEmployeeEntity()
		{
			CreateEntityWithFields(EmployeeEntityId, "hr_employee", "Employee", "Employees", new List<InputField>
			{
				Text("code", "Employee Code", 50),
				Text("first_name", "First Name", 255),
				Text("last_name", "Last Name", 255),
				GuidF("department_id", "Department"),
				Money("base_salary", "Base Salary"),
				DateF("hire_date", "Hire Date")
			});
		}

		private static void CreateLeaveRequestEntity()
		{
			CreateEntityWithFields(LeaveRequestEntityId, "hr_leave_request", "Leave Request", "Leave Requests", new List<InputField>
			{
				GuidF("employee_id", "Employee"),
				DateF("start_date", "Start Date"),
				DateF("end_date", "End Date"),
				Num("days", "Days", 2),
				Select("status", "Status", "draft",
					("draft", "Draft"), ("pending_approval", "Pending Approval"), ("approved", "Approved"), ("rejected", "Rejected")),
				GuidF("approval_flow_id", "Approval Flow")
			});
		}

		private static void CreatePayrollRunEntity()
		{
			CreateEntityWithFields(PayrollRunEntityId, "hr_payroll_run", "Payroll Run", "Payroll Runs", new List<InputField>
			{
				GuidF("employee_id", "Employee"),
				Text("period", "Period", 50),
				DateF("run_date", "Run Date"),
				Select("status", "Status", "draft", ("draft", "Draft"), ("posted", "Posted"), ("void", "Void")),
				Money("total_amount", "Total Amount")
			});
		}
	}
}
