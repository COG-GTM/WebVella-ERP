using System;
using System.Collections.Generic;
using WebVella.Erp.Plugins.Project.Domain.Tasks;
using Xunit;

namespace WebVella.Erp.Plugins.Project.Domain.Tests.Tasks
{
	public class TaskPriorityStyleResolverTests
	{
		private static readonly List<TaskPriorityOption> Options = new List<TaskPriorityOption>
		{
			new TaskPriorityOption("1", "fas fa-fw fa-arrow-down", "#4CAF50"),
			new TaskPriorityOption("2", "fas fa-fw fa-arrows-alt-h", "#2196F3"),
			new TaskPriorityOption("3", "fas fa-fw fa-arrow-up", "#F44336"),
		};

		[Theory]
		[InlineData("1", "fas fa-fw fa-arrow-down", "#4CAF50")]
		[InlineData("2", "fas fa-fw fa-arrows-alt-h", "#2196F3")]
		[InlineData("3", "fas fa-fw fa-arrow-up", "#F44336")]
		public void KnownPriority_ReturnsOptionIconAndColor(string priority, string expectedIcon, string expectedColor)
		{
			var style = TaskPriorityStyleResolver.Resolve(priority, Options);

			Assert.Equal(expectedIcon, style.IconClass);
			Assert.Equal(expectedColor, style.Color);
		}

		[Theory]
		[InlineData("4")]
		[InlineData("0")]
		[InlineData("")]
		[InlineData(" 1")]
		[InlineData(null)]
		public void UnknownPriority_LeavesEmptyIconAndWhiteColor(string priority)
		{
			var style = TaskPriorityStyleResolver.Resolve(priority, Options);

			Assert.Equal("", style.IconClass);
			Assert.Equal("#fff", style.Color);
		}

		[Fact]
		public void Defaults_AreEmptyIconAndWhite()
		{
			Assert.Equal("", TaskPriorityStyleResolver.DefaultIconClass);
			Assert.Equal("#fff", TaskPriorityStyleResolver.DefaultColor);
		}

		[Fact]
		public void NoOptions_ReturnsDefaults()
		{
			var style = TaskPriorityStyleResolver.Resolve("1", new List<TaskPriorityOption>());

			Assert.Equal("", style.IconClass);
			Assert.Equal("#fff", style.Color);
		}

		[Fact]
		public void NullOptions_ThrowsArgumentNullException()
		{
			Assert.Throws<ArgumentNullException>(() => TaskPriorityStyleResolver.Resolve("1", null));
		}

		[Fact]
		public void PreservedQuirk_MatchIsCaseSensitiveOrdinal()
		{
			var options = new List<TaskPriorityOption> { new TaskPriorityOption("high", "icon-high", "#f00") };

			var style = TaskPriorityStyleResolver.Resolve("HIGH", options);

			Assert.Equal("", style.IconClass);
			Assert.Equal("#fff", style.Color);
		}

		[Fact]
		public void PreservedQuirk_DuplicateValues_FirstOptionWins()
		{
			var options = new List<TaskPriorityOption>
			{
				new TaskPriorityOption("1", "first", "#111"),
				new TaskPriorityOption("1", "second", "#222"),
			};

			var style = TaskPriorityStyleResolver.Resolve("1", options);

			Assert.Equal("first", style.IconClass);
			Assert.Equal("#111", style.Color);
		}

		[Fact]
		public void PreservedQuirk_MatchedOptionWithNullIconAndColor_OverridesDefaultsWithNull()
		{
			var options = new List<TaskPriorityOption> { new TaskPriorityOption("1", null, null) };

			var style = TaskPriorityStyleResolver.Resolve("1", options);

			Assert.Null(style.IconClass);
			Assert.Null(style.Color);
		}

		[Fact]
		public void PreservedQuirk_NullPriority_MatchesOptionWithNullValue()
		{
			var options = new List<TaskPriorityOption> { new TaskPriorityOption(null, "null-icon", "#000") };

			var style = TaskPriorityStyleResolver.Resolve(null, options);

			Assert.Equal("null-icon", style.IconClass);
			Assert.Equal("#000", style.Color);
		}
	}
}
