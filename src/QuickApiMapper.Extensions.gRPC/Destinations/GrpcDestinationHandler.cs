using Google.Protobuf;
using Grpc.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System.Xml.Linq;
using QuickApiMapper.Contracts;
using QuickApiMapper.Application.Destinations;

namespace QuickApiMapper.Extensions.gRPC.Destinations;

/// <summary>
/// Destination handler for forwarding gRPC requests to downstream services.
/// Handles both unary and server-streaming gRPC calls.
/// </summary>
public class GrpcDestinationHandler : IDestinationHandler
{
    private readonly ILogger<GrpcDestinationHandler> _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    public GrpcDestinationHandler(
        ILogger<GrpcDestinationHandler> logger,
        IHttpClientFactory httpClientFactory)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
    }

    public bool CanHandle(string destinationType)
    {
        return destinationType.Equals("gRPC", StringComparison.OrdinalIgnoreCase);
    }

    public async Task HandleAsync(
        IntegrationMapping integration,
        JObject? outJson,
        XDocument? outXml,
        HttpRequest req,
        HttpResponse resp,
        IHttpClientFactory httpClientFactory,
        CancellationToken cancellationToken)
    {
        // For gRPC, we would need the actual IMessage instance
        // This is a simplified implementation - in production, you'd need to:
        // 1. Parse the JObject/XDocument based on a .proto schema
        // 2. Create the appropriate IMessage instance
        // 3. Populate it using the GrpcDestinationWriter

        // For now, log a warning that gRPC requires compile-time message types
        _logger.LogWarning("gRPC destination handler requires compile-time message types. " +
            "Consider using a typed integration or dynamic proto parsing.");

        resp.StatusCode = StatusCodes.Status501NotImplemented;
        await resp.WriteAsync("gRPC destination requires typed message definitions", cancellationToken);
    }

    /// <summary>
    /// Maps gRPC status codes to HTTP status codes.
    /// </summary>
    private static int MapGrpcStatusToHttp(StatusCode grpcStatus)
    {
        return grpcStatus switch
        {
            StatusCode.OK => StatusCodes.Status200OK,
            StatusCode.Cancelled => StatusCodes.Status499ClientClosedRequest,
            StatusCode.InvalidArgument => StatusCodes.Status400BadRequest,
            StatusCode.DeadlineExceeded => StatusCodes.Status504GatewayTimeout,
            StatusCode.NotFound => StatusCodes.Status404NotFound,
            StatusCode.AlreadyExists => StatusCodes.Status409Conflict,
            StatusCode.PermissionDenied => StatusCodes.Status403Forbidden,
            StatusCode.ResourceExhausted => StatusCodes.Status429TooManyRequests,
            StatusCode.FailedPrecondition => StatusCodes.Status400BadRequest,
            StatusCode.Aborted => StatusCodes.Status409Conflict,
            StatusCode.OutOfRange => StatusCodes.Status400BadRequest,
            StatusCode.Unimplemented => StatusCodes.Status501NotImplemented,
            StatusCode.Internal => StatusCodes.Status500InternalServerError,
            StatusCode.Unavailable => StatusCodes.Status503ServiceUnavailable,
            StatusCode.DataLoss => StatusCodes.Status500InternalServerError,
            StatusCode.Unauthenticated => StatusCodes.Status401Unauthorized,
            _ => StatusCodes.Status500InternalServerError
        };
    }
}
