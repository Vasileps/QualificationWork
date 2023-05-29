using MessengerApp.Connection.Schemas;
using System;

namespace MessengerApp
{
    public static class Extensions
    {
        private static TimeSpan dayTimeSpan = TimeSpan.FromDays(1);

        public static HttpRequestMessage Clone(this HttpRequestMessage message)
        {
            HttpRequestMessage clone = new HttpRequestMessage(message.Method, message.RequestUri);

            clone.Content = message.Content;
            clone.Version = message.Version;

            foreach (var option in message.Options)
                clone.Options.Set(new HttpRequestOptionsKey<object>(option.Key), option.Value);

            foreach (var header in message.Headers)
                clone.Headers.TryAddWithoutValidation(header.Key, header.Value);

            return clone;
        }

        public static string ToDisplayableString(this DateTime time, bool localTime = false)
        {
            string pattern;
            var startOfThisYear = new DateTime(DateTime.UtcNow.Year, 1, 1);

            if (time < startOfThisYear) pattern = "dd.MM.yy HH:mm";
            else if (DateTime.UtcNow - time > dayTimeSpan) pattern = "dd.MM HH:mm";
            else pattern = "HH:mm";

            var finalTime = localTime ? time : time.ToLocalTime();

            return finalTime.ToString(pattern);
        }

        public static string ToDisplayableString(this Message message)
        {
            return message.Type switch
            {
                MessageType.Text => message.Data,
                _ => string.Empty
            };
        }
    }
}
