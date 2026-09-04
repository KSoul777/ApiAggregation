using System.Reflection;

namespace Aggregator.Aggregation.Application;

public static class AssemblyReference
{
    public static readonly Assembly Assembly = typeof(AssemblyReference).Assembly;
}
