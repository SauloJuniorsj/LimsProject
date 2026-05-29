namespace LimsProject.Application.Models;

public record PagedResult<T>(IEnumerable<T> Items, int Page, int PageSize, int TotalCount)
{
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}
