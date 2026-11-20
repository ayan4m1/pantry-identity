namespace Identity
{
    public interface IShoppingClient
    {
        Task DeleteHouseholdStockAsync(Guid householdId);
    }

    public class ShoppingClient(HttpClient http) : IShoppingClient
    {
        public async Task DeleteHouseholdStockAsync(Guid householdId)
        {
            var response = await http.DeleteAsync($"internal/household/{householdId}/items");

            response.EnsureSuccessStatusCode();
        }
    }
}
