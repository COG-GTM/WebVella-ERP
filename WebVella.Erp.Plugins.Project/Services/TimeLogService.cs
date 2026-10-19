using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using WebVella.Erp.Api;
using WebVella.Erp.Api.Models;
using WebVella.Erp.Database;
using WebVella.Erp.Eql;
using WebVella.Erp.Exceptions;
using WebVella.Erp.Plugins.Project.Domain;
using WebVella.Erp.Web.Models;
using WebVella.Erp.Web.Pages.Application;


//TODO develop service
namespace WebVella.Erp.Plugins.Project.Services
{
	public class TimeLogService : BaseService
	{
		public void Create(Guid? id = null, Guid? createdBy = null, DateTime? createdOn = null, DateTime? loggedOn = null, int minutes = 0, bool isBillable = true, string body = "",
			List<string> scope = null, List<Guid> relatedRecords = null)
		{
			#region << Init >>
			if (id == null)
				id = Guid.NewGuid();

			if (createdBy == null)
				createdBy = SystemIds.SystemUserId;

			if (createdOn == null)
				createdOn = DateTime.UtcNow;

			if (loggedOn == null)
				loggedOn = DateTime.UtcNow;
			#endregion

			try
			{
				var record = new EntityRecord();
				record["id"] = id;
				record["created_by"] = createdBy;
				record["created_on"] = createdOn;
				record["logged_on"] = loggedOn.ConvertAppDateToUtc();
				record["body"] = body;
				record["minutes"] = minutes;
				record["is_billable"] = isBillable;
				record["l_scope"] = JsonConvert.SerializeObject(scope);
				record["l_related_records"] = JsonConvert.SerializeObject(relatedRecords);

				var response = RecMan.CreateRecord("timelog", record);
				if (!response.Success)
				{
					throw new ValidationException(response.Message);
				}
			}
			catch (Exception)
			{
				throw;
			}
		}

		public void Delete(Guid recordId)
		{
			//Validate - only authors can start to delete their posts and comments. Moderation will be later added if needed
			{
				var eqlCommand = "SELECT id,created_by FROM timelog WHERE id = @recordId";
				var eqlParams = new List<EqlParameter>() { new EqlParameter("recordId", recordId) };
				var eqlResult = new EqlCommand(eqlCommand, eqlParams).Execute();
				TimeLogRules.RequireRecordFound(eqlResult.Any());
				TimeLogRules.RequireAuthor((Guid)eqlResult[0]["created_by"], SecurityContext.CurrentUser.Id);
			}

			var deleteResponse = new RecordManager().DeleteRecord("timelog", recordId);
			if (!deleteResponse.Success)
			{
				throw new Exception(deleteResponse.Message);
			}



		}

		public EntityRecordList GetTimelogsForPeriod(Guid? projectId, Guid? userId, DateTime startDate, DateTime endDate)
		{
			var projectRecord = new EntityRecord();
			var eqlCommand = "SELECT * from timelog WHERE logged_on >= @startDate AND logged_on < @endDate ";
			var eqlParams = new List<EqlParameter>() { new EqlParameter("startDate", startDate), new EqlParameter("endDate", endDate) };

			if (projectId != null)
			{
				eqlCommand += " AND l_related_records CONTAINS @projectId";
				eqlParams.Add(new EqlParameter("projectId", projectId));
			}
			if (userId != null)
			{
				eqlCommand += " AND created_by = @userId";
				eqlParams.Add(new EqlParameter("userId", userId));
			}
			if (userId != null) { }

			var eqlResult = new EqlCommand(eqlCommand, eqlParams).Execute();

			return eqlResult;
		}

		public IActionResult PostApplicationNodePageHookLogic(ApplicationNodePageModel pageModel)
		{
			Guid TIMETRACK_PAGE_ID = new Guid("e9c8f7ef-4714-40e9-90cd-3814d89603b1");
			var page = (ErpPage)pageModel.DataModel.GetProperty("Page");

			var currentUser = SecurityContext.CurrentUser;
			if (page != null && page.Id == TIMETRACK_PAGE_ID && currentUser != null)
			{
				var postForm = pageModel.PageContext.HttpContext.Request.Form;
				var form = TimeTrackForm.Parse(postForm["task_id"].ToString(), postForm["minutes"].ToString(), postForm["logged_on"].ToString(),
					postForm["is_billable"].ToString(), postForm["body"].ToString());
				Guid taskId = form.TaskId;

				var eqlCommand = " SELECT *,$project_nn_task.id from task WHERE id = @taskId";
				var eqlParams = new List<EqlParameter>() { new EqlParameter("taskId", taskId) };
				var eqlResult = new EqlCommand(eqlCommand, eqlParams).Execute();
				if (!eqlResult.Any())
					throw new Exception("Task with taskId not found");

				var taskRecord = eqlResult[0];
				var scope = new List<string>() { "projects" };
				var relatedRecords = new List<Guid>() { (Guid)taskRecord["id"] };

				if (taskRecord.Properties.ContainsKey("$project_nn_task") && ((List<EntityRecord>)taskRecord["$project_nn_task"]).Any())
				{
					var projectRecord = ((List<EntityRecord>)taskRecord["$project_nn_task"]).First();
					relatedRecords.Add((Guid)projectRecord["id"]);
				}

				using (var connection = DbContext.Current.CreateConnection())
				{
					try
					{
						connection.BeginTransaction();
						new TaskService().StopTaskTimelog(taskId);
						if (form.ShouldLogTime)
						{
							new TimeLogService().Create(null, currentUser.Id, DateTime.Now, form.LoggedOn, form.Minutes, form.IsBillable, form.Body, scope, relatedRecords);
						}
						connection.CommitTransaction();
						return new RedirectResult("/projects/track-time/track-time/a/track-time");
					}
					catch (Exception)
					{
						connection.RollbackTransaction();
						throw;
					}
				}

			}

			return null;
		}

		public void PreCreateApiHookLogic(string entityName, EntityRecord record, List<ErrorModel> errors)
		{
			TimeLogRules.RequireIdField(record.Properties.ContainsKey("id"));

			Guid recordId = (Guid)record["id"];
			var relatedTaskRecords = new EntityRecordList();
			//Get timelog
			var isProjectTimeLog = TimeLogRules.IsProjectTimeLog((string)record["l_scope"]);

			if (isProjectTimeLog)
			{

				if (TimeLogRules.HasRelatedRecords((string)record["l_related_records"]))
				{
					try
					{
						var relatedRecordGuid = TimeLogRules.ParseRelatedRecordIds((string)record["l_related_records"]);
						var taskEqlCommand = "SELECT *,$project_nn_task.id, $user_nn_task_watchers.id FROM task WHERE ";
						var filterStringList = new List<string>();
						var taskEqlParams = new List<EqlParameter>();
						var index = 1;
						foreach (var taskGuid in relatedRecordGuid)
						{
							var paramName = "taskId" + index;
							filterStringList.Add($" id = @{paramName} ");
							taskEqlParams.Add(new EqlParameter(paramName, taskGuid));
							index++;
						}
						taskEqlCommand += String.Join(" OR ", filterStringList);
						relatedTaskRecords = new EqlCommand(taskEqlCommand, taskEqlParams).Execute();
					}
					catch(Exception)
					{
						throw;
					}
				}

				TimeLogRules.RequireRelatedTask(relatedTaskRecords.Any());

				var taskRecord = relatedTaskRecords[0]; //Currently should be related only to 1 task in projects
				var taskId = (Guid)taskRecord["id"];
				var isBillable = (bool)record["is_billable"];
				var minutesField = TimeLogRules.GetMinutesField(isBillable);
				var loggedTypeString = TimeLogRules.GetLoggedTypeLabel(isBillable);
				var patchRecord = new EntityRecord();
				foreach (var patchField in TimeLogRules.BuildTaskPatchOnCreate(taskId, isBillable, (decimal)taskRecord[minutesField], (int)record["minutes"]))
					patchRecord[patchField.Key] = patchField.Value;

				var updateResponse = new RecordManager(executeHooks: false).UpdateRecord("task", patchRecord);
				if (!updateResponse.Success)
					throw new Exception(updateResponse.Message);



				//Add feed record - include all taskIds and related Project ids in the field
				Guid? projectId = null;
				if (((List<EntityRecord>)taskRecord["$project_nn_task"]).Any())
				{
					var projectRecord = ((List<EntityRecord>)taskRecord["$project_nn_task"]).First();
					if (projectRecord != null)
					{
						projectId = (Guid)projectRecord["id"];
					}
				}

				var taskWatchersList = new List<string>();
				if (((List<EntityRecord>)taskRecord["$user_nn_task_watchers"]).Any())
				{
					taskWatchersList = ((List<EntityRecord>)taskRecord["$user_nn_task_watchers"]).Select(x => ((Guid)x["id"]).ToString()).ToList();
				}

				//Add activity log
				var subject = $"logged {((int)record["minutes"]).ToString("N0")} {loggedTypeString} minutes on <a href=\"/projects/tasks/tasks/r/{taskRecord["id"]}/details\">[{taskRecord["key"]}] {taskRecord["subject"]}</a>";
				var relatedRecords = new List<string>() { taskRecord["id"].ToString(), record["id"].ToString() };
				if (projectId != null)
				{
					relatedRecords.Add(projectId.ToString());
				}
				relatedRecords.AddRange(taskWatchersList);

				var scope = new List<string>() { "projects" };
				var logSnippet = new Web.Services.RenderService().GetSnippetFromHtml((string)record["body"]);
				new FeedItemService().Create(id: Guid.NewGuid(), createdBy: SecurityContext.CurrentUser.Id, subject: subject,
					body: logSnippet, relatedRecords: relatedRecords, scope: scope, type: "timelog");
			}
		}

		public void PreDeleteApiHookLogic(string entityName, EntityRecord record, List<ErrorModel> errors)
		{
			TimeLogRules.RequireIdField(record.Properties.ContainsKey("id"));

			Guid recordId = (Guid)record["id"];
			var timelogRecord = new EntityRecord();
			var relatedTaskRecords = new EntityRecordList();
			//Get timelog

			var eqlCommand = "SELECT * from timelog WHERE id = @recordId";
			var eqlParams = new List<EqlParameter>() { new EqlParameter("recordId", recordId) };
			var eqlResult = new EqlCommand(eqlCommand, eqlParams).Execute();
			TimeLogRules.RequireTimeLogFound(eqlResult.Any());

			timelogRecord = eqlResult[0];
			var isProjectTimeLog = TimeLogRules.IsProjectTimeLog((string)timelogRecord["l_scope"]);

			if (isProjectTimeLog)
			{

				if (TimeLogRules.HasRelatedRecords((string)timelogRecord["l_related_records"]))
				{
					try
					{
						var relatedRecordGuid = TimeLogRules.ParseRelatedRecordIds((string)timelogRecord["l_related_records"]);
						var taskEqlCommand = "SELECT *,$project_nn_task.id from task WHERE ";
						var filterStringList = new List<string>();
						var taskEqlParams = new List<EqlParameter>();
						var index = 1;
						foreach (var taskGuid in relatedRecordGuid)
						{
							var paramName = "taskId" + index;
							filterStringList.Add($" id = @{paramName} ");
							taskEqlParams.Add(new EqlParameter(paramName, taskGuid));
							index++;
						}
						taskEqlCommand += String.Join(" OR ", filterStringList);
						relatedTaskRecords = new EqlCommand(taskEqlCommand, taskEqlParams).Execute();
					}
					catch
					{
						//Do nothing
					}
				}

				var taskRecord = relatedTaskRecords[0]; //Currently should be related only to 1 task in projects
				var taskId = (Guid)taskRecord["id"];
				var isBillable = (bool)timelogRecord["is_billable"];
				var minutesField = TimeLogRules.GetMinutesField(isBillable);
				var patchRecord = new EntityRecord();
				foreach (var patchField in TimeLogRules.BuildTaskPatchOnDelete(taskId, isBillable, (decimal)taskRecord[minutesField], (decimal)timelogRecord["minutes"]))
					patchRecord[patchField.Key] = patchField.Value;
				var updateResponse = new RecordManager(executeHooks: false).UpdateRecord("task", patchRecord);
				if (!updateResponse.Success)
					throw new Exception(updateResponse.Message);

				//Delete feeds that related to this timelog
				var feedEqlCommand = "SELECT id FROM feed_item WHERE l_related_records CONTAINS @recordId";
				var feedEqlParams = new List<EqlParameter>() { new EqlParameter("recordId", recordId) };
				var feedEqlResult = new EqlCommand(feedEqlCommand, feedEqlParams).Execute();
				foreach (var feedId in feedEqlResult)
				{
					var deleteResponse = new RecordManager(executeHooks: false).DeleteRecord("feed_item", (Guid)feedId["id"]);
					if (!deleteResponse.Success)
						throw new Exception(deleteResponse.Message);
				}

			}
		}
	}
}
