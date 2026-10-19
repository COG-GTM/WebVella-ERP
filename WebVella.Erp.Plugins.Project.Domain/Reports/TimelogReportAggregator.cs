using System;
using System.Collections.Generic;

namespace WebVella.Erp.Plugins.Project.Domain.Reports
{
	/// <summary>
	/// Pure aggregation rules of the monthly timelog report. Record types are generic and read through
	/// accessor delegates so that callers keep their storage representation and accessors are evaluated
	/// lazily, in the same order as the original implementation.
	/// </summary>
	public static class TimelogReportAggregator
	{
		/// <summary>
		/// The task of a timelog is the first id in its related records list.
		/// </summary>
		public static Guid GetTaskId(IList<Guid> relatedRecordIds)
		{
			return relatedRecordIds[0];
		}

		public static HashSet<Guid> CollectTaskIds<TTimelog>(IEnumerable<TTimelog> timelogs, Func<TTimelog, Guid> taskIdOf)
		{
			HashSet<Guid> setOfTasksWithTimelog = new HashSet<Guid>();
			foreach (var timelog in timelogs)
			{
				Guid taskId = taskIdOf(timelog);
				if (!setOfTasksWithTimelog.Contains(taskId))
					setOfTasksWithTimelog.Add(taskId);
			}
			return setOfTasksWithTimelog;
		}

		/// <summary>
		/// Keeps tasks that have a timelog and at least one project, and emits one row per (matching) project.
		/// When <paramref name="accountId"/> is set, only projects of that account match and a project
		/// without an account throws <see cref="ProjectWithoutAccountException"/>.
		/// Intentionally preserved quirk: every row of a task carries the LAST matching project of that task
		/// (the original implementation stored the project on the shared task record and overwrote it).
		/// </summary>
		public static List<TaskProjectRow<TTask, TProject>> SplitTasksByProject<TTask, TProject>(
			IEnumerable<TTask> tasks,
			ISet<Guid> tasksWithTimelog,
			Guid? accountId,
			Func<TTask, Guid> taskIdOf,
			Func<TTask, IList<TProject>> projectsOf,
			Func<TProject, Guid?> projectAccountIdOf)
		{
			var rows = new List<TaskProjectRow<TTask, TProject>>();
			foreach (var task in tasks)
			{
				//skip task that has no timelog record
				if (!tasksWithTimelog.Contains(taskIdOf(task)))
					continue;

				IList<TProject> taskProjects = projectsOf(task);
				//skip tasks with no project
				if (taskProjects.Count == 0)
					continue;

				int matchedCount = 0;
				TProject lastMatchedProject = default;
				foreach (var project in taskProjects)
				{
					if (accountId != null)
					{
						Guid? projectAccountId = projectAccountIdOf(project);
						if (projectAccountId == null)
							throw new ProjectWithoutAccountException();

						if (projectAccountId.Value == accountId)
						{
							lastMatchedProject = project;
							matchedCount++;
						}
					}
					else
					{
						lastMatchedProject = project;
						matchedCount++;
					}
				}

				for (int i = 0; i < matchedCount; i++)
					rows.Add(new TaskProjectRow<TTask, TProject>(task, lastMatchedProject));
			}
			return rows;
		}

		/// <summary>
		/// Sums billable and non-billable minutes of all timelogs whose task id equals <paramref name="taskId"/>,
		/// iterating timelogs in order. Accessors for billable flag and minutes are only invoked for matching timelogs.
		/// </summary>
		public static MinutesTotals SumMinutes<TTimelog>(
			IEnumerable<TTimelog> timelogs,
			Guid taskId,
			Func<TTimelog, Guid> taskIdOf,
			Func<TTimelog, bool> isBillableOf,
			Func<TTimelog, decimal> minutesOf)
		{
			decimal billableMinutes = (decimal)0;
			decimal nonBillableMinutes = (decimal)0;
			foreach (var timelog in timelogs)
			{
				if (taskIdOf(timelog) != taskId)
					continue;

				if (isBillableOf(timelog))
					billableMinutes = billableMinutes + minutesOf(timelog);
				else
					nonBillableMinutes = nonBillableMinutes + minutesOf(timelog);
			}
			return new MinutesTotals(billableMinutes, nonBillableMinutes);
		}
	}
}
