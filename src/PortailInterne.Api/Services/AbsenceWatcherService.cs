namespace PortailInterne.Api.Services;

/// <summary>Chaque minute, signale les retours tardifs et non confirmés (R24).</summary>
public class AbsenceWatcherService : BackgroundService
{
	private readonly IServiceScopeFactory _scopes;
	private readonly ILogger<AbsenceWatcherService> _logger;

	public AbsenceWatcherService(IServiceScopeFactory scopes, ILogger<AbsenceWatcherService> logger)
	{
		_scopes = scopes;
		_logger = logger;
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));

		do
		{
			try
			{
				using var scope = _scopes.CreateScope();
				await scope.ServiceProvider.GetRequiredService<AbsenceWatcher>().RunOnceAsync();
			}
			catch (Exception ex)
			{
				// Une erreur ne doit pas arrêter la tâche : on réessaie à la minute suivante
				_logger.LogError(ex, "La vérification des absences a échoué.");
			}
		}
		while (await timer.WaitForNextTickAsync(stoppingToken));
	}
}
