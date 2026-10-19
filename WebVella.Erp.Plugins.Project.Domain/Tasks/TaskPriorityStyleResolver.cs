using System.Collections.Generic;
using System.Linq;

namespace WebVella.Erp.Plugins.Project.Domain.Tasks
{
	public sealed record TaskPriorityOption(string Value, string IconClass, string Color);

	public sealed record TaskPriorityStyle(string IconClass, string Color);

	public static class TaskPriorityStyleResolver
	{
		public const string DefaultIconClass = "";
		public const string DefaultColor = "#fff";

		public static TaskPriorityStyle Resolve(string priorityValue, IEnumerable<TaskPriorityOption> priorityOptions)
		{
			var match = priorityOptions.FirstOrDefault(x => x.Value == priorityValue);
			if (match == null)
				return new TaskPriorityStyle(DefaultIconClass, DefaultColor);

			return new TaskPriorityStyle(match.IconClass, match.Color);
		}
	}
}
