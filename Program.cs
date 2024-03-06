using HotChocolate.Execution;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddScoped<MyService>()
    .AddGraphQLServer()
    .AddDataLoader<TestDataLoader>()
    .RegisterService<MyService>(ServiceKind.Resolver)
    .ModifyRequestOptions(x => x.IncludeExceptionDetails = true)
    .AddTypes();

var app = builder.Build();

var executor = await app.Services.GetRequestExecutorAsync();

var result = await executor.ExecuteAsync("{ subType { subField } }");

Console.WriteLine("Result: " + result.ToJson());

[QueryType]
public class Query
{
    public async Task<SubType> GetSubTypeAsync(TestDataLoader dataLoader)
    {
        // First trigger
        await dataLoader.LoadAsync(1);

        return new SubType();
    }
}

public class SubType
{
    public async Task<string> GetSubField(TestDataLoader dataLoader)
    {
        await dataLoader.LoadAsync(2);

        return "Subfield value";
    }
}

public class MyService
{
    public string Id { get; private set; }

    public MyService()
    {
        Console.WriteLine("Instantiate service");
        Id = Guid.NewGuid().ToString();
    }
}

public class TestDataLoader(MyService service, IBatchScheduler batchScheduler, DataLoaderOptions? options = null) : BatchDataLoader<int, object?>(batchScheduler, options)
{
    protected override Task<IReadOnlyDictionary<int, object?>> LoadBatchAsync(IReadOnlyList<int> keys, CancellationToken cancellationToken)
    {
        Console.WriteLine("Invoked batch with scoped service id: " + service.Id);

        return Task.FromResult<IReadOnlyDictionary<int, object?>>(keys.ToDictionary(x => x, x => (object?)x));
    }
}