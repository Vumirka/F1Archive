using F1Archive.Database;
using System.IO;

namespace F1Archive.Services
{
    // Фото архіву. Файл копіюється в папку Database\Media,
    // а в базу записується лише шлях до нього (ImagePath, LogoPath, LayoutImagePath).
    public static class PhotoService
    {
        public static string Folder => Path.Combine(F1ArchiveContext.DataFolder, "Media");

        // Повний шлях до файлу або null, якщо файлу немає
        public static string? FullPath(string? path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return null;
            }

            string full = Path.Combine(Folder, path);
            return File.Exists(full) ? full : null;
        }

        // Копіює обране фото в папку Media і повертає шлях для бази.
        // folder - папка в Media ("Drivers"), name - початок назви файлу ("driver-12").
        // Час у назві, щоб нове фото не плуталося зі старим.
        public static string Copy(string sourceFile, string folder, string name)
        {
            string extension = Path.GetExtension(sourceFile).ToLower();
            string path = Path.Combine(folder, name + "-" + DateTime.Now.ToString("yyyyMMddHHmmss") + extension);

            Directory.CreateDirectory(Path.Combine(Folder, folder));
            File.Copy(sourceFile, Path.Combine(Folder, path), true);

            return path;
        }

        public static void DeleteFile(string? path)
        {
            string? full = FullPath(path);

            if (full != null)
            {
                File.Delete(full);
            }
        }
    }
}
