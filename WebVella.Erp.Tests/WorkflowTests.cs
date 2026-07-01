using System;
using System.Collections.Generic;
using System.Linq;
using WebVella.Erp.Api;
using WebVella.Erp.Plugins.Workflow.Services;
using Xunit;

namespace WebVella.Erp.Tests
{
	[Collection("erp")]
	public class WorkflowTests
	{
		private readonly ErpTestFixture _fx;
		public WorkflowTests(ErpTestFixture fx) => _fx = fx;

		[Fact]
		public void Workflow_entities_are_created()
		{
			_fx.Run(() =>
			{
				var entMan = new EntityManager();
				foreach (var name in new[]
				{
					"wf_state_definition", "wf_transition", "wf_approval_template",
					"wf_approval_flow", "wf_approval_step"
				})
				{
					var resp = entMan.ReadEntity(name);
					Assert.True(resp.Success && resp.Object != null, $"Entity '{name}' should exist. {resp.Message}");
				}
			});
		}

		[Fact]
		public void State_machine_allows_defined_and_rejects_undefined_transitions()
		{
			_fx.Run(() =>
			{
				var svc = new WorkflowService();
				var wf = "test_order_" + Guid.NewGuid().ToString("N").Substring(0, 8);
				svc.DefineTransition(wf, "draft", "submitted", false);
				svc.DefineTransition(wf, "submitted", "approved", true);

				Assert.True(svc.CanTransition(wf, "draft", "submitted"));
				Assert.True(svc.CanTransition(wf, "submitted", "approved"));
				Assert.False(svc.CanTransition(wf, "draft", "approved")); // not defined
				Assert.False(svc.CanTransition(wf, "submitted", "submitted")); // no self-loop
			});
		}

		[Fact]
		public void Approval_flow_approves_only_when_all_steps_approved()
		{
			_fx.Run(() =>
			{
				var svc = new WorkflowService();
				var flowId = svc.StartApprovalFlow("po_approval", "pur_purchase_order", Guid.NewGuid(),
					new List<string> { "manager", "finance" });

				Assert.False(svc.IsApproved(flowId));

				Assert.True(svc.RecordDecision(flowId, 1, Guid.NewGuid(), true, "ok by manager"));
				Assert.False(svc.IsApproved(flowId)); // second step still pending

				Assert.True(svc.RecordDecision(flowId, 2, Guid.NewGuid(), true, "ok by finance"));
				Assert.True(svc.IsApproved(flowId)); // all approved
			});
		}

		[Fact]
		public void Rejecting_any_step_rejects_the_flow()
		{
			_fx.Run(() =>
			{
				var svc = new WorkflowService();
				var flowId = svc.StartApprovalFlow("po_approval", "pur_purchase_order", Guid.NewGuid(),
					new List<string> { "manager", "finance" });

				svc.RecordDecision(flowId, 1, Guid.NewGuid(), false, "rejected by manager");
				Assert.False(svc.IsApproved(flowId));

				var flow = svc.GetFlow(flowId);
				Assert.Equal("rejected", flow["status"].ToString());
			});
		}
	}
}
