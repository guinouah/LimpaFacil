namespace LimpaFacil.IntegrationTests;

public sealed class MySqlFactAttribute : FactAttribute
{
    public MySqlFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("LIMPAFACIL_TEST_MYSQL")))
            Skip = "Requer MySQL real isolado em LIMPAFACIL_TEST_MYSQL; nunca aponta para produção.";
    }
}
