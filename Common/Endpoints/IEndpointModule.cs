using Microsoft.AspNetCore.Routing;

namespace LimsProject.Common.Endpoints;

/// <summary>Optional marker for reflection-based endpoint discovery.</summary>
public interface IEndpointModule
{
    static abstract void MapEndpoints(IEndpointRouteBuilder app);
}
