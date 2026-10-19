using System;
using System.Linq;
using System.Reflection;
using Xunit;

namespace WebVella.Erp.Plugins.Project.Domain.Tests
{
	public class DomainDependencyTests
	{
		private static readonly string[] ForbiddenAssemblyPrefixes = { "WebVella.Erp", "Npgsql", "Irony" };

		[Fact]
		public void DomainAssembly_DoesNotReferenceErpEqlOrDatabase()
		{
			var domainAssembly = Assembly.Load("WebVella.Erp.Plugins.Project.Domain");

			var forbidden = domainAssembly.GetReferencedAssemblies()
				.Select(a => a.Name)
				.Where(name => ForbiddenAssemblyPrefixes.Any(p => name.StartsWith(p, StringComparison.OrdinalIgnoreCase))
					&& !name.StartsWith("WebVella.Erp.Plugins.Project.Domain", StringComparison.OrdinalIgnoreCase))
				.ToList();

			Assert.Empty(forbidden);
		}
	}
}
