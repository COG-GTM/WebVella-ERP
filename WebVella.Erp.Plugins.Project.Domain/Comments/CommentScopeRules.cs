using System;

namespace WebVella.Erp.Plugins.Project.Domain.Comments
{
	public static class CommentScopeRules
	{
		public const string ProjectsScope = "projects";

		// Raw substring match on the serialized l_scope value (ordinal, case-sensitive).
		public static bool IsProjectScope(string serializedScope)
		{
			return serializedScope != null && serializedScope.Contains(ProjectsScope);
		}

		// Only null and the empty string are treated as "no related records"; whitespace is not.
		public static bool HasRelatedRecords(string serializedRelatedRecords)
		{
			return serializedRelatedRecords != null && serializedRelatedRecords != "";
		}

		public static Guid? ResolveCommentCreator(object createdByValue)
		{
			if (createdByValue != null && createdByValue is Guid)
				return (Guid)createdByValue;
			return null;
		}
	}
}
