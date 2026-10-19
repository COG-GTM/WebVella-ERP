using System;

namespace WebVella.Erp.Plugins.Project.Domain.Reports
{
	public sealed class ProjectWithoutAccountException : Exception
	{
		public const string DefaultMessage = "There is a project without an account";

		public ProjectWithoutAccountException() : base(DefaultMessage) { }
	}
}
