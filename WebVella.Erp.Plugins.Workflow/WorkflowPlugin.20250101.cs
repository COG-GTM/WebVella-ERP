using System;
using System.Collections.Generic;
using WebVella.Erp.Api;
using WebVella.Erp.Api.Models;
using WebVella.Erp.Web.Models;

namespace WebVella.Erp.Plugins.Workflow
{
	public partial class WorkflowPlugin : ErpPlugin
	{
		// Stable entity ids (wf_ namespace: 4f000000-...)
		internal static readonly Guid StateDefinitionEntityId = new Guid("4f000000-0000-0000-0000-000000000001");
		internal static readonly Guid TransitionEntityId = new Guid("4f000000-0000-0000-0000-000000000002");
		internal static readonly Guid ApprovalTemplateEntityId = new Guid("4f000000-0000-0000-0000-000000000003");
		internal static readonly Guid ApprovalFlowEntityId = new Guid("4f000000-0000-0000-0000-000000000004");
		internal static readonly Guid ApprovalStepEntityId = new Guid("4f000000-0000-0000-0000-000000000005");

		private static void Patch20250101()
		{
			#region << Create Workflow Application >>
			{
				var id = new Guid("4fa00000-0000-0000-0000-000000000001");
				var name = "workflow";
				var label = "Workflow";
				var description = "Configurable approval / state-machine engine (states, transitions, approval templates and flows)";
				var iconClass = "fa fa-sitemap";
				var author = "WebVella";
				var color = "#4a148c";
				var weight = 21;
				var access = new List<Guid>();
				access.Add(new Guid("bdc56420-caf0-4030-8a0e-d264938e0cda"));

				new WebVella.Erp.Web.Services.AppService().CreateApplication(id, name, label, description, iconClass, author, color, weight, access, WebVella.Erp.Database.DbContext.Current.Transaction);
			}
			#endregion

			#region << Create Workflow Entities >>
			CreateStateDefinitionEntity();
			CreateTransitionEntity();
			CreateApprovalTemplateEntity();
			CreateApprovalFlowEntity();
			CreateApprovalStepEntity();
			#endregion

			#region << Create Sitemap Area + Nodes >>
			{
				var areaId = new Guid("4fb00000-0000-0000-0000-000000000001");
				var appId = new Guid("4fa00000-0000-0000-0000-000000000001");
				new WebVella.Erp.Web.Services.AppService().CreateArea(areaId, appId, "engine", "Engine",
					new List<TranslationResource>(), "Engine", new List<TranslationResource>(),
					"fa fa-sitemap", "#4a148c", 1, false, new List<Guid>(),
					WebVella.Erp.Database.DbContext.Current.Transaction);

				CreateListNode(new Guid("4fc00000-0000-0000-0000-000000000001"), areaId, "state-definitions", "State Definitions", "/workflow/engine/wf_state_definition/list/r", "fa fa-list", 1);
				CreateListNode(new Guid("4fc00000-0000-0000-0000-000000000002"), areaId, "transitions", "Transitions", "/workflow/engine/wf_transition/list/r", "fa fa-list", 2);
				CreateListNode(new Guid("4fc00000-0000-0000-0000-000000000003"), areaId, "approval-templates", "Approval Templates", "/workflow/engine/wf_approval_template/list/r", "fa fa-list", 3);
				CreateListNode(new Guid("4fc00000-0000-0000-0000-000000000004"), areaId, "approval-flows", "Approval Flows", "/workflow/engine/wf_approval_flow/list/r", "fa fa-list", 4);
				CreateListNode(new Guid("4fc00000-0000-0000-0000-000000000005"), areaId, "approval-steps", "Approval Steps", "/workflow/engine/wf_approval_step/list/r", "fa fa-list", 5);
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
				throw new Exception("Workflow: failed to create entity '" + name + "'. Message: " + response.Message);

			foreach (var field in fields)
			{
				var fieldResponse = entMan.CreateField(entityId, field);
				if (!fieldResponse.Success)
					throw new Exception("Workflow: failed to create field '" + field.Name + "' on '" + name + "'. Message: " + fieldResponse.Message);
			}
		}

		private static InputTextField Text(string n, string l, int max = 255) =>
			new InputTextField { Id = Guid.NewGuid(), Name = n, Label = l, Required = false, Unique = false, MaxLength = max };

		private static InputGuidField GuidF(string n, string l) =>
			new InputGuidField { Id = Guid.NewGuid(), Name = n, Label = l, Required = false };

		private static InputNumberField Num(string n, string l, decimal def = 0) =>
			new InputNumberField { Id = Guid.NewGuid(), Name = n, Label = l, Required = false, DefaultValue = def, DecimalPlaces = 0 };

		private static InputCheckboxField Bool(string n, string l, bool def) =>
			new InputCheckboxField { Id = Guid.NewGuid(), Name = n, Label = l, Required = false, DefaultValue = def };

		private static InputDateTimeField DateTimeF(string n, string l) =>
			new InputDateTimeField { Id = Guid.NewGuid(), Name = n, Label = l, Required = false, Format = "yyyy-MM-dd HH:mm", UseCurrentTimeAsDefaultValue = false };

		private static InputSelectField Select(string n, string l, string def, params (string v, string t)[] opts)
		{
			var options = new List<SelectOption>();
			foreach (var o in opts) options.Add(new SelectOption(o.v, o.t));
			return new InputSelectField { Id = Guid.NewGuid(), Name = n, Label = l, Required = false, DefaultValue = def, Options = options };
		}

		private static void CreateStateDefinitionEntity()
		{
			CreateEntityWithFields(StateDefinitionEntityId, "wf_state_definition", "State Definition", "State Definitions", new List<InputField>
			{
				Text("workflow_name", "Workflow Name", 100),
				Text("entity_name", "Entity Name", 100),
				Text("state_key", "State Key", 100),
				Text("state_label", "State Label", 255),
				Bool("is_initial", "Is Initial", false),
				Bool("is_terminal", "Is Terminal", false)
			});
		}

		private static void CreateTransitionEntity()
		{
			CreateEntityWithFields(TransitionEntityId, "wf_transition", "Transition", "Transitions", new List<InputField>
			{
				Text("workflow_name", "Workflow Name", 100),
				Text("from_state", "From State", 100),
				Text("to_state", "To State", 100),
				Bool("requires_approval", "Requires Approval", false)
			});
		}

		private static void CreateApprovalTemplateEntity()
		{
			CreateEntityWithFields(ApprovalTemplateEntityId, "wf_approval_template", "Approval Template", "Approval Templates", new List<InputField>
			{
				Text("workflow_name", "Workflow Name", 100),
				Text("name", "Name", 255),
				Text("required_roles", "Required Roles (CSV)", 500),
				Num("min_approvals", "Minimum Approvals", 1)
			});
		}

		private static void CreateApprovalFlowEntity()
		{
			CreateEntityWithFields(ApprovalFlowEntityId, "wf_approval_flow", "Approval Flow", "Approval Flows", new List<InputField>
			{
				GuidF("template_id", "Template"),
				Text("workflow_name", "Workflow Name", 100),
				Text("entity_name", "Entity Name", 100),
				GuidF("record_id", "Record"),
				Select("status", "Status", "pending", ("pending", "Pending"), ("approved", "Approved"), ("rejected", "Rejected")),
				Num("current_step", "Current Step", 1)
			});
		}

		private static void CreateApprovalStepEntity()
		{
			CreateEntityWithFields(ApprovalStepEntityId, "wf_approval_step", "Approval Step", "Approval Steps", new List<InputField>
			{
				GuidF("flow_id", "Flow"),
				Num("step_number", "Step Number", 1),
				Text("approver_role", "Approver Role", 100),
				Select("status", "Status", "pending", ("pending", "Pending"), ("approved", "Approved"), ("rejected", "Rejected")),
				GuidF("decided_by", "Decided By"),
				DateTimeF("decided_on", "Decided On"),
				Text("comment", "Comment", 500)
			});
		}
	}
}
