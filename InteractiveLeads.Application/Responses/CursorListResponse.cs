namespace InteractiveLeads.Application.Responses;

public sealed class CursorListResponse<T> : BaseResponse, IResponse
{
    public CursorListResponse()
    {
        Items = new List<T>();
    }

    public CursorListResponse(IEnumerable<T> items, bool hasMore, DateTimeOffset? nextCursor)
    {
        Items = items.ToList();
        HasMore = hasMore;
        NextCursor = nextCursor;
    }

    public List<T> Items { get; set; }

    /// <summary>
    /// Indicates that there are more items available beyond this page.
    /// </summary>
    public bool HasMore { get; set; }

    /// <summary>
    /// Cursor value (typically based on CreatedAt/LastMessageAt) that should be
    /// sent back to retrieve the next page.
    /// </summary>
    public DateTimeOffset? NextCursor { get; set; }
}

