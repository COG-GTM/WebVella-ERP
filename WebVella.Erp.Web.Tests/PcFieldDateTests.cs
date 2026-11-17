using System;
using WebVella.Erp.Web.Components;
using Xunit;

namespace WebVella.Erp.Web.Tests
{
	public class PcFieldDateTests
	{
		public PcFieldDateTests()
		{
			ErpSettings.TimeZoneName = "FLE Standard Time";
		}

		[Fact]
		public void UtcRecordValue_ShowsTheDatePickedInAppTimeZone()
		{
			//17 Nov picked in the app time zone (UTC+2) is stored as 16 Nov 22:00 UTC
			var stored = new DateTime(2026, 11, 16, 22, 0, 0, DateTimeKind.Utc);

			Assert.Equal(new DateTime(2026, 11, 17), PcFieldDate.ToAppDate(stored).Value.Date);
		}

		[Fact]
		public void UnspecifiedValue_IsAlreadyAppTime()
		{
			var value = new DateTime(2026, 11, 17, 0, 0, 0, DateTimeKind.Unspecified);

			Assert.Equal(value, PcFieldDate.ToAppDate(value));
		}

		[Fact]
		public void StringValue_IsParsed()
		{
			Assert.Equal(new DateTime(2026, 11, 17), PcFieldDate.ToAppDate("2026-11-16T22:00:00Z").Value.Date);
		}

		[Fact]
		public void EmptyValues_GiveNull()
		{
			Assert.Null(PcFieldDate.ToAppDate(null));
			Assert.Null(PcFieldDate.ToAppDate(""));
			Assert.Null(PcFieldDate.ToAppDate(42));
		}
	}
}
