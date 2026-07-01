using System;
using System.Collections.Generic;
using WebVella.Erp.Api;
using WebVella.Erp.Api.Models;
using WebVella.Erp.Hooks;
using WebVella.Erp.Plugins.Workflow.Services;

namespace WebVella.Erp.Plugins.HR.Hooks
{
	/// <summary>
	/// Enforces HR leave-request business rules at the record level:
	///  - leave days must be positive and end_date &gt;= start_date;
	///  - status transitions must be legal per the Workflow engine (CanTransition);
	///  - a move to "approved" is only allowed once the associated approval flow
	///    IsApproved.
	/// Auto-discovered by HookManager.RegisterHooks via the assembly scan.
	/// </summary>
	[HookAttachment("hr_leave_request")]
	public class LeaveRequestHooks : IErpPreCreateRecordHook, IErpPreUpdateRecordHook
	{
		public void OnPreCreateRecord(string entityName, EntityRecord record, List<ErrorModel> errors)
		{
			ValidateDates(record, null, errors);
		}

		public void OnPreUpdateRecord(string entityName, EntityRecord record, List<ErrorModel> errors)
		{
			var existing = LoadExisting(record);
			ValidateDates(record, existing, errors);
			EnforceTransition(record, existing, errors);
		}

		private static void ValidateDates(EntityRecord record, EntityRecord existing, List<ErrorModel> errors)
		{
			if (record == null)
				return;

			decimal days = GetDecimal(record, existing, "days");
			if (days <= 0)
			{
				errors.Add(new ErrorModel { Key = "days", Message = "Leave days must be a positive number." });
			}

			DateTime? start = GetDate(record, existing, "start_date");
			DateTime? end = GetDate(record, existing, "end_date");
			if (start.HasValue && end.HasValue && end.Value < start.Value)
			{
				errors.Add(new ErrorModel { Key = "end_date", Message = "Leave end_date must be on or after start_date." });
			}
		}

		private static void EnforceTransition(EntityRecord record, EntityRecord existing, List<ErrorModel> errors)
		{
			if (existing == null || !record.Properties.ContainsKey("status") || record.Properties["status"] == null)
				return;

			string newStatus = record.Properties["status"].ToString();
			string oldStatus = existing.Properties.ContainsKey("status") && existing.Properties["status"] != null
				? existing.Properties["status"].ToString()
				: "draft";

			if (string.Equals(newStatus, oldStatus, StringComparison.OrdinalIgnoreCase))
				return;

			var workflow = new WorkflowService();
			if (!workflow.CanTransition(HRPlugin.LeaveWorkflowName, oldStatus, newStatus))
			{
				errors.Add(new ErrorModel { Key = "status", Message = $"Illegal leave status transition '{oldStatus}' -> '{newStatus}'." });
				return;
			}

			if (newStatus == "approved")
			{
				var flowId = GetGuid(record, existing, "approval_flow_id");
				if (!flowId.HasValue || !workflow.IsApproved(flowId.Value))
				{
					errors.Add(new ErrorModel { Key = "status", Message = "Leave request cannot be approved until its approval flow is fully approved." });
				}
			}
		}

		private static EntityRecord LoadExisting(EntityRecord record)
		{
			if (record == null || !record.Properties.ContainsKey("id") || record.Properties["id"] == null)
				return null;
			Guid id;
			try { id = (Guid)record.Properties["id"]; }
			catch { return null; }

			var recMan = new RecordManager(null, true, true);
			var query = new EntityQuery("hr_leave_request", "*", EntityQuery.QueryEQ("id", id));
			var resp = recMan.Find(query);
			if (resp.Success && resp.Object != null && resp.Object.Data.Count > 0)
				return resp.Object.Data[0];
			return null;
		}

		private static decimal GetDecimal(EntityRecord record, EntityRecord existing, string field)
		{
			var value = GetValue(record, existing, field);
			if (value == null) return 0;
			try { return Convert.ToDecimal(value); }
			catch { return 0; }
		}

		private static DateTime? GetDate(EntityRecord record, EntityRecord existing, string field)
		{
			var value = GetValue(record, existing, field);
			if (value == null) return null;
			try { return Convert.ToDateTime(value); }
			catch { return null; }
		}

		private static Guid? GetGuid(EntityRecord record, EntityRecord existing, string field)
		{
			var value = GetValue(record, existing, field);
			if (value == null) return null;
			try { return (Guid)value; }
			catch
			{
				try { return Guid.Parse(value.ToString()); }
				catch { return null; }
			}
		}

		private static object GetValue(EntityRecord record, EntityRecord existing, string field)
		{
			if (record != null && record.Properties.ContainsKey(field) && record.Properties[field] != null)
				return record.Properties[field];
			if (existing != null && existing.Properties.ContainsKey(field) && existing.Properties[field] != null)
				return existing.Properties[field];
			return null;
		}
	}
}
