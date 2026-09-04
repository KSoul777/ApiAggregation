using System.Reflection;

namespace Aggregator.Aggregation.Presentation;

public static class AssemblyReference
{
    public static readonly Assembly Assembly = typeof(AssemblyReference).Assembly;
}
