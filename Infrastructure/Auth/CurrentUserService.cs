using System.Security.Claims;
using LimsProject.Application.Interfaces;
using Microsoft.AspNetCore.Http;

namespace LimsProject.Infrastructure.Auth;

public class CurrentUserService(IHttpContextAccessor accessor) : ICurrentUserService
{
    public string? GetEmail() =>
        accessor.HttpContext?.User?.FindFirstValue(ClaimTypes.Email);
}
