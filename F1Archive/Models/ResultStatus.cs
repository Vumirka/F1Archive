namespace F1Archive.Models
{
    // Перевірки статусу результату (статуси в базі записані українською)
    public static class ResultStatus
    {
        public const string Finished = "Фінішував";

        // Фінішував або відстав на кола ("+1 коло")
        public static bool IsFinished(string status)
        {
            return status == Finished || status.StartsWith("+");
        }

        public static bool IsDisqualified(string status)
        {
            return status == "Дискваліфікований" || status == "Виключений";
        }

        // Гонщик вийшов на старт
        public static bool IsStart(string status)
        {
            return status != "Знявся" && status != "Не стартував" && !status.Contains("кваліфікац");
        }
    }
}
