using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using WebVella.Erp.Api;
using WebVella.Erp.Api.Models;
using WebVella.Erp.Database;
using WebVella.Erp.Plugins.TravelERP.Services;
using WebVella.Erp.Web.Services;

namespace WebVella.Erp.Plugins.TravelERP.Controllers
{
	public class TravelErpController : Controller
	{
		private readonly TravelErpService _travelErpService;
		private readonly IErpService _erpService;
		private readonly RecordManager _recMan;
		private readonly EntityManager _entMan;
		private readonly SecurityManager _secMan;

		public TravelErpController(IErpService erpService)
		{
			_travelErpService = new TravelErpService();
			_erpService = erpService;
			_recMan = new RecordManager();
			_entMan = new EntityManager();
			_secMan = new SecurityManager();
		}

		public Guid? CurrentUserId
		{
			get
			{
				if (HttpContext != null && HttpContext.User != null && HttpContext.User.Claims != null)
				{
					var nameIdentifier = HttpContext.User.Claims.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier);
					if (nameIdentifier is null)
						return null;

					return new Guid(nameIdentifier.Value);
				}
				return null;
			}
		}

		[HttpGet]
		[Route("missions")]
		public IActionResult Missions()
		{
			try
			{
				var missions = _travelErpService.GetMissionsByStatus("planned");
				var model = new
				{
					Missions = missions,
					Title = "Missions"
				};
				return View("~/Pages/Index.cshtml", model);
			}
			catch (Exception ex)
			{
				TempData["Error"] = $"Error loading missions: {ex.Message}";
				return View("~/Pages/Index.cshtml");
			}
		}

		[HttpGet]
		[Route("travel-requests")]
		public IActionResult TravelRequests()
		{
			try
			{
				var travelRequests = _travelErpService.GetTravelRequestsByMission(Guid.Empty);
				var model = new
				{
					TravelRequests = travelRequests,
					Title = "Travel Requests"
				};
				return View("~/Pages/Index.cshtml", model);
			}
			catch (Exception ex)
			{
				TempData["Error"] = $"Error loading travel requests: {ex.Message}";
				return View("~/Pages/Index.cshtml");
			}
		}

		[HttpGet]
		[Route("claims")]
		public IActionResult Claims()
		{
			try
			{
				var claims = _travelErpService.GetClaimsByTravelRequest(Guid.Empty);
				var model = new
				{
					Claims = claims,
					Title = "Claims"
				};
				return View("~/Pages/Index.cshtml", model);
			}
			catch (Exception ex)
			{
				TempData["Error"] = $"Error loading claims: {ex.Message}";
				return View("~/Pages/Index.cshtml");
			}
		}

		[HttpGet]
		[Route("payments")]
		public IActionResult Payments()
		{
			try
			{
				var payments = _travelErpService.GetPaymentsByClaim(Guid.Empty);
				var model = new
				{
					Payments = payments,
					Title = "Payments"
				};
				return View("~/Pages/Index.cshtml", model);
			}
			catch (Exception ex)
			{
				TempData["Error"] = $"Error loading payments: {ex.Message}";
				return View("~/Pages/Index.cshtml");
			}
		}

		[HttpGet]
		[Route("budgets")]
		public IActionResult Budgets()
		{
			try
			{
				var budgets = _travelErpService.GetBudgetsByDepartment("");
				var model = new
				{
					Budgets = budgets,
					Title = "Budgets"
				};
				return View("~/Pages/Index.cshtml", model);
			}
			catch (Exception ex)
			{
				TempData["Error"] = $"Error loading budgets: {ex.Message}";
				return View("~/Pages/Index.cshtml");
			}
		}

		[HttpGet]
		[Route("bank-accounts")]
		public IActionResult BankAccounts()
		{
			try
			{
				var bankAccounts = _travelErpService.GetActiveBankAccounts();
				var model = new
				{
					BankAccounts = bankAccounts,
					Title = "Bank Accounts"
				};
				return View("~/Pages/Index.cshtml", model);
			}
			catch (Exception ex)
			{
				TempData["Error"] = $"Error loading bank accounts: {ex.Message}";
				return View("~/Pages/Index.cshtml");
			}
		}

		[HttpGet]
		[Route("approvals")]
		public IActionResult Approvals()
		{
			try
			{
				var approvals = _travelErpService.GetApprovalsByEntity("", Guid.Empty);
				var model = new
				{
					Approvals = approvals,
					Title = "Approvals"
				};
				return View("~/Pages/Index.cshtml", model);
			}
			catch (Exception ex)
			{
				TempData["Error"] = $"Error loading approvals: {ex.Message}";
				return View("~/Pages/Index.cshtml");
			}
		}
	}
}
