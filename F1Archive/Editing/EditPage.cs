using F1Archive.Controls;
using F1Archive.Services;
using Microsoft.Win32;
using System.Windows;
using System.Windows.Controls;

namespace F1Archive.Editing
{
    // Спільна основа для форм редагування: кнопки, вибір рядка, пошук і фото.
    // Кожна форма сама описує, що показати, як зберегти і як видалити запис.
    public class EditPage : UserControl
    {
        // id відкритого запису (null - новий запис)
        protected int? EditId;

        // Нове фото, яке обрали на комп'ютері, і чи треба прибрати старе
        private string? _photoFile;
        private bool _removePhoto;

        // Ці методи кожна форма пише по-своєму
        protected virtual void LoadList()
        {
        }

        protected virtual void ClearForm()
        {
        }

        protected virtual void ShowRecord(object item)
        {
        }

        // Повертає id збереженого запису або null, якщо є помилка
        protected virtual int? SaveRecord()
        {
            return null;
        }

        // Повертає текст помилки або null, якщо запис видалено
        protected virtual string? DeleteRecord()
        {
            return null;
        }

        // Елементи форми, з якими працюють спільні кнопки
        private TextBlock _status = null!;
        private Button _deleteButton = null!;
        private MediaImage? _photo;

        // Кожна форма передає свої елементи одразу після InitializeComponent.
        // Фото є лише у формах гонщиків, команд і трас.
        protected void SetControls(TextBlock status, Button deleteButton, MediaImage? photo = null)
        {
            _status = status;
            _deleteButton = deleteButton;
            _photo = photo;
        }

        protected void ShowStatus(string message, bool error = true)
        {
            EditHelper.Status(_status, message, error);
        }

        protected void NewRecord()
        {
            EditId = null;
            ClearForm();
            _status.Text = "";
            _deleteButton.IsEnabled = false;
        }

        // Після збереження запис лишається відкритим, а для наступного є "Новий запис".
        protected void Save_Click(object sender, RoutedEventArgs e)
        {
            int? id = SaveRecord();

            if (id == null)
            {
                return;
            }

            LoadList();
            EditId = id;
            _deleteButton.IsEnabled = true;
            ShowStatus("Збережено.", false);
        }

        protected void New_Click(object sender, RoutedEventArgs e)
        {
            NewRecord();
        }

        protected void Delete_Click(object sender, RoutedEventArgs e)
        {
            if (EditId == null || !EditHelper.ConfirmDelete())
            {
                return;
            }

            string? error = DeleteRecord();

            if (error != null)
            {
                ShowStatus(error);
                return;
            }

            LoadList();
            NewRecord();
        }

        protected void List_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            object? item = ((DataGrid)sender).SelectedItem;

            if (item != null)
            {
                ShowRecord(item);
                _status.Text = "";
                _deleteButton.IsEnabled = true;
            }
        }

        protected void Search_Changed(object sender, TextChangedEventArgs e)
        {
            LoadList();
        }

        protected void ShowPhoto(string? path)
        {
            _photoFile = null;
            _removePhoto = false;
            _photo!.SourcePath = path;
        }

        // Шлях до фото після збереження запису: нове фото копіюється в Media, старе видаляється.
        // folder - папка в Media ("Drivers"), name - початок назви файлу ("driver-12").
        protected string? UpdatePhoto(string? oldPath, string folder, string name)
        {
            string? path = oldPath;

            try
            {
                if (_photoFile != null)
                {
                    path = PhotoService.Copy(_photoFile, folder, name);
                }
                else if (_removePhoto)
                {
                    path = null;
                }

                if (path != oldPath)
                {
                    PhotoService.DeleteFile(oldPath);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Не вдалося зберегти фото: " + ex.Message);
                path = oldPath;
            }

            _photoFile = null;
            _removePhoto = false;

            return path;
        }

        protected void ChoosePhoto_Click(object sender, RoutedEventArgs e)
        {
            string? file = ChoosePhotoFile();

            if (file != null)
            {
                _photoFile = file;
                _photo!.SourcePath = file;
            }
        }

        // Вікно вибору фото на комп'ютері. null, якщо нічого не обрали.
        protected static string? ChoosePhotoFile()
        {
            var dialog = new OpenFileDialog();
            dialog.Filter = "Зображення (*.jpg;*.jpeg;*.png)|*.jpg;*.jpeg;*.png";

            if (dialog.ShowDialog() == true)
            {
                return dialog.FileName;
            }

            return null;
        }

        // Кнопка "Прибрати фото": файл видалиться під час збереження
        protected void RemovePhoto_Click(object sender, RoutedEventArgs e)
        {
            _photoFile = null;
            _removePhoto = true;
            _photo!.SourcePath = null;
        }
    }
}
