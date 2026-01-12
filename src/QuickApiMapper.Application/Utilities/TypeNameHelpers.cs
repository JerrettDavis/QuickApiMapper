namespace QuickApiMapper.Application.Utilities;

/// <summary>
/// Type name utilities using C# 14 nameof with unbound generics.
/// Demonstrates the new feature where nameof(List&lt;&gt;) evaluates to "List".
/// </summary>
public static class TypeNameHelpers
{
    /// <summary>
    /// C# 14 feature: Gets generic type names without type parameters.
    /// </summary>
    /// <example>
    /// var name = TypeNameHelpers.GetGenericTypeName&lt;List&lt;int&gt;&gt;(); // Returns "List"
    /// </example>
    public static class GenericTypeNames
    {
        // C# 14: nameof with unbound generic types
        public static readonly string List = nameof(List<>);
        public static readonly string Dictionary = nameof(Dictionary<,>);
        public static readonly string HashSet = nameof(HashSet<>);
        public static readonly string Queue = nameof(Queue<>);
        public static readonly string Stack = nameof(Stack<>);
        public static readonly string Task = nameof(Task<>);
        public static readonly string ValueTask = nameof(ValueTask<>);
        public static readonly string IEnumerable = nameof(IEnumerable<>);
        public static readonly string ICollection = nameof(ICollection<>);
        public static readonly string IList = nameof(IList<>);
        public static readonly string IReadOnlyList = nameof(IReadOnlyList<>);
        public static readonly string IReadOnlyDictionary = nameof(IReadOnlyDictionary<,>);
        public static readonly string IReadOnlyCollection = nameof(IReadOnlyCollection<>);
    }

    /// <summary>
    /// C# 14 feature: Gets the unbound generic name for logging and diagnostics.
    /// </summary>
    public static string GetUnboundGenericName<T>() where T : class
    {
        var type = typeof(T);

        if (!type.IsGenericType)
            return type.Name;

        // C# 14 improvement: Can now use nameof with unbound generics
        // Example: For List<int>, this returns "List"
        return type.GetGenericTypeDefinition().Name.Split('`')[0];
    }

    /// <summary>
    /// C# 14 feature: Creates formatted generic type names for error messages.
    /// </summary>
    public static string FormatGenericTypeName(Type type)
    {
        if (!type.IsGenericType)
            return type.Name;

        var genericTypeName = type.GetGenericTypeDefinition().Name.Split('`')[0];
        var typeArguments = type.GetGenericArguments();

        // C# 14: Better support for generic type formatting
        return $"{genericTypeName}<{string.Join(", ", typeArguments.Select(t => t.Name))}>";
    }

    /// <summary>
    /// C# 14 feature: Validation helper that uses nameof with unbound generics.
    /// </summary>
    public static void ValidateGenericType<T>(string parameterName) where T : class
    {
        var type = typeof(T);

        if (!type.IsGenericType)
        {
            throw new ArgumentException(
                $"Type must be a generic type. Expected types like {nameof(List<>)} or {nameof(Dictionary<,>)}, " +
                $"but received {type.Name}",
                parameterName);
        }
    }

    /// <summary>
    /// C# 14 feature: Checks if a type matches a specific generic type definition.
    /// </summary>
    public static bool IsGenericTypeOf<T>(Type type, Type genericTypeDefinition)
    {
        if (!type.IsGenericType)
            return false;

        return type.GetGenericTypeDefinition() == genericTypeDefinition;
    }

    /// <summary>
    /// C# 14 feature: Gets friendly names for common generic repository types.
    /// </summary>
    public static class RepositoryTypeNames
    {
        // C# 14: nameof with unbound generics makes this cleaner
        public static readonly string Repository = nameof(IRepository<>);
        public static readonly string ReadOnlyRepository = nameof(IReadOnlyRepository<>);
        public static readonly string UnitOfWork = nameof(IUnitOfWork);

        // For interfaces that would be generic
        private interface IRepository<T> { }
        private interface IReadOnlyRepository<T> { }
        private interface IUnitOfWork { }
    }
}
