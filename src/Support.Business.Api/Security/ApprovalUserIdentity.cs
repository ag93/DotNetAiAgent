using System.Security.Claims;

namespace Support.Business.Api.Security;

public static class ApprovalUserIdentity
{
    private const string EntraObjectIdClaim =
        "http://schemas.microsoft.com/identity/claims/objectidentifier";

    public static string? GetObjectId(ClaimsPrincipal user)
    {
        var objectId = user.FindFirst("oid")?.Value
            ?? user.FindFirst(EntraObjectIdClaim)?.Value;

        return Guid.TryParse(objectId, out var parsed)
            && parsed != Guid.Empty
            ? parsed.ToString("D")
            : null;
    }
}
