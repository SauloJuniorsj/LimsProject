namespace LimsProject.Features.Traceability;

public interface IChainOfCustodyWriter
{
    /// <summary>Persistido junto com <see cref="Microsoft.EntityFrameworkCore.DbContext.SaveChangesAsync"/> do chamador.</summary>
    void Append(Guid batchId, string eventType, object? payload, Guid? operatorId);
}
