using System;
using System.Collections.Generic;
using WebVella.Erp.Api;
using WebVella.Erp.Api.Models;
using WebVella.Erp.Eql;

namespace WebVella.Erp.Plugins.TravelERP.Hooks
{
	public class MissionHooks
	{
		/// <summary>
		/// Validates mission status transitions
		/// </summary>
		public bool CanTransitionStatus(string currentStatus, string newStatus)
		{
			// Define valid status transitions
			var validTransitions = new Dictionary<string, List<string>>
			{
				{ "planned", new List<string> { "approved", "cancelled" } },
				{ "approved", new List<string> { "active", "cancelled" } },
				{ "active", new List<string> { "completed", "cancelled" } },
				{ "completed", new List<string>() }, // Terminal state
				{ "cancelled", new List<string>() }  // Terminal state
			};

			if (!validTransitions.ContainsKey(currentStatus))
				return false;

			return validTransitions[currentStatus].Contains(newStatus);
		}

		/// <summary>
		/// Hook that runs when a mission status changes
		/// </summary>
		public void OnMissionStatusChanged(EntityRecord missionRecord, string oldStatus, string newStatus)
		{
			if (missionRecord == null)
				return;

			try
			{
				// Validate the status transition
				if (!CanTransitionStatus(oldStatus, newStatus))
				{
					// Revert to old status if transition is invalid
					missionRecord.Properties["status"] = oldStatus;
					var recordManager = new RecordManager();
					recordManager.UpdateRecord("tims_mission", missionRecord);
					
					throw new Exception($"Invalid status transition from {oldStatus} to {newStatus}");
				}

				// Additional logic based on status changes
				switch (newStatus)
				{
					case "active":
						OnMissionActivated(missionRecord);
						break;
					case "completed":
						OnMissionCompleted(missionRecord);
						break;
					case "cancelled":
						OnMissionCancelled(missionRecord);
						break;
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error in MissionHooks.OnMissionStatusChanged: {ex.Message}");
				throw;
			}
		}

		/// <summary>
		/// Logic when mission is activated
		/// </summary>
		private void OnMissionActivated(EntityRecord missionRecord)
		{
			try
			{
				// Validate that start date is not in the past
				if (missionRecord.Properties.ContainsKey("start_date"))
				{
					var startDate = (DateTime)missionRecord.Properties["start_date"];
					if (startDate > DateTime.Today)
					{
						throw new Exception("Cannot activate mission before start date");
					}
				}

				// Check if there are associated travel requests
				var recordManager = new RecordManager();
				var missionId = (Guid)missionRecord.Properties["id"];
				
				var query = new EntityQuery("tims_travel_request", "*", EntityQuery.QueryEQ("mission_id", missionId));
				var response = recordManager.Find(query);

				if (response.Object.Data.Count == 0)
				{
					Console.WriteLine($"Warning: Mission {missionId} activated with no travel requests");
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error in OnMissionActivated: {ex.Message}");
			}
		}

		/// <summary>
		/// Logic when mission is completed
		/// </summary>
		private void OnMissionCompleted(EntityRecord missionRecord)
		{
			try
			{
				// Validate that end date is not in the future
				if (missionRecord.Properties.ContainsKey("end_date"))
				{
					var endDate = (DateTime)missionRecord.Properties["end_date"];
					if (endDate > DateTime.Today)
					{
						throw new Exception("Cannot complete mission before end date");
					}
				}

				// Check if all associated claims are paid
				var recordManager = new RecordManager();
				var missionId = (Guid)missionRecord.Properties["id"];

				// Get all travel requests for this mission
				var travelQuery = new EntityQuery("tims_travel_request", "*", EntityQuery.QueryEQ("mission_id", missionId));
				var travelResponse = recordManager.Find(travelQuery);

				foreach (var travelRequest in travelResponse.Object.Data)
				{
					var requestId = (Guid)travelRequest.Properties["id"];
					
					// Get all claims for this travel request
					var claimQuery = new EntityQuery("tims_claim", "*", EntityQuery.QueryEQ("travel_request_id", requestId));
					var claimResponse = recordManager.Find(claimQuery);

					foreach (var claim in claimResponse.Object.Data)
					{
						var status = claim.Properties.ContainsKey("status") ? 
							claim.Properties["status"].ToString() : "";

						if (status != "paid" && status != "rejected")
						{
							Console.WriteLine($"Warning: Mission {missionId} completed with unpaid claim {claim.Properties["id"]}");
						}
					}
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error in OnMissionCompleted: {ex.Message}");
			}
		}

		/// <summary>
		/// Logic when mission is cancelled
		/// </summary>
		private void OnMissionCancelled(EntityRecord missionRecord)
		{
			try
			{
				// Update all associated travel requests to cancelled
				var recordManager = new RecordManager();
				var missionId = (Guid)missionRecord.Properties["id"];

				var travelQuery = new EntityQuery("tims_travel_request", "*", 
					EntityQuery.QueryAND(EntityQuery.QueryEQ("mission_id", missionId), EntityQuery.QueryNOT("status", "cancelled")));
				var travelResponse = recordManager.Find(travelQuery);

				foreach (var travelRequest in travelResponse.Object.Data)
				{
					travelRequest.Properties["status"] = "cancelled";
					recordManager.UpdateRecord("tims_travel_request", travelRequest);
				}

				// Update all associated claims to cancelled
				var travelRequestIds = travelResponse.Object.Data.ConvertAll(tr => (Guid)tr.Properties["id"]);
				var claimQueryParts = new List<QueryObject>();
				foreach (var id in travelRequestIds)
				{
					claimQueryParts.Add(EntityQuery.QueryEQ("travel_request_id", id));
				}
				var claimQuery = new EntityQuery("tims_claim", "*", 
					EntityQuery.QueryAND(
						EntityQuery.QueryOR(claimQueryParts.ToArray()),
						EntityQuery.QueryNOT("status", "paid"),
						EntityQuery.QueryNOT("status", "rejected")));
				var claimResponse = recordManager.Find(claimQuery);

				foreach (var claim in claimResponse.Object.Data)
				{
					claim.Properties["status"] = "cancelled";
					recordManager.UpdateRecord("tims_claim", claim);
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error in OnMissionCancelled: {ex.Message}");
			}
		}

		/// <summary>
		/// Validates mission before creation
		/// </summary>
		public List<ErrorModel> ValidateMissionForCreation(EntityRecord missionRecord)
		{
			var errors = new List<ErrorModel>();

			if (missionRecord == null)
			{
				errors.Add(new ErrorModel { Message = "Mission record is null" });
				return errors;
			}

			// Check if mission code is provided
			if (!missionRecord.Properties.ContainsKey("mission_code") || 
			    string.IsNullOrEmpty(missionRecord.Properties["mission_code"].ToString()))
			{
				errors.Add(new ErrorModel { Message = "Mission code is required" });
			}

			// Check if mission type is valid
			if (missionRecord.Properties.ContainsKey("mission_type"))
			{
				var missionType = missionRecord.Properties["mission_type"].ToString();
				var validTypes = new List<string> { "official", "training", "conference", "consultation" };
				if (!validTypes.Contains(missionType))
				{
					errors.Add(new ErrorModel { Message = "Invalid mission type" });
				}
			}

			// Validate date range
			if (missionRecord.Properties.ContainsKey("start_date") && 
			    missionRecord.Properties.ContainsKey("end_date"))
			{
				var startDate = (DateTime)missionRecord.Properties["start_date"];
				var endDate = (DateTime)missionRecord.Properties["end_date"];

				if (endDate < startDate)
				{
					errors.Add(new ErrorModel { Message = "End date must be after start date" });
				}
			}

			// Check if budget amount is positive
			if (missionRecord.Properties.ContainsKey("budget_amount"))
			{
				var budgetAmount = Convert.ToDecimal(missionRecord.Properties["budget_amount"]);
				if (budgetAmount <= 0)
				{
					errors.Add(new ErrorModel { Message = "Budget amount must be positive" });
				}
			}

			return errors;
		}
	}
}
