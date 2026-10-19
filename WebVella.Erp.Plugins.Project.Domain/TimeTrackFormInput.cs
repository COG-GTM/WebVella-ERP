using System;

namespace WebVella.Erp.Plugins.Project.Domain
{
	public class TimeTrackFormInput
	{
		public Guid TaskId { get; private set; } = Guid.Empty;
		public int Minutes { get; private set; } = 0;
		public DateTime LoggedOn { get; private set; } = DateTime.MinValue;
		public bool IsBillable { get; private set; } = true;
		public string Body { get; private set; } = "";

		//Zero minutes are not logged. Compared against the raw form value, not the parsed one.
		public bool ShouldLogMinutes { get; private set; }

		public static TimeTrackFormInput Parse(string taskId, string minutes, string loggedOn, string isBillable, string body)
		{
			if (String.IsNullOrWhiteSpace(taskId))
				throw new Exception("Task Id is required");
			if (String.IsNullOrWhiteSpace(minutes))
				throw new Exception("Minutes is required");
			if (String.IsNullOrWhiteSpace(loggedOn))
				throw new Exception("Logged on is required");

			var input = new TimeTrackFormInput();
			input.Body = !String.IsNullOrWhiteSpace(body) ? body : "";

			if (Guid.TryParse(taskId, out Guid outTaskId))
				input.TaskId = outTaskId;
			if (DateTime.TryParse(loggedOn, out DateTime outLoggedOn))
				input.LoggedOn = outLoggedOn;
			if (Boolean.TryParse(isBillable, out Boolean outBillable))
				input.IsBillable = outBillable;
			if (int.TryParse(minutes, out int outMinutes))
				input.Minutes = outMinutes;

			input.ShouldLogMinutes = minutes != "0";
			return input;
		}
	}
}
