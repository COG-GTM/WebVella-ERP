using System;
using System.Collections.Generic;
using System.Linq;
using WebVella.Erp.Plugins.Project.Domain.Tasks;
using Xunit;

namespace WebVella.Erp.Plugins.Project.Domain.Tests.Tasks
{
	public class TaskQueueFilterTests
	{
		private static readonly DateTime Now = new DateTime(2024, 3, 15, 14, 30, 45, DateTimeKind.Local);
		private static readonly DateTime Today = new DateTime(2024, 3, 15, 0, 0, 0, DateTimeKind.Local);
		private static readonly DateTime Tomorrow = new DateTime(2024, 3, 16, 0, 0, 0, DateTimeKind.Local);
		private static readonly Guid ProjectId = new Guid("11111111-1111-1111-1111-111111111111");
		private static readonly Guid UserId = new Guid("22222222-2222-2222-2222-222222222222");

		[Fact]
		public void EnumValues_MirrorTasksDueType()
		{
			Assert.Equal(0, (int)TaskQueueDueType.All);
			Assert.Equal(1, (int)TaskQueueDueType.EndTimeOverdue);
			Assert.Equal(2, (int)TaskQueueDueType.EndTimeDueToday);
			Assert.Equal(3, (int)TaskQueueDueType.EndTimeNotDue);
			Assert.Equal(4, (int)TaskQueueDueType.StartTimeDue);
			Assert.Equal(5, (int)TaskQueueDueType.StartTimeNotDue);
		}

		[Fact]
		public void Build_DateWindow_IsTodayMidnightToTomorrowMidnight()
		{
			var plan = TaskQueueFilter.Build(TaskQueueDueType.All, null, null, Now);

			Assert.Equal(Today, plan.CurrentDateStart);
			Assert.Equal(Tomorrow, plan.CurrentDateEnd);
			Assert.Equal(DateTimeKind.Local, plan.CurrentDateStart.Kind);
		}

		[Theory]
		[InlineData(0, 0, 0)]
		[InlineData(23, 59, 59)]
		[InlineData(12, 0, 0)]
		public void Build_NowIsInjectable_AnyTimeOfDayGivesSameWindow(int hour, int minute, int second)
		{
			var plan = TaskQueueFilter.Build(TaskQueueDueType.All, null, null, new DateTime(2024, 3, 15, hour, minute, second));

			Assert.Equal(new DateTime(2024, 3, 15), plan.CurrentDateStart);
			Assert.Equal(new DateTime(2024, 3, 16), plan.CurrentDateEnd);
		}

		[Fact]
		public void Build_WindowCrossesMonthAndYearBoundaries()
		{
			var plan = TaskQueueFilter.Build(TaskQueueDueType.All, null, null, new DateTime(2023, 12, 31, 18, 0, 0));

			Assert.Equal(new DateTime(2023, 12, 31), plan.CurrentDateStart);
			Assert.Equal(new DateTime(2024, 1, 1), plan.CurrentDateEnd);
		}

		[Fact]
		public void Build_LeapDay()
		{
			var plan = TaskQueueFilter.Build(TaskQueueDueType.All, null, null, new DateTime(2024, 2, 28, 9, 0, 0));

			Assert.Equal(new DateTime(2024, 2, 29), plan.CurrentDateEnd);
		}

		[Fact]
		public void Build_DateParametersAlwaysFirst()
		{
			var plan = TaskQueueFilter.Build(TaskQueueDueType.EndTimeOverdue, ProjectId, UserId, Now);

			Assert.Equal("currentDateStart", plan.Parameters[0].Name);
			Assert.Equal(Today, plan.Parameters[0].Value);
			Assert.Equal("currentDateEnd", plan.Parameters[1].Name);
			Assert.Equal(Tomorrow, plan.Parameters[1].Value);
		}

		[Theory]
		[InlineData(TaskQueueDueType.StartTimeDue, "(start_time < @currentDateEnd OR start_time = null)")]
		[InlineData(TaskQueueDueType.StartTimeNotDue, "start_time > @currentDateEnd")]
		[InlineData(TaskQueueDueType.EndTimeOverdue, "end_time < @currentDateStart")]
		[InlineData(TaskQueueDueType.EndTimeDueToday, "(end_time >= @currentDateStart AND end_time < @currentDateEnd)")]
		[InlineData(TaskQueueDueType.EndTimeNotDue, "(end_time >= @currentDateEnd OR end_time = null)")]
		public void Build_DueTypeAddsExactEqlFilter(TaskQueueDueType type, string expectedFilter)
		{
			var plan = TaskQueueFilter.Build(type, null, null, Now);

			Assert.Equal(new[] { expectedFilter }, plan.WhereFilters);
			Assert.Equal(expectedFilter, plan.DueWindow.EqlFilter);
		}

		[Theory]
		[InlineData(TaskQueueDueType.StartTimeDue, "start_time")]
		[InlineData(TaskQueueDueType.StartTimeNotDue, "start_time")]
		[InlineData(TaskQueueDueType.EndTimeOverdue, "end_time")]
		[InlineData(TaskQueueDueType.EndTimeDueToday, "end_time")]
		[InlineData(TaskQueueDueType.EndTimeNotDue, "end_time")]
		public void Build_DueWindowField(TaskQueueDueType type, string expectedField)
		{
			Assert.Equal(expectedField, TaskQueueFilter.Build(type, null, null, Now).DueWindow.Field);
		}

		[Fact]
		public void Build_All_HasNoDueWindowNoFiltersNoStatusExclusionNoOrder()
		{
			var plan = TaskQueueFilter.Build(TaskQueueDueType.All, null, null, Now);

			Assert.Null(plan.DueWindow);
			Assert.Empty(plan.WhereFilters);
			Assert.Equal(2, plan.Parameters.Count);
			Assert.False(plan.ExcludeClosedStatuses);
			Assert.True(plan.IsKnownDueType);
			Assert.Null(plan.OrderByClause);
		}

		[Theory]
		[InlineData(TaskQueueDueType.EndTimeOverdue)]
		[InlineData(TaskQueueDueType.EndTimeDueToday)]
		[InlineData(TaskQueueDueType.EndTimeNotDue)]
		[InlineData(TaskQueueDueType.StartTimeDue)]
		[InlineData(TaskQueueDueType.StartTimeNotDue)]
		public void Build_NonAllTypes_ExcludeClosedStatuses(TaskQueueDueType type)
		{
			Assert.True(TaskQueueFilter.Build(type, null, null, Now).ExcludeClosedStatuses);
		}

		[Theory]
		[InlineData(TaskQueueDueType.EndTimeOverdue, " ORDER BY end_time ASC, priority DESC")]
		[InlineData(TaskQueueDueType.EndTimeNotDue, " ORDER BY end_time ASC, priority DESC")]
		[InlineData(TaskQueueDueType.StartTimeDue, " ORDER BY end_time ASC, priority DESC")]
		[InlineData(TaskQueueDueType.StartTimeNotDue, " ORDER BY end_time ASC, priority DESC")]
		public void Build_OrderByClause(TaskQueueDueType type, string expected)
		{
			var plan = TaskQueueFilter.Build(type, null, null, Now);

			Assert.True(plan.IsKnownDueType);
			Assert.Equal(expected, plan.OrderByClause);
		}

		[Fact]
		public void PreservedQuirk_EndTimeDueToday_OrdersByPriorityOnly()
		{
			Assert.Equal(" ORDER BY priority DESC", TaskQueueFilter.Build(TaskQueueDueType.EndTimeDueToday, null, null, Now).OrderByClause);
		}

		[Theory]
		[InlineData(6)]
		[InlineData(-1)]
		[InlineData(100)]
		public void Build_UnknownType_IsNotKnownAndHasNoWindowOrOrder(int raw)
		{
			var plan = TaskQueueFilter.Build((TaskQueueDueType)raw, null, null, Now);

			Assert.False(plan.IsKnownDueType);
			Assert.Null(plan.DueWindow);
			Assert.Null(plan.OrderByClause);
			Assert.Empty(plan.WhereFilters);
		}

		[Fact]
		public void Build_UnknownTypeMessage_IsExact()
		{
			Assert.Equal("Unknown TasksDueType", TaskQueueFilter.UnknownDueTypeMessage);
		}

		[Fact]
		public void PreservedQuirk_UnknownType_StillExcludesClosedStatusesAndAddsProjectUserFilters_BeforeServiceThrows()
		{
			// The service loads task statuses (a DB call) before throwing "Unknown TasksDueType".
			var plan = TaskQueueFilter.Build((TaskQueueDueType)42, ProjectId, UserId, Now);

			Assert.True(plan.ExcludeClosedStatuses);
			Assert.Equal(new[] { "$project_nn_task.id = @projectId AND owner_id = @userId" }, plan.WhereFilters);
			Assert.Equal(4, plan.Parameters.Count);
		}

		[Theory]
		[InlineData(0, false)]
		[InlineData(6, false)]
		public void TryGetOrderByClause_All_And_Unknown(int raw, bool expectedClause)
		{
			var known = TaskQueueFilter.TryGetOrderByClause((TaskQueueDueType)raw, out var clause);

			Assert.Equal(raw == 0, known);
			Assert.Equal(expectedClause, clause != null);
		}

		[Fact]
		public void Build_ProjectAndUser_CombinedFilterAndBothParams()
		{
			var plan = TaskQueueFilter.Build(TaskQueueDueType.All, ProjectId, UserId, Now);

			Assert.Equal(new[] { "$project_nn_task.id = @projectId AND owner_id = @userId" }, plan.WhereFilters);
			Assert.Equal(new[] { "currentDateStart", "currentDateEnd", "projectId", "userId" }, plan.Parameters.Select(x => x.Name));
			Assert.Equal(ProjectId, plan.Parameters[2].Value);
			Assert.Equal(UserId, plan.Parameters[3].Value);
		}

		[Fact]
		public void Build_ProjectOnly()
		{
			var plan = TaskQueueFilter.Build(TaskQueueDueType.All, ProjectId, null, Now);

			Assert.Equal(new[] { "$project_nn_task.id = @projectId" }, plan.WhereFilters);
			Assert.Equal(new[] { "currentDateStart", "currentDateEnd", "projectId" }, plan.Parameters.Select(x => x.Name));
			Assert.Equal(ProjectId, plan.Parameters[2].Value);
		}

		[Fact]
		public void Build_UserOnly()
		{
			var plan = TaskQueueFilter.Build(TaskQueueDueType.All, null, UserId, Now);

			Assert.Equal(new[] { "owner_id = @userId" }, plan.WhereFilters);
			Assert.Equal(new[] { "currentDateStart", "currentDateEnd", "userId" }, plan.Parameters.Select(x => x.Name));
			Assert.Equal(UserId, plan.Parameters[2].Value);
		}

		[Fact]
		public void Build_DueFilterComesBeforeProjectUserFilter()
		{
			var plan = TaskQueueFilter.Build(TaskQueueDueType.EndTimeOverdue, ProjectId, UserId, Now);

			Assert.Equal(new[]
			{
				"end_time < @currentDateStart",
				"$project_nn_task.id = @projectId AND owner_id = @userId",
			}, plan.WhereFilters);
		}

		[Fact]
		public void Build_EmptyGuidsAreStillTreatedAsProvided()
		{
			var plan = TaskQueueFilter.Build(TaskQueueDueType.All, Guid.Empty, Guid.Empty, Now);

			Assert.Equal(new[] { "$project_nn_task.id = @projectId AND owner_id = @userId" }, plan.WhereFilters);
			Assert.Equal(Guid.Empty, plan.Parameters[2].Value);
		}

		// ---- date window boundaries (in-memory semantics of the EQL filters) ----

		public static IEnumerable<object[]> WindowCases()
		{
			var today = new DateTime(2024, 3, 15);
			var tomorrow = today.AddDays(1);
			var tick = TimeSpan.FromTicks(1);
			// StartTimeDue: start_time < tomorrow OR null
			yield return new object[] { TaskQueueDueType.StartTimeDue, null, true };
			yield return new object[] { TaskQueueDueType.StartTimeDue, today.AddYears(-1), true };
			yield return new object[] { TaskQueueDueType.StartTimeDue, today, true };
			yield return new object[] { TaskQueueDueType.StartTimeDue, tomorrow - tick, true };
			yield return new object[] { TaskQueueDueType.StartTimeDue, tomorrow, false };
			yield return new object[] { TaskQueueDueType.StartTimeDue, tomorrow + tick, false };
			// StartTimeNotDue: start_time > tomorrow
			yield return new object[] { TaskQueueDueType.StartTimeNotDue, null, false };
			yield return new object[] { TaskQueueDueType.StartTimeNotDue, today, false };
			yield return new object[] { TaskQueueDueType.StartTimeNotDue, tomorrow, false };
			yield return new object[] { TaskQueueDueType.StartTimeNotDue, tomorrow + tick, true };
			yield return new object[] { TaskQueueDueType.StartTimeNotDue, tomorrow.AddYears(1), true };
			// EndTimeOverdue: end_time < today
			yield return new object[] { TaskQueueDueType.EndTimeOverdue, null, false };
			yield return new object[] { TaskQueueDueType.EndTimeOverdue, today - tick, true };
			yield return new object[] { TaskQueueDueType.EndTimeOverdue, today, false };
			yield return new object[] { TaskQueueDueType.EndTimeOverdue, today.AddHours(12), false };
			// EndTimeDueToday: today <= end_time < tomorrow
			yield return new object[] { TaskQueueDueType.EndTimeDueToday, null, false };
			yield return new object[] { TaskQueueDueType.EndTimeDueToday, today - tick, false };
			yield return new object[] { TaskQueueDueType.EndTimeDueToday, today, true };
			yield return new object[] { TaskQueueDueType.EndTimeDueToday, today.AddHours(23).AddMinutes(59), true };
			yield return new object[] { TaskQueueDueType.EndTimeDueToday, tomorrow - tick, true };
			yield return new object[] { TaskQueueDueType.EndTimeDueToday, tomorrow, false };
			// EndTimeNotDue: end_time >= tomorrow OR null
			yield return new object[] { TaskQueueDueType.EndTimeNotDue, null, true };
			yield return new object[] { TaskQueueDueType.EndTimeNotDue, tomorrow - tick, false };
			yield return new object[] { TaskQueueDueType.EndTimeNotDue, tomorrow, true };
			yield return new object[] { TaskQueueDueType.EndTimeNotDue, tomorrow.AddDays(10), true };
		}

		[Theory]
		[MemberData(nameof(WindowCases))]
		public void DueWindow_Boundaries(TaskQueueDueType type, DateTime? value, bool expected)
		{
			var plan = TaskQueueFilter.Build(type, null, null, new DateTime(2024, 3, 15, 10, 0, 0));

			Assert.Equal(expected, plan.DueWindow.Contains(value));
		}

		[Fact]
		public void DueWindow_EndTimeTypesPartitionAllNonNullEndTimes()
		{
			var plan = TaskQueueFilter.Build(TaskQueueDueType.All, null, null, Now);
			var overdue = TaskQueueFilter.GetDueWindow(TaskQueueDueType.EndTimeOverdue, plan.CurrentDateStart, plan.CurrentDateEnd);
			var today = TaskQueueFilter.GetDueWindow(TaskQueueDueType.EndTimeDueToday, plan.CurrentDateStart, plan.CurrentDateEnd);
			var notDue = TaskQueueFilter.GetDueWindow(TaskQueueDueType.EndTimeNotDue, plan.CurrentDateStart, plan.CurrentDateEnd);

			foreach (var v in new[] { Today.AddDays(-1), Today.AddTicks(-1), Today, Today.AddHours(5), Tomorrow.AddTicks(-1), Tomorrow, Tomorrow.AddDays(3) })
			{
				var hits = new[] { overdue.Contains(v), today.Contains(v), notDue.Contains(v) }.Count(x => x);
				Assert.Equal(1, hits);
			}
		}

		[Fact]
		public void PreservedQuirk_StartTimeExactlyTomorrowMidnight_IsNeitherDueNorNotDue()
		{
			var plan = TaskQueueFilter.Build(TaskQueueDueType.All, null, null, Now);
			var due = TaskQueueFilter.GetDueWindow(TaskQueueDueType.StartTimeDue, plan.CurrentDateStart, plan.CurrentDateEnd);
			var notDue = TaskQueueFilter.GetDueWindow(TaskQueueDueType.StartTimeNotDue, plan.CurrentDateStart, plan.CurrentDateEnd);

			Assert.False(due.Contains(Tomorrow));
			Assert.False(notDue.Contains(Tomorrow));
		}

		[Fact]
		public void PreservedQuirk_NullStartTime_IsDueButNeverNotDue()
		{
			Assert.True(TaskQueueFilter.Build(TaskQueueDueType.StartTimeDue, null, null, Now).DueWindow.Contains(null));
			Assert.False(TaskQueueFilter.Build(TaskQueueDueType.StartTimeNotDue, null, null, Now).DueWindow.Contains(null));
		}

		[Fact]
		public void PreservedQuirk_NullEndTime_IsNotDueButNeverOverdueOrDueToday()
		{
			Assert.True(TaskQueueFilter.Build(TaskQueueDueType.EndTimeNotDue, null, null, Now).DueWindow.Contains(null));
			Assert.False(TaskQueueFilter.Build(TaskQueueDueType.EndTimeOverdue, null, null, Now).DueWindow.Contains(null));
			Assert.False(TaskQueueFilter.Build(TaskQueueDueType.EndTimeDueToday, null, null, Now).DueWindow.Contains(null));
		}

		[Fact]
		public void PreservedQuirk_StartTimeDue_UsesTomorrowMidnightNotNow()
		{
			// A task starting later today (after "now") is already considered start-time due.
			var plan = TaskQueueFilter.Build(TaskQueueDueType.StartTimeDue, null, null, Now);

			Assert.True(plan.DueWindow.Contains(Now.AddHours(5)));
		}

		[Theory]
		[InlineData(TaskQueueDueType.StartTimeDue, null, false, "2024-03-16", false, true)]
		[InlineData(TaskQueueDueType.StartTimeNotDue, "2024-03-16", false, null, false, false)]
		[InlineData(TaskQueueDueType.EndTimeOverdue, null, false, "2024-03-15", false, false)]
		[InlineData(TaskQueueDueType.EndTimeDueToday, "2024-03-15", true, "2024-03-16", false, false)]
		[InlineData(TaskQueueDueType.EndTimeNotDue, "2024-03-16", true, null, false, true)]
		public void DueWindow_Structure(TaskQueueDueType type, string lower, bool lowerInclusive, string upper, bool upperInclusive, bool includesNull)
		{
			var w = TaskQueueFilter.Build(type, null, null, Now).DueWindow;

			Assert.Equal(lower == null ? (DateTime?)null : DateTime.Parse(lower), w.LowerBound);
			Assert.Equal(lowerInclusive, w.LowerInclusive);
			Assert.Equal(upper == null ? (DateTime?)null : DateTime.Parse(upper), w.UpperBound);
			Assert.Equal(upperInclusive, w.UpperInclusive);
			Assert.Equal(includesNull, w.IncludesNull);
		}

		// ---- closed status exclusions ----

		[Fact]
		public void ClosedStatusExclusions_OnlyClosedStatuses_NumberedFromOne()
		{
			var a = Guid.NewGuid();
			var b = Guid.NewGuid();
			var c = Guid.NewGuid();
			var result = TaskQueueFilter.BuildClosedStatusExclusions(new[]
			{
				new TaskStatusInfo(a, true),
				new TaskStatusInfo(b, false),
				new TaskStatusInfo(c, true),
			});

			Assert.Equal(2, result.Count);
			Assert.Equal("status_id <> @status1", result[0].Filter);
			Assert.Equal("status1", result[0].Parameter.Name);
			Assert.Equal(a, result[0].Parameter.Value);
			Assert.Equal("status_id <> @status2", result[1].Filter);
			Assert.Equal("status2", result[1].Parameter.Name);
			Assert.Equal(c, result[1].Parameter.Value);
		}

		[Fact]
		public void ClosedStatusExclusions_DuplicateClosedIds_AreDeduplicatedKeepingFirstOrder()
		{
			var a = Guid.NewGuid();
			var b = Guid.NewGuid();
			var result = TaskQueueFilter.BuildClosedStatusExclusions(new[]
			{
				new TaskStatusInfo(a, true),
				new TaskStatusInfo(b, true),
				new TaskStatusInfo(a, true),
			});

			Assert.Equal(new object[] { a, b }, result.Select(x => x.Parameter.Value));
			Assert.Equal(new[] { "status1", "status2" }, result.Select(x => x.Parameter.Name));
		}

		[Fact]
		public void ClosedStatusExclusions_NoClosedStatuses_Empty()
		{
			Assert.Empty(TaskQueueFilter.BuildClosedStatusExclusions(new[] { new TaskStatusInfo(Guid.NewGuid(), false) }));
			Assert.Empty(TaskQueueFilter.BuildClosedStatusExclusions(new TaskStatusInfo[0]));
		}
	}
}
