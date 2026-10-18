using System.Runtime.CompilerServices;
using Construction.Application.Common;

namespace Construction.UnitTests;

/// <summary>
/// The unit tests build their times in UTC, so they run on a UTC company clock. The tests of the company's own
/// zone set it for themselves and put it back (see <c>CompanyTimeTests</c>).
/// </summary>
internal static class UnitTestZone
{
    [ModuleInitializer]
    internal static void Init() => CompanyTime.Configure("UTC", out _);
}
