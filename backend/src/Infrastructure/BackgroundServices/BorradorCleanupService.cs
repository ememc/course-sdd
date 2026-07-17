using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using Domain.Enums;

namespace Infrastructure.BackgroundServices
{
    public class BorradorCleanupService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<BorradorCleanupService> _logger;
        private readonly TimeSpan _checkInterval = TimeSpan.FromHours(6); // Run every 6 hours

        public BorradorCleanupService(IServiceScopeFactory scopeFactory, ILogger<BorradorCleanupService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Borrador Cleanup Service starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CleanExpiredDraftsAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred executing CleanExpiredDrafts.");
                }

                await Task.Delay(_checkInterval, stoppingToken);
            }

            _logger.LogInformation("Borrador Cleanup Service stopping.");
        }

        private async Task CleanExpiredDraftsAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Running expired drafts cleanup...");
            using var scope = _scopeFactory.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<MediatR.ISender>();
            await mediator.Send(new Application.Solicitudes.Commands.CleanExpiredDrafts.CleanExpiredDraftsCommand(), cancellationToken);
            _logger.LogInformation("Expired drafts cleanup complete.");
        }
    }
}
