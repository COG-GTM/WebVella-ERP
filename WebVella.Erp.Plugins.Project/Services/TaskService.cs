using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using WebVella.Erp.Api;
using WebVella.Erp.Api.Models;
using WebVella.Erp.Eql;
using WebVella.Erp.Exceptions;
using WebVella.Erp.Plugins.Project.Domain.Tasks;
using WebVella.Erp.Plugins.Project.Model;
using WebVella.Erp.Recurrence;
using WebVella.Erp.Web.Models;
using WebVella.Erp.Web.Pages.Application;


//TODO develop service
namespace WebVella.Erp.Plugins.Project.Services
{
	public class TaskService : BaseService
	{
		/// <summary>
		/// Calculates the key and x_search contents and updates the task
		/// </summary>
		/// <param name="data"></param>
		public EntityRecord SetCalculationFields(Guid taskId, out string subject, out Guid projectId, out Guid? projectOwnerId)
		{
			subject = "";
			projectId = Guid.Empty;
			projectOwnerId = null;

			EntityRecord patchRecord = new EntityRecord();

			var getTaskResponse = new RecordManager().Find(new EntityQuery("task", "*,$task_type_1n_task.label,$task_status_1n_task.label,$project_nn_task.abbr,$project_nn_task.id, $project_nn_task.owner_id", EntityQuery.QueryEQ("id", taskId)));
			if (!getTaskResponse.Success)
				throw new Exception(getTaskResponse.Message);
			if (!getTaskResponse.Object.Data.Any())
				throw new Exception("Task with this Id was not found");

			var taskRecord = getTaskResponse.Object.Data.First();
			var taskSubject = (string)taskRecord["subject"];
			subject = taskSubject;
			TaskProjectInfo projectInfo = null;
			var status = "";
			var type = "";
			if (((List<EntityRecord>)taskRecord["$project_nn_task"]).Any())
			{
				var projectRecord = ((List<EntityRecord>)taskRecord["$project_nn_task"]).First();
				if (projectRecord != null)
				{
					var hasAbbr = projectRecord.Properties.ContainsKey("abbr");
					var abbr = hasAbbr ? (string)projectRecord["abbr"] : null;
					projectInfo = new TaskProjectInfo((Guid)projectRecord["id"], (Guid?)projectRecord["owner_id"], hasAbbr, abbr);
				}
			}
			if (((List<EntityRecord>)taskRecord["$task_status_1n_task"]).Any())
			{
				var statusRecord = ((List<EntityRecord>)taskRecord["$task_status_1n_task"]).First();
				if (statusRecord != null && statusRecord.Properties.ContainsKey("label"))
				{
					status = (string)statusRecord["label"];
				}
			}
			if (((List<EntityRecord>)taskRecord["$task_type_1n_task"]).Any())
			{
				var typeRecord = ((List<EntityRecord>)taskRecord["$task_type_1n_task"]).First();
				if (typeRecord != null && typeRecord.Properties.ContainsKey("label"))
				{
					type = (string)typeRecord["label"];
				}
			}

			var calculation = TaskKeyCalculator.Calculate(taskSubject, (decimal)taskRecord["number"], projectInfo);
			projectId = calculation.ProjectId;
			projectOwnerId = calculation.ProjectOwnerId;

			patchRecord["id"] = taskId;
			patchRecord["key"] = calculation.Key;

			return patchRecord;
		}

		public EntityRecordList GetTaskStatuses()
		{
			var projectRecord = new EntityRecord();
			var eqlCommand = "SELECT * from task_status";
			var eqlParams = new List<EqlParameter>();
			var eqlResult = new EqlCommand(eqlCommand, eqlParams).Execute();
			if (!eqlResult.Any())
				throw new Exception("Error: No task statuses found");

			return eqlResult;
		}

		public EntityRecord GetTask(Guid taskId)
		{
			var projectRecord = new EntityRecord();
			var eqlCommand = " SELECT * from task WHERE id = @taskId";
			var eqlParams = new List<EqlParameter>() { new EqlParameter("taskId", taskId) };

			var eqlResult = new EqlCommand(eqlCommand, eqlParams).Execute();
			if (!eqlResult.Any())
				return null;
			else
				return eqlResult[0];
		}

		public EntityRecordList GetTaskQueue(Guid? projectId, Guid? userId, TasksDueType type = TasksDueType.All, int? limit = null, bool includeProjectData = false)
		{
			var selectedFields = "*";
			if (includeProjectData)
			{
				selectedFields += ",$project_nn_task.is_billable";
			}

			var eqlCommand = $"SELECT {selectedFields} from task ";
			var plan = TaskQueueFilter.Build((TaskQueueDueType)(int)type, projectId, userId, DateTime.Now);
			var eqlParams = plan.Parameters.Select(x => new EqlParameter(x.Name, x.Value)).ToList();
			var whereFilters = new List<string>(plan.WhereFilters);

			//Status open
			if (plan.ExcludeClosedStatuses)
			{
				var taskStatuses = new TaskService().GetTaskStatuses();
				var statusInfos = new List<TaskStatusInfo>();
				foreach (var taskStatus in taskStatuses)
				{
					var isClosed = (bool)taskStatus["is_closed"];
					statusInfos.Add(new TaskStatusInfo(isClosed ? (Guid)taskStatus["id"] : Guid.Empty, isClosed));
				}
				foreach (var exclusion in TaskQueueFilter.BuildClosedStatusExclusions(statusInfos))
				{
					whereFilters.Add(exclusion.Filter);
					eqlParams.Add(new EqlParameter(exclusion.Parameter.Name, exclusion.Parameter.Value));
				}
			}

			if (whereFilters.Count > 0)
			{
				eqlCommand += " WHERE " + string.Join(" AND ", whereFilters);
			}

			//Order
			if (!plan.IsKnownDueType)
				throw new Exception(TaskQueueFilter.UnknownDueTypeMessage);
			if (plan.OrderByClause != null)
				eqlCommand += plan.OrderByClause;


			//Limit
			if (limit != null)
				eqlCommand += $" PAGE 1 PAGESIZE {limit} ";


			var eqlResult = new EqlCommand(eqlCommand, eqlParams).Execute();

			return eqlResult;
		}

		public void GetTaskIconAndColor(string priorityValue, out string iconClass, out string color)
		{
			iconClass = "";
			color = "#fff";

			var priorityOptions = ((SelectField)new EntityManager().ReadEntity("task").Object.Fields.First(x => x.Name == "priority")).Options;
			var style = TaskPriorityStyleResolver.Resolve(priorityValue, priorityOptions.Select(x => new TaskPriorityOption(x.Value, x.IconClass, x.Color)));
			iconClass = style.IconClass;
			color = style.Color;

		}

		public void StartTaskTimelog(Guid taskId)
		{
			var patchRecord = new EntityRecord();
			patchRecord["id"] = taskId;
			patchRecord["timelog_started_on"] = DateTime.Now;
			var updateResponse = new RecordManager().UpdateRecord("task", patchRecord);
			if (!updateResponse.Success)
				throw new Exception(updateResponse.Message);
		}

		public void StopTaskTimelog(Guid taskId)
		{
			//Create transaction
			var patchRecord = new EntityRecord();
			patchRecord["id"] = taskId;
			patchRecord["timelog_started_on"] = null;
			var updateResponse = new RecordManager().UpdateRecord("task", patchRecord);
			if (!updateResponse.Success)
				throw new Exception(updateResponse.Message);
		}

		public EntityRecord GetPageHookLogic(BaseErpPageModel pageModel, EntityRecord record)
		{

			if (record == null)
				record = new EntityRecord();

			//Preselect owner
			ErpUser currentUser = (ErpUser)pageModel.DataModel.GetProperty("CurrentUser");
			if (currentUser != null)
				record["owner_id"] = currentUser.Id;
			//$project_nn_task.id
			//Preselect project
			if (pageModel.HttpContext.Request.Query.ContainsKey("projectId"))
			{
				var projectQueryId = pageModel.HttpContext.Request.Query["projectId"].ToString();
				if (Guid.TryParse(projectQueryId, out Guid outGuid))
				{
					var projectIdList = new List<Guid>();
					projectIdList.Add(outGuid);
					record["$project_nn_task.id"] = projectIdList;
				}
			}
			else
			{
				var eqlCommand = "SELECT created_on,type_id,$project_nn_task.id FROM task WHERE created_by = @currentUserId ORDER BY created_on DESC PAGE 1 PAGESIZE 1";
				var eqlParams = new List<EqlParameter>() { new EqlParameter("currentUserId", currentUser.Id) };
				var eqlResult = new EqlCommand(eqlCommand, eqlParams).Execute();
				if (eqlResult != null && eqlResult is EntityRecordList && eqlResult.Count > 0)
				{
					var relatedProjects = (List<EntityRecord>)eqlResult[0]["$project_nn_task"];
					if (relatedProjects.Count > 0)
					{
						var projectIdList = new List<Guid>();
						projectIdList.Add((Guid)relatedProjects[0]["id"]);
						record["$project_nn_task.id"] = projectIdList;
					}
					record["type_id"] = (Guid?)eqlResult[0]["type_id"];
				}
			}

			//Preset start date
			record["start_time"] = DateTime.Now.Date.ClearKind();
			record["end_time"] = DateTime.Now.Date.ClearKind().AddDays(1);
			return record;
		}


		public void PreCreateRecordPageHookLogic(string entityName, EntityRecord record, List<ErrorModel> errors)
		{
			var hasProjectField = record.Properties.ContainsKey(TaskValidationRules.ProjectFieldKey);
			var selectedProjectCount = hasProjectField ? ((List<Guid>)record[TaskValidationRules.ProjectFieldKey]).Count : 0;
			foreach (var error in TaskValidationRules.ValidateProjectSelectionOnCreate(hasProjectField, selectedProjectCount))
			{
				errors.Add(new ErrorModel()
				{
					Key = error.Key,
					Message = error.Message
				});
			}
		}

		public void PostCreateApiHookLogic(string entityName, EntityRecord record)
		{
			//Update key and search fields
			Guid projectId = Guid.Empty;
			Guid? projectOwnerId = null;
			string taskSubject = "";
			var patchRecord = new TaskService().SetCalculationFields((Guid)record["id"], subject: out taskSubject, projectId: out projectId, projectOwnerId: out projectOwnerId);
			var updateResponse = new RecordManager(executeHooks: false).UpdateRecord("task", patchRecord);
			if (!updateResponse.Success)
				throw new Exception(updateResponse.Message);

			//Set the initial watchers list - project lead, creator, owner
			Guid? ownerId = null;
			if (record.Properties.ContainsKey("owner_id") && record["owner_id"] != null)
				ownerId = (Guid)record["owner_id"];
			Guid? createdBy = null;
			if (record.Properties.ContainsKey("created_by") && record["created_by"] != null)
				createdBy = (Guid)record["created_by"];
			var watchers = TaskValidationRules.BuildInitialWatchers(ownerId, createdBy, projectOwnerId);

			//Create relations
			var watchRelation = new EntityRelationManager().Read("user_nn_task_watchers").Object;
			if (watchRelation == null)
				throw new Exception("Watch relation not found");

			foreach (var userId in watchers)
			{
				var createRelResponse = new RecordManager().CreateRelationManyToManyRecord(watchRelation.Id, userId, (Guid)record["id"]);
				if (!createRelResponse.Success)
					throw new Exception(createRelResponse.Message);
			}


			//Add activity log
			var subject = $"created <a href=\"/projects/tasks/tasks/r/{patchRecord["id"]}/details\">[{patchRecord["key"]}] {taskSubject}</a>";
			var relatedRecords = new List<string>() { patchRecord["id"].ToString(), projectId.ToString() };
			var scope = new List<string>() { "projects" };
			//Add watchers as scope
			foreach (var userId in watchers)
			{
				relatedRecords.Add(userId.ToString());
			}
			var taskSnippet = new Web.Services.RenderService().GetSnippetFromHtml((string)record["body"]);
			new FeedItemService().Create(id: Guid.NewGuid(), createdBy: SecurityContext.CurrentUser.Id, subject: subject,
				body: taskSnippet, relatedRecords: relatedRecords, scope: scope, type: "task");
		}

		public void PostPreUpdateApiHookLogic(string entityName, EntityRecord record, List<ErrorModel> errors)
		{
			var eqlResult = new EqlCommand("SELECT id,number, $project_nn_task.id, $project_nn_task.abbr, $user_nn_task_watchers.id FROM task WHERE id = @taskId", new EqlParameter("taskId", (Guid)record["id"])).Execute();
			if (eqlResult.Count > 0)
			{
				var oldRecord = eqlResult[0];
				Guid? oldProjectId = null;
				Guid? newProjectId = null;
				var taskWatcherIdList = new List<Guid>();
				var projectAbbr = "";
				if (oldRecord.Properties.ContainsKey("$project_nn_task") && oldRecord["$project_nn_task"] is List<EntityRecord>)
				{
					var projectRecords = (List<EntityRecord>)oldRecord["$project_nn_task"];
					if (projectRecords.Any())
					{
						if (projectRecords[0].Properties.ContainsKey("id"))
						{
							oldProjectId = (Guid)projectRecords[0]["id"];
						}
						if (projectRecords[0].Properties.ContainsKey("abbr"))
						{
							projectAbbr = (string)projectRecords[0]["abbr"];
						}
					}
				}

				if (record.Properties.ContainsKey("$project_nn_task.id"))
					newProjectId = TaskValidationRules.ParseGuidOrString(record["$project_nn_task.id"]);

				if (oldRecord.Properties.ContainsKey("$user_nn_task_watchers") && oldRecord["$user_nn_task_watchers"] is List<EntityRecord>)
				{
					var watcherRecords = (List<EntityRecord>)oldRecord["$user_nn_task_watchers"];
					foreach (var watcherRecord in watcherRecords)
					{
						taskWatcherIdList.Add((Guid)watcherRecord["id"]);
					}
				}


				if (TaskValidationRules.IsProjectChange(oldProjectId, newProjectId))
				{
					var recMan = new RecordManager();
					var relations = new EntityRelationManager().Read().Object;
					var projectTaskRel = relations.First(x => x.Name == "project_nn_task");
					if (projectTaskRel == null)
						throw new Exception("project_nn_task relation not found");

					//Remove all NN relation
					var removeRelResponse = recMan.RemoveRelationManyToManyRecord(projectTaskRel.Id, oldProjectId, (Guid)record["id"]);
					if (!removeRelResponse.Success)
						throw new Exception(removeRelResponse.Message);

					//Create new NN relation
					var addRelResponse = recMan.RemoveRelationManyToManyRecord(projectTaskRel.Id, newProjectId, (Guid)record["id"]);
					if (!addRelResponse.Success)
						throw new Exception(addRelResponse.Message);

					//change key
					record["key"] = TaskKeyCalculator.CalculateKey(projectAbbr, (decimal)oldRecord["number"]);

					var projectEqlResult = new EqlCommand("SELECT id,owner_id FROM project WHERE id = @projectId",new EqlParameter("projectId", newProjectId)).Execute();
					Guid? projectOwnerId = null;
					if (projectEqlResult != null && ((List<EntityRecord>)projectEqlResult).Any())
					{
						var newProjectRecord = ((List<EntityRecord>)projectEqlResult).First();
						if (newProjectRecord.Properties.ContainsKey("owner_id"))
							projectOwnerId = TaskValidationRules.ParseGuidOrString(newProjectRecord["owner_id"]);
					}
					//check if the new project owner is in the watcher list and add it if not
					if (TaskValidationRules.ShouldAddWatcher(projectOwnerId, taskWatcherIdList))
					{
						var watcherTaskRel = relations.First(x => x.Name == "user_nn_task_watchers");
						if (watcherTaskRel == null)
							throw new Exception("user_nn_task_watchers relation not found");

						//Create new NN relation
						var addWatcherRelResponse = recMan.RemoveRelationManyToManyRecord(watcherTaskRel.Id, projectOwnerId, (Guid)record["id"]);
						if (!addWatcherRelResponse.Success)
							throw new Exception(addWatcherRelResponse.Message);
					}
				}
			}
		}

		public void PostUpdateApiHookLogic(string entityName, EntityRecord record)
		{
			//Update key and search fields
			Guid projectId = Guid.Empty;
			Guid? projectOwnerId = null;
			string taskSubject = "";


			var patchRecord = new TaskService().SetCalculationFields((Guid)record["id"], subject: out taskSubject, projectId: out projectId, projectOwnerId: out projectOwnerId);
			var updateResponse = new RecordManager(executeHooks: false).UpdateRecord("task", patchRecord);
			if (!updateResponse.Success)
				throw new Exception(updateResponse.Message);

			//Check if owner is in watchers list. If not create relation
			if (record.Properties.ContainsKey("owner_id") && record["owner_id"] != null)
			{
				var watchers = new List<Guid>();
				var eqlCommand = "SELECT id, $user_nn_task_watchers.id FROM task WHERE id = @taskId";
				var eqlParams = new List<EqlParameter>() { new EqlParameter("taskId", (Guid)record["id"]) };
				var eqlResult = new EqlCommand(eqlCommand, eqlParams).Execute();
				foreach (var relRecord in eqlResult)
				{
					if (relRecord.Properties.ContainsKey("$user_nn_task_watchers") && relRecord["$user_nn_task_watchers"] is List<EntityRecord>)
					{
						var currentWatchers = (List<EntityRecord>)relRecord["$user_nn_task_watchers"];
						foreach (var watchRecord in currentWatchers)
						{
							watchers.Add((Guid)watchRecord["id"]);
						}
					}
				}
				if (TaskValidationRules.ShouldAddWatcher((Guid)record["owner_id"], watchers))
				{
					var watchRelation = new EntityRelationManager().Read("user_nn_task_watchers").Object;
					if (watchRelation == null)
						throw new Exception("Watch relation not found");

					var createRelResponse = new RecordManager().CreateRelationManyToManyRecord(watchRelation.Id, (Guid)record["owner_id"], (Guid)record["id"]);
					if (!createRelResponse.Success)
						throw new Exception(createRelResponse.Message);
				}
			}
		}

		public void SetStatus(Guid taskId, Guid statusId)
		{
			var patchRecord = new EntityRecord();
			patchRecord["id"] = taskId;
			patchRecord["status_id"] = statusId;
			var updateResponse = new RecordManager().UpdateRecord("task", patchRecord);
			if (!updateResponse.Success)
				throw new Exception(updateResponse.Message);
		}

		public List<EntityRecord> GetTasksThatNeedStarting()
		{
			var eqlCommand = "SELECT id FROM task WHERE status_id = @notStartedStatusId AND start_time <= @currentDate";
			var eqlParams = new List<EqlParameter>() {
				new EqlParameter("notStartedStatusId", new Guid("f3fdd750-0c16-4215-93b3-5373bd528d1f")),
				new EqlParameter("currentDate", DateTime.Now.Date),
			};

			return new EqlCommand(eqlCommand, eqlParams).Execute();
		}

		public IActionResult SetTaskRecurrenceOnPost(RecordDetailsPageModel pageModel)
		{
			ValidationException valEx = new ValidationException();

			if (!pageModel.HttpContext.Request.Form.ContainsKey("recurrence_template"))
			{
				valEx.AddError("recurrence_template", "Recurrence settings missing in post data");
				throw valEx;
			}

			DateTime startTime = DateTime.Today;
			DateTime endTime = DateTime.Today;

			if (!pageModel.HttpContext.Request.Form.ContainsKey("start_time"))
				valEx.AddError("start_time", "Start time is required");
			else if (!DateTime.TryParse(pageModel.HttpContext.Request.Form["start_time"], out startTime))
				valEx.AddError("start_time", "Start time is required");

			if (!pageModel.HttpContext.Request.Form.ContainsKey("end_time"))
				valEx.AddError("end_time", "End time is required");
			else if (!DateTime.TryParse(pageModel.HttpContext.Request.Form["start_time"], out endTime))
				valEx.AddError("end_time", "End time is required");

			valEx.CheckAndThrow();

			if (startTime < DateTime.Today)
				valEx.AddError("start_time", "Start time should be today or any future date.");

			//because in this case we post dates, we need to move time to the end of the day
			endTime = endTime.AddHours(23).AddMinutes(59).AddSeconds(59);

			if (endTime <= startTime)
				valEx.AddError("end_time", "End time should be today or any future date.");

			//todo validate status ??
			//todo validate completed on ??

			valEx.CheckAndThrow();

			//EntityRecord taskRecord = (EntityRecord)pageModel.DataModel.GetProperty("Record");
			//if (taskRecord["recurrence_id"] == null)
			//{
			//	Guid recurrenceId = Guid.NewGuid();

			//	RecurrenceTemplate recurrenceData = JsonConvert.DeserializeObject<RecurrenceTemplate>(pageModel.HttpContext.Request.Form["recurrence_template"]);
			//	var occurrences = recurrenceData.CalculateOccurrences(startTime, endTime, startTime, startTime.AddYears(5) );
			//	foreach (var o in occurrences)
			//	{
			//		var ocStartTime = o.Period.StartTime.AsDateTimeOffset.DateTime;
			//		var ocEndTime = o.Period.EndTime.AsDateTimeOffset.DateTime;

			//		EntityRecord newTask = new EntityRecord();
			//		newTask["id"] = Guid.NewGuid();
			//		newTask["start_time"] = ocStartTime;
			//		newTask["end_time"] = ocEndTime;
			//		newTask["l_scope"] = taskRecord["l_scope"];
			//		newTask["subject"] = taskRecord["subject"];
			//		newTask["body"] = taskRecord["body"];
			//		newTask["owner_id"] = taskRecord["owner_id"];
			//		newTask["created_on"] = taskRecord["created_on"];
			//		newTask["created_by"] = taskRecord["created_by"];
			//		newTask["completed_on"] = null;
			//		newTask["parent_id"] = taskRecord["parent_id"];
			//		newTask["status_id"] = taskRecord["status_id"]; // ??set always as pending
			//		newTask["priority"] = taskRecord["priority"];
			//		newTask["type_id"] = taskRecord["type_id"];
			//		newTask["key"] = Guid.NewGuid().ToString(); //set as unique guid text, post create hook will update it
			//		newTask["x_billable_minutes"] = 0;
			//		newTask["x_nonbillable_minutes"] = 0;
			//		newTask["estimated_minutes"] = taskRecord["estimated_minutes"];
			//		newTask["timelog_started_on"] = null;
			//		newTask["recurrence_id"] = recurrenceId;
			//		newTask["reserve_time"] = taskRecord["reserve_time"];
			//		newTask["recurrence_template"] = JsonConvert.SerializeObject(recurrenceData);

			//		//Debug.WriteLine($"{o.Period.StartTime}-{o.Period.EndTime}");
			//	}

			//}
			//else
			//{
			//	//UPDATE RECURRENCE CHAIN
			//}


			return null;
		}
	}
}
