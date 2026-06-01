namespace LimsProject.Domain.Common;

/// <summary>
/// Entidades que mantêm trilha de quem criou e atualizou. O DbContext preenche
/// CreatedAt/CreatedBy em Added e UpdatedAt/UpdatedBy em Modified — endpoints
/// não precisam tocar nesses campos.
/// </summary>
public interface IAuditable
{
    DateTime CreatedAt { get; set; }
    string? CreatedBy { get; set; }
    DateTime? UpdatedAt { get; set; }
    string? UpdatedBy { get; set; }
}
