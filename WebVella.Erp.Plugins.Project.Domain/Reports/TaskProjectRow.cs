namespace WebVella.Erp.Plugins.Project.Domain.Reports
{
	public sealed record TaskProjectRow<TTask, TProject>(TTask Task, TProject Project);
}
