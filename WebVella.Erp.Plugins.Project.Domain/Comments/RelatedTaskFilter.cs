using System;
using System.Collections.Generic;

namespace WebVella.Erp.Plugins.Project.Domain.Comments
{
	public sealed class RelatedTaskFilter
	{
		public RelatedTaskFilter(string whereClause, IReadOnlyList<KeyValuePair<string, Guid>> parameters)
		{
			WhereClause = whereClause;
			Parameters = parameters;
		}

		public string WhereClause { get; }

		public IReadOnlyList<KeyValuePair<string, Guid>> Parameters { get; }

		// Every id from l_related_records is inspected as a candidate task id (OR-ed together),
		// with parameters named taskId1..taskIdN in input order.
		public static RelatedTaskFilter Build(IEnumerable<Guid> relatedRecordIds)
		{
			var filterStringList = new List<string>();
			var parameters = new List<KeyValuePair<string, Guid>>();
			var index = 1;
			foreach (var taskGuid in relatedRecordIds)
			{
				var paramName = "taskId" + index;
				filterStringList.Add($" id = @{paramName} ");
				parameters.Add(new KeyValuePair<string, Guid>(paramName, taskGuid));
				index++;
			}
			return new RelatedTaskFilter(String.Join(" OR ", filterStringList), parameters);
		}
	}
}
