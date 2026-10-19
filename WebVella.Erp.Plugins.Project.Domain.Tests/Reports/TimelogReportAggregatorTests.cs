using System;
using System.Collections.Generic;
using System.Linq;
using WebVella.Erp.Plugins.Project.Domain.Reports;
using Xunit;

namespace WebVella.Erp.Plugins.Project.Domain.Tests.Reports
{
	public class TimelogReportAggregatorTests
	{
		private sealed class TestProject
		{
			public string Name { get; init; }
			public Guid? AccountId { get; init; }
		}

		private sealed class TestTask
		{
			public Guid Id { get; init; } = Guid.NewGuid();
			public List<TestProject> Projects { get; init; } = new List<TestProject>();
		}

		private sealed class TestTimelog
		{
			public Guid TaskId { get; init; }
			public bool IsBillable { get; init; }
			public decimal Minutes { get; init; }
		}

		private static List<TaskProjectRow<TestTask, TestProject>> Split(IEnumerable<TestTask> tasks, ISet<Guid> withTimelog, Guid? accountId)
			=> TimelogReportAggregator.SplitTasksByProject(tasks, withTimelog, accountId, t => t.Id, t => t.Projects, p => p.AccountId);

		private static MinutesTotals Sum(IEnumerable<TestTimelog> timelogs, Guid taskId)
			=> TimelogReportAggregator.SumMinutes(timelogs, taskId, t => t.TaskId, t => t.IsBillable, t => t.Minutes);

		// ---- GetTaskId ----

		[Fact]
		public void GetTaskId_ReturnsFirstRelatedRecord()
		{
			var first = Guid.NewGuid();
			Assert.Equal(first, TimelogReportAggregator.GetTaskId(new List<Guid> { first, Guid.NewGuid(), Guid.NewGuid() }));
		}

		[Fact]
		public void GetTaskId_EmptyList_ThrowsArgumentOutOfRange()
		{
			Assert.Throws<ArgumentOutOfRangeException>(() => TimelogReportAggregator.GetTaskId(new List<Guid>()));
		}

		[Fact]
		public void GetTaskId_Null_ThrowsNullReference()
		{
			Assert.Throws<NullReferenceException>(() => TimelogReportAggregator.GetTaskId(null));
		}

		// ---- CollectTaskIds ----

		[Fact]
		public void CollectTaskIds_ReturnsDistinctTaskIds()
		{
			var a = Guid.NewGuid();
			var b = Guid.NewGuid();
			var set = TimelogReportAggregator.CollectTaskIds(new[] { a, b, a, a }, x => x);
			Assert.Equal(2, set.Count);
			Assert.Contains(a, set);
			Assert.Contains(b, set);
		}

		[Fact]
		public void CollectTaskIds_Empty_ReturnsEmptySet()
		{
			Assert.Empty(TimelogReportAggregator.CollectTaskIds(Array.Empty<Guid>(), x => x));
		}

		[Fact]
		public void CollectTaskIds_InvokesSelectorOncePerTimelog_InOrder()
		{
			var visited = new List<int>();
			TimelogReportAggregator.CollectTaskIds(new[] { 1, 2, 3 }, i => { visited.Add(i); return Guid.Empty; });
			Assert.Equal(new[] { 1, 2, 3 }, visited);
		}

		// ---- SplitTasksByProject ----

		[Fact]
		public void Split_SkipsTasksWithoutTimelog_WithoutReadingProjects()
		{
			var task = new TestTask { Projects = null };
			var rows = Split(new[] { task }, new HashSet<Guid>(), null);
			Assert.Empty(rows);
		}

		[Fact]
		public void Split_SkipsTasksWithNoProjects()
		{
			var task = new TestTask();
			var rows = Split(new[] { task }, new HashSet<Guid> { task.Id }, null);
			Assert.Empty(rows);
		}

		[Fact]
		public void Split_NullProjectList_ThrowsNullReference()
		{
			var task = new TestTask { Projects = null };
			Assert.Throws<NullReferenceException>(() => Split(new[] { task }, new HashSet<Guid> { task.Id }, null));
		}

		[Fact]
		public void Split_NoAccountFilter_SingleProject_OneRow()
		{
			var p = new TestProject { Name = "P1", AccountId = Guid.NewGuid() };
			var task = new TestTask { Projects = { p } };
			var row = Assert.Single(Split(new[] { task }, new HashSet<Guid> { task.Id }, null));
			Assert.Same(task, row.Task);
			Assert.Same(p, row.Project);
		}

		[Fact]
		public void Split_NoAccountFilter_ProjectWithoutAccount_IsAllowed()
		{
			var p = new TestProject { Name = "P1", AccountId = null };
			var task = new TestTask { Projects = { p } };
			var row = Assert.Single(Split(new[] { task }, new HashSet<Guid> { task.Id }, null));
			Assert.Same(p, row.Project);
		}

		[Fact]
		public void Split_NoAccountFilter_DoesNotReadProjectAccount()
		{
			var task = new TestTask { Projects = { new TestProject() } };
			var rows = TimelogReportAggregator.SplitTasksByProject<TestTask, TestProject>(
				new[] { task }, new HashSet<Guid> { task.Id }, null, t => t.Id, t => t.Projects,
				_ => throw new InvalidOperationException("should not be read"));
			Assert.Single(rows);
		}

		[Fact]
		public void Split_AccountFilter_KeepsOnlyMatchingProjects()
		{
			var account = Guid.NewGuid();
			var match = new TestProject { Name = "match", AccountId = account };
			var other = new TestProject { Name = "other", AccountId = Guid.NewGuid() };
			var task = new TestTask { Projects = { other, match } };
			var row = Assert.Single(Split(new[] { task }, new HashSet<Guid> { task.Id }, account));
			Assert.Same(match, row.Project);
		}

		[Fact]
		public void Split_AccountFilter_NoMatchingProject_NoRows()
		{
			var task = new TestTask { Projects = { new TestProject { AccountId = Guid.NewGuid() } } };
			Assert.Empty(Split(new[] { task }, new HashSet<Guid> { task.Id }, Guid.NewGuid()));
		}

		[Fact]
		public void Split_AccountFilter_ProjectWithoutAccount_Throws()
		{
			var account = Guid.NewGuid();
			var task = new TestTask { Projects = { new TestProject { AccountId = null } } };
			var ex = Assert.Throws<ProjectWithoutAccountException>(() => Split(new[] { task }, new HashSet<Guid> { task.Id }, account));
			Assert.Equal("There is a project without an account", ex.Message);
		}

		[Fact]
		public void Split_AccountFilter_ProjectWithoutAccount_ThrowsEvenAfterAMatch()
		{
			var account = Guid.NewGuid();
			var task = new TestTask { Projects = { new TestProject { AccountId = account }, new TestProject { AccountId = null } } };
			Assert.Throws<ProjectWithoutAccountException>(() => Split(new[] { task }, new HashSet<Guid> { task.Id }, account));
		}

		[Fact]
		public void Split_AccountFilter_ProjectWithoutAccount_OnTaskWithoutTimelog_DoesNotThrow()
		{
			var task = new TestTask { Projects = { new TestProject { AccountId = null } } };
			Assert.Empty(Split(new[] { task }, new HashSet<Guid>(), Guid.NewGuid()));
		}

		[Fact]
		public void Split_PreservesTaskOrder()
		{
			var tasks = Enumerable.Range(0, 5).Select(i => new TestTask { Projects = { new TestProject { Name = "P" + i } } }).ToList();
			var rows = Split(tasks, new HashSet<Guid>(tasks.Select(t => t.Id)), null);
			Assert.Equal(tasks, rows.Select(r => r.Task).ToList());
		}

		[Fact]
		public void PreservedQuirk_TaskWithMultipleProjects_AllRowsCarryLastProject()
		{
			// Intentionally preserved quirk: the original stored the project on the shared task record,
			// so every split row of a task ends up with the last project instead of its own.
			var p1 = new TestProject { Name = "P1" };
			var p2 = new TestProject { Name = "P2" };
			var p3 = new TestProject { Name = "P3" };
			var task = new TestTask { Projects = { p1, p2, p3 } };
			var rows = Split(new[] { task }, new HashSet<Guid> { task.Id }, null);
			Assert.Equal(3, rows.Count);
			Assert.All(rows, r => { Assert.Same(task, r.Task); Assert.Same(p3, r.Project); });
		}

		[Fact]
		public void PreservedQuirk_AccountFilter_MultipleMatches_AllRowsCarryLastMatchingProject()
		{
			// Intentionally preserved quirk: last *matching* project wins for every row of the task.
			var account = Guid.NewGuid();
			var m1 = new TestProject { Name = "M1", AccountId = account };
			var other = new TestProject { Name = "O", AccountId = Guid.NewGuid() };
			var m2 = new TestProject { Name = "M2", AccountId = account };
			var trailingOther = new TestProject { Name = "O2", AccountId = Guid.NewGuid() };
			var task = new TestTask { Projects = { m1, other, m2, trailingOther } };
			var rows = Split(new[] { task }, new HashSet<Guid> { task.Id }, account);
			Assert.Equal(2, rows.Count);
			Assert.All(rows, r => Assert.Same(m2, r.Project));
		}

		[Fact]
		public void Split_MixedTasks_ProducesExpectedRows()
		{
			var account = Guid.NewGuid();
			var pA = new TestProject { Name = "A", AccountId = account };
			var pB = new TestProject { Name = "B", AccountId = Guid.NewGuid() };
			var t1 = new TestTask { Projects = { pA } };
			var t2 = new TestTask { Projects = { pB } };
			var t3 = new TestTask { Projects = { pA } }; // no timelog
			var t4 = new TestTask(); // no project
			var rows = Split(new[] { t1, t2, t3, t4 }, new HashSet<Guid> { t1.Id, t2.Id, t4.Id }, account);
			var row = Assert.Single(rows);
			Assert.Same(t1, row.Task);
		}

		// ---- SumMinutes ----

		[Fact]
		public void Sum_NoTimelogs_ReturnsZeros()
		{
			var totals = Sum(Array.Empty<TestTimelog>(), Guid.NewGuid());
			Assert.Equal(0m, totals.BillableMinutes);
			Assert.Equal(0m, totals.NonBillableMinutes);
		}

		[Fact]
		public void Sum_SplitsBillableAndNonBillable_ForMatchingTaskOnly()
		{
			var task = Guid.NewGuid();
			var other = Guid.NewGuid();
			var timelogs = new[]
			{
				new TestTimelog { TaskId = task, IsBillable = true, Minutes = 30 },
				new TestTimelog { TaskId = task, IsBillable = false, Minutes = 15 },
				new TestTimelog { TaskId = other, IsBillable = true, Minutes = 1000 },
				new TestTimelog { TaskId = task, IsBillable = true, Minutes = 45.5m },
				new TestTimelog { TaskId = other, IsBillable = false, Minutes = 999 },
			};
			var totals = Sum(timelogs, task);
			Assert.Equal(75.5m, totals.BillableMinutes);
			Assert.Equal(15m, totals.NonBillableMinutes);
		}

		[Theory]
		[InlineData(true, 10, 10, 0)]
		[InlineData(false, 10, 0, 10)]
		[InlineData(true, 0, 0, 0)]
		[InlineData(false, -5, 0, -5)]
		public void Sum_SingleTimelog(bool billable, int minutes, int expectedBillable, int expectedNonBillable)
		{
			var task = Guid.NewGuid();
			var totals = Sum(new[] { new TestTimelog { TaskId = task, IsBillable = billable, Minutes = minutes } }, task);
			Assert.Equal((decimal)expectedBillable, totals.BillableMinutes);
			Assert.Equal((decimal)expectedNonBillable, totals.NonBillableMinutes);
		}

		[Fact]
		public void Sum_PreservesDecimalScale()
		{
			var task = Guid.NewGuid();
			var totals = Sum(new[] { new TestTimelog { TaskId = task, IsBillable = true, Minutes = 1.50m } }, task);
			Assert.Equal("1.50", totals.BillableMinutes.ToString(System.Globalization.CultureInfo.InvariantCulture));
			Assert.Equal("0", totals.NonBillableMinutes.ToString(System.Globalization.CultureInfo.InvariantCulture));
		}

		[Fact]
		public void Sum_OnlyReadsBillableAndMinutesOfMatchingTimelogs()
		{
			var task = Guid.NewGuid();
			var timelogs = new[]
			{
				new TestTimelog { TaskId = Guid.NewGuid() },
				new TestTimelog { TaskId = task, IsBillable = true, Minutes = 5 },
			};
			var totals = TimelogReportAggregator.SumMinutes(timelogs, task, t => t.TaskId,
				t => t.TaskId == task ? t.IsBillable : throw new InvalidOperationException(),
				t => t.TaskId == task ? t.Minutes : throw new InvalidOperationException());
			Assert.Equal(5m, totals.BillableMinutes);
		}

		[Fact]
		public void PreservedQuirk_SplitRowsOfSameTask_EachGetFullTaskMinutes()
		{
			// Intentionally preserved quirk: minutes are summed per task, so a task split over N projects
			// reports its full minutes on each of the N rows (double counting across projects).
			var task = new TestTask { Projects = { new TestProject { Name = "P1" }, new TestProject { Name = "P2" } } };
			var timelogs = new[] { new TestTimelog { TaskId = task.Id, IsBillable = true, Minutes = 60 } };
			var rows = Split(new[] { task }, TimelogReportAggregator.CollectTaskIds(timelogs, t => t.TaskId), null);
			Assert.Equal(2, rows.Count);
			Assert.All(rows, r => Assert.Equal(60m, Sum(timelogs, r.Task.Id).BillableMinutes));
		}
	}
}
