using System.Text.Json;
using System.Threading.Tasks;

namespace Threadelle.Services
{
    public interface IStripeService
    {
        bool IsSimulationMode { get; }
        string PublishableKey { get; }
        Task<string> CreateCheckoutSessionAsync(string checkoutJson, decimal totalPrice, string successUrl, string cancelUrl);
        Task<string> CreatePaymentIntentAsync(decimal amount);
    }
}
