namespace Identity.Services
{
    public interface IInventoryClient
    {
        Task DeleteHouseholdProductsAsync(Guid householdId);
    }

    public class InventoryClient(HttpClient http) : IInventoryClient
    {
        public async Task DeleteHouseholdProductsAsync(Guid householdId)
        {
            var response = await http.DeleteAsync($"internal/household/{householdId}/products");

            response.EnsureSuccessStatusCode();
        }
    }
}
