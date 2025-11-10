using System;

namespace MiTiendaConLogin.Helpers
{
    public static class TimeZoneHelper
    {
        // La zona horaria de Costa Rica es UTC-6
        private const int CostaRicaOffset = -6;

        public static DateTime ToCostaRicaTime(DateTime utcDate)
        {
            // Convierte la fecha UTC sumando (en este caso, restando) 
            // las horas de diferencia.
            return utcDate.AddHours(CostaRicaOffset);
        }
    }
}