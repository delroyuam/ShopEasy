using System.Globalization;

namespace ShopEasyMVC.Helpers
{
    public static class CurrencyExtensions
    {
        private static readonly CultureInfo CostaRica = CultureInfo.GetCultureInfo("es-CR");

        public static string ToColones(this decimal value) => value.ToString("C", CostaRica);
    }
}
