using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace SkyHelp.Context
{
    // PostgreSQL (Npgsql) exige DateTime con Kind=UTC para "timestamp with time zone".
    // SQL Server aceptaba datetime2 sin Kind; este conversor evita el error al insertar.
    internal sealed class UtcDateTimeConverter : ValueConverter<DateTime, DateTime>
    {
        public UtcDateTimeConverter()
            : base(toProvider => ToUtc(toProvider), fromProvider => DateTime.SpecifyKind(fromProvider, DateTimeKind.Utc))
        {
        }

        private static DateTime ToUtc(DateTime value) => value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };
    }

    internal sealed class UtcNullableDateTimeConverter : ValueConverter<DateTime?, DateTime?>
    {
        public UtcNullableDateTimeConverter()
            : base(
                toProvider => toProvider.HasValue ? ToUtc(toProvider.Value) : toProvider,
                fromProvider => fromProvider.HasValue
                    ? DateTime.SpecifyKind(fromProvider.Value, DateTimeKind.Utc)
                    : fromProvider)
        {
        }

        private static DateTime ToUtc(DateTime value) => value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };
    }
}
