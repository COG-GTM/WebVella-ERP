using System;
using WebVella.Erp.Plugins.Project.Domain.Reports;
using Xunit;

namespace WebVella.Erp.Plugins.Project.Domain.Tests.Reports
{
	public class MonthDateWindowTests
	{
		[Theory]
		[InlineData(2024, 1, 31)]
		[InlineData(2024, 2, 29)]
		[InlineData(2023, 2, 28)]
		[InlineData(1900, 2, 28)]
		[InlineData(2000, 2, 29)]
		[InlineData(2024, 4, 30)]
		[InlineData(2024, 6, 30)]
		[InlineData(2024, 9, 30)]
		[InlineData(2024, 11, 30)]
		[InlineData(2024, 12, 31)]
		[InlineData(1, 1, 31)]
		[InlineData(9999, 12, 31)]
		public void For_ReturnsFirstAndLastDayOfMonth(int year, int month, int lastDay)
		{
			var window = MonthDateWindow.For(year, month);
			Assert.Equal(new DateTime(year, month, 1), window.FromDate);
			Assert.Equal(new DateTime(year, month, lastDay), window.ToDate);
		}

		[Fact]
		public void PreservedQuirk_ToDateIsMidnightOfLastDay_NotEndOfDay()
		{
			// Intentionally preserved quirk: the upper bound is 00:00 of the last day, so with the
			// inclusive "logged_on <= to_date" filter anything logged later that day is excluded.
			var window = MonthDateWindow.For(2024, 3);
			Assert.Equal(TimeSpan.Zero, window.ToDate.TimeOfDay);
			Assert.Equal(TimeSpan.Zero, window.FromDate.TimeOfDay);
			Assert.True(new DateTime(2024, 3, 31, 0, 0, 1) > window.ToDate);
		}

		[Fact]
		public void Dates_AreUnspecifiedKind_NoTimezoneConversion()
		{
			var window = MonthDateWindow.For(2024, 7);
			Assert.Equal(DateTimeKind.Unspecified, window.FromDate.Kind);
			Assert.Equal(DateTimeKind.Unspecified, window.ToDate.Kind);
		}

		[Theory]
		[InlineData(10000, 1)]
		[InlineData(0, 1)]
		[InlineData(2024, 0)]
		[InlineData(2024, 13)]
		public void OutOfRangeValues_ThrowArgumentOutOfRange(int year, int month)
		{
			Assert.Throws<ArgumentOutOfRangeException>(() => MonthDateWindow.For(year, month));
		}

		[Fact]
		public void PreservedQuirk_YearPassingValidationButAbove9999_ThrowsWhenBuildingWindow()
		{
			// Intentionally preserved quirk: validation accepts year 10000 and the date construction then throws.
			Assert.Empty(TimelogReportValidator.Validate(10000, 1, null, _ => true));
			Assert.Throws<ArgumentOutOfRangeException>(() => MonthDateWindow.For(10000, 1));
		}
	}
}
