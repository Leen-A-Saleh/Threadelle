namespace Threadelle.Helpers
{
    public static class CurrencyFormatter
    {
        public static string Format(decimal amount)
        {
            return $"{amount:0.00} JOD";
        }
    }
}
