using System;
using System.Collections.Generic;

namespace WebVella.Erp.Plugins.Project.Domain.Tasks
{
	public sealed record TaskValidationError(string Key, string Message);

	public static class TaskValidationRules
	{
		public const string ProjectFieldKey = "$project_nn_task.id";
		public const string ProjectNotSpecifiedMessage = "Project is not specified.";
		public const string MoreThanOneProjectMessage = "More than one project is selected.";

		/// <summary>
		/// Create validation of the project selection. <paramref name="selectedProjectCount"/> is ignored
		/// when <paramref name="hasProjectField"/> is false.
		/// </summary>
		public static IReadOnlyList<TaskValidationError> ValidateProjectSelectionOnCreate(bool hasProjectField, int selectedProjectCount)
		{
			var errors = new List<TaskValidationError>();
			if (!hasProjectField)
			{
				errors.Add(new TaskValidationError(ProjectFieldKey, ProjectNotSpecifiedMessage));
			}
			else if (selectedProjectCount == 0)
			{
				errors.Add(new TaskValidationError(ProjectFieldKey, ProjectNotSpecifiedMessage));
			}
			else if (selectedProjectCount > 1)
			{
				errors.Add(new TaskValidationError(ProjectFieldKey, MoreThanOneProjectMessage));
			}
			return errors;
		}

		/// <summary>
		/// Converts a raw Guid-or-string value to a Guid. Other types (and null) yield null.
		/// Invalid strings throw the same exceptions as <c>new Guid(string)</c>.
		/// </summary>
		public static Guid? ParseGuidOrString(object value)
		{
			if (value == null)
				return null;
			Guid? result = null;
			if (value is Guid)
				result = (Guid)value;
			if (value is string)
				result = new Guid(value.ToString());
			return result;
		}

		/// <summary>The project is considered changed only when both old and new ids are known and differ.</summary>
		public static bool IsProjectChange(Guid? oldProjectId, Guid? newProjectId)
		{
			return oldProjectId != null && newProjectId != null && oldProjectId != newProjectId;
		}

		/// <summary>Whether <paramref name="userId"/> must be added to the task watchers.</summary>
		public static bool ShouldAddWatcher(Guid? userId, IEnumerable<Guid> currentWatcherIds)
		{
			if (userId == null)
				return false;
			foreach (var id in currentWatcherIds)
			{
				if (id == userId.Value)
					return false;
			}
			return true;
		}

		/// <summary>Initial watchers on create: owner, creator, project owner — distinct, in that order.</summary>
		public static IReadOnlyList<Guid> BuildInitialWatchers(Guid? ownerId, Guid? createdBy, Guid? projectOwnerId)
		{
			var watchers = new List<Guid>();
			foreach (var candidate in new[] { ownerId, createdBy, projectOwnerId })
			{
				if (candidate != null && !watchers.Contains(candidate.Value))
					watchers.Add(candidate.Value);
			}
			return watchers;
		}
	}
}
