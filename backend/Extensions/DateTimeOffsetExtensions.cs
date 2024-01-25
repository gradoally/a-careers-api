namespace System
{
    using System.Globalization;

    /// <summary>
    /// Вспомогательные функции по работе с <see cref="DateTimeOffset"/>.
    /// </summary>
    /// <remarks>
    /// Взяты с https://bitbucket.org/snippets/just_dmitry/LEaRG/datetimeoffset-extensions.
    /// </remarks>
    public static class DateTimeOffsetExtensions
    {
        private static readonly TimeZoneInfo MoscowZoneInfo = GetMoscowTimeZone();

        /// <summary>
        /// Переводит время в часовой пояс Москвы (абсолютная точка на оси времени остается).
        /// </summary>
        /// <param name="value">Исходное значение даты-времени.</param>
        /// <returns>Изменённое значение даты-времени.</returns>
        public static DateTimeOffset ToMoscowTime(this DateTimeOffset value)
        {
            return TimeZoneInfo.ConvertTime(value, MoscowZoneInfo);
        }

        /// <summary>
        /// Переписывает время в часовой пояс Москвы (локальное время остается, абсолютная точка сдвигается).
        /// </summary>
        /// <param name="value">Исходное значение даты-времени.</param>
        /// <returns>Измененное значение даты-времени.</returns>
        public static DateTimeOffset AsMoscowTime(this DateTimeOffset value)
        {
            return new DateTimeOffset(value.Year, value.Month, value.Day, value.Hour, value.Minute, value.Second, value.Millisecond, MoscowZoneInfo.BaseUtcOffset);
        }

        /// <summary>
        /// Преобразовывает указанное значение в строку согласно шаблону <c>G zzz</c> (краткая дата, полное время, часовой пояс).
        /// </summary>
        /// <param name="value">Значение даты-времени.</param>
        /// <param name="formatProvider">Объект с настройками представления даты и времени (если не указано, то будет использоваться <see cref="CultureInfo.CurrentCulture"/>).</param>
        /// <returns>Строка с текстовым представлением даты, времени и часового пояса.</returns>
        public static string ToStringWithZone(this DateTimeOffset value, IFormatProvider? formatProvider = null)
        {
            return string.Format(formatProvider ?? CultureInfo.CurrentCulture, "{0:G} {0:zzz}", value);
        }

        /// <summary>
        /// Converts to string with time only (pattern 'T') if value is less than 3 hour before now,
        ///   to string with date and time (pattern 'd  t') if value is less than 3 days before now,
        ///   to string with day and month (pattern 'd MMM') if value is less than 90 days before now,
        ///   and to string with date (pattern 'd') otherwise.
        /// </summary>
        /// <param name="value">Исходное значение даты-времени.</param>
        /// <param name="formatProvider">Объект с настройками представления даты и времени (если не указано, то будет использоваться <see cref="CultureInfo.CurrentCulture"/>).</param>
        /// <returns>Строковое представление указанной даты и/или времени.</returns>
        public static string ToFriendString(this DateTimeOffset value, IFormatProvider? formatProvider = null)
        {
            var elapsed = DateTimeOffset.Now.Subtract(value);
            formatProvider ??= CultureInfo.CurrentCulture;

            if (elapsed.Duration().TotalHours < 3)
            {
                return value.ToString("T", formatProvider);
            }

            if (elapsed.Duration().TotalDays < 3)
            {
                return value.ToString("d MMM", formatProvider) + " " + value.ToString("t", formatProvider);
            }

            if (elapsed.Duration().TotalDays < 90)
            {
                return value.ToString("d MMM", formatProvider);
            }

            return value.ToString("d", formatProvider);
        }

        public static string ToIsoString(this DateTimeOffset value)
        {
            return value.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss\\Z", DateTimeFormatInfo.InvariantInfo);
        }

        public static string ToInvertedTicks(this DateTimeOffset value)
        {
            return (DateTimeOffset.MaxValue.Ticks - value.UtcTicks).ToString(DateTimeFormatInfo.InvariantInfo).PadLeft(19, '0');
        }

        public static DateTimeOffset FromInvertedTicks(this string value)
        {
            var ticks = long.Parse(value, DateTimeFormatInfo.InvariantInfo);
            return new DateTimeOffset(DateTimeOffset.MaxValue.Ticks - ticks, TimeSpan.Zero);
        }

        public static DateTimeOffset Truncate(this DateTimeOffset value, TimeSpan timeSpan)
        {
            if (timeSpan == TimeSpan.Zero)
            {
                return value;
            }

            if (value == DateTimeOffset.MinValue || value == DateTimeOffset.MaxValue)
            {
                return value; // do not modify "guard" values
            }

            return value.AddTicks(-(value.Ticks % timeSpan.Ticks));
        }

        private static TimeZoneInfo GetMoscowTimeZone()
        {
            // Try both Windows and IANA names, until https://github.com/dotnet/corefx/issues/2538
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById("Russian Standard Time");
            }
            catch (TimeZoneNotFoundException)
            {
                try
                {
                    return TimeZoneInfo.FindSystemTimeZoneById("Europe/Moscow");
                }
                catch (TimeZoneNotFoundException)
                {
                    return TimeZoneInfo.Local;
                }
            }
        }
    }
}