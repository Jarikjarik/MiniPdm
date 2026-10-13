namespace MiniPdm.IntegrationTests.Persistence;

internal static class TestDatabase
{
    private const string DefaultConnectionString =
        "Host=localhost;Port=5433;Database=minipdm;" +
        "Username=minipdm;Password=minipdm_dev";

    public static string ConnectionString =>
        Environment.GetEnvironmentVariable(
            "MINIPDM_CONNECTION_STRING")
        ?? DefaultConnectionString;
}