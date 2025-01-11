using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Domain.Entities;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace POSIMSWebApi.Interceptors
{
    public class AuditInterceptor : SaveChangesInterceptor
    {
        private readonly IHttpContextAccessor _contextAccessor;
        public AuditInterceptor(IHttpContextAccessor contextAccessor)
        {
            _contextAccessor = contextAccessor;
        }
        public override InterceptionResult<int> SavingChanges(
            DbContextEventData eventData,
            InterceptionResult<int> result
        )
        {
            if (eventData.Context == null) return result;
            foreach(var entry in eventData.Context.ChangeTracker.Entries())
            {
                var claimuserId = _contextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (Guid.TryParse(claimuserId, out Guid userId))
                {
                    if (entry.Entity is AuditedEntity auditableEntity)
                    {
                        if (entry.State == EntityState.Added)
                        {
                            auditableEntity.CreationTime = DateTimeOffset.UtcNow;
                            //TODO: change it once auth is implemented
                            auditableEntity.CreatedBy = userId;
                        }
                        if (entry.State == EntityState.Modified)
                        {
                            auditableEntity.ModifiedBy = userId;
                            auditableEntity.ModifiedTime = DateTimeOffset.UtcNow;
                        }
                    }
                }
                else
                {
                    throw new Exception("Error! Interceptor can't find UserId");
                }
                
            }
            return base.SavingChanges(eventData, result);
        }


    }
}
