using BlazorCSVHelper.Shared.DTOs;
using System.Net.Http.Json;

namespace BlazorCSVHelper.Client.APIService
{
    public class ApiService
    {
        private readonly HttpClient _httpClient;

        public ApiService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<PagedResult<Person>> GetPersonsAsync(
            int page = 1,
            int pageSize = 10,
            string? search = null,
            string? sortBy = null,
            bool sortDesc = false)
        {
            var queryParams = new List<string>
            {
                $"page={page}",
                $"pageSize={pageSize}"
            };

            if (!string.IsNullOrWhiteSpace(search))
                queryParams.Add($"search={Uri.EscapeDataString(search)}");

            if (!string.IsNullOrWhiteSpace(sortBy))
                queryParams.Add($"sortBy={Uri.EscapeDataString(sortBy)}");

            if (sortBy != null) // sortDesc makes sense only if sortBy is set
                queryParams.Add($"sortDesc={sortDesc.ToString().ToLower()}");

            var url = $"https://localhost:7029/persons?{string.Join("&", queryParams)}";

            return await _httpClient.GetFromJsonAsync<PagedResult<Person>>(url);
        }
    }

    public class PagedResult<T>
    {
        public int TotalItems { get; set; }
        public int TotalPages { get; set; }
        public int Page { get; set; }         // Optional: helpful if your API returns current page
        public int PageSize { get; set; }     // Optional: helpful if your API returns page size
        public List<T> Items { get; set; } = new();
    }
}
 