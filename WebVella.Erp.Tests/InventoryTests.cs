using System;
using System.Collections.Generic;
using System.Linq;
using WebVella.Erp.Api;
using WebVella.Erp.Api.Models;
using WebVella.Erp.Plugins.Inventory.Services;
using WebVella.Erp.Plugins.Workflow.Services;
using Xunit;

namespace WebVella.Erp.Tests
{
	[Collection("erp")]
	public class InventoryTests
	{
		private readonly ErpTestFixture _fx;
		public InventoryTests(ErpTestFixture fx) => _fx = fx;

		private static string Suffix() => Guid.NewGuid().ToString("N").Substring(0, 8);

		private static int JournalCountForMovement(Guid movementId)
		{
			var recMan = new RecordManager();
			var query = new EntityQuery("fin_journal_entry", "*",
				EntityQuery.QueryAND(
					EntityQuery.QueryEQ("source_module", "inventory"),
					EntityQuery.QueryEQ("source_reference", movementId.ToString())));
			var resp = recMan.Find(query);
			return (resp.Success && resp.Object != null) ? resp.Object.Data.Count : 0;
		}

		[Fact]
		public void Inventory_entities_fields_and_relations_are_created()
		{
			_fx.Run(() =>
			{
				var entMan = new EntityManager();
				foreach (var name in new[] { "inv_item", "inv_warehouse", "inv_stock_level", "inv_stock_movement" })
				{
					var resp = entMan.ReadEntity(name);
					Assert.True(resp.Success && resp.Object != null, $"Entity '{name}' should exist. {resp.Message}");
				}

				var item = entMan.ReadEntity("inv_item").Object;
				var itemFields = item.Fields.Select(f => f.Name).ToList();
				foreach (var f in new[] { "code", "name", "unit_of_measure", "standard_cost", "gl_inventory_account_code", "gl_cogs_account_code" })
					Assert.Contains(f, itemFields);

				var movement = entMan.ReadEntity("inv_stock_movement").Object;
				var movementFields = movement.Fields.Select(f => f.Name).ToList();
				foreach (var f in new[] { "item_id", "warehouse_id", "movement_type", "quantity", "unit_cost", "reference", "status" })
					Assert.Contains(f, movementFields);

				var relMan = new EntityRelationManager();
				foreach (var relName in new[]
				{
					"inv_item_stock_level", "inv_warehouse_stock_level",
					"inv_item_stock_movement", "inv_warehouse_stock_movement"
				})
				{
					var rel = relMan.Read(relName);
					Assert.True(rel.Object != null, $"Relation '{relName}' should exist.");
				}
			});
		}

		[Fact]
		public void Receipt_increases_stock_and_posts_gl()
		{
			_fx.Run(() =>
			{
				var svc = new InventoryService();
				var s = Suffix();
				var itemId = svc.EnsureItem("SKU-" + s, "Widget " + s, "EA", 20m, "1300", "5000");
				var whId = svc.EnsureWarehouse("WH-" + s, "Main " + s);

				var resp = svc.CreateAndPostMovement(itemId, whId, "receipt", 100m, 20m, "PO-" + s);
				Assert.True(resp.Success, "Receipt posting should succeed. " + resp.Message);
				Assert.Equal(100m, svc.GetOnHand(itemId, whId));

				var movementId = (Guid)resp.Object.Data[0]["id"];
				Assert.Equal(1, JournalCountForMovement(movementId));
			});
		}

		[Fact]
		public void Issue_within_on_hand_decreases_stock_and_posts_gl()
		{
			_fx.Run(() =>
			{
				var svc = new InventoryService();
				var s = Suffix();
				var itemId = svc.EnsureItem("SKU-" + s, "Widget " + s, "EA", 20m, "1300", "5000");
				var whId = svc.EnsureWarehouse("WH-" + s, "Main " + s);

				svc.CreateAndPostMovement(itemId, whId, "receipt", 100m, 20m, "PO-" + s);
				Assert.Equal(100m, svc.GetOnHand(itemId, whId));

				var issue = svc.CreateAndPostMovement(itemId, whId, "issue", 40m, 20m, "SO-" + s);
				Assert.True(issue.Success, "Issue within on-hand should succeed. " + issue.Message);
				Assert.Equal(60m, svc.GetOnHand(itemId, whId));

				var issueId = (Guid)issue.Object.Data[0]["id"];
				Assert.Equal(1, JournalCountForMovement(issueId));
			});
		}

		[Fact]
		public void Issue_exceeding_on_hand_is_rejected_and_stock_unchanged()
		{
			_fx.Run(() =>
			{
				var svc = new InventoryService();
				var s = Suffix();
				var itemId = svc.EnsureItem("SKU-" + s, "Widget " + s, "EA", 20m, "1300", "5000");
				var whId = svc.EnsureWarehouse("WH-" + s, "Main " + s);

				svc.CreateAndPostMovement(itemId, whId, "receipt", 50m, 20m, "PO-" + s);
				Assert.Equal(50m, svc.GetOnHand(itemId, whId));

				var bad = svc.CreateAndPostMovement(itemId, whId, "issue", 1000m, 20m, "SO-" + s);
				Assert.False(bad.Success, "Issuing more than on-hand must be rejected (stock cannot go negative).");
				Assert.Equal(50m, svc.GetOnHand(itemId, whId));
			});
		}

		[Fact]
		public void Large_adjustment_pending_approval_does_not_post()
		{
			_fx.Run(() =>
			{
				var svc = new InventoryService();
				var wf = new WorkflowService();
				var s = Suffix();
				var itemId = svc.EnsureItem("SKU-" + s, "Widget " + s, "EA", 20m, "1300", "5000");
				var whId = svc.EnsureWarehouse("WH-" + s, "Main " + s);

				// amount = 100 * 20 = 2000 > threshold (1000) => needs approval
				var movementId = svc.CreateDraftMovement(itemId, whId, "adjustment", 100m, 20m, "ADJ-" + s);
				var flowId = wf.StartApprovalFlow("inv_adjustment", "inv_stock_movement", movementId,
					new List<string> { "inventory_manager" });

				Assert.False(wf.IsApproved(flowId));

				var resp = svc.PostMovement(movementId, flowId);
				Assert.False(resp.Success, "A large adjustment with a pending approval must not post.");
				Assert.Equal(0m, svc.GetOnHand(itemId, whId));
				Assert.Equal(0, JournalCountForMovement(movementId));
			});
		}

		[Fact]
		public void Large_adjustment_approved_posts_and_posts_gl()
		{
			_fx.Run(() =>
			{
				var svc = new InventoryService();
				var wf = new WorkflowService();
				var s = Suffix();
				var itemId = svc.EnsureItem("SKU-" + s, "Widget " + s, "EA", 20m, "1300", "5000");
				var whId = svc.EnsureWarehouse("WH-" + s, "Main " + s);

				var movementId = svc.CreateDraftMovement(itemId, whId, "adjustment", 100m, 20m, "ADJ-" + s);
				var flowId = wf.StartApprovalFlow("inv_adjustment", "inv_stock_movement", movementId,
					new List<string> { "inventory_manager" });
				Assert.True(wf.RecordDecision(flowId, 1, Guid.NewGuid(), true, "approved"));
				Assert.True(wf.IsApproved(flowId));

				var resp = svc.PostMovement(movementId, flowId);
				Assert.True(resp.Success, "An approved large adjustment should post. " + resp.Message);
				Assert.Equal(100m, svc.GetOnHand(itemId, whId));
				Assert.Equal(1, JournalCountForMovement(movementId));
			});
		}
	}
}
