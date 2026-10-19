using System;
using System.Collections.Generic;
using System.Linq;
using WebVella.Erp.Plugins.Project.Domain.Reports;
using Xunit;

namespace WebVella.Erp.Plugins.Project.Domain.Tests.Reports
{
	public class TimelogReportValidatorTests
	{
		private static readonly Func<Guid, bool> AnyAccountExists = _ => true;
		private static readonly Func<Guid, bool> NoAccountExists = _ => false;

		[Theory]
		[InlineData(1)]
		[InlineData(2)]
		[InlineData(6)]
		[InlineData(11)]
		[InlineData(12)]
		public void ValidMonth_ProducesNoError(int month)
		{
			var errors = TimelogReportValidator.Validate(2024, month, null, AnyAccountExists);
			Assert.Empty(errors);
		}

		[Theory]
		[InlineData(0)]
		[InlineData(-1)]
		[InlineData(13)]
		[InlineData(100)]
		[InlineData(int.MinValue)]
		[InlineData(int.MaxValue)]
		public void InvalidMonth_ProducesMonthError(int month)
		{
			var errors = TimelogReportValidator.Validate(2024, month, null, AnyAccountExists);
			var error = Assert.Single(errors);
			Assert.Equal("month", error.Key);
			Assert.Equal("Invalid month.", error.Message);
		}

		[Theory]
		[InlineData(1)]
		[InlineData(2024)]
		[InlineData(9999)]
		public void ValidYear_ProducesNoError(int year)
		{
			var errors = TimelogReportValidator.Validate(year, 5, null, AnyAccountExists);
			Assert.Empty(errors);
		}

		[Theory]
		[InlineData(0)]
		[InlineData(-1)]
		[InlineData(int.MinValue)]
		public void InvalidYear_ProducesYearError(int year)
		{
			var errors = TimelogReportValidator.Validate(year, 5, null, AnyAccountExists);
			var error = Assert.Single(errors);
			Assert.Equal("year", error.Key);
			Assert.Equal("Invalid year.", error.Message);
		}

		[Theory]
		[InlineData(10000)]
		[InlineData(int.MaxValue)]
		public void PreservedQuirk_YearAbove9999_PassesValidation(int year)
		{
			// Intentionally preserved quirk: there is no upper bound on year, although DateTime only supports 1..9999.
			var errors = TimelogReportValidator.Validate(year, 5, null, AnyAccountExists);
			Assert.Empty(errors);
		}

		[Fact]
		public void NoAccountId_DoesNotQueryAccount()
		{
			var calls = 0;
			var errors = TimelogReportValidator.Validate(2024, 5, null, _ => { calls++; return false; });
			Assert.Empty(errors);
			Assert.Equal(0, calls);
		}

		[Fact]
		public void ExistingAccount_ProducesNoError_AndQueriesWithAccountId()
		{
			var accountId = Guid.NewGuid();
			var queried = new List<Guid>();
			var errors = TimelogReportValidator.Validate(2024, 5, accountId, id => { queried.Add(id); return true; });
			Assert.Empty(errors);
			Assert.Equal(new[] { accountId }, queried);
		}

		[Fact]
		public void MissingAccount_ProducesAccountIdError_WithGuidInMessage()
		{
			var accountId = new Guid("6d3fa3a6-2b4c-4bc4-9f39-6ec1f1f9b7a1");
			var errors = TimelogReportValidator.Validate(2024, 5, accountId, NoAccountExists);
			var error = Assert.Single(errors);
			Assert.Equal("accountId", error.Key);
			Assert.Equal("Account with ID:6d3fa3a6-2b4c-4bc4-9f39-6ec1f1f9b7a1 not found.", error.Message);
		}

		[Fact]
		public void EmptyGuidAccount_IsStillLookedUp()
		{
			var calls = 0;
			var errors = TimelogReportValidator.Validate(2024, 5, Guid.Empty, _ => { calls++; return false; });
			Assert.Equal(1, calls);
			Assert.Equal("Account with ID:00000000-0000-0000-0000-000000000000 not found.", Assert.Single(errors).Message);
		}

		[Fact]
		public void AllInvalid_ErrorsCollectedInOrder_MonthYearAccount()
		{
			var accountId = Guid.NewGuid();
			var errors = TimelogReportValidator.Validate(0, 13, accountId, NoAccountExists);
			Assert.Equal(new[] { "month", "year", "accountId" }, errors.Select(e => e.Key).ToArray());
			Assert.Equal(new[] { "Invalid month.", "Invalid year.", $"Account with ID:{accountId} not found." }, errors.Select(e => e.Message).ToArray());
		}

		[Fact]
		public void AccountIsLookedUp_EvenWhenMonthAndYearInvalid()
		{
			var calls = 0;
			TimelogReportValidator.Validate(-5, -5, Guid.NewGuid(), _ => { calls++; return true; });
			Assert.Equal(1, calls);
		}

		[Fact]
		public void MonthAndYearInvalid_ExistingAccount_ReturnsTwoErrors()
		{
			var errors = TimelogReportValidator.Validate(0, 0, Guid.NewGuid(), AnyAccountExists);
			Assert.Equal(new[] { "month", "year" }, errors.Select(e => e.Key).ToArray());
		}

		[Fact]
		public void AccountNotFoundMessage_FormatsNullAsEmpty()
		{
			Assert.Equal("Account with ID: not found.", TimelogReportValidator.AccountNotFoundMessage(null));
		}

		[Fact]
		public void NullLookupDelegate_Throws()
		{
			Assert.Throws<ArgumentNullException>(() => TimelogReportValidator.Validate(2024, 5, null, null));
		}
	}
}
