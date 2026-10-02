namespace PortailInterne.Api.Services;

public interface IAppClock
{
    /// <summary>La date du jour dans le fuseau de l'entreprise.</summary>
    DateTime Today { get; }
}

public class AppClock : IAppClock
{
    private readonly TimeZoneInfo _timeZone;

    public AppClock(IConfiguration configuration)
    {
        var timeZone = configuration["App:TimeZone"];
        if (string.IsNullOrWhiteSpace(timeZone))
            throw new InvalidOperationException("Le réglage App:TimeZone est manquant dans appsettings.json.");

        _timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZone);
    }

    public DateTime Today => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _timeZone).Date;
}
