// <copyright file="DateTimeOffsetConverter.cs" company="wut">
//   Copyright © wut
// </copyright>

using System;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace GraphQL.Conversion
{
    public class DateTimeOffsetConverter
    {
        public const string Today = "TODAY";

        private static readonly Regex iso8601Expression = new Regex(@"^([\+-]?\d{4}(?!\d{2}\b))((-?)((0[1-9]|1[0-2])(\3([12]\d|0[1-9]|3[01]))?|W([0-4]\d|5[0-2])(-?[1-7])?|(00[1-9]|0[1-9]\d|[12]\d{2}|3([0-5]\d|6[1-6])))([T\s]((([01]\d|2[0-3])((:?)[0-5]\d)?|24\:?00)([\.,]\d+(?!:))?)?(\17[0-5]\d([\.,]\d+)?)?([zZ]|([\+-])([01]\d|2[0-3]):?([0-5]\d)?)?)?)?$");

        public static DateTimeOffset GetDateTime(string dateString)
        {
            var trimmedString = dateString.Trim();
            if (trimmedString == Today)
            {
                return DateTimeOffset.UtcNow.DateTime;
            }

            if (trimmedString.Contains(Today))
            {
                var dayString = trimmedString.Substring(5, trimmedString.Length - 5);
                var days = int.Parse(dayString);

                return DateTimeOffset.UtcNow.DateTime.AddDays(days);
            }

            if (IsDayOfWeek(dateString))
            {
                return ConvertToDateFromDayAndTime(dateString);
            }

            if (iso8601Expression.IsMatch(trimmedString))
            {
                //Thank you jon skeet : http://stackoverflow.com/questions/10029099/DateTimeOffset-parse2012-09-30t230000-0000000z-always-converts-to-datetimekind-l
                var success = DateTimeOffset.TryParse(trimmedString, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var result);

                if (success)
                {
                    return result;
                }
            }

            return DateTimeOffset.Parse(trimmedString);
        }

        private static DateTimeOffset ConvertToDateFromDayAndTime(string dateString)
        {
            dateString = dateString.Replace("  ", " ");
            var parts = dateString.Split(' ');
            var day = (DayOfWeek)Enum.Parse(typeof(DayOfWeek), parts[0], true);
            var minutes = MinutesFrom24HourTime(parts[1]);

            var date = DateTimeOffset.UtcNow.DateTime.AddMinutes(minutes);
            while (date.DayOfWeek != day)
            {
                date = date.AddDays(1);
            }

            return date;
        }

        private static bool IsDayOfWeek(string text)
        {
            var days = Enum.GetNames(typeof(DayOfWeek));
            return days.FirstOrDefault(x => text.ToLower().StartsWith(x.ToLower())) != null;
        }

        private static int MinutesFrom24HourTime(string time)
        {
            var parts = time.Split(':');
            return 60 * int.Parse(parts[0]) + int.Parse(parts[1]);
        }
    }
}
