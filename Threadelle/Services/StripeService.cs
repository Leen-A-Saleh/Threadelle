using Microsoft.Extensions.Configuration;
using Threadelle.Models;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;

namespace Threadelle.Services
{
    public class StripeService : IStripeService
    {
        private readonly IConfiguration _config;
        private readonly HttpClient _http;

        public StripeService(IConfiguration config)
        {
            _config = config;
            _http = new HttpClient();
        }

        public bool IsSimulationMode
        {
            get
            {
                var key = _config["Stripe:SecretKey"];
                return string.IsNullOrWhiteSpace(key) || key == "mock";
            }
        }

        public string PublishableKey
        {
            get
            {
                var key = _config["Stripe:PublishableKey"];
                return string.IsNullOrWhiteSpace(key) ? "pk_test_mock" : key;
            }
        }

        public async Task<string> CreateCheckoutSessionAsync(string checkoutJson, decimal totalPrice, string successUrl, string cancelUrl)
        {
            if (IsSimulationMode)
            {
                // Return our custom mock Stripe checkout URL
                return $"/Checkout/SimulateStripe"; // Handled via TempData
            }

            var key = _config["Stripe:SecretKey"];
            var request = new HttpRequestMessage(HttpMethod.Post, "https://api.stripe.com/v1/checkout/sessions");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", key);

            // Send checkoutJson inside metadata. 
            // Warning: Stripe limits metadata value to 500 characters. 
            // We can chunk it if it's too long, or use multiple keys.
            var chunks = ChunkString(checkoutJson, 499);
            
            var form = new List<KeyValuePair<string, string>>
            {
                new("payment_method_types[0]", "card"),
                new("mode", "payment"),
                new("success_url", successUrl + "?session_id={CHECKOUT_SESSION_ID}"),
                new("cancel_url", cancelUrl),
                new("line_items[0][price_data][currency]", "usd"),
                new("line_items[0][price_data][product_data][name]", "Threadelle Checkout"),
                new("line_items[0][price_data][unit_amount]", ((int)(totalPrice * 100)).ToString()),
                new("line_items[0][quantity]", "1")
            };

            for (int i = 0; i < chunks.Count; i++)
            {
                form.Add(new($"metadata[chk_{i}]", chunks[i]));
            }

            request.Content = new FormUrlEncodedContent(form);
            var response = await _http.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception($"Stripe API error: {error}");
            }

            var content = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(content);
            return doc.RootElement.GetProperty("url").GetString() ?? "";
        }

        private static List<string> ChunkString(string str, int maxChunkSize)
        {
            var list = new List<string>();
            for (int i = 0; i < str.Length; i += maxChunkSize)
            {
                list.Add(str.Substring(i, Math.Min(maxChunkSize, str.Length - i)));
            }
            return list;
        }

        public async Task<string> CreatePaymentIntentAsync(decimal amount)
        {
            if (IsSimulationMode)
            {
                return "pi_mock_secret_" + Guid.NewGuid().ToString("N");
            }

            var key = _config["Stripe:SecretKey"];
            var request = new HttpRequestMessage(HttpMethod.Post, "https://api.stripe.com/v1/payment_intents");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", key);

            var form = new List<KeyValuePair<string, string>>
            {
                new("amount", ((int)(amount * 100)).ToString()),
                new("currency", "usd"),
                new("automatic_payment_methods[enabled]", "true")
            };

            request.Content = new FormUrlEncodedContent(form);
            var response = await _http.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception($"Stripe API error: {error}");
            }

            var content = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(content);
            return doc.RootElement.GetProperty("client_secret").GetString() ?? "";
        }
    }
}
