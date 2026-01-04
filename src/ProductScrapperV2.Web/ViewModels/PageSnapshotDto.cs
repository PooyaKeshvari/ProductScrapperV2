namespace ProductScrapperV2.Web.ViewModels;


public sealed class LinkDto
{
    public string Text { get; init; } = "";
    public string Href { get; init; } = "";
    public string CssHint { get; init; } = "";     // selector hint
}

public sealed class TextBlockDto
{
    public string Text { get; init; } = "";
    public string CssHint { get; init; } = "";     // selector hint
}

public sealed class PageSnapshotDto
{
    public string Url { get; init; } = "";
    public string Title { get; init; } = "";
    public IReadOnlyCollection<string> VisibleTexts { get; init; } = [];
    public IReadOnlyCollection<LinkSnapshotDto> Links { get; init; } = [];

    // 👇 مهم
    public string FullPageText { get; init; } = "";
}


public sealed class LinkSnapshotDto
{
    public string Text { get; init; } = "";
    public string Href { get; init; } = "";
    public string CssSelector { get; init; } = "";
}