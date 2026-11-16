namespace ChatWithMe.Infrastructure.ConnectionStrings;

internal sealed class ConnectionStringEnvelope
{
    public int Code { get; set; }

    public string? Message { get; set; }

    public ConnectionStringPayload? Data { get; set; }
}

internal sealed class ConnectionStringPayload
{
    public string? ConnName { get; set; }

    public string? DbType { get; set; }

    public string? ConnectionStringEncrypted { get; set; }

    public string? Algorithm { get; set; }

    public string? KeySource { get; set; }

    public string? PayloadLayout { get; set; }
}
