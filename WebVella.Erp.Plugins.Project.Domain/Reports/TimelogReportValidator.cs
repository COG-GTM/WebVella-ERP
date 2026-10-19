using System;
using System.Collections.Generic;

namespace WebVella.Erp.Plugins.Project.Domain.Reports
{
	public static class TimelogReportValidator
	{
		public const string MonthKey = "month";
		public const string YearKey = "year";
		public const string AccountIdKey = "accountId";
		public const string InvalidMonthMessage = "Invalid month.";
		public const string InvalidYearMessage = "Invalid year.";

		public static string AccountNotFoundMessage(Guid? accountId) => $"Account with ID:{accountId} not found.";

		/// <summary>
		/// Validates the arguments of the monthly timelog report. Errors are returned in the order
		/// month, year, accountId. <paramref name="accountExists"/> is only invoked when an account id
		/// is supplied, and it is invoked even when month/year are already invalid.
		/// </summary>
		public static IReadOnlyList<ReportValidationError> Validate(int year, int month, Guid? accountId, Func<Guid, bool> accountExists)
		{
			if (accountExists == null)
				throw new ArgumentNullException(nameof(accountExists));

			var errors = new List<ReportValidationError>();

			if (month > 12 || month <= 0)
				errors.Add(new ReportValidationError(MonthKey, InvalidMonthMessage));

			if (year <= 0)
				errors.Add(new ReportValidationError(YearKey, InvalidYearMessage));

			if (accountId.HasValue)
			{
				if (!accountExists(accountId.Value))
					errors.Add(new ReportValidationError(AccountIdKey, AccountNotFoundMessage(accountId)));
			}

			return errors;
		}
	}
}
