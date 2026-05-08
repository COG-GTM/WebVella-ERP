using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using WebVella.Erp.Api;
using WebVella.Erp.Api.Models;
using WebVella.Erp.Plugins.TravelERP.Services;

namespace WebVella.Erp.Site.TravelERP.Controllers
{
	public class TravelErpController : Controller
	{
		private readonly TravelErpService _travelErpService;

		public TravelErpController()
		{
			_travelErpService = new TravelErpService();
		}

		public IActionResult Missions()
		{
			try
			{
				var missions = _travelErpService.GetAllMissions();
				ViewData["Title"] = "Missions";
				ViewData["Missions"] = missions;
				return View();
			}
			catch (Exception ex)
			{
				TempData["Error"] = $"Error loading missions: {ex.Message}";
				return View();
			}
		}

		public IActionResult TravelRequests()
		{
			try
			{
				var travelRequests = _travelErpService.GetAllTravelRequests();
				ViewData["Title"] = "Travel Requests";
				ViewData["TravelRequests"] = travelRequests;
				return View();
			}
			catch (Exception ex)
			{
				TempData["Error"] = $"Error loading travel requests: {ex.Message}";
				return View();
			}
		}

		public IActionResult Claims()
		{
			try
			{
				var claims = _travelErpService.GetAllClaims();
				ViewData["Title"] = "Claims";
				ViewData["Claims"] = claims;
				return View();
			}
			catch (Exception ex)
			{
				TempData["Error"] = $"Error loading claims: {ex.Message}";
				return View();
			}
		}

		public IActionResult Payments()
		{
			try
			{
				var payments = _travelErpService.GetAllPayments();
				ViewData["Title"] = "Payments";
				ViewData["Payments"] = payments;
				return View();
			}
			catch (Exception ex)
			{
				TempData["Error"] = $"Error loading payments: {ex.Message}";
				return View();
			}
		}

		public IActionResult Budgets()
		{
			try
			{
				var budgets = _travelErpService.GetBudgetsByDepartment("");
				ViewData["Title"] = "Budgets";
				ViewData["Budgets"] = budgets;
				return View();
			}
			catch (Exception ex)
			{
				TempData["Error"] = $"Error loading budgets: {ex.Message}";
				return View();
			}
		}

		public IActionResult BankAccounts()
		{
			try
			{
				var bankAccounts = _travelErpService.GetActiveBankAccounts();
				ViewData["Title"] = "Bank Accounts";
				ViewData["BankAccounts"] = bankAccounts;
				return View();
			}
			catch (Exception ex)
			{
				TempData["Error"] = $"Error loading bank accounts: {ex.Message}";
				return View();
			}
		}

		public IActionResult Approvals()
		{
			try
			{
				var approvals = _travelErpService.GetApprovalsByEntity("", Guid.Empty);
				ViewData["Title"] = "Approvals";
				ViewData["Approvals"] = approvals;
				return View();
			}
			catch (Exception ex)
			{
				TempData["Error"] = $"Error loading approvals: {ex.Message}";
				return View();
			}
		}

		[Microsoft.AspNetCore.Authorization.AllowAnonymous]
		public IActionResult Seed()
		{
			var summary = new List<string>();
			try
			{
				// Skip if data already exists
				if (_travelErpService.GetAllMissions().Count > 0)
				{
					summary.Add("Skipped: missions already exist. Delete records first to reseed.");
					return Content(string.Join("\n", summary), "text/plain");
				}

				var now = DateTime.UtcNow;

				// --- Missions ---
				var missions = new[]
				{
					new { code = "M-2026-001", type = "official",     title = "Article IV Consultation - Brazil", desc = "Annual Article IV consultation mission to Brasilia.",  start = now.Date.AddDays(-5),  end = now.Date.AddDays(5),  budget = 45000m, status = "active",    psId = "PS-PRJ-1001" },
					new { code = "M-2026-002", type = "training",     title = "FSAP Capacity Development - Kenya", desc = "Financial Sector Assessment Program training.",         start = now.Date.AddDays(10), end = now.Date.AddDays(20), budget = 28000m, status = "approved",  psId = "PS-PRJ-1002" },
					new { code = "M-2026-003", type = "conference",   title = "Spring Meetings - Washington DC",   desc = "IMF/World Bank Spring Meetings attendance.",            start = now.Date.AddDays(30), end = now.Date.AddDays(34), budget = 12000m, status = "planned",   psId = "PS-PRJ-1003" },
					new { code = "M-2025-099", type = "consultation", title = "Technical Assistance - Vietnam",    desc = "Tax policy technical assistance mission.",              start = now.Date.AddDays(-60),end = now.Date.AddDays(-50),budget = 35000m, status = "completed", psId = "PS-PRJ-0999" }
				};

				var missionIds = new Dictionary<string, Guid>();
				foreach (var m in missions)
				{
					var id = Guid.NewGuid();
					var rec = new EntityRecord();
					rec["id"] = id;
					rec["mission_code"] = m.code;
					rec["mission_type"] = m.type;
					rec["title"] = m.title;
					rec["description"] = m.desc;
					rec["start_date"] = m.start;
					rec["end_date"] = m.end;
					rec["budget_amount"] = m.budget;
					rec["status"] = m.status;
					rec["peoplesoft_project_id"] = m.psId;
					var resp = _travelErpService.CreateMission(rec);
					if (!resp.Success) { summary.Add($"Mission {m.code} FAILED: {resp.Message}"); continue; }
					missionIds[m.code] = id;
					summary.Add($"Created mission {m.code}");
				}

				// --- Travel Requests ---
				var travelRequests = new[]
				{
					new { num = "TR-2026-001", missionCode = "M-2026-001", traveler = "Maria Garcia",    dest = "Brasilia, Brazil",     purpose = "Article IV mission lead",      cost =  8500m, status = "director_approved" },
					new { num = "TR-2026-002", missionCode = "M-2026-001", traveler = "James Liu",       dest = "Brasilia, Brazil",     purpose = "Fiscal economist support",     cost =  7200m, status = "finance_approved"  },
					new { num = "TR-2026-003", missionCode = "M-2026-002", traveler = "Aisha Mwangi",    dest = "Nairobi, Kenya",       purpose = "FSAP trainer",                  cost =  6300m, status = "manager_approved"  },
					new { num = "TR-2026-004", missionCode = "M-2026-003", traveler = "Robert Chen",     dest = "Washington, DC",       purpose = "Spring meetings delegate",      cost =  3400m, status = "submitted"         },
					new { num = "TR-2025-099", missionCode = "M-2025-099", traveler = "Priya Sharma",    dest = "Hanoi, Vietnam",       purpose = "Tax policy consultant",         cost =  9100m, status = "director_approved" }
				};

				var trIds = new Dictionary<string, Guid>();
				foreach (var t in travelRequests)
				{
					if (!missionIds.ContainsKey(t.missionCode)) continue;
					var id = Guid.NewGuid();
					var rec = new EntityRecord();
					rec["id"] = id;
					rec["request_number"] = t.num;
					rec["mission_id"] = missionIds[t.missionCode];
					rec["traveler_name"] = t.traveler;
					rec["destination"] = t.dest;
					rec["purpose"] = t.purpose;
					rec["estimated_cost"] = t.cost;
					rec["status"] = t.status;
					rec["peoplesoft_request_id"] = $"PS-REQ-{t.num.Replace("TR-", "")}";
					var resp = _travelErpService.CreateTravelRequest(rec);
					if (!resp.Success) { summary.Add($"TravelRequest {t.num} FAILED: {resp.Message}"); continue; }
					trIds[t.num] = id;
					summary.Add($"Created travel request {t.num}");
				}

				// --- Claims ---
				var claims = new[]
				{
					new { num = "CL-2026-001", trNum = "TR-2026-001", claim =  8420m, budget =  8500m, invoice =  8420m, currency = "USD", status = "approved",     match = "matched"  },
					new { num = "CL-2026-002", trNum = "TR-2026-002", claim =  7050m, budget =  7200m, invoice =  7100m, currency = "USD", status = "under_review", match = "matched"  },
					new { num = "CL-2026-003", trNum = "TR-2026-003", claim =  6480m, budget =  6300m, invoice =  6450m, currency = "USD", status = "submitted",    match = "variance" },
					new { num = "CL-2025-099", trNum = "TR-2025-099", claim =  9050m, budget =  9100m, invoice =  9050m, currency = "USD", status = "paid",         match = "matched"  }
				};

				var claimIds = new Dictionary<string, Guid>();
				foreach (var c in claims)
				{
					if (!trIds.ContainsKey(c.trNum)) continue;
					var id = Guid.NewGuid();
					var rec = new EntityRecord();
					rec["id"] = id;
					rec["claim_number"] = c.num;
					rec["travel_request_id"] = trIds[c.trNum];
					rec["claim_amount"] = c.claim;
					rec["budget_amount"] = c.budget;
					rec["invoice_amount"] = c.invoice;
					rec["currency"] = c.currency;
					rec["status"] = c.status;
					rec["match_status"] = c.match;
					rec["peoplesoft_voucher_id"] = $"PS-VCH-{c.num.Replace("CL-", "")}";
					var resp = _travelErpService.CreateClaim(rec);
					if (!resp.Success) { summary.Add($"Claim {c.num} FAILED: {resp.Message}"); continue; }
					claimIds[c.num] = id;
					summary.Add($"Created claim {c.num}");
				}

				// --- Payments ---
				var payments = new[]
				{
					new { num = "PAY-2026-001", claimNum = "CL-2026-001", amt =  8420m, method = "etransfer", date = now.Date.AddDays(-1), status = "completed"  },
					new { num = "PAY-2025-099", claimNum = "CL-2025-099", amt =  9050m, method = "wire",      date = now.Date.AddDays(-45),status = "completed"  },
					new { num = "PAY-2026-002", claimNum = "CL-2026-002", amt =  7050m, method = "etransfer", date = now.Date,             status = "pending"    }
				};

				foreach (var p in payments)
				{
					if (!claimIds.ContainsKey(p.claimNum)) continue;
					var rec = new EntityRecord();
					rec["id"] = Guid.NewGuid();
					rec["payment_number"] = p.num;
					rec["claim_id"] = claimIds[p.claimNum];
					rec["payment_amount"] = p.amt;
					rec["payment_method"] = p.method;
					rec["payment_date"] = p.date;
					rec["status"] = p.status;
					rec["peoplesoft_payment_id"] = $"PS-PAY-{p.num.Replace("PAY-", "")}";
					var resp = _travelErpService.CreatePayment(rec);
					if (!resp.Success) { summary.Add($"Payment {p.num} FAILED: {resp.Message}"); continue; }
					summary.Add($"Created payment {p.num}");
				}

				summary.Insert(0, "TravelERP seed complete.");
				return Content(string.Join("\n", summary), "text/plain");
			}
			catch (Exception ex)
			{
				summary.Add($"ERROR: {ex.Message}");
				summary.Add(ex.StackTrace);
				return Content(string.Join("\n", summary), "text/plain");
			}
		}
	}
}
