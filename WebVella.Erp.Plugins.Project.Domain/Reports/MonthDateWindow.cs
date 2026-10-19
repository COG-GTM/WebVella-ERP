using System;

namespace WebVella.Erp.Plugins.Project.Domain.Reports
{
	public readonly record struct MonthDateWindow(DateTime FromDate, DateTime ToDate)
	{
		/// <summary>
		/// FromDate is midnight of the first day of the month and ToDate is midnight (not end of day)
		/// of the last day of the month. Both are DateTimeKind.Unspecified; no timezone conversion is applied.
		/// </summary>
		public static MonthDateWindow For(int year, int month)
		{
			DateTime fromDate = new DateTime(year, month, 1);
			DateTime toDate = new DateTime(year, month, DateTime.DaysInMonth(year, month));
			return new MonthDateWindow(fromDate, toDate);
		}
	}
}
