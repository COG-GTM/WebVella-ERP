using System;
using System.Collections.Generic;
using WebVella.Erp.Api.Models;
using WebVella.Erp.Hooks;

namespace WebVella.Erp.Plugins.Workflow.Hooks
{
	/// <summary>
	/// Guards the approval-flow status field: only the configured terminal values
	/// are accepted. Auto-discovered by HookManager.RegisterHooks.
	/// </summary>
	[HookAttachment("wf_approval_flow")]
	public class ApprovalFlowHooks : IErpPreCreateRecordHook, IErpPreUpdateRecordHook
	{
		private static readonly HashSet<string> ValidStatuses = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
		{
			"pending", "approved", "rejected"
		};

		public void OnPreCreateRecord(string entityName, EntityRecord record, List<ErrorModel> errors)
		{
			Validate(record, errors);
		}

		public void OnPreUpdateRecord(string entityName, EntityRecord record, List<ErrorModel> errors)
		{
			Validate(record, errors);
		}

		private static void Validate(EntityRecord record, List<ErrorModel> errors)
		{
			if (record == null || !record.Properties.ContainsKey("status") || record.Properties["status"] == null)
				return;

			var status = record.Properties["status"].ToString();
			if (!ValidStatuses.Contains(status))
			{
				errors.Add(new ErrorModel
				{
					Key = "status",
					Message = $"Invalid approval-flow status '{status}'. Allowed: pending, approved, rejected."
				});
			}
		}
	}
}
