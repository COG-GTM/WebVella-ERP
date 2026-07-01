using Newtonsoft.Json;
using System;
using WebVella.Erp.Api;

namespace WebVella.Erp.Plugins.Procurement
{
	public partial class ProcurementPlugin : ErpPlugin
	{
		[JsonProperty(PropertyName = "name")]
		public override string Name { get; protected set; } = "procurement";

		/// <summary>Name of the purchase-order approval workflow (shared by the patch, service and hooks).</summary>
		public const string PoApprovalWorkflow = "po_approval";

		public override void Initialize(IServiceProvider serviceProvider)
		{
			using (var ctx = SecurityContext.OpenSystemScope())
			{
				ProcessPatches();
			}
		}
	}
}
