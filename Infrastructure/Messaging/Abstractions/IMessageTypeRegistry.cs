using System.Collections.Concurrent;
using System.Reflection;

namespace Infrastructure.Messaging.Abstractions
{
    /// <summary>
    /// Registry for managing integration event types.
    /// Provides fast, thread-safe type resolution without reflection overhead.
    /// </summary>
    public interface IMessageTypeRegistry
    {
        /// <summary>
        /// Register an integration event type for deserialization.
        /// </summary>
        void RegisterType<T>() where T : class;

        /// <summary>
        /// Register all types from an assembly that match the predicate.
        /// </summary>
        void RegisterTypesFromAssembly(Assembly assembly, Func<Type, bool>? predicate = null);

        /// <summary>
        /// Resolve a type from its assembly qualified name.
        /// </summary>
        Type? ResolveType(string assemblyQualifiedName);

        /// <summary>
        /// Get all registered types.
        /// </summary>
        IReadOnlyCollection<Type> GetRegisteredTypes();
    }

    /// <summary>
    /// Default implementation using concurrent dictionary for thread-safety.
    /// </summary>
    public class MessageTypeRegistry : IMessageTypeRegistry
    {
        private readonly ConcurrentDictionary<string, Type> _typeCache = new();

        public void RegisterType<T>() where T : class
        {
            var type = typeof(T);
            var key = type.AssemblyQualifiedName ?? type.FullName!;
            _typeCache.TryAdd(key, type);
        }

        public void RegisterTypesFromAssembly(Assembly assembly, Func<Type, bool>? predicate = null)
        {
            var types = assembly.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract);

            if (predicate != null)
            {
                types = types.Where(predicate);
            }

            foreach (var type in types)
            {
                var key = type.AssemblyQualifiedName ?? type.FullName!;
                _typeCache.TryAdd(key, type);
            }
        }

        public Type? ResolveType(string assemblyQualifiedName)
        {
            if (_typeCache.TryGetValue(assemblyQualifiedName, out var type))
            {
                return type;
            }

            // Fallback to Type.GetType if not in cache
            type = Type.GetType(assemblyQualifiedName);
            if (type != null)
            {
                _typeCache.TryAdd(assemblyQualifiedName, type);
            }

            return type;
        }

        public IReadOnlyCollection<Type> GetRegisteredTypes()
        {
            return _typeCache.Values.ToList().AsReadOnly();
        }
    }
}
