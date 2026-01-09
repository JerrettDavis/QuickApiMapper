namespace QuickApiMapper.Contracts;

public interface ITransformer
{
    string Name { get; }
    string Transform(string? input, IReadOnlyDictionary<string, string?>? args);
}
