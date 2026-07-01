using System;
using System.Collections.Generic;
using System.Linq;
using WebVella.Erp.Api;
using WebVella.Erp.Api.Models;
using WebVella.Erp.Plugins.HR;
using WebVella.Erp.Plugins.HR.Services;
using WebVella.Erp.Plugins.Workflow.Services;
using Xunit;

namespace WebVella.Erp.Tests
{
	[Collection("erp")]
	public class HRTests
	{
		private readonly ErpTestFixture _fx;
		public HRTests(ErpTestFixture fx) => _fx = fx;

		[Fact]
		public void HR_entities_and_fields_are_created()
		{
			_fx.Run(() =>
			{
				var entMan = new EntityManager();
				var expected = new[] { "hr_department", "hr_employee", "hr_leave_request", "hr_payroll_run" };
				foreach (var name in expected)
				{
					var resp = entMan.ReadEntity(name);
					Assert.True(resp.Success && resp.Object != null, $"Entity '{name}' should exist. {resp.Message}");
				}

				var emp = entMan.ReadEntity("hr_employee").Object;
				var empFields = emp.Fields.Select(f => f.Name).ToList();
				foreach (var f in new[] { "code", "first_name", "last_name", "department_id", "base_salary", "hire_date" })
					Assert.Contains(f, empFields);

				var leave = entMan.ReadEntity("hr_leave_request").Object;
				var leaveFields = leave.Fields.Select(f => f.Name).ToList();
				foreach (var f in new[] { "employee_id", "start_date", "end_date", "days", "status" })
					Assert.Contains(f, leaveFields);

				var run = entMan.ReadEntity("hr_payroll_run").Object;
				var runFields = run.Fields.Select(f => f.Name).ToList();
				foreach (var f in new[] { "period", "run_date", "status", "total_amount" })
					Assert.Contains(f, runFields);
			});
		}

		[Fact]
		public void HR_relations_are_created()
		{
			_fx.Run(() =>
			{
				var relMan = new EntityRelationManager();
				foreach (var name in new[] { "hr_department_1n_employee", "hr_employee_1n_leave_request", "hr_employee_1n_payroll_run" })
				{
					var resp = relMan.Read(name);
					Assert.True(resp.Success && resp.Object != null, $"Relation '{name}' should exist. {resp.Message}");
				}
			});
		}

		[Fact]
		public void Leave_workflow_states_and_transitions_are_defined()
		{
			_fx.Run(() =>
			{
				var wf = new WorkflowService();
				Assert.True(wf.CanTransition(HRPlugin.LeaveWorkflowName, "draft", "pending_approval"));
				Assert.True(wf.CanTransition(HRPlugin.LeaveWorkflowName, "pending_approval", "approved"));
				Assert.True(wf.CanTransition(HRPlugin.LeaveWorkflowName, "pending_approval", "rejected"));
				Assert.False(wf.CanTransition(HRPlugin.LeaveWorkflowName, "draft", "approved"));
			});
		}

		[Fact]
		public void Leave_days_must_be_positive()
		{
			_fx.Run(() =>
			{
				var svc = new HRService();
				var empId = svc.CreateEmployee("E-POS", "Ann", "Pos", null, 5000m, DateTime.UtcNow);
				Assert.Throws<InvalidOperationException>(() =>
					svc.CreateLeaveRequest(empId, new DateTime(2025, 1, 1), new DateTime(2025, 1, 3), 0m));
			});
		}

		[Fact]
		public void Leave_end_date_must_not_precede_start_date()
		{
			_fx.Run(() =>
			{
				var svc = new HRService();
				var empId = svc.CreateEmployee("E-DATE", "Bob", "Date", null, 5000m, DateTime.UtcNow);
				Assert.Throws<InvalidOperationException>(() =>
					svc.CreateLeaveRequest(empId, new DateTime(2025, 1, 5), new DateTime(2025, 1, 1), 3m));
			});
		}

		[Fact]
		public void Valid_leave_request_is_accepted()
		{
			_fx.Run(() =>
			{
				var svc = new HRService();
				var empId = svc.CreateEmployee("E-OK", "Cara", "Ok", null, 5000m, DateTime.UtcNow);
				var leaveId = svc.CreateLeaveRequest(empId, new DateTime(2025, 1, 1), new DateTime(2025, 1, 3), 3m);
				Assert.NotEqual(Guid.Empty, leaveId);
				var leave = svc.GetLeaveRequest(leaveId);
				Assert.Equal("draft", leave["status"].ToString());
			});
		}

		[Fact]
		public void Approved_flow_moves_leave_request_to_approved()
		{
			_fx.Run(() =>
			{
				var svc = new HRService();
				var wf = new WorkflowService();

				var empId = svc.CreateEmployee("E-APP", "Dan", "App", null, 6000m, DateTime.UtcNow);
				var leaveId = svc.CreateLeaveRequest(empId, new DateTime(2025, 2, 1), new DateTime(2025, 2, 4), 4m);

				var flowId = svc.SubmitLeaveRequest(leaveId, new List<string> { "manager", "hr" });
				Assert.Equal("pending_approval", svc.GetLeaveRequest(leaveId)["status"].ToString());

				// cannot approve before the flow is fully approved (hook enforces IsApproved)
				Assert.False(svc.ApproveLeaveRequest(leaveId));
				Assert.Equal("pending_approval", svc.GetLeaveRequest(leaveId)["status"].ToString());

				wf.RecordDecision(flowId, 1, Guid.NewGuid(), true, "ok manager");
				wf.RecordDecision(flowId, 2, Guid.NewGuid(), true, "ok hr");
				Assert.True(wf.IsApproved(flowId));

				Assert.True(svc.ApproveLeaveRequest(leaveId));
				Assert.Equal("approved", svc.GetLeaveRequest(leaveId)["status"].ToString());
			});
		}

		[Fact]
		public void Rejected_flow_moves_leave_request_to_rejected()
		{
			_fx.Run(() =>
			{
				var svc = new HRService();
				var wf = new WorkflowService();

				var empId = svc.CreateEmployee("E-REJ", "Eve", "Rej", null, 6000m, DateTime.UtcNow);
				var leaveId = svc.CreateLeaveRequest(empId, new DateTime(2025, 3, 1), new DateTime(2025, 3, 4), 4m);

				var flowId = svc.SubmitLeaveRequest(leaveId, new List<string> { "manager", "hr" });
				wf.RecordDecision(flowId, 1, Guid.NewGuid(), false, "denied");
				Assert.False(wf.IsApproved(flowId));

				Assert.True(svc.RejectLeaveRequest(leaveId));
				Assert.Equal("rejected", svc.GetLeaveRequest(leaveId)["status"].ToString());
			});
		}

		[Fact]
		public void Posting_payroll_run_posts_gl_expense_entry()
		{
			_fx.Run(() =>
			{
				var svc = new HRService();
				var recMan = new RecordManager();

				var empId = svc.CreateEmployee("E-PAY", "Finn", "Pay", null, 8000m, DateTime.UtcNow);
				var runId = svc.CreatePayrollRun(empId, "2025-04", DateTime.UtcNow, 8000m);

				Assert.True(svc.PostPayrollRun(runId));

				var query = new EntityQuery("fin_journal_entry", "*",
					EntityQuery.QueryAND(
						EntityQuery.QueryEQ("source_module", "hr-payroll"),
						EntityQuery.QueryEQ("source_reference", runId.ToString())));
				var resp = recMan.Find(query);
				Assert.True(resp.Success && resp.Object != null && resp.Object.Data.Count == 1,
					"Posting a payroll run must create exactly one fin_journal_entry.");

				var je = resp.Object.Data[0];
				Assert.Equal(8000m, Convert.ToDecimal(je["total_debit"]));
				Assert.Equal(8000m, Convert.ToDecimal(je["total_credit"]));
			});
		}
	}
}
