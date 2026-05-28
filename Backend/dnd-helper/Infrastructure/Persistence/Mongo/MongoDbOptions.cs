namespace dnd_helper.Infrastructure.Persistence.Mongo;

public sealed class MongoDbOptions
{
    public const string SectionName = "MongoDb";

    public string ConnectionString { get; set; } = "mongodb://localhost:27017";
    public string DatabaseName { get; set; } = "dnd_helper_rules";
}
