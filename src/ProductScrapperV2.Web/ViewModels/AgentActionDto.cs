namespace ProductScrapperV2.Web.ViewModels;

public enum AgentActionType
{
    Navigate,
    Click,
    ExtractProduct,
    Stop
}

public sealed class AgentActionDto
{
    public AgentActionType Type { get; init; }
    public string? Url { get; init; }
    public string? CssSelector { get; init; }
    public string? Reason { get; init; }
}
