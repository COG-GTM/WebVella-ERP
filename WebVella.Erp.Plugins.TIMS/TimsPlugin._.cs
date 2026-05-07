using Newtonsoft.Json;
using System;

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

			//Patch 20250102 - Add missing required fields that were silently dropped in 20250101
			{
				var patchVersion = 20250102;
				if (currentPluginSettings.Version < patchVersion)
				{
					currentPluginSettings.Version = patchVersion;
					Patch20250102();
				}
			}

			SavePluginData(JsonConvert.SerializeObject(currentPluginSettings));
		}
	}
}
