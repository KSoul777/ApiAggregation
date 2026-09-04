using System.Reflection;
using Aggregator.ArchitectureTests.Abstractions;
using Aggregator.Common.Domain;
using NetArchTest.Rules;

namespace Aggregator.ArchitectureTests.Layers;

public class ModuleTests
{
    private const string ApplicationNamespace = "Aggregator.Aggregation.Application";
    private const string InfrastructureNamespace = "Aggregator.Aggregation.Infrastructure";
    private const string PresentationNamespace = "Aggregator.Aggregation.Presentation";

    private static readonly Assembly ApplicationAssembly =
        typeof(Aggregator.Aggregation.Application.AssemblyReference).Assembly;

    private static readonly Assembly PresentationAssembly =
        typeof(Aggregator.Aggregation.Presentation.AssemblyReference).Assembly;

    [Fact]
    public void Application_ShouldNotDependOn_Infrastructure()
    {
        Types.InAssembly(ApplicationAssembly)
            .Should()
            .NotHaveDependencyOn(InfrastructureNamespace)
            .GetResult()
            .ShouldBeSuccessful();
    }

    [Fact]
    public void Application_ShouldNotDependOn_Presentation()
    {
        Types.InAssembly(ApplicationAssembly)
            .Should()
            .NotHaveDependencyOn(PresentationNamespace)
            .GetResult()
            .ShouldBeSuccessful();
    }

    [Fact]
    public void Presentation_ShouldNotDependOn_Infrastructure()
    {
        Types.InAssembly(PresentationAssembly)
            .Should()
            .NotHaveDependencyOn(InfrastructureNamespace)
            .GetResult()
            .ShouldBeSuccessful();
    }

    [Fact]
    public void Domain_ShouldNotDependOn_ApplicationOrInfrastructure()
    {
        Types.InAssembly(typeof(Result).Assembly)
            .Should()
            .NotHaveDependencyOnAny(ApplicationNamespace, InfrastructureNamespace, PresentationNamespace)
            .GetResult()
            .ShouldBeSuccessful();
    }
}
