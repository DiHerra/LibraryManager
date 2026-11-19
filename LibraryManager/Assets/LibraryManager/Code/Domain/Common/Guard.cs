using System;

namespace LibraryManager.Domain.Common
{
    /// <summary>Validaciones de argumentos compartidas por el dominio.</summary>
    public static class Guard
    {
        public static string NotBlank(string value, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("El valor no puede estar vacío.", parameterName);
            }

            return value.Trim();
        }

        public static T NotNull<T>(T value, string parameterName) where T : class
        {
            if (value == null)
            {
                throw new ArgumentNullException(parameterName);
            }

            return value;
        }

        public static long NotNegative(long value, string parameterName)
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(parameterName, value, "El valor no puede ser negativo.");
            }

            return value;
        }

        public static int InRange(int value, int minimum, int maximum, string parameterName)
        {
            if (value < minimum || value > maximum)
            {
                throw new ArgumentOutOfRangeException(
                    parameterName,
                    value,
                    $"El valor debe estar entre {minimum} y {maximum}.");
            }

            return value;
        }
    }
}
