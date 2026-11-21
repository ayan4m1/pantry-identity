namespace Identity
{
    public interface IInventoryClient
    {
        Task DeleteHouseholdStockAsync(Guid householdId, CancellationToken ct);
    }

    public class InventoryClient(HttpClient http) : IInventoryClient
    {
        public async Task DeleteHouseholdStockAsync(Guid householdId, CancellationToken ct)
        {
            var response = await http.DeleteAsync($"internal/household/{householdId}/stock", ct);

            response.EnsureSuccessStatusCode();
        }
    }
}
