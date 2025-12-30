namespace ProductScrapperV2.Application.Interfaces;

public interface IClock
{
    DateTimeOffset UtcNow { get; }
}
