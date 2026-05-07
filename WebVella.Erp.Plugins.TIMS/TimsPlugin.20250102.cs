using System;
using System.Collections.Generic;
using WebVella.Erp.Api;
using WebVella.Erp.Api.Models;

namespace WebVella.Erp.Plugins.TIMS
{
	public partial class TimsPlugin : ErpPlugin
	{
		// Adds the fields that were silently dropped during the initial 20250101
		// migration because they were marked Required=true without a DefaultValue,
		// which fails EntityManager.CreateField validation.
		// Here we add them as non-required (Required=false) so the migration succeeds.
		// Application-level validation in *Hooks.cs already enforces the required semantics.
		private static void Patch20250102()
		{
			var entMan = new EntityManager();

			AddMissingMissionFields(entMan);
			AddMissingTravelRequestFields(entMan);
			AddMissingClaimFields(entMan);
			AddMissingPaymentFields(entMan);
			AddMissingBudgetFields(entMan);
			AddMissingBankAccountFields(entMan);
			AddMissingApprovalFields(entMan);
		}

		private static bool HasField(EntityManager entMan, Guid entityId, string fieldName)
		{
			var resp = entMan.ReadEntity(entityId);
			if (!resp.Success || resp.Object == null) return false;
			foreach (var f in resp.Object.Fields)
				if (string.Equals(f.Name, fieldName, StringComparison.OrdinalIgnoreCase))
					return true;
			return false;
		}

		private static void TryCreate(EntityManager entMan, Guid entityId, InputField field)
		{
			if (HasField(entMan, entityId, field.Name)) return;
			entMan.CreateField(entityId, field);
		}

		private static void AddMissingMissionFields(EntityManager entMan)
		{
			var entityId = new Guid("10000000-0000-0000-0000-000000000001");

			TryCreate(entMan, entityId, new InputTextField { Id = Guid.NewGuid(), Name = "mission_code", Label = "Mission Code", Required = false, Unique = false, MaxLength = 50 });
			TryCreate(entMan, entityId, new InputSelectField
			{
				Id = Guid.NewGuid(), Name = "mission_type", Label = "Mission Type", Required = false,
				Options = new List<SelectOption>
				{
					new SelectOption("official", "Official"),
					new SelectOption("training", "Training"),
					new SelectOption("conference", "Conference"),
					new SelectOption("consultation", "Consultation")
				}
			});
			TryCreate(entMan, entityId, new InputTextField { Id = Guid.NewGuid(), Name = "title", Label = "Title", Required = false, MaxLength = 255 });
			TryCreate(entMan, entityId, new InputDateField { Id = Guid.NewGuid(), Name = "start_date", Label = "Start Date", Required = false, Format = "yyyy-MM-dd", UseCurrentTimeAsDefaultValue = false });
			TryCreate(entMan, entityId, new InputDateField { Id = Guid.NewGuid(), Name = "end_date", Label = "End Date", Required = false, Format = "yyyy-MM-dd", UseCurrentTimeAsDefaultValue = false });
			TryCreate(entMan, entityId, new InputCurrencyField { Id = Guid.NewGuid(), Name = "budget_amount", Label = "Budget Amount", Required = false, Currency = new CurrencyType { Code = "USD" } });
		}

		private static void AddMissingTravelRequestFields(EntityManager entMan)
		{
			var entityId = new Guid("10000000-0000-0000-0000-000000000002");

			TryCreate(entMan, entityId, new InputTextField { Id = Guid.NewGuid(), Name = "request_number", Label = "Request Number", Required = false, Unique = false, MaxLength = 50 });
			TryCreate(entMan, entityId, new InputGuidField { Id = Guid.NewGuid(), Name = "mission_id", Label = "Mission", Required = false });
			TryCreate(entMan, entityId, new InputTextField { Id = Guid.NewGuid(), Name = "traveler_name", Label = "Traveler Name", Required = false, MaxLength = 255 });
			TryCreate(entMan, entityId, new InputTextField { Id = Guid.NewGuid(), Name = "destination", Label = "Destination", Required = false, MaxLength = 255 });
			TryCreate(entMan, entityId, new InputTextField { Id = Guid.NewGuid(), Name = "purpose", Label = "Purpose", Required = false, MaxLength = 500 });
			TryCreate(entMan, entityId, new InputCurrencyField { Id = Guid.NewGuid(), Name = "estimated_cost", Label = "Estimated Cost", Required = false, Currency = new CurrencyType { Code = "USD" } });
		}

		private static void AddMissingClaimFields(EntityManager entMan)
		{
			var entityId = new Guid("10000000-0000-0000-0000-000000000003");

			TryCreate(entMan, entityId, new InputTextField { Id = Guid.NewGuid(), Name = "claim_number", Label = "Claim Number", Required = false, Unique = false, MaxLength = 50 });
			TryCreate(entMan, entityId, new InputGuidField { Id = Guid.NewGuid(), Name = "travel_request_id", Label = "Travel Request", Required = false });
			TryCreate(entMan, entityId, new InputCurrencyField { Id = Guid.NewGuid(), Name = "claim_amount", Label = "Claim Amount", Required = false, Currency = new CurrencyType { Code = "USD" } });
			TryCreate(entMan, entityId, new InputCurrencyField { Id = Guid.NewGuid(), Name = "budget_amount", Label = "Budget Amount", Required = false, Currency = new CurrencyType { Code = "USD" } });
		}

		private static void AddMissingPaymentFields(EntityManager entMan)
		{
			var entityId = new Guid("10000000-0000-0000-0000-000000000005");

			TryCreate(entMan, entityId, new InputTextField { Id = Guid.NewGuid(), Name = "payment_number", Label = "Payment Number", Required = false, Unique = false, MaxLength = 50 });
			TryCreate(entMan, entityId, new InputGuidField { Id = Guid.NewGuid(), Name = "claim_id", Label = "Claim", Required = false });
			TryCreate(entMan, entityId, new InputCurrencyField { Id = Guid.NewGuid(), Name = "payment_amount", Label = "Payment Amount", Required = false, Currency = new CurrencyType { Code = "USD" } });
			TryCreate(entMan, entityId, new InputSelectField
			{
				Id = Guid.NewGuid(), Name = "payment_method", Label = "Payment Method", Required = false,
				Options = new List<SelectOption>
				{
					new SelectOption("etransfer", "E-Transfer"),
					new SelectOption("wire", "Wire Transfer"),
					new SelectOption("check", "Check"),
					new SelectOption("corporate_card", "Corporate Card")
				}
			});
			TryCreate(entMan, entityId, new InputDateField { Id = Guid.NewGuid(), Name = "payment_date", Label = "Payment Date", Required = false, Format = "yyyy-MM-dd", UseCurrentTimeAsDefaultValue = false });
		}

		private static void AddMissingBudgetFields(EntityManager entMan)
		{
			var entityId = new Guid("10000000-0000-0000-0000-000000000004");

			TryCreate(entMan, entityId, new InputTextField { Id = Guid.NewGuid(), Name = "budget_code", Label = "Budget Code", Required = false, Unique = false, MaxLength = 50 });
			TryCreate(entMan, entityId, new InputTextField { Id = Guid.NewGuid(), Name = "department", Label = "Department", Required = false, MaxLength = 255 });
			TryCreate(entMan, entityId, new InputNumberField { Id = Guid.NewGuid(), Name = "fiscal_year", Label = "Fiscal Year", Required = false });
			TryCreate(entMan, entityId, new InputCurrencyField { Id = Guid.NewGuid(), Name = "allocated_amount", Label = "Allocated Amount", Required = false, Currency = new CurrencyType { Code = "USD" } });
		}

		private static void AddMissingBankAccountFields(EntityManager entMan)
		{
			var entityId = new Guid("10000000-0000-0000-0000-000000000006");

			TryCreate(entMan, entityId, new InputTextField { Id = Guid.NewGuid(), Name = "account_number", Label = "Account Number", Required = false, Unique = false, MaxLength = 100 });
			TryCreate(entMan, entityId, new InputTextField { Id = Guid.NewGuid(), Name = "account_name", Label = "Account Name", Required = false, MaxLength = 255 });
			TryCreate(entMan, entityId, new InputTextField { Id = Guid.NewGuid(), Name = "bank_name", Label = "Bank Name", Required = false, MaxLength = 255 });
		}

		private static void AddMissingApprovalFields(EntityManager entMan)
		{
			var entityId = new Guid("10000000-0000-0000-0000-000000000007");

			TryCreate(entMan, entityId, new InputTextField { Id = Guid.NewGuid(), Name = "entity_type", Label = "Entity Type", Required = false, MaxLength = 100 });
			TryCreate(entMan, entityId, new InputGuidField { Id = Guid.NewGuid(), Name = "entity_id", Label = "Entity", Required = false });
			TryCreate(entMan, entityId, new InputTextField { Id = Guid.NewGuid(), Name = "approver_role", Label = "Approver Role", Required = false, MaxLength = 100 });
		}
	}
}
