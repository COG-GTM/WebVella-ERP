using System;
using System.Collections.Generic;
using WebVella.Erp.Api;
using WebVella.Erp.Api.Models;
using WebVella.Erp.Eql;
using WebVella.Erp.Database;

namespace WebVella.Erp.Plugins.TravelERP.Services
{
	public class TravelErpService
	{
		private readonly EntityManager _entityManager;
		private readonly RecordManager _recordManager;

		public TravelErpService(DbContext dbContext = null)
		{
			_entityManager = new EntityManager();
			_recordManager = new RecordManager(dbContext, true, true);
		}

		#region << Mission Methods >>

		public QueryResponse CreateMission(EntityRecord missionRecord)
		{
			var response = _recordManager.CreateRecord("tims_mission", missionRecord);
			return response;
		}

		public QueryResponse UpdateMission(EntityRecord missionRecord)
		{
			var response = _recordManager.UpdateRecord("tims_mission", missionRecord);
			return response;
		}

		public QueryResponse DeleteMission(Guid missionId)
		{
			var response = _recordManager.DeleteRecord("tims_mission", missionId);
			return response;
		}

		public EntityRecord GetMission(Guid missionId)
		{
			var query = new EntityQuery("tims_mission", "*", EntityQuery.QueryEQ("id", missionId));
			var response = _recordManager.Find(query);
			if (response.Success && response.Object.Data.Count > 0)
				return response.Object.Data[0];
			return null;
		}

		public List<EntityRecord> GetMissionsByStatus(string status)
		{
			var query = new EntityQuery("tims_mission", "*", EntityQuery.QueryEQ("status", status));
			var response = _recordManager.Find(query);
			if (response.Success && response.Object != null)
				return response.Object.Data;
			return new List<EntityRecord>();
		}

		public List<EntityRecord> GetAllMissions()
		{
			var query = new EntityQuery("tims_mission", "*");
			var response = _recordManager.Find(query);
			if (response.Success && response.Object != null)
				return response.Object.Data;
			return new List<EntityRecord>();
		}

		#endregion

		#region << Travel Request Methods >>

		public QueryResponse CreateTravelRequest(EntityRecord travelRequestRecord)
		{
			var response = _recordManager.CreateRecord("tims_travel_request", travelRequestRecord);
			return response;
		}

		public QueryResponse UpdateTravelRequest(EntityRecord travelRequestRecord)
		{
			var response = _recordManager.UpdateRecord("tims_travel_request", travelRequestRecord);
			return response;
		}

		public QueryResponse DeleteTravelRequest(Guid requestId)
		{
			var response = _recordManager.DeleteRecord("tims_travel_request", requestId);
			return response;
		}

		public EntityRecord GetTravelRequest(Guid requestId)
		{
			var query = new EntityQuery("tims_travel_request", "*", EntityQuery.QueryEQ("id", requestId));
			var response = _recordManager.Find(query);
			if (response.Success && response.Object.Data.Count > 0)
				return response.Object.Data[0];
			return null;
		}

		public List<EntityRecord> GetTravelRequestsByMission(Guid missionId)
		{
			var query = new EntityQuery("tims_travel_request", "*", EntityQuery.QueryEQ("mission_id", missionId));
			var response = _recordManager.Find(query);
			if (response.Success && response.Object != null)
				return response.Object.Data;
			return new List<EntityRecord>();
		}

		public List<EntityRecord> GetAllTravelRequests()
		{
			var query = new EntityQuery("tims_travel_request", "*");
			var response = _recordManager.Find(query);
			if (response.Success && response.Object != null)
				return response.Object.Data;
			return new List<EntityRecord>();
		}

		#endregion

		#region << Claim Methods >>

		public QueryResponse CreateClaim(EntityRecord claimRecord)
		{
			var response = _recordManager.CreateRecord("tims_claim", claimRecord);
			return response;
		}

		public QueryResponse UpdateClaim(EntityRecord claimRecord)
		{
			var response = _recordManager.UpdateRecord("tims_claim", claimRecord);
			return response;
		}

		public QueryResponse DeleteClaim(Guid claimId)
		{
			var response = _recordManager.DeleteRecord("tims_claim", claimId);
			return response;
		}

		public EntityRecord GetClaim(Guid claimId)
		{
			var query = new EntityQuery("tims_claim", "*", EntityQuery.QueryEQ("id", claimId));
			var response = _recordManager.Find(query);
			if (response.Success && response.Object.Data.Count > 0)
				return response.Object.Data[0];
			return null;
		}

		public List<EntityRecord> GetClaimsByTravelRequest(Guid travelRequestId)
		{
			var query = new EntityQuery("tims_claim", "*", EntityQuery.QueryEQ("travel_request_id", travelRequestId));
			var response = _recordManager.Find(query);
			if (response.Success && response.Object != null)
				return response.Object.Data;
			return new List<EntityRecord>();
		}

		public List<EntityRecord> GetAllClaims()
		{
			var query = new EntityQuery("tims_claim", "*");
			var response = _recordManager.Find(query);
			if (response.Success && response.Object != null)
				return response.Object.Data;
			return new List<EntityRecord>();
		}

		/// <summary>
		/// Performs three-way matching for a claim: Budget-Invoice-Amount validation
		/// </summary>
		public string PerformThreeWayMatch(Guid claimId)
		{
			var claim = GetClaim(claimId);
			if (claim == null)
				return "failed";

			decimal budgetAmount = claim.Properties.ContainsKey("budget_amount") ? 
				Convert.ToDecimal(claim.Properties["budget_amount"]) : 0;
			decimal invoiceAmount = claim.Properties.ContainsKey("invoice_amount") ? 
				Convert.ToDecimal(claim.Properties["invoice_amount"]) : 0;
			decimal claimAmount = claim.Properties.ContainsKey("claim_amount") ? 
				Convert.ToDecimal(claim.Properties["claim_amount"]) : 0;

			// Define tolerance threshold (5%)
			decimal tolerance = 0.05m;

			// Check if all amounts are within tolerance
			bool budgetMatch = Math.Abs(budgetAmount - claimAmount) <= (budgetAmount * tolerance);
			bool invoiceMatch = Math.Abs(invoiceAmount - claimAmount) <= (invoiceAmount * tolerance);

			if (budgetMatch && invoiceMatch)
			{
				return "matched";
			}
			else if (Math.Abs(budgetAmount - claimAmount) <= (budgetAmount * tolerance * 2) &&
			         Math.Abs(invoiceAmount - claimAmount) <= (invoiceAmount * tolerance * 2))
			{
				return "variance";
			}
			else
			{
				return "failed";
			}
		}

		#endregion

		#region << Payment Methods >>

		public QueryResponse CreatePayment(EntityRecord paymentRecord)
		{
			var response = _recordManager.CreateRecord("tims_payment", paymentRecord);
			return response;
		}

		public QueryResponse UpdatePayment(EntityRecord paymentRecord)
		{
			var response = _recordManager.UpdateRecord("tims_payment", paymentRecord);
			return response;
		}

		public QueryResponse DeletePayment(Guid paymentId)
		{
			var response = _recordManager.DeleteRecord("tims_payment", paymentId);
			return response;
		}

		public EntityRecord GetPayment(Guid paymentId)
		{
			var query = new EntityQuery("tims_payment", "*", EntityQuery.QueryEQ("id", paymentId));
			var response = _recordManager.Find(query);
			if (response.Success && response.Object.Data.Count > 0)
				return response.Object.Data[0];
			return null;
		}

		public List<EntityRecord> GetPaymentsByClaim(Guid claimId)
		{
			var query = new EntityQuery("tims_payment", "*", EntityQuery.QueryEQ("claim_id", claimId));
			var response = _recordManager.Find(query);
			if (response.Success && response.Object != null)
				return response.Object.Data;
			return new List<EntityRecord>();
		}

		public List<EntityRecord> GetAllPayments()
		{
			var query = new EntityQuery("tims_payment", "*");
			var response = _recordManager.Find(query);
			if (response.Success && response.Object != null)
				return response.Object.Data;
			return new List<EntityRecord>();
		}

		#endregion

		#region << Budget Methods >>

		public QueryResponse CreateBudget(EntityRecord budgetRecord)
		{
			var response = _recordManager.CreateRecord("tims_budget", budgetRecord);
			return response;
		}

		public QueryResponse UpdateBudget(EntityRecord budgetRecord)
		{
			var response = _recordManager.UpdateRecord("tims_budget", budgetRecord);
			return response;
		}

		public QueryResponse DeleteBudget(Guid budgetId)
		{
			var response = _recordManager.DeleteRecord("tims_budget", budgetId);
			return response;
		}

		public EntityRecord GetBudget(Guid budgetId)
		{
			var query = new EntityQuery("tims_budget", "*", EntityQuery.QueryEQ("id", budgetId));
			var response = _recordManager.Find(query);
			if (response.Success && response.Object.Data.Count > 0)
				return response.Object.Data[0];
			return null;
		}

		public List<EntityRecord> GetBudgetsByDepartment(string department)
		{
			var query = new EntityQuery("tims_budget", "*", EntityQuery.QueryEQ("department", department));
			var response = _recordManager.Find(query);
			if (response.Success && response.Object != null)
				return response.Object.Data;
			return new List<EntityRecord>();
		}

		#endregion

		#region << Bank Account Methods >>

		public QueryResponse CreateBankAccount(EntityRecord bankAccountRecord)
		{
			var response = _recordManager.CreateRecord("tims_bank_account", bankAccountRecord);
			return response;
		}

		public QueryResponse UpdateBankAccount(EntityRecord bankAccountRecord)
		{
			var response = _recordManager.UpdateRecord("tims_bank_account", bankAccountRecord);
			return response;
		}

		public QueryResponse DeleteBankAccount(Guid accountId)
		{
			var response = _recordManager.DeleteRecord("tims_bank_account", accountId);
			return response;
		}

		public EntityRecord GetBankAccount(Guid accountId)
		{
			var query = new EntityQuery("tims_bank_account", "*", EntityQuery.QueryEQ("id", accountId));
			var response = _recordManager.Find(query);
			if (response.Success && response.Object.Data.Count > 0)
				return response.Object.Data[0];
			return null;
		}

		public List<EntityRecord> GetActiveBankAccounts()
		{
			var query = new EntityQuery("tims_bank_account", "*", EntityQuery.QueryEQ("status", "active"));
			var response = _recordManager.Find(query);
			if (response.Success && response.Object != null)
				return response.Object.Data;
			return new List<EntityRecord>();
		}

		#endregion

		#region << Approval Methods >>

		public QueryResponse CreateApproval(EntityRecord approvalRecord)
		{
			var response = _recordManager.CreateRecord("tims_approval", approvalRecord);
			return response;
		}

		public QueryResponse UpdateApproval(EntityRecord approvalRecord)
		{
			var response = _recordManager.UpdateRecord("tims_approval", approvalRecord);
			return response;
		}

		public QueryResponse DeleteApproval(Guid approvalId)
		{
			var response = _recordManager.DeleteRecord("tims_approval", approvalId);
			return response;
		}

		public EntityRecord GetApproval(Guid approvalId)
		{
			var query = new EntityQuery("tims_approval", "*", EntityQuery.QueryEQ("id", approvalId));
			var response = _recordManager.Find(query);
			if (response.Success && response.Object.Data.Count > 0)
				return response.Object.Data[0];
			return null;
		}

		public List<EntityRecord> GetApprovalsByEntity(string entityType, Guid entityId)
		{
			var query = new EntityQuery("tims_approval", "*", EntityQuery.QueryAND(EntityQuery.QueryEQ("entity_type", entityType), EntityQuery.QueryEQ("entity_id", entityId)));
			var response = _recordManager.Find(query);
			if (response.Success && response.Object != null)
				return response.Object.Data;
			return new List<EntityRecord>();
		}

		/// <summary>
		/// Checks if an entity has all required approvals
		/// </summary>
		public bool HasAllRequiredApprovals(string entityType, Guid entityId)
		{
			var approvals = GetApprovalsByEntity(entityType, entityId);
			
			bool hasManagerApproval = approvals.Exists(a => 
				a.Properties.ContainsKey("approver_role") && 
				a.Properties["approver_role"].ToString() == "manager" &&
				a.Properties.ContainsKey("status") && 
				a.Properties["status"].ToString() == "approved");

			bool hasFinanceApproval = approvals.Exists(a => 
				a.Properties.ContainsKey("approver_role") && 
				a.Properties["approver_role"].ToString() == "finance" &&
				a.Properties.ContainsKey("status") && 
				a.Properties["status"].ToString() == "approved");

			bool hasDirectorApproval = approvals.Exists(a => 
				a.Properties.ContainsKey("approver_role") && 
				a.Properties["approver_role"].ToString() == "director" &&
				a.Properties.ContainsKey("status") && 
				a.Properties["status"].ToString() == "approved");

			return hasManagerApproval && hasFinanceApproval && hasDirectorApproval;
		}

		#endregion
	}
}
