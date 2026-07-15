using MeterForge.Domain.Common;

namespace MeterForge.Architecture.Tests;

public sealed class DependencyDirectionTests
{
    [Fact]
    public void Domain_DoesNotReferenceFrameworkOrOuterProjects()
    {
        var references = typeof(Money).Assembly.GetReferencedAssemblies().Select(reference => reference.Name).ToArray();

        Assert.DoesNotContain(references, name => name?.StartsWith("Microsoft.AspNetCore", StringComparison.Ordinal) == true);
        Assert.DoesNotContain(references, name => name?.StartsWith("MeterForge.", StringComparison.Ordinal) == true);
    }
}
