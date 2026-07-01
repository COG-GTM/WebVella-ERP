using Newtonsoft.Json;
using System;
using WebVella.Erp.Api;

namespace WebVella.Erp.Plugins.HR
{
	public partial class HRPlugin : ErpPlugin
	{
		[JsonProperty(PropertyName = "name")]
		public override string Name { get; protected set; } = "hr";

		public override void Initialize(IServiceProvider serviceProvider)
		{
			using (var ctx = SecurityContext.OpenSystemScope())
			{
				ProcessPatches();
			}
		}
	}
}
