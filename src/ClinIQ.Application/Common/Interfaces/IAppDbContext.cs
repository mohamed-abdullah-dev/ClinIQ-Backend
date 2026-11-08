using ClinIQ.Domain.Specialties;
using Microsoft.EntityFrameworkCore;

namespace ClinIQ.Application.Common.Interfaces;

public interface IAppDbContext
{
    DbSet<Specialty> Specialties { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
