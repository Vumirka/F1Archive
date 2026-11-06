namespace F1Archive.Services
{
    // Посилання на інший запис: id і назва (клік відкриває його сторінку)
    public record Link(int Id, string Name);

    // Цифра з підписом у смузі внизу шапки
    public record StatItem(string Label, string Value);
}
