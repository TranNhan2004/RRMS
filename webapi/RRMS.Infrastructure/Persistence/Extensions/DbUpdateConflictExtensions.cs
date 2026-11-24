using Microsoft.EntityFrameworkCore;
using MySqlConnector;
using RRMS.Domain.Exceptions;

namespace RRMS.Infrastructure.Persistence.Extensions;

public static class DbUpdateConflictExtensions
{
    public static bool IsUniqueConstraintViolation(this DbUpdateException exception) =>
        exception.InnerException is MySqlException { Number: 1062 };

    public static ConflictException ToConflictException(this DbUpdateException exception, DbContext context) =>
        new("Duplicate value for unique constraint.");
}
