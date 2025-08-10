using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KuyumStokApi.Application.Common
{
    public class ApiResult<T>
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public List<string>? Errors { get; set; }
        public T? Data { get; set; }
        public int StatusCode { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string TraceId { get; set; } = Guid.NewGuid().ToString();

        public static ApiResult<T> Ok(T data, string message = "", int statusCode = 200) =>
            new ApiResult<T>
            {
                Success = true,
                Message = message,
                Data = data,
                StatusCode = statusCode
            };

        public static ApiResult<T> Fail(string message, List<string>? errors = null, int statusCode = 400) =>
            new ApiResult<T>
            {
                Success = false,
                Message = message,
                Errors = errors,
                StatusCode = statusCode
            };
    }
    public sealed class PagedResult<TItem>
    {
        public IReadOnlyList<TItem> Items { get; init; } = Array.Empty<TItem>();
        public int Page { get; init; }
        public int PageSize { get; init; }
        public long TotalCount { get; init; }
    }

}
