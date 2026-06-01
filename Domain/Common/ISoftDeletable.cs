namespace LimsProject.Domain.Common;

/// <summary>
/// Entidades cujo DELETE deve preservar o registro fisicamente (rastreabilidade
/// regulatória). O DbContext intercepta EntityState.Deleted e converte pra UPDATE
/// setando DeletedAt/DeletedBy. Combinado com global query filter, ficam
/// invisíveis pra queries normais — use IgnoreQueryFilters() pra inspecionar.
/// </summary>
public interface ISoftDeletable
{
    DateTime? DeletedAt { get; set; }
    string? DeletedBy { get; set; }
}
