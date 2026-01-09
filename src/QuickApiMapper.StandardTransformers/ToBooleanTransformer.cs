using QuickApiMapper.Contracts;

namespace QuickApiMapper.StandardTransformers;

public class ToBooleanTransformer : ITransformer
{
    public string Name => "toBoolean";
    private readonly HashSet<string> _trueValues = ["true", "t", "yes", "y", "1"];
    
    public string Transform(
        string? input, 
        IReadOnlyDictionary<string, string?>? args)
    {
        if (string.IsNullOrEmpty(input)) return bool.FalseString;
        
        input = input.Trim().ToLowerInvariant();
        
        return _trueValues.Contains(input) ? bool.TrueString : bool.FalseString;
    }
}