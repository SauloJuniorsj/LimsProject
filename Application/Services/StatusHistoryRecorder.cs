using System.Security.Claims;
using LimsProject.Application.Interfaces;
using LimsProject.Domain.Entities;
using LimsProject.Domain.Enums;

namespace LimsProject.Application.Services;

public static class StatusHistoryRecorder
{
    public static BatchStatusHistory Record(
        ILimsDbContext db,
        Guid batchId,
        BatchStatus? from,
        BatchStatus to,
        ClaimsPrincipal user,
        string? reason = null)
    {
        var entry = new BatchStatusHistory
        {
            BatchId = batchId,
            FromStatus = from,
            ToStatus = to,
            ChangedBy = user.FindFirstValue(ClaimTypes.Email) ?? "anonymous",
            Reason = reason
        };
        db.BatchStatusHistories.Add(entry);
        return entry;
    }
}
