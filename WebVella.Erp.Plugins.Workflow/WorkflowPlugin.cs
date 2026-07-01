using Newtonsoft.Json;
using System;
using WebVella.Erp.Api;

namespace WebVella.Erp.Plugins.Workflow
{
	public partial class WorkflowPlugin : ErpPlugin
	{
		[JsonProperty(PropertyName = "name")]
		public override string Name { get; protected set; } = "workflow";

		public override void Initialize(IServiceProvider serviceProvider)
		{
			using (var ctx = SecurityContext.OpenSystemScope())
			{
				ProcessPatches();
			}
		}
	}
}
