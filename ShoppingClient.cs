namespace Identity
{
    public interface IShoppingClient
    {
        Task DeleteHouseholdProductsAsync(Guid householdId);
    }

    public class ShoppingClient(HttpClient http) : IShoppingClient
    {
        public async Task DeleteHouseholdProductsAsync(Guid householdId)
        {
            var response = await http.DeleteAsync($"internal/household/{householdId}/products");

            response.EnsureSuccessStatusCode();
        }
    }
}
