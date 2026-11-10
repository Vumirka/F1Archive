using F1Archive.Editing;
using F1Archive.Pages;
using System.Windows.Controls;

namespace F1Archive.Helpers
{
    // Перехід між сторінками всередині головного вікна і кнопка "Назад"
    public static class Navigator
    {
        // Сторінка, яку відкрили: назва, параметр (наприклад, id гонщика) і заголовок
        public class Entry
        {
            public string PageKey { get; set; } = "";
            public object? Parameter { get; set; }
            public string Title { get; set; } = "";
        }

        private static ContentControl _host = null!;
        private static readonly Stack<Entry> _history = new();

        public static Entry? Current { get; private set; }
        public static bool CanGoBack => _history.Count > 0;
        public static event Action? Navigated;

        public static void Init(ContentControl host)
        {
            _host = host;
        }

        public static void NavigateTo(string pageKey, object? parameter = null, string? title = null)
        {
            if (Current != null && Current.PageKey == pageKey && Equals(Current.Parameter, parameter))
            {
                return;
            }

            if (Current != null)
            {
                _history.Push(Current);
            }

            Show(new Entry
            {
                PageKey = pageKey,
                Parameter = parameter,
                Title = title ?? PageKeys.TitleOf(pageKey)
            });
        }

        public static void GoBack()
        {
            if (_history.Count > 0)
            {
                Show(_history.Pop());
            }
        }

        private static void Show(Entry entry)
        {
            Current = entry;
            _host.Content = CreatePage(entry.PageKey, entry.Parameter);
            Navigated?.Invoke();
        }

        private static UserControl CreatePage(string key, object? parameter)
        {
            int id = parameter is int value ? value : 0;

            return key switch
            {
                PageKeys.Seasons => new SeasonsPage(),
                PageKeys.SeasonDetails => new SeasonDetailsPage(id),
                PageKeys.GrandPrix => new GrandPrixPage(),
                PageKeys.GrandPrixDetails => new GrandPrixDetailsPage(id),
                PageKeys.Drivers => new DriversPage(),
                PageKeys.DriverDetails => new DriverDetailsPage(id),
                PageKeys.Teams => new TeamsPage(),
                PageKeys.TeamDetails => new TeamDetailsPage(id),
                PageKeys.Circuits => new CircuitsPage(),
                PageKeys.CircuitDetails => new CircuitDetailsPage(id),
                PageKeys.Data => new DataEditPage(),
                _ => new HomePage()
            };
        }
    }

    public static class PageKeys
    {
        public const string Home = "Home";
        public const string Seasons = "Seasons";
        public const string GrandPrix = "GrandPrix";
        public const string Drivers = "Drivers";
        public const string Teams = "Teams";
        public const string Circuits = "Circuits";
        public const string Data = "Data";

        public const string SeasonDetails = "SeasonDetails";
        public const string GrandPrixDetails = "GrandPrixDetails";
        public const string DriverDetails = "DriverDetails";
        public const string TeamDetails = "TeamDetails";
        public const string CircuitDetails = "CircuitDetails";

        public static string SectionOf(string key)
        {
            return key switch
            {
                SeasonDetails => Seasons,
                GrandPrixDetails => GrandPrix,
                DriverDetails => Drivers,
                TeamDetails => Teams,
                CircuitDetails => Circuits,
                _ => key
            };
        }

        public static string TitleOf(string key)
        {
            return key switch
            {
                Home => "Головна",
                SeasonDetails => "Сезон",
                GrandPrixDetails => "Гран-прі",
                DriverDetails => "Гонщик",
                TeamDetails => "Команда",
                CircuitDetails => "Траса",
                Seasons => "Сезони",
                GrandPrix => "Гран-прі",
                Drivers => "Гонщики",
                Teams => "Команди",
                Circuits => "Траси",
                _ => "Керування даними"
            };
        }
    }
}
