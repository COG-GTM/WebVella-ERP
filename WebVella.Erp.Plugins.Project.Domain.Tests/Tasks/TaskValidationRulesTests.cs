using System;
using System.Collections.Generic;
using WebVella.Erp.Plugins.Project.Domain.Tasks;
using Xunit;

namespace WebVella.Erp.Plugins.Project.Domain.Tests.Tasks
{
	public class TaskValidationRulesTests
	{
		// ---- create: project selection ----

		[Fact]
		public void Create_MissingProjectField_ProjectNotSpecified()
		{
			var errors = TaskValidationRules.ValidateProjectSelectionOnCreate(false, 0);

			var error = Assert.Single(errors);
			Assert.Equal("$project_nn_task.id", error.Key);
			Assert.Equal("Project is not specified.", error.Message);
		}

		[Fact]
		public void Create_EmptyProjectList_ProjectNotSpecified()
		{
			var error = Assert.Single(TaskValidationRules.ValidateProjectSelectionOnCreate(true, 0));

			Assert.Equal("$project_nn_task.id", error.Key);
			Assert.Equal("Project is not specified.", error.Message);
		}

		[Fact]
		public void Create_ExactlyOneProject_NoErrors()
		{
			Assert.Empty(TaskValidationRules.ValidateProjectSelectionOnCreate(true, 1));
		}

		[Theory]
		[InlineData(2)]
		[InlineData(3)]
		[InlineData(100)]
		public void Create_MoreThanOneProject_Error(int count)
		{
			var error = Assert.Single(TaskValidationRules.ValidateProjectSelectionOnCreate(true, count));

			Assert.Equal("$project_nn_task.id", error.Key);
			Assert.Equal("More than one project is selected.", error.Message);
		}

		[Theory]
		[InlineData(1)]
		[InlineData(5)]
		public void Create_MissingField_IgnoresCount(int count)
		{
			var error = Assert.Single(TaskValidationRules.ValidateProjectSelectionOnCreate(false, count));

			Assert.Equal("Project is not specified.", error.Message);
		}

		[Fact]
		public void PreservedQuirk_Create_NegativeCountIsAccepted()
		{
			// Unreachable with a real list, but the original only checks == 0 and > 1.
			Assert.Empty(TaskValidationRules.ValidateProjectSelectionOnCreate(true, -1));
		}

		[Fact]
		public void Create_ConstantsAreExact()
		{
			Assert.Equal("$project_nn_task.id", TaskValidationRules.ProjectFieldKey);
			Assert.Equal("Project is not specified.", TaskValidationRules.ProjectNotSpecifiedMessage);
			Assert.Equal("More than one project is selected.", TaskValidationRules.MoreThanOneProjectMessage);
		}

		// ---- update: Guid-or-string parsing ----

		[Fact]
		public void Parse_Guid_ReturnsGuid()
		{
			var id = Guid.NewGuid();

			Assert.Equal(id, TaskValidationRules.ParseGuidOrString(id));
		}

		[Fact]
		public void Parse_GuidString_ReturnsGuid()
		{
			var id = Guid.NewGuid();

			Assert.Equal(id, TaskValidationRules.ParseGuidOrString(id.ToString()));
			Assert.Equal(id, TaskValidationRules.ParseGuidOrString(id.ToString("B")));
		}

		[Fact]
		public void Parse_Null_ReturnsNull()
		{
			Assert.Null(TaskValidationRules.ParseGuidOrString(null));
		}

		[Fact]
		public void PreservedQuirk_Parse_OtherTypes_SilentlyIgnored()
		{
			// e.g. a List<Guid> posted for the NN field leaves newProjectId null, so no project change happens.
			Assert.Null(TaskValidationRules.ParseGuidOrString(new List<Guid> { Guid.NewGuid() }));
			Assert.Null(TaskValidationRules.ParseGuidOrString(42));
		}

		[Theory]
		[InlineData("")]
		[InlineData("not-a-guid")]
		public void Parse_InvalidString_ThrowsFormatException(string value)
		{
			Assert.Throws<FormatException>(() => TaskValidationRules.ParseGuidOrString(value));
		}

		// ---- update: project change rule ----

		[Fact]
		public void ProjectChange_DifferentIds_IsChange()
		{
			Assert.True(TaskValidationRules.IsProjectChange(Guid.NewGuid(), Guid.NewGuid()));
		}

		[Fact]
		public void ProjectChange_SameId_IsNotChange()
		{
			var id = Guid.NewGuid();

			Assert.False(TaskValidationRules.IsProjectChange(id, id));
		}

		[Fact]
		public void ProjectChange_NewIdNull_IsNotChange()
		{
			Assert.False(TaskValidationRules.IsProjectChange(Guid.NewGuid(), null));
		}

		[Fact]
		public void PreservedQuirk_ProjectChange_OldIdNull_IsNotChange()
		{
			// A task without a project can never get one through the update hook.
			Assert.False(TaskValidationRules.IsProjectChange(null, Guid.NewGuid()));
		}

		[Fact]
		public void ProjectChange_BothNull_IsNotChange()
		{
			Assert.False(TaskValidationRules.IsProjectChange(null, null));
		}

		[Fact]
		public void ProjectChange_EmptyGuidVsValue_IsChange()
		{
			Assert.True(TaskValidationRules.IsProjectChange(Guid.Empty, Guid.NewGuid()));
		}

		[Fact]
		public void PreservedQuirk_ProjectChange_KeyIsRecalculatedWithOldProjectAbbr()
		{
			// The service passes the OLD project's abbr (read before the change) to CalculateKey.
			var oldAbbr = "OLD";
			var key = TaskKeyCalculator.CalculateKey(oldAbbr, 5m);

			Assert.StartsWith("OLD-", key);
		}

		// ---- watcher rules ----

		[Fact]
		public void ShouldAddWatcher_NotInList_True()
		{
			Assert.True(TaskValidationRules.ShouldAddWatcher(Guid.NewGuid(), new[] { Guid.NewGuid() }));
		}

		[Fact]
		public void ShouldAddWatcher_AlreadyInList_False()
		{
			var id = Guid.NewGuid();

			Assert.False(TaskValidationRules.ShouldAddWatcher(id, new[] { Guid.NewGuid(), id }));
		}

		[Fact]
		public void ShouldAddWatcher_NullUser_False()
		{
			Assert.False(TaskValidationRules.ShouldAddWatcher(null, new Guid[0]));
		}

		[Fact]
		public void ShouldAddWatcher_EmptyList_True()
		{
			Assert.True(TaskValidationRules.ShouldAddWatcher(Guid.NewGuid(), new Guid[0]));
		}

		[Fact]
		public void InitialWatchers_OwnerCreatorProjectOwner_InOrder()
		{
			var owner = Guid.NewGuid();
			var creator = Guid.NewGuid();
			var lead = Guid.NewGuid();

			Assert.Equal(new[] { owner, creator, lead }, TaskValidationRules.BuildInitialWatchers(owner, creator, lead));
		}

		[Fact]
		public void InitialWatchers_Deduplicated_KeepsFirstPosition()
		{
			var a = Guid.NewGuid();
			var b = Guid.NewGuid();

			Assert.Equal(new[] { a }, TaskValidationRules.BuildInitialWatchers(a, a, a));
			Assert.Equal(new[] { a, b }, TaskValidationRules.BuildInitialWatchers(a, b, a));
			Assert.Equal(new[] { b, a }, TaskValidationRules.BuildInitialWatchers(b, a, b));
		}

		[Fact]
		public void InitialWatchers_NullsSkipped()
		{
			var a = Guid.NewGuid();

			Assert.Empty(TaskValidationRules.BuildInitialWatchers(null, null, null));
			Assert.Equal(new[] { a }, TaskValidationRules.BuildInitialWatchers(null, a, null));
			Assert.Equal(new[] { a }, TaskValidationRules.BuildInitialWatchers(null, null, a));
		}
	}
}
