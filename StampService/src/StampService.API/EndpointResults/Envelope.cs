namespace StampService.API.EndpointResults;

public record Envelope
{
    public object? Result { get; }
    public IReadOnlyCollection<ApiErrorResponse>? Errors { get; }
    public bool IsError => Errors is { Count: > 0 };
    public DateTime TimeGenerated { get; }

    private Envelope(object? result, IReadOnlyCollection<ApiErrorResponse>? errors)
    {
        Result = result;
        Errors = errors;
        TimeGenerated = DateTime.UtcNow;
    }

    public static Envelope Ok(object? result = null) => new(result, null);

    public static Envelope Error(IReadOnlyCollection<ApiErrorResponse> errors) => new(null, errors);
}

public record Envelope<T>
{
    public T? Result { get; }
    public IReadOnlyCollection<ApiErrorResponse>? Errors { get; }
    public bool IsError => Errors is { Count: > 0 };
    public DateTime TimeGenerated { get; }

    private Envelope(T? result, IReadOnlyCollection<ApiErrorResponse>? errors)
    {
        Result = result;
        Errors = errors;
        TimeGenerated = DateTime.UtcNow;
    }

    public static Envelope<T> Ok(T? result = default) => new(result, null);

    public static Envelope<T> Error(IReadOnlyCollection<ApiErrorResponse> errors) => new(default, errors);
}

public record ApiErrorResponse(
    string Code,
    string Message,
    string Type,
    string? InvalidField);
