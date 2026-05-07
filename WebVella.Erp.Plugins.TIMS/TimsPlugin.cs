using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using WebVella.Erp.Api;
using WebVella.Erp.Jobs;

namespace WebVella.Erp.Plugins.TIMS
{
	public partial class TimsPlugin : ErpPlugin
	{
		[JsonProperty(PropertyName = "name")]
		public override string Name { get; protected set; } = "tims";

		public override void Initialize(IServiceProvider serviceProvider)
		{
			using (var ctx = SecurityContext.OpenSystemScope())
			{
				ProcessPatches();
				SetSchedulePlans();
			}
		}

		public void SetSchedulePlans()
		{
			// Schedule plans for background jobs can be added here
			// Examples: payment processing, claim reminders, budget checks
		}
	}
}
