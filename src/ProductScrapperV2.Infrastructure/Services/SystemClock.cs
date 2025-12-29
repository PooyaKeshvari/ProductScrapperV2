using ProductScrapperV2.Application.Interfaces;

namespace ProductScrapperV2.Infrastructure.Services;

public class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
