using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using WebVella.Erp.Api.Models;
using WebVella.Erp.Eql;
using WebVella.Erp.Exceptions;
using WebVella.Erp.Plugins.Project.Domain.Reports;

namespace WebVella.Erp.Plugins.Project.Services
{
	public class ReportService
	{
		public List<EntityRecord> GetTimelogData(int year, int month, Guid? accountId)
		{
			ValidationException valEx = new ValidationException();

			List<EqlParameter> eqlParams;
			var validationErrors = TimelogReportValidator.Validate(year, month, accountId, id =>
			{
				eqlParams = new List<EqlParameter>() { new EqlParameter("id", id) };
				var eqlResult = new EqlCommand("SELECT * FROM account WHERE id = @id ", eqlParams).Execute();
				return eqlResult.Any();
			});
			foreach (var error in validationErrors)
				valEx.AddError(error.Key, error.Message);

			valEx.CheckAndThrow();

			/// load timelog records from database
			var dateWindow = MonthDateWindow.For(year, month);
			eqlParams = new List<EqlParameter>() {
				new EqlParameter("from_date", dateWindow.FromDate),
				new EqlParameter("to_date", dateWindow.ToDate),
				new EqlParameter("scope", "projects" ),
			};
			string eql = @"SELECT id,is_billable,l_related_records,minutes FROM timelog 
						   WHERE logged_on >= @from_date AND 
							  logged_on <= @to_date AND
							  l_scope CONTAINS @scope";
			var timelogRecords = new EqlCommand(eql, eqlParams).Execute();

			HashSet<Guid> setOfTasksWithTimelog = TimelogReportAggregator.CollectTaskIds(timelogRecords, timelog =>
			{
				List<Guid> ids = JsonConvert.DeserializeObject<List<Guid>>((string)timelog["l_related_records"]);
				Guid taskId = TimelogReportAggregator.GetTaskId(ids);
				timelog["task_id"] = taskId;
				return taskId;
			});

			//load all tasks 
			eqlParams = new List<EqlParameter>();
			eql = @"SELECT id,subject, $project_nn_task.id, $project_nn_task.name,$project_nn_task.account_id, $task_type_1n_task.label FROM task";
			var tasks = new EqlCommand(eql, eqlParams).Execute();

			//process tasks - split records for projects - filter by account - filter by timelog
			List<TaskProjectRow<EntityRecord, EntityRecord>> taskRows;
			try
			{
				taskRows = TimelogReportAggregator.SplitTasksByProject<EntityRecord, EntityRecord>(
					tasks,
					setOfTasksWithTimelog,
					accountId,
					task => (Guid)task["id"],
					task => (List<EntityRecord>)task["$project_nn_task"],
					project => (Guid?)project["account_id"]);
			}
			catch (ProjectWithoutAccountException ex)
			{
				throw new Exception(ex.Message);
			}

			List<EntityRecord> result = new List<EntityRecord>();
			foreach (var row in taskRows)
			{
				var task = row.Task;
				var project = row.Project;
				EntityRecord rec = new EntityRecord();
				rec["task_id"] = task["id"];
				rec["project_id"] = project["id"];
				rec["task_subject"] = task["subject"];
				rec["project_name"] = project["name"];
				rec["task_type"] = ((List<EntityRecord>)task["$task_type_1n_task"])[0]["label"];
				rec["billable_minutes"] = (decimal)0;
				rec["non_billable_minutes"] = (decimal)0;

				var totals = TimelogReportAggregator.SumMinutes(
					timelogRecords,
					(Guid)task["id"],
					timelog => (Guid)timelog["task_id"],
					timelog => (bool)timelog["is_billable"],
					timelog => (decimal)timelog["minutes"]);
				rec["billable_minutes"] = totals.BillableMinutes;
				rec["non_billable_minutes"] = totals.NonBillableMinutes;

				result.Add(rec);
			}

			return result;
		}
	}
}
