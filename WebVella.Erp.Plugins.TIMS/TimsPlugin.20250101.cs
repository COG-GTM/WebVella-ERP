using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using WebVella.Erp.Web.Models;

namespace WebVella.Erp.Plugins.TIMS
{
	public partial class TimsPlugin : ErpPlugin
	{
		private static void Patch20250101()
		{
			#region << Create TIMS Application >>
			{
				var id = new Guid("a1b2c3d4-e5f6-7890-abcd-ef1234567890");
				var name = "tims";
				var label = "TIMS";
				var description = "Travel Information Management System - Mission-based travel with PeopleSoft integration";
				var iconClass = "fa fa-plane";
				var author = "IMF";
				var color = "#003399";
				var weight = 10;
				var access = new List<Guid>();
				access.Add(new Guid("bdc56420-caf0-4030-8a0e-d264938e0cda"));

				new WebVella.Erp.Web.Services.AppService().CreateApplication(id, name, label, description, iconClass, author, color, weight, access, WebVella.Erp.Database.DbContext.Current.Transaction);
			}
			#endregion

			#region << Create Sitemap Areas >>
			// Dashboard
			{
				var id = new Guid("b1c2d3e4-f5a6-7890-bcde-f12345678901");
				var appId = new Guid("a1b2c3d4-e5f6-7890-abcd-ef1234567890");
				var name = "dashboard";
				var label = "Dashboard";
				var description = "TIMS Overview";
				var iconClass = "fas fa-tachometer-alt";
				var color = "#003399";
				var weight = 1;
				var showGroupNames = false;
				var access = new List<Guid>();
				var labelTranslations = new List<TranslationResource>();
				var descriptionTranslations = new List<TranslationResource>();

				new WebVella.Erp.Web.Services.AppService().CreateArea(id, appId, name, label, labelTranslations, description, descriptionTranslations, iconClass, color, weight, showGroupNames, access, WebVella.Erp.Database.DbContext.Current.Transaction);
			}

			// Missions
			{
				var id = new Guid("c1d2e3f4-a5b6-7890-cdef-123456789012");
				var appId = new Guid("a1b2c3d4-e5f6-7890-abcd-ef1234567890");
				var name = "missions";
				var label = "Missions";
				var description = "Mission Management";
				var iconClass = "fas fa-globe-americas";
				var color = "#003399";
				var weight = 2;
				var showGroupNames = false;
				var access = new List<Guid>();
				var labelTranslations = new List<TranslationResource>();
				var descriptionTranslations = new List<TranslationResource>();

				new WebVella.Erp.Web.Services.AppService().CreateArea(id, appId, name, label, labelTranslations, description, descriptionTranslations, iconClass, color, weight, showGroupNames, access, WebVella.Erp.Database.DbContext.Current.Transaction);
			}

			// Travel Requests
			{
				var id = new Guid("d1e2f3a4-b5c6-7890-def1-234567890123");
				var appId = new Guid("a1b2c3d4-e5f6-7890-abcd-ef1234567890");
				var name = "travel-requests";
				var label = "Travel Requests";
				var description = "Travel Authorization";
				var iconClass = "fas fa-file-alt";
				var color = "#003399";
				var weight = 3;
				var showGroupNames = false;
				var access = new List<Guid>();
				var labelTranslations = new List<TranslationResource>();
				var descriptionTranslations = new List<TranslationResource>();

				new WebVella.Erp.Web.Services.AppService().CreateArea(id, appId, name, label, labelTranslations, description, descriptionTranslations, iconClass, color, weight, showGroupNames, access, WebVella.Erp.Database.DbContext.Current.Transaction);
			}

			// Claims
			{
				var id = new Guid("e1f2a3b4-c5d6-7890-ef12-345678901234");
				var appId = new Guid("a1b2c3d4-e5f6-7890-abcd-ef1234567890");
				var name = "claims";
				var label = "Claims";
				var description = "Expense Claims Processing";
				var iconClass = "fas fa-receipt";
				var color = "#003399";
				var weight = 4;
				var showGroupNames = false;
				var access = new List<Guid>();
				var labelTranslations = new List<TranslationResource>();
				var descriptionTranslations = new List<TranslationResource>();

				new WebVella.Erp.Web.Services.AppService().CreateArea(id, appId, name, label, labelTranslations, description, descriptionTranslations, iconClass, color, weight, showGroupNames, access, WebVella.Erp.Database.DbContext.Current.Transaction);
			}

			// Payments
			{
				var id = new Guid("f1a2b3c4-d5e6-7890-f123-456789012345");
				var appId = new Guid("a1b2c3d4-e5f6-7890-abcd-ef1234567890");
				var name = "payments";
				var label = "Payments";
				var description = "Payment Processing";
				var iconClass = "fas fa-university";
				var color = "#003399";
				var weight = 5;
				var showGroupNames = false;
				var access = new List<Guid>();
				var labelTranslations = new List<TranslationResource>();
				var descriptionTranslations = new List<TranslationResource>();

				new WebVella.Erp.Web.Services.AppService().CreateArea(id, appId, name, label, labelTranslations, description, descriptionTranslations, iconClass, color, weight, showGroupNames, access, WebVella.Erp.Database.DbContext.Current.Transaction);
			}
			#endregion

			#region << Create Sitemap Nodes >>
			// Dashboard Overview
			{
				var id = new Guid("e2f3a4b5-c6d7-8901-2345-678901234567");
				var areaId = new Guid("b1c2d3e4-f5a6-7890-bcde-f12345678901");
				var name = "overview";
				var label = "Overview";
				var url = "/tims/dashboard/overview/a";
				var iconClass = "fas fa-tachometer-alt";
				var weight = 1;
				var type = ((int)2); // ApplicationPage
				var access = new List<Guid>();
				var labelTranslations = new List<TranslationResource>();

				new WebVella.Erp.Web.Services.AppService().CreateAreaNode(id, areaId, name, label, labelTranslations, iconClass, url, type, null, weight, access, new List<Guid>(), new List<Guid>(), new List<Guid>(), new List<Guid>(), WebVella.Erp.Database.DbContext.Current.Transaction);
			}
			#endregion
		}
	}
}
