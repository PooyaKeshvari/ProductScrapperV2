namespace ProductScrapperV2.Application.DTOs;

public record ProductDto(Guid Id, string Name, string? Sku, decimal OwnPrice);
