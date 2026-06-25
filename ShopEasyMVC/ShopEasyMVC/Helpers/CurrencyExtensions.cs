using System.Globalization;

namespace ShopEasyMVC.Helpers
{
    public static class CurrencyExtensions
    {
        // Cultura de Costa Rica para mostrar montos en colones (₡) sin afectar
        // el análisis/validación de decimales del resto de la aplicación.
        private static readonly CultureInfo CostaRica = CultureInfo.GetCultureInfo("es-CR");

        public static string ToColones(this decimal value) => value.ToString("C", CostaRica);
    }
}
