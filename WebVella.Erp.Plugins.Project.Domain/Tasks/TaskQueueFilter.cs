using System;
using System.Collections.Generic;

namespace WebVella.Erp.Plugins.Project.Domain.Tasks
{
	public sealed record TaskStatusInfo(Guid Id, bool IsClosed);

	public sealed record TaskQueueParameter(string Name, object Value);

	/// <summary>
	/// Date window applied to a task date field. Bounds are evaluated against
	/// <see cref="TaskQueuePlan.CurrentDateStart"/> / <see cref="TaskQueuePlan.CurrentDateEnd"/>.
	/// </summary>
	public sealed record TaskDueWindow(
		string Field,
		DateTime? LowerBound,
		bool LowerInclusive,
		DateTime? UpperBound,
		bool UpperInclusive,
		bool IncludesNull,
		string EqlFilter)
	{
		public bool Contains(DateTime? value)
		{
			if (value == null)
				return IncludesNull;
			var v = value.Value;
			if (LowerBound != null && (LowerInclusive ? v < LowerBound.Value : v <= LowerBound.Value))
				return false;
			if (UpperBound != null && (UpperInclusive ? v > UpperBound.Value : v >= UpperBound.Value))
				return false;
			return true;
		}
	}

	public sealed class TaskQueuePlan
	{
		public TaskQueueDueType DueType { get; init; }
		public DateTime CurrentDateStart { get; init; }
		public DateTime CurrentDateEnd { get; init; }
		/// <summary>Null for <see cref="TaskQueueDueType.All"/> and unknown types.</summary>
		public TaskDueWindow DueWindow { get; init; }
		/// <summary>Where filters (due window, then project/user) in the order they must be applied.</summary>
		public IReadOnlyList<string> WhereFilters { get; init; }
		/// <summary>Parameters in the order they must be added (date params, then project/user).</summary>
		public IReadOnlyList<TaskQueueParameter> Parameters { get; init; }
		/// <summary>True for every type except <see cref="TaskQueueDueType.All"/>, including unknown types.</summary>
		public bool ExcludeClosedStatuses { get; init; }
		public bool IsKnownDueType { get; init; }
		/// <summary>Order clause to append (with leading space), null when no ordering is applied.</summary>
		public string OrderByClause { get; init; }
	}

	public static class TaskQueueFilter
	{
		public const string UnknownDueTypeMessage = "Unknown TasksDueType";
		public const string CurrentDateStartParam = "currentDateStart";
		public const string CurrentDateEndParam = "currentDateEnd";
		public const string ProjectIdParam = "projectId";
		public const string UserIdParam = "userId";

		private const string OrderByEndTimeThenPriority = " ORDER BY end_time ASC, priority DESC";
		private const string OrderByPriority = " ORDER BY priority DESC";

		public static TaskQueuePlan Build(TaskQueueDueType type, Guid? projectId, Guid? userId, DateTime now)
		{
			var currentDateStart = now.Date;
			var currentDateEnd = now.Date.AddDays(1);

			var parameters = new List<TaskQueueParameter>
			{
				new TaskQueueParameter(CurrentDateStartParam, currentDateStart),
				new TaskQueueParameter(CurrentDateEndParam, currentDateEnd),
			};
			var whereFilters = new List<string>();

			var window = GetDueWindow(type, currentDateStart, currentDateEnd);
			if (window != null)
				whereFilters.Add(window.EqlFilter);

			if (projectId != null && userId != null)
			{
				whereFilters.Add("$project_nn_task.id = @projectId AND owner_id = @userId");
				parameters.Add(new TaskQueueParameter(ProjectIdParam, projectId));
				parameters.Add(new TaskQueueParameter(UserIdParam, userId));
			}
			else if (projectId != null)
			{
				whereFilters.Add("$project_nn_task.id = @projectId");
				parameters.Add(new TaskQueueParameter(ProjectIdParam, projectId));
			}
			else if (userId != null)
			{
				whereFilters.Add("owner_id = @userId");
				parameters.Add(new TaskQueueParameter(UserIdParam, userId));
			}

			var isKnown = TryGetOrderByClause(type, out var orderBy);

			return new TaskQueuePlan
			{
				DueType = type,
				CurrentDateStart = currentDateStart,
				CurrentDateEnd = currentDateEnd,
				DueWindow = window,
				WhereFilters = whereFilters,
				Parameters = parameters,
				ExcludeClosedStatuses = type != TaskQueueDueType.All,
				IsKnownDueType = isKnown,
				OrderByClause = orderBy,
			};
		}

		public static TaskDueWindow GetDueWindow(TaskQueueDueType type, DateTime currentDateStart, DateTime currentDateEnd)
		{
			switch (type)
			{
				case TaskQueueDueType.StartTimeDue:
					return new TaskDueWindow("start_time", null, false, currentDateEnd, false, true,
						"(start_time < @currentDateEnd OR start_time = null)");
				case TaskQueueDueType.StartTimeNotDue:
					return new TaskDueWindow("start_time", currentDateEnd, false, null, false, false,
						"start_time > @currentDateEnd");
				case TaskQueueDueType.EndTimeOverdue:
					return new TaskDueWindow("end_time", null, false, currentDateStart, false, false,
						"end_time < @currentDateStart");
				case TaskQueueDueType.EndTimeDueToday:
					return new TaskDueWindow("end_time", currentDateStart, true, currentDateEnd, false, false,
						"(end_time >= @currentDateStart AND end_time < @currentDateEnd)");
				case TaskQueueDueType.EndTimeNotDue:
					return new TaskDueWindow("end_time", currentDateEnd, true, null, false, true,
						"(end_time >= @currentDateEnd OR end_time = null)");
				default:
					return null;
			}
		}

		public static bool TryGetOrderByClause(TaskQueueDueType type, out string orderByClause)
		{
			switch (type)
			{
				case TaskQueueDueType.All:
					orderByClause = null;
					return true;
				case TaskQueueDueType.EndTimeOverdue:
				case TaskQueueDueType.EndTimeNotDue:
				case TaskQueueDueType.StartTimeDue:
				case TaskQueueDueType.StartTimeNotDue:
					orderByClause = OrderByEndTimeThenPriority;
					return true;
				case TaskQueueDueType.EndTimeDueToday:
					orderByClause = OrderByPriority;
					return true;
				default:
					orderByClause = null;
					return false;
			}
		}

		/// <summary>
		/// Builds "status_id &lt;&gt; @statusN" filters for each distinct closed status, in first-seen order.
		/// </summary>
		public static IReadOnlyList<(string Filter, TaskQueueParameter Parameter)> BuildClosedStatusExclusions(IEnumerable<TaskStatusInfo> statuses)
		{
			var closedStatusHashset = new HashSet<Guid>();
			var orderedClosed = new List<Guid>();
			foreach (var status in statuses)
			{
				if (status.IsClosed && closedStatusHashset.Add(status.Id))
					orderedClosed.Add(status.Id);
			}

			var result = new List<(string, TaskQueueParameter)>();
			var index = 1;
			foreach (var id in orderedClosed)
			{
				var paramName = "status" + index;
				result.Add(($"status_id <> @{paramName}", new TaskQueueParameter(paramName, id)));
				index++;
			}
			return result;
		}
	}
}
