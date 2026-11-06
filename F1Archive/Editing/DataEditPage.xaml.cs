using System.Windows;
using System.Windows.Controls;

namespace F1Archive.Editing
{
    // Сторінка "Керування даними": зверху вибір таблиці, нижче таблиця з формою
    public partial class DataEditPage : UserControl
    {
        public DataEditPage()
        {
            InitializeComponent();
            FirstTable.IsChecked = true;
        }

        private void Table_Checked(object sender, RoutedEventArgs e)
        {
            var button = e.OriginalSource as RadioButton;

            if (button == null || button.Tag == null)
            {
                return;
            }

            string table = (string)button.Tag;

            if (table == "Race")
            {
                Host.Content = new RaceEdit();
            }
            else if (table == "Drivers")
            {
                Host.Content = new DriversEdit();
            }
            else if (table == "Teams")
            {
                Host.Content = new TeamsEdit();
            }
            else if (table == "Circuits")
            {
                Host.Content = new CircuitsEdit();
            }
            else
            {
                Host.Content = new GrandPrixEdit();
            }
        }
    }
}
