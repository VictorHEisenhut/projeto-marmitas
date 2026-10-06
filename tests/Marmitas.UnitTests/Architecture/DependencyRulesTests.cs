using NetArchTest.Rules;

namespace Marmitas.UnitTests.Architecture;

public class DependencyRulesTests
{
    private const string Domain = "Marmitas.Domain";
    private const string Application = "Marmitas.Application";
    private const string Infrastructure = "Marmitas.Infrastructure";
    private const string Api = "Marmitas.Api";

    private static readonly System.Reflection.Assembly DomainAssembly = typeof(Domain.Entities.Entity).Assembly;
    private static readonly System.Reflection.Assembly ApplicationAssembly = typeof(Application.Users.IRegisterUserService).Assembly;
    private static readonly System.Reflection.Assembly InfrastructureAssembly = typeof(Infrastructure.Persistence.AppDbContext).Assembly;

    [Fact]
    public void Domain_NaoDependeDeNenhumaOutraCamada()
    {
        var result = Types.InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(Application, Infrastructure, Api, "Microsoft.EntityFrameworkCore", "Microsoft.AspNetCore")
            .GetResult();

        Assert.True(result.IsSuccessful, Describe(result));
    }

    [Fact]
    public void Application_NaoDependeDeInfrastructureNemApi()
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(Infrastructure, Api, "Microsoft.EntityFrameworkCore", "Microsoft.AspNetCore")
            .GetResult();

        Assert.True(result.IsSuccessful, Describe(result));
    }

    [Fact]
    public void Infrastructure_NaoDependeDaApi()
    {
        var result = Types.InAssembly(InfrastructureAssembly)
            .ShouldNot()
            .HaveDependencyOn(Api)
            .GetResult();

        Assert.True(result.IsSuccessful, Describe(result));
    }

    private static string Describe(TestResult result) =>
        "Tipos violando a regra: " + string.Join(", ", result.FailingTypeNames ?? []);
}

