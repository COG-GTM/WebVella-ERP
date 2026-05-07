using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using WebVella.Erp.Web.Models;
using WebVella.Erp.Api;
using WebVella.Erp.Api.Models;
using WebVella.Erp.Api.Models.AutoMapper;

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

			#region << Create TIMS Entities >>
			CreateTimsMissionEntity();
			CreateTimsTravelRequestEntity();
			CreateTimsClaimEntity();
			CreateTimsBudgetEntity();
			CreateTimsPaymentEntity();
			CreateTimsBankAccountEntity();
			CreateTimsApprovalEntity();
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

			// Budgets
			{
				var id = new Guid("01234567-89ab-cdef-0123-456789abcdef");
				var appId = new Guid("a1b2c3d4-e5f6-7890-abcd-ef1234567890");
				var name = "budgets";
				var label = "Budgets";
				var description = "Budget Management";
				var iconClass = "fas fa-chart-pie";
				var color = "#003399";
				var weight = 6;
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

			// Missions List
			{
				var id = new Guid("f2a3b4c5-d6e7-8901-3456-789012345678");
				var areaId = new Guid("c1d2e3f4-a5b6-7890-cdef-123456789012");
				var name = "missions-list";
				var label = "All Missions";
				var url = "/tims/missions/list/r";
				var iconClass = "fas fa-list";
				var weight = 1;
				var type = ((int)1); // EntityList
				var access = new List<Guid>();
				var labelTranslations = new List<TranslationResource>();
				var entityIds = new List<Guid>();
				entityIds.Add(new Guid("10000000-0000-0000-0000-000000000001"));

				new WebVella.Erp.Web.Services.AppService().CreateAreaNode(id, areaId, name, label, labelTranslations, iconClass, url, type, null, weight, access, entityIds, new List<Guid>(), new List<Guid>(), new List<Guid>(), WebVella.Erp.Database.DbContext.Current.Transaction);
			}

			// Missions Create
			{
				var id = new Guid("a3b4c5d6-e7f8-9012-4567-890123456789");
				var areaId = new Guid("c1d2e3f4-a5b6-7890-cdef-123456789012");
				var name = "missions-create";
				var label = "New Mission";
				var url = "/tims/missions/create/r";
				var iconClass = "fas fa-plus";
				var weight = 2;
				var type = ((int)0); // EntityRecord
				var access = new List<Guid>();
				var labelTranslations = new List<TranslationResource>();
				var entityIds = new List<Guid>();
				entityIds.Add(new Guid("10000000-0000-0000-0000-000000000001"));

				new WebVella.Erp.Web.Services.AppService().CreateAreaNode(id, areaId, name, label, labelTranslations, iconClass, url, type, null, weight, access, entityIds, new List<Guid>(), new List<Guid>(), new List<Guid>(), WebVella.Erp.Database.DbContext.Current.Transaction);
			}

			// Travel Requests List
			{
				var id = new Guid("b4c5d6e7-f8a9-0123-5678-901234567890");
				var areaId = new Guid("d1e2f3a4-b5c6-7890-def1-234567890123");
				var name = "travel-requests-list";
				var label = "All Travel Requests";
				var url = "/tims/travel-requests/list/r";
				var iconClass = "fas fa-list";
				var weight = 1;
				var type = ((int)1); // EntityList
				var access = new List<Guid>();
				var labelTranslations = new List<TranslationResource>();
				var entityIds = new List<Guid>();
				entityIds.Add(new Guid("10000000-0000-0000-0000-000000000002"));

				new WebVella.Erp.Web.Services.AppService().CreateAreaNode(id, areaId, name, label, labelTranslations, iconClass, url, type, null, weight, access, entityIds, new List<Guid>(), new List<Guid>(), new List<Guid>(), WebVella.Erp.Database.DbContext.Current.Transaction);
			}

			// Travel Requests Create
			{
				var id = new Guid("c5d6e7f8-a9b0-1234-6789-012345678901");
				var areaId = new Guid("d1e2f3a4-b5c6-7890-def1-234567890123");
				var name = "travel-requests-create";
				var label = "New Travel Request";
				var url = "/tims/travel-requests/create/r";
				var iconClass = "fas fa-plus";
				var weight = 2;
				var type = ((int)0); // EntityRecord
				var access = new List<Guid>();
				var labelTranslations = new List<TranslationResource>();
				var entityIds = new List<Guid>();
				entityIds.Add(new Guid("10000000-0000-0000-0000-000000000002"));

				new WebVella.Erp.Web.Services.AppService().CreateAreaNode(id, areaId, name, label, labelTranslations, iconClass, url, type, null, weight, access, entityIds, new List<Guid>(), new List<Guid>(), new List<Guid>(), WebVella.Erp.Database.DbContext.Current.Transaction);
			}

			// Claims List
			{
				var id = new Guid("d6e7f8a9-b0c1-2345-7890-123456789012");
				var areaId = new Guid("e1f2a3b4-c5d6-7890-ef12-345678901234");
				var name = "claims-list";
				var label = "All Claims";
				var url = "/tims/claims/list/r";
				var iconClass = "fas fa-list";
				var weight = 1;
				var type = ((int)1); // EntityList
				var access = new List<Guid>();
				var labelTranslations = new List<TranslationResource>();
				var entityIds = new List<Guid>();
				entityIds.Add(new Guid("10000000-0000-0000-0000-000000000003"));

				new WebVella.Erp.Web.Services.AppService().CreateAreaNode(id, areaId, name, label, labelTranslations, iconClass, url, type, null, weight, access, entityIds, new List<Guid>(), new List<Guid>(), new List<Guid>(), WebVella.Erp.Database.DbContext.Current.Transaction);
			}

			// Claims Create
			{
				var id = new Guid("e7f8a9b0-c1d2-3456-8901-234567890123");
				var areaId = new Guid("e1f2a3b4-c5d6-7890-ef12-345678901234");
				var name = "claims-create";
				var label = "New Claim";
				var url = "/tims/claims/create/r";
				var iconClass = "fas fa-plus";
				var weight = 2;
				var type = ((int)0); // EntityRecord
				var access = new List<Guid>();
				var labelTranslations = new List<TranslationResource>();
				var entityIds = new List<Guid>();
				entityIds.Add(new Guid("10000000-0000-0000-0000-000000000003"));

				new WebVella.Erp.Web.Services.AppService().CreateAreaNode(id, areaId, name, label, labelTranslations, iconClass, url, type, null, weight, access, entityIds, new List<Guid>(), new List<Guid>(), new List<Guid>(), WebVella.Erp.Database.DbContext.Current.Transaction);
			}

			// Payments List
			{
				var id = new Guid("f8a9b0c1-d2e3-4567-9012-345678901234");
				var areaId = new Guid("f1a2b3c4-d5e6-7890-f123-456789012345");
				var name = "payments-list";
				var label = "All Payments";
				var url = "/tims/payments/list/r";
				var iconClass = "fas fa-list";
				var weight = 1;
				var type = ((int)1); // EntityList
				var access = new List<Guid>();
				var labelTranslations = new List<TranslationResource>();
				var entityIds = new List<Guid>();
				entityIds.Add(new Guid("10000000-0000-0000-0000-000000000005"));

				new WebVella.Erp.Web.Services.AppService().CreateAreaNode(id, areaId, name, label, labelTranslations, iconClass, url, type, null, weight, access, entityIds, new List<Guid>(), new List<Guid>(), new List<Guid>(), WebVella.Erp.Database.DbContext.Current.Transaction);
			}

			// Payments Create
			{
				var id = new Guid("a9b0c1d2-e3f4-5678-0123-456789012345");
				var areaId = new Guid("f1a2b3c4-d5e6-7890-f123-456789012345");
				var name = "payments-create";
				var label = "New Payment";
				var url = "/tims/payments/create/r";
				var iconClass = "fas fa-plus";
				var weight = 2;
				var type = ((int)0); // EntityRecord
				var access = new List<Guid>();
				var labelTranslations = new List<TranslationResource>();
				var entityIds = new List<Guid>();
				entityIds.Add(new Guid("10000000-0000-0000-0000-000000000005"));

				new WebVella.Erp.Web.Services.AppService().CreateAreaNode(id, areaId, name, label, labelTranslations, iconClass, url, type, null, weight, access, entityIds, new List<Guid>(), new List<Guid>(), new List<Guid>(), WebVella.Erp.Database.DbContext.Current.Transaction);
			}

			// Budgets List
			{
				var id = new Guid("b0c1d2e3-f4a5-6789-1234-567890123456");
				var areaId = new Guid("01234567-89ab-cdef-0123-456789abcdef");
				var name = "budgets-list";
				var label = "All Budgets";
				var url = "/tims/budgets/list/r";
				var iconClass = "fas fa-list";
				var weight = 1;
				var type = ((int)1); // EntityList
				var access = new List<Guid>();
				var labelTranslations = new List<TranslationResource>();
				var entityIds = new List<Guid>();
				entityIds.Add(new Guid("10000000-0000-0000-0000-000000000004"));

				new WebVella.Erp.Web.Services.AppService().CreateAreaNode(id, areaId, name, label, labelTranslations, iconClass, url, type, null, weight, access, entityIds, new List<Guid>(), new List<Guid>(), new List<Guid>(), WebVella.Erp.Database.DbContext.Current.Transaction);
			}

			// Budgets Create
			{
				var id = new Guid("d1e2f3a4-b5c6-7890-3456-789012345678");
				var areaId = new Guid("01234567-89ab-cdef-0123-456789abcdef");
				var name = "budgets-create";
				var label = "New Budget";
				var url = "/tims/budgets/create/r";
				var iconClass = "fas fa-plus";
				var weight = 2;
				var type = ((int)0); // EntityRecord
				var access = new List<Guid>();
				var labelTranslations = new List<TranslationResource>();
				var entityIds = new List<Guid>();
				entityIds.Add(new Guid("10000000-0000-0000-0000-000000000004"));

				new WebVella.Erp.Web.Services.AppService().CreateAreaNode(id, areaId, name, label, labelTranslations, iconClass, url, type, null, weight, access, entityIds, new List<Guid>(), new List<Guid>(), new List<Guid>(), WebVella.Erp.Database.DbContext.Current.Transaction);
			}

			// Bank Accounts List
			{
				var id = new Guid("c1d2e3f4-a5b6-7890-2345-678901234567");
				var areaId = new Guid("01234567-89ab-cdef-0123-456789abcdef");
				var name = "bank-accounts-list";
				var label = "Bank Accounts";
				var url = "/tims/bank-accounts/list/r";
				var iconClass = "fas fa-list";
				var weight = 3;
				var type = ((int)1); // EntityList
				var access = new List<Guid>();
				var labelTranslations = new List<TranslationResource>();
				var entityIds = new List<Guid>();
				entityIds.Add(new Guid("10000000-0000-0000-0000-000000000006"));

				new WebVella.Erp.Web.Services.AppService().CreateAreaNode(id, areaId, name, label, labelTranslations, iconClass, url, type, null, weight, access, entityIds, new List<Guid>(), new List<Guid>(), new List<Guid>(), WebVella.Erp.Database.DbContext.Current.Transaction);
			}

			// Bank Accounts Create
			{
				var id = new Guid("e2f3a4b5-c6d7-8901-4567-890123456789");
				var areaId = new Guid("01234567-89ab-cdef-0123-456789abcdef");
				var name = "bank-accounts-create";
				var label = "New Bank Account";
				var url = "/tims/bank-accounts/create/r";
				var iconClass = "fas fa-plus";
				var weight = 4;
				var type = ((int)0); // EntityRecord
				var access = new List<Guid>();
				var labelTranslations = new List<TranslationResource>();
				var entityIds = new List<Guid>();
				entityIds.Add(new Guid("10000000-0000-0000-0000-000000000006"));

				new WebVella.Erp.Web.Services.AppService().CreateAreaNode(id, areaId, name, label, labelTranslations, iconClass, url, type, null, weight, access, entityIds, new List<Guid>(), new List<Guid>(), new List<Guid>(), WebVella.Erp.Database.DbContext.Current.Transaction);
			}
			#endregion
		}

		#region << Entity Creation Methods >>

		private static void CreateTimsMissionEntity()
		{
			var entityId = new Guid("10000000-0000-0000-0000-000000000001");
			var entityName = "tims_mission";
			var entityLabel = "Mission";
			var entityLabelPlural = "Missions";

			var entMan = new EntityManager();
			var response = entMan.CreateEntity(entityId, entityName, entityLabel, entityLabelPlural, createOnlyIdField: false);

			if (response.Success)
			{
				var entity = response.Object;
				var fields = new List<InputField>();

				// Mission Code
				var missionCodeField = new InputTextField
				{
					Id = Guid.NewGuid(),
					Name = "mission_code",
					Label = "Mission Code",
					Required = true,
					Unique = true,
					MaxLength = 50
				};
				fields.Add(missionCodeField);

				// Mission Type
				var missionTypeField = new InputSelectField
				{
					Id = Guid.NewGuid(),
					Name = "mission_type",
					Label = "Mission Type",
					Required = true,
					Options = new List<SelectOption>
					{
						new SelectOption("official", "Official"),
						new SelectOption("training", "Training"),
						new SelectOption("conference", "Conference"),
						new SelectOption("consultation", "Consultation")
					}
				};
				fields.Add(missionTypeField);

				// Title
				var titleField = new InputTextField
				{
					Id = Guid.NewGuid(),
					Name = "title",
					Label = "Title",
					Required = true,
					MaxLength = 255
				};
				fields.Add(titleField);

				// Description
				var descriptionField = new InputMultiLineTextField
				{
					Id = Guid.NewGuid(),
					Name = "description",
					Label = "Description"
				};
				fields.Add(descriptionField);

				// Start Date
				var startDateField = new InputDateField
				{
					Id = Guid.NewGuid(),
					Name = "start_date",
					Label = "Start Date",
					Required = true
				};
				fields.Add(startDateField);

				// End Date
				var endDateField = new InputDateField
				{
					Id = Guid.NewGuid(),
					Name = "end_date",
					Label = "End Date",
					Required = true
				};
				fields.Add(endDateField);

				// Budget Amount
				var budgetAmountField = new InputCurrencyField
				{
					Id = Guid.NewGuid(),
					Name = "budget_amount",
					Label = "Budget Amount",
					Required = true,
					Currency = new CurrencyType { Code = "USD" }
				};
				fields.Add(budgetAmountField);

				// Status
				var statusField = new InputSelectField
				{
					Id = Guid.NewGuid(),
					Name = "status",
					Label = "Status",
					Required = true,
					DefaultValue = "planned",
					Options = new List<SelectOption>
					{
						new SelectOption("planned", "Planned"),
						new SelectOption("approved", "Approved"),
						new SelectOption("active", "Active"),
						new SelectOption("completed", "Completed"),
						new SelectOption("cancelled", "Cancelled")
					}
				};
				fields.Add(statusField);

				// PeopleSoft Project ID
				var psProjectIdField = new InputTextField
				{
					Id = Guid.NewGuid(),
					Name = "peoplesoft_project_id",
					Label = "PeopleSoft Project ID",
					MaxLength = 50
				};
				fields.Add(psProjectIdField);

				// Created By
				var createdByField = new InputGuidField
				{
					Id = Guid.NewGuid(),
					Name = "created_by",
					Label = "Created By"
				};
				fields.Add(createdByField);

				// Created Date
				var createdDateField = new InputDateTimeField
				{
					Id = Guid.NewGuid(),
					Name = "created_date",
					Label = "Created Date"
				};
				fields.Add(createdDateField);

				foreach (var field in fields)
				{
					var fieldResponse = entMan.CreateField(entityId, field);
				}
			}
		}

		private static void CreateTimsTravelRequestEntity()
		{
			var entityId = new Guid("10000000-0000-0000-0000-000000000002");
			var entityName = "tims_travel_request";
			var entityLabel = "Travel Request";
			var entityLabelPlural = "Travel Requests";

			var entMan = new EntityManager();
			var response = entMan.CreateEntity(entityId, entityName, entityLabel, entityLabelPlural, createOnlyIdField: false);

			if (response.Success)
			{
				var entity = response.Object;
				var fields = new List<InputField>();

				// Request Number
				var requestNumberField = new InputTextField
				{
					Id = Guid.NewGuid(),
					Name = "request_number",
					Label = "Request Number",
					Required = true,
					Unique = true,
					MaxLength = 50
				};
				fields.Add(requestNumberField);

				// Mission ID
				var missionIdField = new InputGuidField
				{
					Id = Guid.NewGuid(),
					Name = "mission_id",
					Label = "Mission",
					Required = true
				};
				fields.Add(missionIdField);

				// Traveler Name
				var travelerNameField = new InputTextField
				{
					Id = Guid.NewGuid(),
					Name = "traveler_name",
					Label = "Traveler Name",
					Required = true,
					MaxLength = 255
				};
				fields.Add(travelerNameField);

				// Destination
				var destinationField = new InputTextField
				{
					Id = Guid.NewGuid(),
					Name = "destination",
					Label = "Destination",
					Required = true,
					MaxLength = 255
				};
				fields.Add(destinationField);

				// Purpose
				var purposeField = new InputTextField
				{
					Id = Guid.NewGuid(),
					Name = "purpose",
					Label = "Purpose",
					Required = true,
					MaxLength = 500
				};
				fields.Add(purposeField);

				// Estimated Cost
				var estimatedCostField = new InputCurrencyField
				{
					Id = Guid.NewGuid(),
					Name = "estimated_cost",
					Label = "Estimated Cost",
					Required = true,
					Currency = new CurrencyType { Code = "USD" }
				};
				fields.Add(estimatedCostField);

				// Status
				var statusField = new InputSelectField
				{
					Id = Guid.NewGuid(),
					Name = "status",
					Label = "Status",
					Required = true,
					DefaultValue = "draft",
					Options = new List<SelectOption>
					{
						new SelectOption("draft", "Draft"),
						new SelectOption("submitted", "Submitted"),
						new SelectOption("manager_approved", "Manager Approved"),
						new SelectOption("finance_approved", "Finance Approved"),
						new SelectOption("director_approved", "Director Approved"),
						new SelectOption("rejected", "Rejected")
					}
				};
				fields.Add(statusField);

				// PeopleSoft Request ID
				var psRequestIdField = new InputTextField
				{
					Id = Guid.NewGuid(),
					Name = "peoplesoft_request_id",
					Label = "PeopleSoft Request ID",
					MaxLength = 50
				};
				fields.Add(psRequestIdField);

				// Created By
				var createdByField = new InputGuidField
				{
					Id = Guid.NewGuid(),
					Name = "created_by",
					Label = "Created By"
				};
				fields.Add(createdByField);

				// Created Date
				var createdDateField = new InputDateTimeField
				{
					Id = Guid.NewGuid(),
					Name = "created_date",
					Label = "Created Date"
				};
				fields.Add(createdDateField);

				foreach (var field in fields)
				{
					var fieldResponse = entMan.CreateField(entityId, field);
				}
			}
		}

		private static void CreateTimsClaimEntity()
		{
			var entityId = new Guid("10000000-0000-0000-0000-000000000003");
			var entityName = "tims_claim";
			var entityLabel = "Claim";
			var entityLabelPlural = "Claims";

			var entMan = new EntityManager();
			var response = entMan.CreateEntity(entityId, entityName, entityLabel, entityLabelPlural, createOnlyIdField: false);

			if (response.Success)
			{
				var entity = response.Object;
				var fields = new List<InputField>();

				// Claim Number
				var claimNumberField = new InputTextField
				{
					Id = Guid.NewGuid(),
					Name = "claim_number",
					Label = "Claim Number",
					Required = true,
					Unique = true,
					MaxLength = 50
				};
				fields.Add(claimNumberField);

				// Travel Request ID
				var travelRequestIdField = new InputGuidField
				{
					Id = Guid.NewGuid(),
					Name = "travel_request_id",
					Label = "Travel Request",
					Required = true
				};
				fields.Add(travelRequestIdField);

				// Claim Amount
				var claimAmountField = new InputCurrencyField
				{
					Id = Guid.NewGuid(),
					Name = "claim_amount",
					Label = "Claim Amount",
					Required = true,
					Currency = new CurrencyType { Code = "USD" }
				};
				fields.Add(claimAmountField);

				// Budget Amount
				var budgetAmountField = new InputCurrencyField
				{
					Id = Guid.NewGuid(),
					Name = "budget_amount",
					Label = "Budget Amount",
					Required = true,
					Currency = new CurrencyType { Code = "USD" }
				};
				fields.Add(budgetAmountField);

				// Invoice Amount
				var invoiceAmountField = new InputCurrencyField
				{
					Id = Guid.NewGuid(),
					Name = "invoice_amount",
					Label = "Invoice Amount",
					Currency = new CurrencyType { Code = "USD" }
				};
				fields.Add(invoiceAmountField);

				// Three-Way Match Status
				var matchStatusField = new InputSelectField
				{
					Id = Guid.NewGuid(),
					Name = "match_status",
					Label = "Three-Way Match Status",
					Options = new List<SelectOption>
					{
						new SelectOption("pending", "Pending"),
						new SelectOption("matched", "Matched"),
						new SelectOption("variance", "Variance"),
						new SelectOption("failed", "Failed")
					}
				};
				fields.Add(matchStatusField);

				// Status
				var statusField = new InputSelectField
				{
					Id = Guid.NewGuid(),
					Name = "status",
					Label = "Status",
					Required = true,
					DefaultValue = "draft",
					Options = new List<SelectOption>
					{
						new SelectOption("draft", "Draft"),
						new SelectOption("submitted", "Submitted"),
						new SelectOption("under_review", "Under Review"),
						new SelectOption("approved", "Approved"),
						new SelectOption("rejected", "Rejected"),
						new SelectOption("paid", "Paid")
					}
				};
				fields.Add(statusField);

				// Currency
				var currencyField = new InputSelectField
				{
					Id = Guid.NewGuid(),
					Name = "currency",
					Label = "Currency",
					Required = true,
					DefaultValue = "USD",
					Options = new List<SelectOption>
					{
						new SelectOption("USD", "USD"),
						new SelectOption("EUR", "EUR"),
						new SelectOption("GBP", "GBP"),
						new SelectOption("JPY", "JPY")
					}
				};
				fields.Add(currencyField);

				// PeopleSoft Voucher ID
				var psVoucherIdField = new InputTextField
				{
					Id = Guid.NewGuid(),
					Name = "peoplesoft_voucher_id",
					Label = "PeopleSoft Voucher ID",
					MaxLength = 50
				};
				fields.Add(psVoucherIdField);

				// Created By
				var createdByField = new InputGuidField
				{
					Id = Guid.NewGuid(),
					Name = "created_by",
					Label = "Created By"
				};
				fields.Add(createdByField);

				// Created Date
				var createdDateField = new InputDateTimeField
				{
					Id = Guid.NewGuid(),
					Name = "created_date",
					Label = "Created Date"
				};
				fields.Add(createdDateField);

				foreach (var field in fields)
				{
					var fieldResponse = entMan.CreateField(entityId, field);
				}
			}
		}

		private static void CreateTimsBudgetEntity()
		{
			var entityId = new Guid("10000000-0000-0000-0000-000000000004");
			var entityName = "tims_budget";
			var entityLabel = "Budget";
			var entityLabelPlural = "Budgets";

			var entMan = new EntityManager();
			var response = entMan.CreateEntity(entityId, entityName, entityLabel, entityLabelPlural, createOnlyIdField: false);

			if (response.Success)
			{
				var entity = response.Object;
				var fields = new List<InputField>();

				// Budget Code
				var budgetCodeField = new InputTextField
				{
					Id = Guid.NewGuid(),
					Name = "budget_code",
					Label = "Budget Code",
					Required = true,
					Unique = true,
					MaxLength = 50
				};
				fields.Add(budgetCodeField);

				// Department
				var departmentField = new InputTextField
				{
					Id = Guid.NewGuid(),
					Name = "department",
					Label = "Department",
					Required = true,
					MaxLength = 255
				};
				fields.Add(departmentField);

				// Fiscal Year
				var fiscalYearField = new InputNumberField
				{
					Id = Guid.NewGuid(),
					Name = "fiscal_year",
					Label = "Fiscal Year",
					Required = true
				};
				fields.Add(fiscalYearField);

				// Allocated Amount
				var allocatedAmountField = new InputCurrencyField
				{
					Id = Guid.NewGuid(),
					Name = "allocated_amount",
					Label = "Allocated Amount",
					Required = true,
					Currency = new CurrencyType { Code = "USD" }
				};
				fields.Add(allocatedAmountField);

				// Committed Amount
				var committedAmountField = new InputCurrencyField
				{
					Id = Guid.NewGuid(),
					Name = "committed_amount",
					Label = "Committed Amount",
					Currency = new CurrencyType { Code = "USD" }
				};
				fields.Add(committedAmountField);

				// Spent Amount
				var spentAmountField = new InputCurrencyField
				{
					Id = Guid.NewGuid(),
					Name = "spent_amount",
					Label = "Spent Amount",
					Currency = new CurrencyType { Code = "USD" }
				};
				fields.Add(spentAmountField);

				// Status
				var statusField = new InputSelectField
				{
					Id = Guid.NewGuid(),
					Name = "status",
					Label = "Status",
					Required = true,
					DefaultValue = "active",
					Options = new List<SelectOption>
					{
						new SelectOption("active", "Active"),
						new SelectOption("closed", "Closed"),
						new SelectOption("pending", "Pending")
					}
				};
				fields.Add(statusField);

				// PeopleSoft Budget ID
				var psBudgetIdField = new InputTextField
				{
					Id = Guid.NewGuid(),
					Name = "peoplesoft_budget_id",
					Label = "PeopleSoft Budget ID",
					MaxLength = 50
				};
				fields.Add(psBudgetIdField);

				foreach (var field in fields)
				{
					var fieldResponse = entMan.CreateField(entityId, field);
				}
			}
		}

		private static void CreateTimsPaymentEntity()
		{
			var entityId = new Guid("10000000-0000-0000-0000-000000000005");
			var entityName = "tims_payment";
			var entityLabel = "Payment";
			var entityLabelPlural = "Payments";

			var entMan = new EntityManager();
			var response = entMan.CreateEntity(entityId, entityName, entityLabel, entityLabelPlural, createOnlyIdField: false);

			if (response.Success)
			{
				var entity = response.Object;
				var fields = new List<InputField>();

				// Payment Number
				var paymentNumberField = new InputTextField
				{
					Id = Guid.NewGuid(),
					Name = "payment_number",
					Label = "Payment Number",
					Required = true,
					Unique = true,
					MaxLength = 50
				};
				fields.Add(paymentNumberField);

				// Claim ID
				var claimIdField = new InputGuidField
				{
					Id = Guid.NewGuid(),
					Name = "claim_id",
					Label = "Claim",
					Required = true
				};
				fields.Add(claimIdField);

				// Payment Amount
				var paymentAmountField = new InputCurrencyField
				{
					Id = Guid.NewGuid(),
					Name = "payment_amount",
					Label = "Payment Amount",
					Required = true,
					Currency = new CurrencyType { Code = "USD" }
				};
				fields.Add(paymentAmountField);

				// Payment Method
				var paymentMethodField = new InputSelectField
				{
					Id = Guid.NewGuid(),
					Name = "payment_method",
					Label = "Payment Method",
					Required = true,
					Options = new List<SelectOption>
					{
						new SelectOption("etransfer", "E-Transfer"),
						new SelectOption("wire", "Wire Transfer"),
						new SelectOption("check", "Check"),
						new SelectOption("corporate_card", "Corporate Card")
					}
				};
				fields.Add(paymentMethodField);

				// Bank Account ID
				var bankAccountIdField = new InputGuidField
				{
					Id = Guid.NewGuid(),
					Name = "bank_account_id",
					Label = "Bank Account"
				};
				fields.Add(bankAccountIdField);

				// Payment Date
				var paymentDateField = new InputDateField
				{
					Id = Guid.NewGuid(),
					Name = "payment_date",
					Label = "Payment Date"
				};
				fields.Add(paymentDateField);

				// Status
				var statusField = new InputSelectField
				{
					Id = Guid.NewGuid(),
					Name = "status",
					Label = "Status",
					Required = true,
					DefaultValue = "pending",
					Options = new List<SelectOption>
					{
						new SelectOption("pending", "Pending"),
						new SelectOption("processing", "Processing"),
						new SelectOption("completed", "Completed"),
						new SelectOption("failed", "Failed"),
						new SelectOption("cancelled", "Cancelled")
					}
				};
				fields.Add(statusField);

				// PeopleSoft Payment ID
				var psPaymentIdField = new InputTextField
				{
					Id = Guid.NewGuid(),
					Name = "peoplesoft_payment_id",
					Label = "PeopleSoft Payment ID",
					MaxLength = 50
				};
				fields.Add(psPaymentIdField);

				foreach (var field in fields)
				{
					var fieldResponse = entMan.CreateField(entityId, field);
				}
			}
		}

		private static void CreateTimsBankAccountEntity()
		{
			var entityId = new Guid("10000000-0000-0000-0000-000000000006");
			var entityName = "tims_bank_account";
			var entityLabel = "Bank Account";
			var entityLabelPlural = "Bank Accounts";

			var entMan = new EntityManager();
			var response = entMan.CreateEntity(entityId, entityName, entityLabel, entityLabelPlural, createOnlyIdField: false);

			if (response.Success)
			{
				var entity = response.Object;
				var fields = new List<InputField>();

				// Account Name
				var accountNameField = new InputTextField
				{
					Id = Guid.NewGuid(),
					Name = "account_name",
					Label = "Account Name",
					Required = true,
					MaxLength = 255
				};
				fields.Add(accountNameField);

				// Bank Name
				var bankNameField = new InputTextField
				{
					Id = Guid.NewGuid(),
					Name = "bank_name",
					Label = "Bank Name",
					Required = true,
					MaxLength = 255
				};
				fields.Add(bankNameField);

				// Account Number
				var accountNumberField = new InputTextField
				{
					Id = Guid.NewGuid(),
					Name = "account_number",
					Label = "Account Number",
					Required = true,
					MaxLength = 50
				};
				fields.Add(accountNumberField);

				// Currency
				var currencyField = new InputSelectField
				{
					Id = Guid.NewGuid(),
					Name = "currency",
					Label = "Currency",
					Required = true,
					DefaultValue = "USD",
					Options = new List<SelectOption>
					{
						new SelectOption("USD", "USD"),
						new SelectOption("EUR", "EUR"),
						new SelectOption("GBP", "GBP"),
						new SelectOption("JPY", "JPY")
					}
				};
				fields.Add(currencyField);

				// Status
				var statusField = new InputSelectField
				{
					Id = Guid.NewGuid(),
					Name = "status",
					Label = "Status",
					Required = true,
					DefaultValue = "active",
					Options = new List<SelectOption>
					{
						new SelectOption("active", "Active"),
						new SelectOption("inactive", "Inactive")
					}
				};
				fields.Add(statusField);

				// PeopleSoft Account ID
				var psAccountIdField = new InputTextField
				{
					Id = Guid.NewGuid(),
					Name = "peoplesoft_account_id",
					Label = "PeopleSoft Account ID",
					MaxLength = 50
				};
				fields.Add(psAccountIdField);

				foreach (var field in fields)
				{
					var fieldResponse = entMan.CreateField(entityId, field);
				}
			}
		}

		private static void CreateTimsApprovalEntity()
		{
			var entityId = new Guid("10000000-0000-0000-0000-000000000007");
			var entityName = "tims_approval";
			var entityLabel = "Approval";
			var entityLabelPlural = "Approvals";

			var entMan = new EntityManager();
			var response = entMan.CreateEntity(entityId, entityName, entityLabel, entityLabelPlural, createOnlyIdField: false);

			if (response.Success)
			{
				var entity = response.Object;
				var fields = new List<InputField>();

				// Entity Type
				var entityTypeField = new InputSelectField
				{
					Id = Guid.NewGuid(),
					Name = "entity_type",
					Label = "Entity Type",
					Required = true,
					Options = new List<SelectOption>
					{
						new SelectOption("travel_request", "Travel Request"),
						new SelectOption("claim", "Claim")
					}
				};
				fields.Add(entityTypeField);

				// Entity ID
				var entityIdField = new InputGuidField
				{
					Id = Guid.NewGuid(),
					Name = "entity_id",
					Label = "Entity ID",
					Required = true
				};
				fields.Add(entityIdField);

				// Approver Role
				var approverRoleField = new InputSelectField
				{
					Id = Guid.NewGuid(),
					Name = "approver_role",
					Label = "Approver Role",
					Required = true,
					Options = new List<SelectOption>
					{
						new SelectOption("manager", "Manager"),
						new SelectOption("finance", "Finance"),
						new SelectOption("director", "Director")
					}
				};
				fields.Add(approverRoleField);

				// Approver ID
				var approverIdField = new InputGuidField
				{
					Id = Guid.NewGuid(),
					Name = "approver_id",
					Label = "Approver"
				};
				fields.Add(approverIdField);

				// Status
				var statusField = new InputSelectField
				{
					Id = Guid.NewGuid(),
					Name = "status",
					Label = "Status",
					Required = true,
					DefaultValue = "pending",
					Options = new List<SelectOption>
					{
						new SelectOption("pending", "Pending"),
						new SelectOption("approved", "Approved"),
						new SelectOption("rejected", "Rejected")
					}
				};
				fields.Add(statusField);

				// Comments
				var commentsField = new InputMultiLineTextField
				{
					Id = Guid.NewGuid(),
					Name = "comments",
					Label = "Comments"
				};
				fields.Add(commentsField);

				// Approval Date
				var approvalDateField = new InputDateTimeField
				{
					Id = Guid.NewGuid(),
					Name = "approval_date",
					Label = "Approval Date"
				};
				fields.Add(approvalDateField);

				foreach (var field in fields)
				{
					var fieldResponse = entMan.CreateField(entityId, field);
				}
			}
		}

		#endregion
	}
}
