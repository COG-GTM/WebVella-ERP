using Newtonsoft.Json;
using System;
using WebVella.Erp.Plugins.TIMS.Model;

namespace WebVella.Erp.Plugins.TIMS
{
	public partial class TimsPlugin : ErpPlugin
	{
		private const int TIMS_INIT_VERSION = 20250101;

		public void ProcessPatches()
		{
			var currentPluginSettings = new PluginSettings() { Version = TIMS_INIT_VERSION };
			string jsonData = GetPluginData();
			if (!string.IsNullOrWhiteSpace(jsonData))
				currentPluginSettings = JsonConvert.DeserializeObject<PluginSettings>(jsonData);

			//Patch 20250101 - Initial TIMS Setup
			{
				var patchVersion = 20250101;
				if (currentPluginSettings.Version < patchVersion)
				{
					currentPluginSettings.Version = patchVersion;
					Patch20250101();
				}
			}

			SavePluginData(JsonConvert.SerializeObject(currentPluginSettings));
		}
	}
}
