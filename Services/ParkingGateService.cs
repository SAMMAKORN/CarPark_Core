using CarPark.Data;
using CarPark.Models;
using CarPark.Shared;
using Microsoft.EntityFrameworkCore;

namespace CarPark.Services
{
    public sealed class ParkingGateService(
        IDbContextFactory<AppDbContext> dbContextFactory,
        CurrentUserContext currentUserContext)
    {
        public async Task<List<ParkingGate>> GetByLotIdAsync(Guid parkingLotId, CancellationToken cancellationToken = default)
        {
            await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);

            return await db.ParkingGates
                .AsNoTracking()
                .Where(x => x.ParkingLotId == parkingLotId)
                .OrderBy(x => x.GateName)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<ParkingGate>> GetActiveByLotIdAsync(Guid parkingLotId, CancellationToken cancellationToken = default)
        {
            await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);

            return await db.ParkingGates
                .AsNoTracking()
                .Where(x => x.ParkingLotId == parkingLotId && x.IsActive)
                .OrderBy(x => x.GateName)
                .ToListAsync(cancellationToken);
        }

        public async Task<ParkingGate> CreateAsync(ParkingGate gate, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(gate.GateName))
                throw new InvalidOperationException("กรุณากรอกชื่อประตู");

            await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);

            if (!await db.ParkingLots.AnyAsync(x => x.Id == gate.ParkingLotId, cancellationToken))
                throw new InvalidOperationException("ไม่พบลานจอดรถ");

            var entity = new ParkingGate
            {
                ParkingLotId = gate.ParkingLotId,
                GateName = gate.GateName.Trim(),
                IsActive = gate.IsActive,
            };
            entity.SetCreated(currentUserContext.CurrentUserId);

            db.ParkingGates.Add(entity);
            await db.SaveChangesAsync(cancellationToken);
            return entity;
        }

        public async Task UpdateAsync(ParkingGate gate, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(gate.GateName))
                throw new InvalidOperationException("กรุณากรอกชื่อประตู");

            await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);

            var existing = await db.ParkingGates.FirstOrDefaultAsync(x => x.Id == gate.Id, cancellationToken)
                ?? throw new InvalidOperationException("ไม่พบข้อมูลประตู");

            existing.GateName = gate.GateName.Trim();
            existing.IsActive = gate.IsActive;
            existing.SetUpdated(currentUserContext.CurrentUserId);

            await db.SaveChangesAsync(cancellationToken);
        }

        public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);

            var existing = await db.ParkingGates.FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
                ?? throw new InvalidOperationException("ไม่พบข้อมูลประตู");

            existing.SetDeleted(currentUserContext.CurrentUserId);
            await db.SaveChangesAsync(cancellationToken);
        }
    }
}