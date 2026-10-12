using System;

namespace WebVella.Erp.Plugins.Project.Domain
{
	public class TimeTrackForm
	{
		public Guid TaskId { get; private set; } = Guid.Empty;
		public DateTime LoggedOn { get; private set; } = DateTime.MinValue;
		public string Body { get; private set; } = "";
		public bool IsBillable { get; private set; } = true;
		public int Minutes { get; private set; } = 0;

		//Zero minutes are not logged
		public bool ShouldLogMinutes { get; private set; }

		public static TimeTrackForm Parse(string taskId, string minutes, string loggedOn, string isBillable, string body)
		{
			if (String.IsNullOrWhiteSpace(taskId))
				throw new Exception("Task Id is required");
			if (String.IsNullOrWhiteSpace(minutes))
				throw new Exception("Minutes is required");
			if (String.IsNullOrWhiteSpace(loggedOn))
				throw new Exception("Logged on is required");

			var form = new TimeTrackForm();
			if (!String.IsNullOrWhiteSpace(body))
				form.Body = body;
			if (Guid.TryParse(taskId, out Guid outTaskId))
				form.TaskId = outTaskId;
			if (DateTime.TryParse(loggedOn, out DateTime outLoggedOn))
				form.LoggedOn = outLoggedOn;
			if (Boolean.TryParse(isBillable, out Boolean outBillable))
				form.IsBillable = outBillable;
			if (int.TryParse(minutes, out int outMinutes))
				form.Minutes = outMinutes;
			form.ShouldLogMinutes = minutes != "0";

			return form;
		}
	}
}
