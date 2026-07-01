using Newtonsoft.Json;
using System;
using WebVella.Erp.Api;
using WebVella.Erp.Database;
using WebVella.Erp.Exceptions;

namespace WebVella.Erp.Plugins.Inventory
{
	public partial class InventoryPlugin : ErpPlugin
	{
		// INIT_VERSION must be strictly LESS than the first patch date, otherwise
		// (currentPluginSettings.Version < patchVersion) is false and the patch
		// silently never runs.
		private const int WEBVELLA_INVENTORY_INIT_VERSION = 20250100;

		public void ProcessPatches()
		{
			using (SecurityContext.OpenSystemScope())
			{
				var entMan = new EntityManager();
				var relMan = new EntityRelationManager();
				var recMan = new RecordManager();

				using (var connection = DbContext.Current.CreateConnection())
				{
					try
					{
						connection.BeginTransaction();

						var currentPluginSettings = new PluginSettings() { Version = WEBVELLA_INVENTORY_INIT_VERSION };
						string jsonData = GetPluginData();
						if (!string.IsNullOrWhiteSpace(jsonData))
							currentPluginSettings = JsonConvert.DeserializeObject<PluginSettings>(jsonData);

						//Patch 20250101 - initial Inventory metadata
						{
							var patchVersion = 20250101;
							if (currentPluginSettings.Version < patchVersion)
							{
								currentPluginSettings.Version = patchVersion;
								Patch20250101();
							}
						}

						SavePluginData(JsonConvert.SerializeObject(currentPluginSettings));
						connection.CommitTransaction();
					}
					catch (ValidationException ex)
					{
						connection.RollbackTransaction();
						throw ex;
					}
					catch (Exception)
					{
						connection.RollbackTransaction();
						throw;
					}
				}
			}
		}
	}
}
