using System.Xml.Linq;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using QuickApiMapper.Application.Core;
using QuickApiMapper.Application.Resolvers;
using QuickApiMapper.Application.Transformers;
using QuickApiMapper.Application.Writers;
using QuickApiMapper.Contracts;
using MappingResult = QuickApiMapper.Contracts.MappingResult;

namespace QuickApiMapper.Application.Examples;

/// <summary>
/// Example demonstrating how to use the new generic mapping system.
/// This shows how to move away from hardcoded JObject/XDocument dependencies.
/// </summary>
public static class GenericMappingExample
{
 /// <summary>
 /// Example: JSON to JSON mapping using the generic system.
 /// This is much cleaner than the old hardcoded approach.
 /// </summary>
 public static async Task<MappingResult> MapJsonToJsonAsync(
 IServiceProvider serviceProvider,
 JObject sourceJson,
 JObject destinationJson,
 IEnumerable<FieldMapping> mappings)
 {
 // Get the generic mapping engine factory
 var factory = serviceProvider.GetRequiredService<IMappingEngineFactory>();
 
 // Create a JSON-to-JSON mapping engine
 var engine = factory.CreateEngine<JObject, JObject>();
 
 // Execute the mapping
 return await engine.ApplyMappingAsync(
 mappings,
 sourceJson,
 destinationJson,
 statics: null,
 globalStatics: null,
 serviceProvider: serviceProvider);
 }

 /// <summary>
 /// Example: XML to XML mapping using the generic system.
 /// </summary>
 public static async Task<MappingResult> MapXmlToXmlAsync(
 IServiceProvider serviceProvider,
 XDocument sourceXml,
 XDocument destinationXml,
 IEnumerable<FieldMapping> mappings)
 {
 var factory = serviceProvider.GetRequiredService<IMappingEngineFactory>();
 var engine = factory.CreateEngine<XDocument, XDocument>();
 
 return await engine.ApplyMappingAsync(
 mappings,
 sourceXml,
 destinationXml,
 serviceProvider: serviceProvider);
 }

 /// <summary>
 /// Example: JSON to XML mapping (cross-format mapping).
 /// This demonstrates the true power of the generic system.
 /// </summary>
 public static async Task<MappingResult> MapJsonToXmlAsync(
 IServiceProvider serviceProvider,
 JObject sourceJson,
 XDocument destinationXml,
 IEnumerable<FieldMapping> mappings)
 {
 var factory = serviceProvider.GetRequiredService<IMappingEngineFactory>();
 var engine = factory.CreateEngine<JObject, XDocument>();
 
 return await engine.ApplyMappingAsync(
 mappings,
 sourceJson,
 destinationXml,
 serviceProvider: serviceProvider);
 }

 /// <summary>
 /// Example: Custom type mapping.
 /// This shows how to extend the system for custom business objects.
 /// </summary>
 public static async Task<MappingResult> MapCustomTypesAsync<TSource, TDestination>(
 IServiceProvider serviceProvider,
 TSource source,
 TDestination destination,
 IEnumerable<FieldMapping> mappings)
 where TSource : class
 where TDestination : class
 {
 var factory = serviceProvider.GetRequiredService<IMappingEngineFactory>();
 var engine = factory.CreateEngine<TSource, TDestination>();
 
 return await engine.ApplyMappingAsync(
 mappings,
 source,
 destination,
 serviceProvider: serviceProvider);
 }

 /// <summary>
 /// Example: Register the generic mapping system with dependency injection.
 /// </summary>
 public static void ConfigureServices(IServiceCollection services)
 {
 // Register the factory and registry
 services.AddSingleton<IMappingEngineFactory, MappingEngineFactory>();

 // Register type-specific resolvers
 services.AddSingleton<ISourceResolver<JObject>, JsonSourceResolver>();
 services.AddSingleton<ISourceResolver<XDocument>, XmlSourceResolver>();
 services.AddSingleton<ISourceResolver<IReadOnlyDictionary<string, string>>, StaticSourceResolver>();

 // Register type-specific writers
 services.AddSingleton<IDestinationWriter<JObject>, JsonDestinationWriter>();
 services.AddSingleton<IDestinationWriter<XDocument>, XmlDestinationWriter>();

 // Register other dependencies
 services.AddSingleton<ITransformerRegistry, TransformerRegistry>();
 services.AddSingleton<BehaviorPipeline>();
 
 // Register behaviors
 // services.AddSingleton<IPreRunBehavior, AuthenticationBehavior>();
 // services.AddSingleton<IPreRunBehavior, HttpClientConfigurationBehavior>();
 // services.AddSingleton<IPreRunBehavior, ValidationBehavior>();
 // services.AddSingleton<IPostRunBehavior, LoggingBehavior>();
 // services.AddSingleton<IWholeRunBehavior, TimingBehavior>();
 }
}

/// <summary>
/// Example custom business object for demonstrating custom type mapping.
/// </summary>
public class CustomerData
{
 public string? Name { get; set; }
 public string? Email { get; set; }
 public string? Phone { get; set; }
}

/// <summary>
/// Example custom source resolver for business objects.
/// This demonstrates how to extend the system for custom types.
/// </summary>
public class CustomerDataSourceResolver : ISourceResolver<CustomerData>
{
 public IReadOnlyList<string> SupportedTokens => new[] { "customer." };

 public bool CanResolve(string sourcePath) => sourcePath.StartsWith("customer.");

 public string? Resolve(string sourcePath, CustomerData source, IReadOnlyDictionary<string, string>? staticValues = null)
 {
 var property = sourcePath[9..]; // Remove "customer." prefix
 
 return property.ToLower() switch
 {
 "name" => source.Name,
 "email" => source.Email,
 "phone" => source.Phone,
 _ => null
 };
 }
}

/// <summary>
/// Example custom destination writer for business objects.
/// </summary>
public class CustomerDataDestinationWriter : IDestinationWriter<CustomerData>
{
 public IReadOnlyList<string> SupportedTokens => new[] { "customer." };

 public bool CanWrite(string destinationPath) => destinationPath.StartsWith("customer.");

 public bool Write(string destinationPath, string? value, CustomerData destination)
 {
 var property = destinationPath[9..]; // Remove "customer." prefix
 
 switch (property.ToLower())
 {
 case "name":
 destination.Name = value;
 return true;
 case "email":
 destination.Email = value;
 return true;
 case "phone":
 destination.Phone = value;
 return true;
 default:
 return false;
 }
 }
}
