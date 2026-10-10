namespace Ipkiss.Api.Http;

public sealed record EventRequest(string? Type, string? Origin, string? Destination, decimal Amount);
