using SmartHorse.Application.Common.Interfaces;

namespace SmartHorse.Infrastructure.Services;

public class DateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
}
