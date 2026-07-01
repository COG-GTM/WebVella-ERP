using System;
using System.Collections.Generic;
using System.Linq;
using WebVella.Erp.Api;
using WebVella.Erp.Api.Models;

namespace WebVella.Erp.Plugins.Workflow.Services
{
	/// <summary>
	/// Cross-module contract for the configurable approval / state-machine engine.
	/// Other modules (Inventory, Procurement, Sales, HR) route approvals and
	/// validate state transitions through this interface only — this generalizes
	/// the hardcoded logic in TravelERP (HasAllRequiredApprovals / CanTransitionStatus).
	/// </summary>
	public interface IWorkflowService
	{
		bool CanTransition(string workflowName, string fromState, string toState);
		Guid StartApprovalFlow(string workflowName, string entityName, Guid recordId, IList<string> approverRoles);
		bool RecordDecision(Guid flowId, int stepNumber, Guid decidedBy, bool approved, string comment);
		bool IsApproved(Guid flowId);
	}

	public class WorkflowService : IWorkflowService
	{
		private readonly RecordManager _recordManager;

		public WorkflowService()
		{
			_recordManager = new RecordManager(null, true, true);
		}

		#region << State machine >>

		public void DefineState(string workflowName, string entityName, string stateKey, string stateLabel, bool isInitial, bool isTerminal)
		{
			var rec = new EntityRecord();
			rec["id"] = Guid.NewGuid();
			rec["workflow_name"] = workflowName;
			rec["entity_name"] = entityName;
			rec["state_key"] = stateKey;
			rec["state_label"] = stateLabel ?? stateKey;
			rec["is_initial"] = isInitial;
			rec["is_terminal"] = isTerminal;
			var resp = _recordManager.CreateRecord("wf_state_definition", rec);
			if (!resp.Success)
				throw new InvalidOperationException("Workflow: failed to create state definition. " + resp.Message);
		}

		public void DefineTransition(string workflowName, string fromState, string toState, bool requiresApproval)
		{
			var rec = new EntityRecord();
			rec["id"] = Guid.NewGuid();
			rec["workflow_name"] = workflowName;
			rec["from_state"] = fromState;
			rec["to_state"] = toState;
			rec["requires_approval"] = requiresApproval;
			var resp = _recordManager.CreateRecord("wf_transition", rec);
			if (!resp.Success)
				throw new InvalidOperationException("Workflow: failed to create transition. " + resp.Message);
		}

		/// <summary>
		/// Returns true if a transition from <paramref name="fromState"/> to
		/// <paramref name="toState"/> is configured for the given workflow.
		/// Generalizes TravelERP MissionHooks.CanTransitionStatus.
		/// </summary>
		public bool CanTransition(string workflowName, string fromState, string toState)
		{
			if (string.Equals(fromState, toState, StringComparison.OrdinalIgnoreCase))
				return false;

			var query = new EntityQuery("wf_transition", "*",
				EntityQuery.QueryAND(
					EntityQuery.QueryEQ("workflow_name", workflowName),
					EntityQuery.QueryEQ("from_state", fromState),
					EntityQuery.QueryEQ("to_state", toState)));
			var resp = _recordManager.Find(query);
			return resp.Success && resp.Object != null && resp.Object.Data.Count > 0;
		}

		#endregion

		#region << Approval routing >>

		/// <summary>
		/// Starts an approval flow for a record, generating one pending step per
		/// required approver role. Returns the created flow id.
		/// </summary>
		public Guid StartApprovalFlow(string workflowName, string entityName, Guid recordId, IList<string> approverRoles)
		{
			if (approverRoles == null || approverRoles.Count == 0)
				throw new InvalidOperationException("Workflow: an approval flow requires at least one approver role.");

			var flowId = Guid.NewGuid();
			var flow = new EntityRecord();
			flow["id"] = flowId;
			flow["workflow_name"] = workflowName ?? string.Empty;
			flow["entity_name"] = entityName;
			flow["record_id"] = recordId;
			flow["status"] = "pending";
			flow["current_step"] = (decimal)1;
			var flowResp = _recordManager.CreateRecord("wf_approval_flow", flow);
			if (!flowResp.Success)
				throw new InvalidOperationException("Workflow: failed to create approval flow. " + flowResp.Message);

			int stepNo = 1;
			foreach (var role in approverRoles)
			{
				var step = new EntityRecord();
				step["id"] = Guid.NewGuid();
				step["flow_id"] = flowId;
				step["step_number"] = (decimal)stepNo++;
				step["approver_role"] = role;
				step["status"] = "pending";
				var stepResp = _recordManager.CreateRecord("wf_approval_step", step);
				if (!stepResp.Success)
					throw new InvalidOperationException("Workflow: failed to create approval step. " + stepResp.Message);
			}

			return flowId;
		}

		/// <summary>
		/// Records an approve/reject decision for a step. A rejection immediately
		/// rejects the whole flow; once all steps are approved the flow is approved.
		/// </summary>
		public bool RecordDecision(Guid flowId, int stepNumber, Guid decidedBy, bool approved, string comment)
		{
			var step = GetStep(flowId, stepNumber);
			if (step == null)
				return false;

			step["status"] = approved ? "approved" : "rejected";
			step["decided_by"] = decidedBy;
			step["decided_on"] = DateTime.UtcNow;
			step["comment"] = comment ?? string.Empty;
			var updResp = _recordManager.UpdateRecord("wf_approval_step", step);
			if (!updResp.Success)
				throw new InvalidOperationException("Workflow: failed to update approval step. " + updResp.Message);

			var flow = GetFlow(flowId);
			if (flow == null)
				return false;

			if (!approved)
			{
				flow["status"] = "rejected";
				_recordManager.UpdateRecord("wf_approval_flow", flow);
				return true;
			}

			var steps = GetSteps(flowId);
			bool allApproved = steps.All(s => s.Properties.ContainsKey("status") && s.Properties["status"].ToString() == "approved");
			if (allApproved)
			{
				flow["status"] = "approved";
			}
			else
			{
				flow["current_step"] = (decimal)(stepNumber + 1);
			}
			_recordManager.UpdateRecord("wf_approval_flow", flow);
			return true;
		}

		/// <summary>
		/// Returns true when every step of the flow has been approved.
		/// Generalizes TravelERP HasAllRequiredApprovals.
		/// </summary>
		public bool IsApproved(Guid flowId)
		{
			var flow = GetFlow(flowId);
			if (flow != null && flow.Properties.ContainsKey("status") && flow.Properties["status"].ToString() == "approved")
				return true;

			var steps = GetSteps(flowId);
			if (steps.Count == 0)
				return false;
			return steps.All(s => s.Properties.ContainsKey("status") && s.Properties["status"].ToString() == "approved");
		}

		#endregion

		#region << Queries >>

		public EntityRecord GetFlow(Guid flowId)
		{
			var query = new EntityQuery("wf_approval_flow", "*", EntityQuery.QueryEQ("id", flowId));
			var resp = _recordManager.Find(query);
			if (resp.Success && resp.Object != null && resp.Object.Data.Count > 0)
				return resp.Object.Data[0];
			return null;
		}

		public List<EntityRecord> GetSteps(Guid flowId)
		{
			var query = new EntityQuery("wf_approval_step", "*", EntityQuery.QueryEQ("flow_id", flowId));
			var resp = _recordManager.Find(query);
			if (resp.Success && resp.Object != null)
				return resp.Object.Data;
			return new List<EntityRecord>();
		}

		public EntityRecord GetStep(Guid flowId, int stepNumber)
		{
			var query = new EntityQuery("wf_approval_step", "*",
				EntityQuery.QueryAND(
					EntityQuery.QueryEQ("flow_id", flowId),
					EntityQuery.QueryEQ("step_number", (decimal)stepNumber)));
			var resp = _recordManager.Find(query);
			if (resp.Success && resp.Object != null && resp.Object.Data.Count > 0)
				return resp.Object.Data[0];
			return null;
		}

		#endregion
	}
}
