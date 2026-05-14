using CarPark.Data;
using Microsoft.EntityFrameworkCore;

namespace CarPark.Services
{
    public sealed class DatabaseHealthService : IDisposable
    {
        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
        private readonly CancellationTokenSource _cts = new();

        public bool IsConnected { get; private set; } = true;

        /// <summary>เรียกทุกครั้งที่สถานะเปลี่ยน (connected ↔ disconnected)</summary>
        public event Action? OnStatusChanged;

        public DatabaseHealthService(IDbContextFactory<AppDbContext> dbContextFactory)
        {
            _dbContextFactory = dbContextFactory;
            _ = StartMonitoringAsync();
        }

        private async Task StartMonitoringAsync()
        {
            // ตรวจสอบทันทีตอน start
            await CheckAsync();

            while (!_cts.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(30), _cts.Token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }

                await CheckAsync();
            }
        }

        private async Task CheckAsync()
        {
            bool connected;
            try
            {
                await using var db = await _dbContextFactory.CreateDbContextAsync(_cts.Token);
                connected = await db.Database.CanConnectAsync(_cts.Token);
            }
            catch
            {
                connected = false;
            }

            if (IsConnected != connected)
            {
                IsConnected = connected;
                OnStatusChanged?.Invoke();
            }
        }

        public void Dispose()
        {
            _cts.Cancel();
            _cts.Dispose();
        }
    }
}
