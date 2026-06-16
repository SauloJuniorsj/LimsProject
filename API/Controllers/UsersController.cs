using System.Security.Claims;
using Asp.Versioning;
using LimsProject.Application.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LimsProject.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("users")]
[Authorize(Policy = "AdminOnly")]
public class UsersController(UserManager<IdentityUser> userMgr) : ControllerBase
{
    private static readonly string[] ValidRoles = ["Lab", "Admin"];

    [HttpGet]
    public async Task<IResult> List(int page = 1, int pageSize = 50, string? email = null)
    {
        pageSize = Math.Clamp(pageSize, 1, 200);
        page = Math.Max(1, page);

        var query = userMgr.Users.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(email))
            query = query.Where(u => u.Email!.ToLower().Contains(email.ToLower()));

        var total = await query.CountAsync();
        var users = await query
            .OrderBy(u => u.Email)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        // GetRolesAsync é per-user; pra portfolio é OK rodar em loop (n queries),
        // pra prod faria join na AspNetUserRoles + AspNetRoles direto.
        var items = new List<UserListItem>(users.Count);
        foreach (var u in users)
        {
            var roles = await userMgr.GetRolesAsync(u);
            items.Add(new UserListItem(u.Id, u.Email ?? "", u.UserName ?? "", [.. roles]));
        }

        return Results.Ok(new PagedResult<UserListItem>(items, page, pageSize, total));
    }

    [HttpDelete("{id}")]
    public async Task<IResult> Delete(string id)
    {
        var currentId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (id == currentId)
            return Results.Problem(
                statusCode: StatusCodes.Status422UnprocessableEntity,
                title: "Cannot delete self",
                detail: "Você não pode excluir sua própria conta. Peça pra outro admin.");

        var user = await userMgr.FindByIdAsync(id);
        if (user is null)
            return Results.Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "User not found",
                detail: "Usuário não encontrado.");

        var result = await userMgr.DeleteAsync(user);
        return result.Succeeded
            ? Results.NoContent()
            : Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Delete failed",
                detail: string.Join("; ", result.Errors.Select(e => e.Description)));
    }

    [HttpPut("{id}/role")]
    public async Task<IResult> UpdateRole(string id, [FromBody] RoleUpdateRequest req)
    {
        if (!ValidRoles.Contains(req.Role))
            return Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid role",
                detail: $"Role inválida. Use: {string.Join(", ", ValidRoles)}.");

        var currentId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (id == currentId && req.Role != "Admin")
            return Results.Problem(
                statusCode: StatusCodes.Status422UnprocessableEntity,
                title: "Cannot demote self",
                detail: "Você não pode rebaixar sua própria role de Admin. Peça pra outro admin.");

        var user = await userMgr.FindByIdAsync(id);
        if (user is null)
            return Results.Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "User not found",
                detail: "Usuário não encontrado.");

        var currentRoles = await userMgr.GetRolesAsync(user);
        if (currentRoles.Count > 0)
            await userMgr.RemoveFromRolesAsync(user, currentRoles);
        await userMgr.AddToRoleAsync(user, req.Role);

        return Results.NoContent();
    }
}

public record UserListItem(string Id, string Email, string UserName, string[] Roles);
public record RoleUpdateRequest(string Role);
