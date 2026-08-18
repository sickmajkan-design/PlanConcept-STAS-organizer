using System.Linq.Expressions;
using System.Reflection;
using Construction.Application.Features.Employees.Models;

namespace Construction.UnitTests.Mapping;

/// <summary>
/// Every DTO property is actually assigned by its projection.
/// </summary>
/// <remarks>
/// <para>
/// The failure this exists for is silence. A hand-written projection that
/// forgets a property does not fail to compile and does not throw: the field
/// simply arrives as null, or zero, or the empty string, and it looks exactly
/// like a record that genuinely has no value there. Only a test that asserts
/// that particular field would notice, and no suite asserts every field of
/// every DTO.
/// </para>
/// <para>
/// So this does not test values at all. It reads the expression tree itself —
/// the same object EF turns into a SELECT list — and checks the set of
/// properties it binds against the set the DTO declares. It is the invariant
/// AutoMapper used to enforce with its configuration validation, which is the
/// one thing worth keeping from it.
/// </para>
/// <para>
/// It applies to mappings that do not exist yet: the list is discovered by
/// reflection, so a DTO added next year with a property nobody projects fails
/// here without anyone remembering to add it.
/// </para>
/// </remarks>
public class ProjectionCompletenessTests
{
    /// <summary>Every <c>*Mapping</c> class in the application assembly.</summary>
    public static TheoryData<string> Projections()
    {
        var data = new TheoryData<string>();

        foreach (var type in MappingTypes())
        {
            data.Add(type.FullName!);
        }

        return data;
    }

    private static IEnumerable<Type> MappingTypes() =>
        typeof(EmployeeMapping).Assembly
            .GetTypes()
            .Where(t => t.IsClass && t.IsAbstract && t.IsSealed) // static
            .Where(t => t.Name.EndsWith("Mapping", StringComparison.Ordinal))
            .Where(t => ProjectionField(t) is not null)
            .OrderBy(t => t.FullName, StringComparer.Ordinal);

    private static FieldInfo? ProjectionField(Type type) =>
        type.GetField("Projection", BindingFlags.Public | BindingFlags.Static);

    [Theory]
    [MemberData(nameof(Projections))]
    public void Every_property_of_the_dto_is_assigned(string typeName)
    {
        var type = typeof(EmployeeMapping).Assembly.GetType(typeName)!;

        var lambda = (LambdaExpression)ProjectionField(type)!.GetValue(null)!;

        // The projection has to be `entity => new Dto { ... }` for EF to turn
        // it into a SELECT list, so anything else is worth failing on too.
        var init = Assert.IsType<MemberInitExpression>(lambda.Body);

        var assigned = init.Bindings.Select(b => b.Member.Name).ToHashSet(StringComparer.Ordinal);

        var expected = init.Type
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            // Get-only computed properties — TimeEntryDto.WorkedMinutes,
            // WorkItemDto.IsFinished — are derived from other fields and
            // deliberately have nothing to assign.
            .Where(p => p.SetMethod is not null)
            .Select(p => p.Name)
            .ToList();

        var missing = expected.Where(name => !assigned.Contains(name)).ToList();

        Assert.True(
            missing.Count == 0,
            $"{type.Name} does not assign: {string.Join(", ", missing)}. "
            + "An unassigned property reaches the client as null or zero and "
            + "looks like missing data rather than a missing mapping.");
    }

    /// <summary>
    /// There are as many projections as there are DTOs that need one.
    /// </summary>
    /// <remarks>
    /// A guard on the guard. If the reflection above stopped matching — a
    /// renamed suffix, a mapping written as a non-static class — this file
    /// would keep passing while testing nothing at all, which is the worst
    /// state a test can be in.
    /// </remarks>
    [Fact]
    public void The_projections_are_found()
    {
        Assert.Equal(21, MappingTypes().Count());
    }
}
