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

        public async Task<PagedResult<Person>> GetPersonsAsync(int page, int pageSize)
        {
            var url = $"https://localhost:7029/persons?page={page}&pageSize={pageSize}";
            return await _httpClient.GetFromJsonAsync<PagedResult<Person>>(url);
        }
    }
    public class PagedResult<T>
    {
        public int TotalItems { get; set; }
        public int TotalPages { get; set; }
        public List<T> Items { get; set; }
    }
}
