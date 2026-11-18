namespace Identity.Services
{
    public interface IInventoryClient
    {
        Task DeleteHouseholdStockAsync(Guid householdId);
    }

    public class InventoryClient(HttpClient http) : IInventoryClient
    {
        public async Task DeleteHouseholdStockAsync(Guid householdId)
        {
            var response = await http.DeleteAsync($"internal/household/{householdId}/stock");

            response.EnsureSuccessStatusCode();
        }
    }
}
