namespace web_DACS.Services.Interfaces
{
    public interface IApiResponse
    {
        bool Status { get; }
        string Message { get; }
        object? Data { get; }
    }

    public class ApiResponse : IApiResponse, Microsoft.AspNetCore.Mvc.IActionResult
    {
        public bool Status { get; set; }
        public string Message { get; set; } = string.Empty;
        public object? Data { get; set; }

        public ApiResponse() { }

        public ApiResponse(bool status, string message, object? data = null)
        {
            Status = status;
            Message = message;
            Data = data;
        }

        public static ApiResponse Ok(string message = "Thành công", object? data = null)
            => new(true, message, data);

        public static ApiResponse Fail(string message, object? data = null)
            => new(false, message, data);

        // Implement IActionResult để controller có thể return trực tiếp ApiResponse
        // mà không cần bọc Ok()/BadRequest() → tránh double-wrap
        public async Task ExecuteResultAsync(Microsoft.AspNetCore.Mvc.ActionContext context)
        {
            var json = System.Text.Json.JsonSerializer.Serialize(this,
                new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                });

            context.HttpContext.Response.ContentType = "application/json";
            await context.HttpContext.Response.WriteAsync(json);
        }
    }
}
