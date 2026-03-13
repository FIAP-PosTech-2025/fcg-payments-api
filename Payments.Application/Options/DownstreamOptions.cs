namespace Payments.Application.Options;

public class DownstreamOptions
{
    public const string SectionName = "Downstream";

    public string CatalogBaseUrl { get; set; } = "http://localhost:5001";
    public string NotificationsBaseUrl { get; set; } = "http://localhost:5003";
}
