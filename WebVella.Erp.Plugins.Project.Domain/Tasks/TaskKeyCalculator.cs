using System;

namespace WebVella.Erp.Plugins.Project.Domain.Tasks
{
	public sealed record TaskProjectInfo(Guid Id, Guid? OwnerId, bool HasAbbr, string Abbr);

	public sealed record TaskCalculationResult(string Key, string Subject, Guid ProjectId, Guid? ProjectOwnerId);

	public static class TaskKeyCalculator
	{
		/// <summary>
		/// Task key = project abbreviation + "-" + number formatted with "N0" in the current culture.
		/// </summary>
		public static string CalculateKey(string projectAbbr, decimal number)
		{
			return projectAbbr + "-" + number.ToString("N0");
		}

		/// <summary>
		/// Mirrors TaskService.SetCalculationFields: <paramref name="project"/> is the first related
		/// project (null when there is none).
		/// </summary>
		public static TaskCalculationResult Calculate(string subject, decimal number, TaskProjectInfo project)
		{
			var projectAbbr = "";
			var projectId = Guid.Empty;
			Guid? projectOwnerId = null;
			if (project != null)
			{
				if (project.HasAbbr)
					projectAbbr = project.Abbr;
				projectId = project.Id;
				projectOwnerId = project.OwnerId;
			}

			return new TaskCalculationResult(CalculateKey(projectAbbr, number), subject, projectId, projectOwnerId);
		}
	}
}
