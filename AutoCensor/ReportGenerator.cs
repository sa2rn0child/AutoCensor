using System;
using System.Collections.Generic;
using System.Text;

namespace AutoCensor
{
    // ══════════════════════════════════════════════════════════════════
    //  ReportGenerator — Milestone 9
    //  Формирует текстовый отчёт, который вставляется в лог и
    //  может быть сохранён рядом с result.txt.
    // ══════════════════════════════════════════════════════════════════
    public static class ReportGenerator
    {
        private static readonly Logger Log = Logger.Instance;

        public static string Build(
            string sourceFile,
            string outputFile,
            int totalCharsIn,
            int totalCharsOut,
            int replacedCount,
            TimeSpan elapsed,
            List<(string Word, int Count)> wordStats)
        {
            var sb = new StringBuilder();
            string sep = new string('═', 54);
            string sep2 = new string('─', 54);

            sb.AppendLine(sep);
            sb.AppendLine("  AUTOCENSOR — ОТЧЁТ ОБ ОБРАБОТКЕ");
            sb.AppendLine($"  Дата:        {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine(sep2);
            sb.AppendLine($"  Источник:    {sourceFile}");
            sb.AppendLine($"  Результат:   {outputFile}");
            sb.AppendLine(sep2);
            sb.AppendLine($"  Символов вх: {totalCharsIn,10}");
            sb.AppendLine($"  Символов вых:{totalCharsOut,10}");
            sb.AppendLine($"  Замен:       {replacedCount,10}");
            sb.AppendLine($"  Время:       {elapsed.TotalSeconds,9:F3} с");
            sb.AppendLine(sep2);

            if (wordStats.Count > 0)
            {
                sb.AppendLine("  СТАТИСТИКА ПО СЛОВАМ:");
                foreach (var (word, count) in wordStats)
                {
                    if (count > 0)
                        sb.AppendLine($"    {count,5}x   «{word}»");
                }
            }
            else
            {
                sb.AppendLine("  Замен не произведено.");
            }

            sb.AppendLine(sep);

            string report = sb.ToString();

            // Дублируем в лог
            Log.Info("Сформирован отчёт:");
            foreach (var line in report.Split('\n'))
                if (!string.IsNullOrWhiteSpace(line)) Log.Info(line.TrimEnd());

            return report;
        }
    }
}