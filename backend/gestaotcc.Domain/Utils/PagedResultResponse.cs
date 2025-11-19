namespace gestaotcc.Domain.Utils;

public class PagedResultResponse<T>
{
    public T Data { get; set; }
    public int TotalRecords { get; set; }
    public long CurrentPageNumber { get; set; }
    public long CurrentPageSize { get; set; }
    public long? TotalPages { get; set; }

    public PagedResultResponse(T data, int totalRecords, long currentPageNumber, long currentPageSize, long totalPages = 1)
    {
        Data = data;
        TotalRecords = totalRecords;
        CurrentPageNumber = currentPageNumber;
        CurrentPageSize = currentPageSize;
        TotalPages = totalPages;
    }
}
