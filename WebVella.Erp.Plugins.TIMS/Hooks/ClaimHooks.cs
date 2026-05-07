using System;
using System.Collections.Generic;
using WebVella.Erp.Api;
using WebVella.Erp.Api.Models;
using WebVella.Erp.Plugins.TIMS.Services;
using WebVella.Erp.Eql;

namespace WebVella.Erp.Plugins.TIMS.Hooks
{
	public class ClaimHooks
	{
		private readonly TimsService _timsService;

		public ClaimHooks()
		{
			_timsService = new TimsService();
		}

		/// <summary>
		/// Hook that runs after a claim is created to perform three-way matching
		/// </summary>
		public void OnClaimCreated(EntityRecord claimRecord)
		{
			if (claimRecord == null)
				return;

			try
			{
				// Automatically perform three-way matching when claim is created
				var claimId = (Guid)claimRecord.Properties["id"];
				var matchStatus = _timsService.PerformThreeWayMatch(claimId);

				// Update the claim with the match status
				claimRecord.Properties["match_status"] = matchStatus;

				// Save the updated claim
				var recordManager = new RecordManager();
				recordManager.UpdateRecord("tims_claim", claimRecord);
			}
			catch (Exception ex)
			{
				// Log error but don't throw - hooks shouldn't break the main operation
				Console.WriteLine($"Error in ClaimHooks.OnClaimCreated: {ex.Message}");
			}
		}

		/// <summary>
		/// Hook that runs after a claim is updated to re-perform three-way matching
		/// </summary>
		public void OnClaimUpdated(EntityRecord claimRecord)
		{
			if (claimRecord == null)
				return;

			try
			{
				// Re-perform three-way matching when claim is updated
				var claimId = (Guid)claimRecord.Properties["id"];
				var matchStatus = _timsService.PerformThreeWayMatch(claimId);

				// Update the claim with the match status
				claimRecord.Properties["match_status"] = matchStatus;

				// Save the updated claim
				var recordManager = new RecordManager();
				recordManager.UpdateRecord("tims_claim", claimRecord);
			}
			catch (Exception ex)
			{
				// Log error but don't throw
				Console.WriteLine($"Error in ClaimHooks.OnClaimUpdated: {ex.Message}");
			}
		}

		/// <summary>
		/// Validates claim before submission
		/// </summary>
		public List<ErrorModel> ValidateClaimForSubmission(EntityRecord claimRecord)
		{
			var errors = new List<ErrorModel>();

			if (claimRecord == null)
			{
				errors.Add(new ErrorModel { Message = "Claim record is null" });
				return errors;
			}

			// Check if claim amount is provided
			if (!claimRecord.Properties.ContainsKey("claim_amount") || 
			    claimRecord.Properties["claim_amount"] == null)
			{
				errors.Add(new ErrorModel { Message = "Claim amount is required" });
			}

			// Check if budget amount is provided
			if (!claimRecord.Properties.ContainsKey("budget_amount") || 
			    claimRecord.Properties["budget_amount"] == null)
			{
				errors.Add(new ErrorModel { Message = "Budget amount is required" });
			}

			// Check if travel request is linked
			if (!claimRecord.Properties.ContainsKey("travel_request_id") || 
			    claimRecord.Properties["travel_request_id"] == null || 
			    (Guid)claimRecord.Properties["travel_request_id"] == Guid.Empty)
			{
				errors.Add(new ErrorModel { Message = "Travel request is required" });
			}

			// Perform three-way match validation
			var claimId = (Guid)claimRecord.Properties["id"];
			var matchStatus = _timsService.PerformThreeWayMatch(claimId);

			if (matchStatus == "failed")
			{
				errors.Add(new ErrorModel { Message = "Three-way matching failed. Claim amount exceeds tolerance." });
			}

			return errors;
		}

		/// <summary>
		/// Hook that runs when claim status changes to submitted
		/// </summary>
		public void OnClaimStatusChanged(EntityRecord claimRecord, string oldStatus, string newStatus)
		{
			if (claimRecord == null)
				return;

			try
			{
				// When claim is submitted, validate it
				if (newStatus == "submitted")
				{
					var errors = ValidateClaimForSubmission(claimRecord);
					if (errors.Count > 0)
					{
						// Revert status to draft if validation fails
						claimRecord.Properties["status"] = "draft";
						var recordManager = new RecordManager();
						recordManager.UpdateRecord("tims_claim", claimRecord);
						
						throw new Exception($"Claim validation failed: {string.Join(", ", errors.ConvertAll(e => e.Message))}");
					}
				}

				// When claim is approved, update budget spent amount
				if (newStatus == "approved")
				{
					UpdateBudgetSpentAmount(claimRecord);
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error in ClaimHooks.OnClaimStatusChanged: {ex.Message}");
				throw;
			}
		}

		/// <summary>
		/// Updates budget spent amount when claim is approved
		/// </summary>
		private void UpdateBudgetSpentAmount(EntityRecord claimRecord)
		{
			try
			{
				var claimAmount = claimRecord.Properties.ContainsKey("claim_amount") ? 
					Convert.ToDecimal(claimRecord.Properties["claim_amount"]) : 0;

				// Get the travel request to find the mission
				var travelRequestId = (Guid)claimRecord.Properties["travel_request_id"];
				var recordManager = new RecordManager();
				var travelQuery = new EntityQuery("tims_travel_request", "*", EntityQuery.QueryEQ("id", travelRequestId));
				var travelResponse = recordManager.Find(travelQuery);
				var travelRequest = travelResponse.Success && travelResponse.Object.Data.Count > 0 ? travelResponse.Object.Data[0] : null;

				if (travelRequest != null && travelRequest.Properties.ContainsKey("mission_id"))
				{
					var missionId = (Guid)travelRequest.Properties["mission_id"];
					var missionQuery = new EntityQuery("tims_mission", "*", EntityQuery.QueryEQ("id", missionId));
					var missionResponse = recordManager.Find(missionQuery);
					var mission = missionResponse.Success && missionResponse.Object.Data.Count > 0 ? missionResponse.Object.Data[0] : null;

					if (mission != null && mission.Properties.ContainsKey("budget_amount"))
					{
						// For simplicity, we're updating the mission's budget tracking
						// In a real implementation, this would update the budget entity
						var currentSpent = mission.Properties.ContainsKey("spent_amount") ? 
							Convert.ToDecimal(mission.Properties["spent_amount"]) : 0;
						mission.Properties["spent_amount"] = currentSpent + claimAmount;
						recordManager.UpdateRecord("tims_mission", mission);
					}
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error updating budget spent amount: {ex.Message}");
			}
		}
	}
}
