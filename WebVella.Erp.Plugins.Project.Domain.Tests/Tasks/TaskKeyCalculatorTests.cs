using System;
using System.Globalization;
using WebVella.Erp.Plugins.Project.Domain.Tasks;
using Xunit;

namespace WebVella.Erp.Plugins.Project.Domain.Tests.Tasks
{
	public class TaskKeyCalculatorTests
	{
		private static T WithCulture<T>(string culture, Func<T> action)
		{
			var previous = CultureInfo.CurrentCulture;
			try
			{
				CultureInfo.CurrentCulture = new CultureInfo(culture);
				return action();
			}
			finally
			{
				CultureInfo.CurrentCulture = previous;
			}
		}

		[Theory]
		[InlineData("PRJ", 1, "PRJ-1")]
		[InlineData("PRJ", 42, "PRJ-42")]
		[InlineData("A", 999, "A-999")]
		[InlineData("", 7, "-7")]
		[InlineData("PRJ", 0, "PRJ-0")]
		public void CalculateKey_AbbrDashNumber(string abbr, int number, string expected)
		{
			Assert.Equal(expected, WithCulture("en-US", () => TaskKeyCalculator.CalculateKey(abbr, number)));
		}

		[Theory]
		[InlineData(1000, "PRJ-1,000")]
		[InlineData(1234567, "PRJ-1,234,567")]
		public void PreservedQuirk_KeyUsesN0_IncludesThousandsSeparator(int number, string expected)
		{
			Assert.Equal(expected, WithCulture("en-US", () => TaskKeyCalculator.CalculateKey("PRJ", number)));
		}

		[Fact]
		public void PreservedQuirk_KeyFormatting_DependsOnCurrentCulture()
		{
			Assert.Equal("PRJ-1.000", WithCulture("de-DE", () => TaskKeyCalculator.CalculateKey("PRJ", 1000m)));
		}

		[Theory]
		[InlineData("1.4", "PRJ-1")]
		[InlineData("1.6", "PRJ-2")]
		public void PreservedQuirk_FractionalNumbers_AreRoundedByN0(string number, string expected)
		{
			var value = decimal.Parse(number, CultureInfo.InvariantCulture);

			Assert.Equal(expected, WithCulture("en-US", () => TaskKeyCalculator.CalculateKey("PRJ", value)));
		}

		[Fact]
		public void PreservedQuirk_NegativeNumber()
		{
			Assert.Equal("PRJ--5", WithCulture("en-US", () => TaskKeyCalculator.CalculateKey("PRJ", -5m)));
		}

		[Fact]
		public void PreservedQuirk_NullAbbr_ProducesLeadingDash()
		{
			Assert.Equal("-3", WithCulture("en-US", () => TaskKeyCalculator.CalculateKey(null, 3m)));
		}

		[Fact]
		public void Calculate_WithProject_UsesAbbrIdAndOwner()
		{
			var projectId = Guid.NewGuid();
			var ownerId = Guid.NewGuid();

			var result = WithCulture("en-US", () => TaskKeyCalculator.Calculate("Fix bug", 12m, new TaskProjectInfo(projectId, ownerId, true, "WV")));

			Assert.Equal("WV-12", result.Key);
			Assert.Equal("Fix bug", result.Subject);
			Assert.Equal(projectId, result.ProjectId);
			Assert.Equal(ownerId, result.ProjectOwnerId);
		}

		[Fact]
		public void Calculate_WithoutProject_EmptyAbbrEmptyIdNullOwner()
		{
			var result = WithCulture("en-US", () => TaskKeyCalculator.Calculate("Subject", 5m, null));

			Assert.Equal("-5", result.Key);
			Assert.Equal("Subject", result.Subject);
			Assert.Equal(Guid.Empty, result.ProjectId);
			Assert.Null(result.ProjectOwnerId);
		}

		[Fact]
		public void Calculate_ProjectWithoutAbbrField_UsesEmptyAbbrButKeepsIdAndOwner()
		{
			var projectId = Guid.NewGuid();

			var result = WithCulture("en-US", () => TaskKeyCalculator.Calculate("S", 8m, new TaskProjectInfo(projectId, null, false, "IGNORED")));

			Assert.Equal("-8", result.Key);
			Assert.Equal(projectId, result.ProjectId);
			Assert.Null(result.ProjectOwnerId);
		}

		[Fact]
		public void PreservedQuirk_ProjectWithNullAbbrValue_KeyStartsWithDash()
		{
			var result = WithCulture("en-US", () => TaskKeyCalculator.Calculate("S", 9m, new TaskProjectInfo(Guid.NewGuid(), null, true, null)));

			Assert.Equal("-9", result.Key);
		}

		[Fact]
		public void Calculate_NullSubject_PassesThrough()
		{
			Assert.Null(TaskKeyCalculator.Calculate(null, 1m, null).Subject);
		}
	}
}
