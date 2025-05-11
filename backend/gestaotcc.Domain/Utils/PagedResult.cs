namespace gestaotcc.Domain.Utils;

public class PagedResult<T>
{
    public T Pages { get; set; }
    public int TotalRecords { get; set; }
}