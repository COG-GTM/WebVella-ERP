using Newtonsoft.Json;

namespace WebVella.Erp.Plugins.TIMS.Model
{
	public class PluginSettings
	{
		[JsonProperty("version")]
		public int Version { get; set; }
	}
}
