namespace WebVella.Erp.Plugins.Project.Domain.Tasks
{
	/// <summary>
	/// Mirrors WebVella.Erp.Plugins.Project.Model.TasksDueType (same underlying values).
	/// </summary>
	public enum TaskQueueDueType
	{
		All = 0,
		EndTimeOverdue = 1,
		EndTimeDueToday = 2,
		EndTimeNotDue = 3,
		StartTimeDue = 4,
		StartTimeNotDue = 5,
	}
}
