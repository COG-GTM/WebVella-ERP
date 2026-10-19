using System;

namespace WebVella.Erp.Plugins.Project.Domain
{
	public class TimeTrackForm
	{
		public const string TaskIdRequiredMessage = "Task Id is required";
		public const string MinutesRequiredMessage = "Minutes is required";
		public const string LoggedOnRequiredMessage = "Logged on is required";

		public Guid TaskId { get; private set; } = Guid.Empty;
		public DateTime LoggedOn { get; private set; } = DateTime.MinValue;
		public string Body { get; private set; } = "";
		public bool IsBillable { get; private set; } = true;
		public int Minutes { get; private set; } = 0;

		//Zero minutes are not logged
		public bool ShouldLogTime { get; private set; }

		public static TimeTrackForm Parse(string taskId, string minutes, string loggedOn, string isBillable, string body)
		{
			if (String.IsNullOrWhiteSpace(taskId))
				throw new Exception(TaskIdRequiredMessage);
			if (String.IsNullOrWhiteSpace(minutes))
				throw new Exception(MinutesRequiredMessage);
			if (String.IsNullOrWhiteSpace(loggedOn))
				throw new Exception(LoggedOnRequiredMessage);

			var form = new TimeTrackForm();
			form.Body = !String.IsNullOrWhiteSpace(body) ? body : "";
			if (Guid.TryParse(taskId, out Guid outTaskId))
				form.TaskId = outTaskId;
			if (DateTime.TryParse(loggedOn, out DateTime outLoggedOn))
				form.LoggedOn = outLoggedOn;
			if (Boolean.TryParse(isBillable, out Boolean outBillable))
				form.IsBillable = outBillable;
			if (int.TryParse(minutes, out int outMinutes))
				form.Minutes = outMinutes;
			form.ShouldLogTime = minutes != "0";
			return form;
		}
	}
}
