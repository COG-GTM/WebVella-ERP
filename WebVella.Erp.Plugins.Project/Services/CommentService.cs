using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using WebVella.Erp.Api;
using WebVella.Erp.Api.Models;
using WebVella.Erp.Eql;
using WebVella.Erp.Exceptions;
using WebVella.Erp.Plugins.Project.Domain.Comments;


//TODO develop service
namespace WebVella.Erp.Plugins.Project.Services
{
	public class CommentService : BaseService
	{
		public void Create(Guid? id = null, Guid? createdBy = null, DateTime? createdOn = null, string body = "", Guid? parentId = null,
			List<string> scope = null, List<Guid> relatedRecords = null)
		{
			#region << Init >>
			if (id == null)
				id = Guid.NewGuid();

			if (createdBy == null)
				createdBy = SystemIds.SystemUserId;

			if (createdOn == null)
				createdOn = DateTime.UtcNow;
			#endregion

			try
			{
				var record = new EntityRecord();
				record["id"] = id;
				record["created_by"] = createdBy;
				record["created_on"] = createdOn;
				record["body"] = body;
				record["parent_id"] = parentId;
				record["l_scope"] = JsonConvert.SerializeObject(scope);
				record["l_related_records"] = JsonConvert.SerializeObject(relatedRecords);

				var response = RecMan.CreateRecord("comment", record);
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
				var eqlCommand = "SELECT id,created_by FROM comment WHERE id = @commentId";
				var eqlParams = new List<EqlParameter>() { new EqlParameter("commentId", recordId) };
				var eqlResult = new EqlCommand(eqlCommand, eqlParams).Execute();
				var authorization = CommentDeletionRules.AuthorizeDelete(eqlResult.Any(),
					() => (Guid)eqlResult[0]["created_by"], () => SecurityContext.CurrentUser.Id);
				if (authorization == CommentDeleteAuthorization.RecordNotFound)
					throw new Exception(CommentDeletionRules.RecordNotFoundMessage);
				if (authorization == CommentDeleteAuthorization.NotAuthor)
					throw new Exception(CommentDeletionRules.NotAuthorMessage);
			}

			List<Guid> commentIdListForDeletion;
			//Find and add all the child comments
			//TODO currently only on level if comment nesting is implemented. If it is increased this method should be changed
			{
				var eqlCommand = "SELECT id FROM comment WHERE parent_id = @commentId";
				var eqlParams = new List<EqlParameter>() { new EqlParameter("commentId", recordId) };
				var eqlResult = new EqlCommand(eqlCommand, eqlParams).Execute();
				commentIdListForDeletion = CommentDeletionRules.BuildDeletionList(recordId, eqlResult.Select(x => (Guid)x["id"]).ToList());
			}

			//Create transaction 

			//Trigger delete
			foreach (var commentId in commentIdListForDeletion)
			{

				//Remove case relations
				//TODO


				var deleteResponse = new RecordManager().DeleteRecord("comment", commentId);
				if (!deleteResponse.Success)
				{
					throw new Exception(deleteResponse.Message);
				}
			}



		}

		public void PreCreateApiHookLogic(string entityName, EntityRecord record, List<ErrorModel> errors)
		{
			var isProjectComment = false;
			var relatedTaskRecords = new EntityRecordList();
			//Get timelog
			if (record.Properties.ContainsKey("l_scope") && record["l_scope"] != null && CommentScopeRules.IsProjectScope((string)record["l_scope"]))
			{
				isProjectComment = true;
			}

			if (isProjectComment)
			{
				//Get related tasks from related records field
				if (record.Properties.ContainsKey("l_related_records") && record["l_related_records"] != null && CommentScopeRules.HasRelatedRecords((string)record["l_related_records"]))
				{
					try
					{
						var relatedRecordGuid = JsonConvert.DeserializeObject<List<Guid>>((string)record["l_related_records"]);
						var taskEqlCommand = "SELECT *,$project_nn_task.id, $user_nn_task_watchers.id FROM task WHERE ";
						var taskFilter = RelatedTaskFilter.Build(relatedRecordGuid);
						var taskEqlParams = taskFilter.Parameters.Select(x => new EqlParameter(x.Key, x.Value)).ToList();
						taskEqlCommand += taskFilter.WhereClause;
						relatedTaskRecords = new EqlCommand(taskEqlCommand, taskEqlParams).Execute();
					}
					catch (Exception)
					{
						throw;
					}
				}
				if (!ProjectCommentRules.HasExistingRelatedTask(relatedTaskRecords.Count))
					throw new Exception(ProjectCommentRules.MissingRelatedTaskMessage);

				var taskRecord = relatedTaskRecords[0]; //Currently should be related only to 1 task in projects

				//Get Project Id
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
					taskWatchersList = ((List<EntityRecord>)taskRecord["$user_nn_task_watchers"]).Select(x=>((Guid)x["id"]).ToString()).ToList();
				}

				//Add activity log
				var feedItem = ProjectCommentRules.BuildFeedItem(taskRecord["id"].ToString(), taskRecord["key"]?.ToString(),
					taskRecord["subject"]?.ToString(), record["id"].ToString(), projectId?.ToString(), taskWatchersList);

				var body = new Web.Services.RenderService().GetSnippetFromHtml((string)record["body"]);
				new FeedItemService().Create(id: Guid.NewGuid(), createdBy: SecurityContext.CurrentUser.Id, subject: feedItem.Subject,
					body: body, relatedRecords: feedItem.RelatedRecords, scope: feedItem.Scope, type: feedItem.Type);
			}

		}

		public void PostCreateApiHookLogic(string entityName, EntityRecord record)
		{
			Guid? commentCreator = CommentScopeRules.ResolveCommentCreator(record.Properties.ContainsKey("created_by") ? record["created_by"] : null);
			if (commentCreator == null)
				return;

			if (!record.Properties.ContainsKey("l_scope") || record["l_scope"] == null || !CommentScopeRules.IsProjectScope((string)record["l_scope"]))
				return;

			if (record.Properties.ContainsKey("l_related_records") && record["l_related_records"] != null && CommentScopeRules.HasRelatedRecords((string)record["l_related_records"]))
			{
				var relatedRecordGuid = JsonConvert.DeserializeObject<List<Guid>>((string)record["l_related_records"]);
				var taskEqlCommand = "SELECT *,$user_nn_task_watchers.id from task WHERE ";
				var taskFilter = RelatedTaskFilter.Build(relatedRecordGuid);
				var taskEqlParams = taskFilter.Parameters.Select(x => new EqlParameter(x.Key, x.Value)).ToList();
				taskEqlCommand += taskFilter.WhereClause;
				var relatedTaskRecords = new EqlCommand(taskEqlCommand, taskEqlParams).Execute();
				var watchRelation = new EntityRelationManager().Read(CommentWatcherRules.WatchRelationName).Object;
				if (watchRelation == null)
					throw new Exception(CommentWatcherRules.WatchRelationNotFoundMessage);

				foreach (var task in relatedTaskRecords)
				{
					var hasWatcherList = task.Properties.ContainsKey("$user_nn_task_watchers") && task["$user_nn_task_watchers"] != null && task["$user_nn_task_watchers"] is List<EntityRecord>;
					if (hasWatcherList)
					{
						var watcherIds = ((List<EntityRecord>)task["$user_nn_task_watchers"]).Select(x=> (Guid)x["id"]);
						if (CommentWatcherRules.ShouldAddCreatorAsWatcher(commentCreator.Value, hasWatcherList, watcherIds)){
							//Create relation
							var createRelResponse = new RecordManager().CreateRelationManyToManyRecord(watchRelation.Id, commentCreator.Value, (Guid)task["id"]);
							if (!createRelResponse.Success)
								throw new Exception(createRelResponse.Message);
						}
					}
				}
			}
		}
	}
}
