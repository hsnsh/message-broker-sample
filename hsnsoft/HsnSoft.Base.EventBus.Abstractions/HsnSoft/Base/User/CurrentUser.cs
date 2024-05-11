using System;
using System.Linq;
using System.Security.Claims;
using HsnSoft.Base.DependencyInjection;

namespace HsnSoft.Base.User;

public class CurrentUser : ICurrentUser, ITransientDependency
{
    private static readonly Claim[] EmptyClaimsArray = Array.Empty<Claim>();

    public virtual bool IsAuthenticated => Id.HasValue;

    public virtual Guid? Id => Guid.NewGuid();

    public virtual string UserName => "scope-user";

    public virtual string Name => "Scope";

    public virtual string SurName => "User";

    public virtual string PhoneNumber => null;

    public virtual bool PhoneNumberVerified => false;

    public virtual string Email => "hsnsh@outlook.com";

    public virtual bool EmailVerified => true;

    public virtual Guid? TenantId => null;
    public virtual string TenantDomain => null;

    public virtual string[] Roles => new[] { "scope-user" };

    public virtual Claim FindClaim(string claimType)
    {
        return null;
    }

    public virtual Claim[] FindClaims(string claimType)
    {
        return EmptyClaimsArray;
    }

    public virtual Claim[] GetAllClaims()
    {
        return EmptyClaimsArray;
    }

    public virtual bool IsInRole(string roleName)
    {
        return false;
    }
}