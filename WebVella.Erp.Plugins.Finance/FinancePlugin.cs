using Newtonsoft.Json;
using System;
using WebVella.Erp.Api;

namespace WebVella.Erp.Plugins.Finance
{
	public partial class FinancePlugin : ErpPlugin
	{
		[JsonProperty(PropertyName = "name")]
		public override string Name { get; protected set; } = "finance";

		public override void Initialize(IServiceProvider serviceProvider)
		{
			using (var ctx = SecurityContext.OpenSystemScope())
			{
				ProcessPatches();
			}
		}
	}
}
