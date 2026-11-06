using System.Globalization;
using System.Text;

namespace F1Archive.Helpers
{
    // Як показувати числа, дати й назви на сторінках
    public static class UiFormat
    {
        private static readonly CultureInfo Uk = new("uk-UA");

        // 13 травня 1950
        public static string Date(DateTime date)
        {
            return date.ToString("d MMMM yyyy", Uk);
        }

        // 25 або 2,5 (у старих сезонах були дробові очки)
        public static string Points(double points)
        {
            return points.ToString("0.##", Uk);
        }

        // 27 000
        public static string Number(int value)
        {
            return value.ToString("N0", Uk);
        }

        // 01, 02 ... для номера етапу
        public static string Round(int round)
        {
            return round.ToString("00");
        }

        // 5,891 км
        public static string Length(double? km)
        {
            return km > 0 ? km.Value.ToString("0.###", Uk) + " км" : "—";
        }

        // "1950", "2020–2025" або "—"
        public static string Years(int? first, int? last)
        {
            if (first == null)
            {
                return "—";
            }

            return last == null || last == first ? first.ToString()! : $"{first}–{last}";
        }

        // "1 сезон", "3 сезони", "12 сезонів"
        public static string CountText(int count, string one, string few, string many)
        {
            return count + " " + Plural(count, one, few, many);
        }

        public static string Plural(int number, string one, string few, string many)
        {
            if (number % 100 >= 11 && number % 100 <= 14)
            {
                return many;
            }

            if (number % 10 == 1)
            {
                return one;
            }

            if (number % 10 >= 2 && number % 10 <= 4)
            {
                return few;
            }

            return many;
        }

        // Текст для пошуку: малі літери без наголосів, щоб "raikkonen" знаходило "Räikkönen"
        public static string ForSearch(string? text)
        {
            var result = new StringBuilder();

            foreach (char c in (text ?? "").Trim().Normalize(NormalizationForm.FormD))
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                {
                    result.Append(c);
                }
            }

            return result.ToString().ToLower();
        }

        // Двобуквений код країни для значка на картці (GB, IT ...)
        public static string CountryCode(string country)
        {
            return CountryCodes.GetValueOrDefault(country, "");
        }

        private static readonly Dictionary<string, string> CountryCodes = new()
        {
            ["Велика Британія"] = "GB", ["Італія"] = "IT", ["Франція"] = "FR", ["Аргентина"] = "AR",
            ["США"] = "US", ["Таїланд"] = "TH", ["Монако"] = "MC", ["Швейцарія"] = "CH",
            ["Бельгія"] = "BE", ["Нідерланди"] = "NL", ["Бразилія"] = "BR", ["Німеччина"] = "DE",
            ["Східна Німеччина"] = "DD", ["Австрія"] = "AT", ["Іспанія"] = "ES", ["Австралія"] = "AU",
            ["Канада"] = "CA", ["Нова Зеландія"] = "NZ", ["Мексика"] = "MX", ["Швеція"] = "SE",
            ["Фінляндія"] = "FI", ["Данія"] = "DK", ["Польща"] = "PL", ["Японія"] = "JP",
            ["Китай"] = "CN", ["Росія"] = "RU", ["Індія"] = "IN", ["Венесуела"] = "VE",
            ["Колумбія"] = "CO", ["Чилі"] = "CL", ["Уругвай"] = "UY", ["Південна Африка"] = "ZA",
            ["Ірландія"] = "IE", ["Португалія"] = "PT", ["Угорщина"] = "HU", ["Чехія"] = "CZ",
            ["Малайзія"] = "MY", ["Індонезія"] = "ID", ["Родезія"] = "RH", ["Ліхтенштейн"] = "LI",
            ["Люксембург"] = "LU", ["Марокко"] = "MA", ["Єгипет"] = "EG", ["Перу"] = "PE",
            ["Сінгапур"] = "SG"
        };
    }
}
