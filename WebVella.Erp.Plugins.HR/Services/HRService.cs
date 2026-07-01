using System;
using System.Collections.Generic;
using WebVella.Erp.Api;
using WebVella.Erp.Api.Models;
using WebVella.Erp.Plugins.Workflow.Services;

namespace WebVella.Erp.Plugins.HR.Services
{
	/// <summary>
	/// HR domain operations. Leave requests are routed through the Workflow
	/// approval engine (WebVella.Erp.Plugins.Workflow) and payroll runs post GL
	/// journals through Finance (WebVella.Erp.Plugins.Finance) via a record hook.
	/// </summary>
	public class HRService
	{
		private readonly RecordManager _recordManager;
		private readonly WorkflowService _workflow;

		public HRService()
		{
			_recordManager = new RecordManager(null, true, true);
			_workflow = new WorkflowService();
		}

		#region << Departments / Employees >>

		public Guid CreateDepartment(string code, string name, string glExpenseAccountCode)
		{
			var id = Guid.NewGuid();
			var rec = new EntityRecord();
			rec["id"] = id;
			rec["code"] = code;
			rec["name"] = name;
			rec["gl_expense_account_code"] = glExpenseAccountCode ?? string.Empty;
			var resp = _recordManager.CreateRecord("hr_department", rec);
			if (!resp.Success)
				throw new InvalidOperationException("HR: failed to create department. " + resp.Message);
			return id;
		}

		public Guid CreateEmployee(string code, string firstName, string lastName, Guid? departmentId, decimal baseSalary, DateTime? hireDate)
		{
			var id = Guid.NewGuid();
			var rec = new EntityRecord();
			rec["id"] = id;
			rec["code"] = code;
			rec["first_name"] = firstName;
			rec["last_name"] = lastName;
			if (departmentId.HasValue) rec["department_id"] = departmentId.Value;
			rec["base_salary"] = baseSalary;
			if (hireDate.HasValue) rec["hire_date"] = hireDate.Value;
			var resp = _recordManager.CreateRecord("hr_employee", rec);
			if (!resp.Success)
				throw new InvalidOperationException("HR: failed to create employee. " + resp.Message);
			return id;
		}

		#endregion

		#region << Leave requests >>

		public Guid CreateLeaveRequest(Guid employeeId, DateTime startDate, DateTime endDate, decimal days)
		{
			var id = Guid.NewGuid();
			var rec = new EntityRecord();
			rec["id"] = id;
			rec["employee_id"] = employeeId;
			rec["start_date"] = startDate;
			rec["end_date"] = endDate;
			rec["days"] = days;
			rec["status"] = "draft";
			var resp = _recordManager.CreateRecord("hr_leave_request", rec);
			if (!resp.Success)
				throw new InvalidOperationException("HR: failed to create leave request. " + resp.Message);
			return id;
		}

		/// <summary>
		/// Submits a leave request: transitions draft -> pending_approval and starts
		/// an approval flow in the Workflow engine. Returns the created flow id.
		/// </summary>
		public Guid SubmitLeaveRequest(Guid leaveId, IList<string> approverRoles)
		{
			var leave = GetLeaveRequest(leaveId);
			if (leave == null)
				throw new InvalidOperationException("HR: leave request not found.");

			var flowId = _workflow.StartApprovalFlow(HRPlugin.LeaveWorkflowName, "hr_leave_request", leaveId, approverRoles);

			leave["approval_flow_id"] = flowId;
			leave["status"] = "pending_approval";
			var resp = _recordManager.UpdateRecord("hr_leave_request", leave);
			if (!resp.Success)
				throw new InvalidOperationException("HR: failed to submit leave request. " + resp.Message);
			return flowId;
		}

		/// <summary>
		/// Moves a pending leave request to approved. The record hook enforces that
		/// the associated approval flow IsApproved before the transition succeeds.
		/// </summary>
		public bool ApproveLeaveRequest(Guid leaveId)
		{
			var leave = GetLeaveRequest(leaveId);
			if (leave == null)
				return false;
			leave["status"] = "approved";
			var resp = _recordManager.UpdateRecord("hr_leave_request", leave);
			return resp.Success;
		}

		public bool RejectLeaveRequest(Guid leaveId)
		{
			var leave = GetLeaveRequest(leaveId);
			if (leave == null)
				return false;
			leave["status"] = "rejected";
			var resp = _recordManager.UpdateRecord("hr_leave_request", leave);
			return resp.Success;
		}

		public EntityRecord GetLeaveRequest(Guid leaveId)
		{
			var query = new EntityQuery("hr_leave_request", "*", EntityQuery.QueryEQ("id", leaveId));
			var resp = _recordManager.Find(query);
			if (resp.Success && resp.Object != null && resp.Object.Data.Count > 0)
				return resp.Object.Data[0];
			return null;
		}

		#endregion

		#region << Payroll >>

		public Guid CreatePayrollRun(Guid? employeeId, string period, DateTime runDate, decimal totalAmount)
		{
			var id = Guid.NewGuid();
			var rec = new EntityRecord();
			rec["id"] = id;
			if (employeeId.HasValue) rec["employee_id"] = employeeId.Value;
			rec["period"] = period;
			rec["run_date"] = runDate;
			rec["status"] = "draft";
			rec["total_amount"] = totalAmount;
			var resp = _recordManager.CreateRecord("hr_payroll_run", rec);
			if (!resp.Success)
				throw new InvalidOperationException("HR: failed to create payroll run. " + resp.Message);
			return id;
		}

		/// <summary>
		/// Posts a payroll run (status -> posted). The PayrollRunHooks post-update
		/// hook posts the GL expense entry through Finance once persisted.
		/// </summary>
		public bool PostPayrollRun(Guid runId)
		{
			var run = GetPayrollRun(runId);
			if (run == null)
				return false;
			run["status"] = "posted";
			var resp = _recordManager.UpdateRecord("hr_payroll_run", run);
			return resp.Success;
		}

		public EntityRecord GetPayrollRun(Guid runId)
		{
			var query = new EntityQuery("hr_payroll_run", "*", EntityQuery.QueryEQ("id", runId));
			var resp = _recordManager.Find(query);
			if (resp.Success && resp.Object != null && resp.Object.Data.Count > 0)
				return resp.Object.Data[0];
			return null;
		}

		#endregion
	}
}
