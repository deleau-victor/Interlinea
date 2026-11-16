using System.Reflection;
using Xunit.Sdk;
using Xunit.v3;

namespace Domain.UnitTests.Attributes;

/// <summary>
/// Generates one theory row per value, for tests with a single parameter.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
internal sealed class CasesAttribute : DataAttribute
{
    private readonly object?[] _values;

    public CasesAttribute(params object?[] values)
    {
        _values = values;
    }

    public override ValueTask<IReadOnlyCollection<ITheoryDataRow>> GetData(
        MethodInfo testMethod,
        DisposalTracker disposalTracker)
    {
        if (testMethod.GetParameters().Length != 1)
            throw new InvalidOperationException(
                $"[Cases] requires exactly one parameter on {testMethod.Name}.");

        IReadOnlyCollection<ITheoryDataRow> rows = _values
            .Select(value => (ITheoryDataRow)new TheoryDataRow(value))
            .ToArray();

        return new ValueTask<IReadOnlyCollection<ITheoryDataRow>>(rows);
    }

    public override bool SupportsDiscoveryEnumeration() => true;
}
