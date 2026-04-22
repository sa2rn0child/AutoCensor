using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace AutoCensor
{
    // ══════════════════════════════════════════════════════════════════
    //  CensorEngine — Milestone 7
    //  Асинхронная цензура текста по списку слов.
    //  Прогресс репортится через IProgress<int>.
    // ══════════════════════════════════════════════════════════════════
    public sealed class CensorEngine
    {
        private static readonly Logger Log = Logger.Instance;

        // ── Публичный результат обработки ────────────────────────────
        public sealed class CensorResult
        {
            public string CensoredText { get; init; } = string.Empty;
            public int ReplacedCount { get; init; }
            public TimeSpan Elapsed { get; init; }
            public List<(string Word, int Count)> WordStats { get; init; } = new();
        }

        // ── Основной метод ────────────────────────────────────────────
        public async Task<CensorResult> ProcessAsync(
            string sourceText,
            IEnumerable<string> words,
            IProgress<int>? progress = null,
            CancellationToken cancellationToken = default)
        {
            var wordList = words
                .Select(w => w.Trim())
                .Where(w => !string.IsNullOrEmpty(w))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            Log.Info($"Цензура запущена. Слов в списке: {wordList.Count}. Длина текста: {sourceText.Length} символов.");

            if (wordList.Count == 0)
            {
                Log.Warning("Список слов пуст — текст не изменён.");
                return new CensorResult
                {
                    CensoredText = sourceText,
                    ReplacedCount = 0,
                    Elapsed = TimeSpan.Zero
                };
            }

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var stats = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            int totalReplaced = 0;

            // Разбиваем по строкам для точного прогресса
            string[] lines = sourceText.Split('\n');
            int total = lines.Length;
            var resultLines = new string[total];

            // Компилируем один общий regex со всеми словами
            string pattern = string.Join("|",
                wordList.Select(w => $@"\b{Regex.Escape(w)}\b"));

            var regex = new Regex(pattern,
                RegexOptions.IgnoreCase | RegexOptions.Compiled);

            // Инициализируем счётчики
            foreach (var w in wordList) stats[w] = 0;

            await Task.Run(() =>
            {
                for (int i = 0; i < total; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    resultLines[i] = regex.Replace(lines[i], m =>
                    {
                        string matched = m.Value;
                        // Находим оригинальное слово для статистики
                        string? key = wordList.FirstOrDefault(w =>
                            string.Equals(w, matched, StringComparison.OrdinalIgnoreCase));

                        if (key != null)
                        {
                            lock (stats) stats[key]++;
                        }

                        Interlocked.Increment(ref totalReplaced);
                        return BuildCensor(matched);
                    });

                    // Репортим прогресс каждые 100 строк
                    if (i % 100 == 0 || i == total - 1)
                    {
                        int pct = (int)((double)(i + 1) / total * 100);
                        progress?.Report(pct);
                    }
                }
            }, cancellationToken);

            stopwatch.Stop();

            Log.Success($"Цензура завершена. Замен: {totalReplaced}. Время: {stopwatch.Elapsed.TotalSeconds:F2}s.");
            foreach (var (word, count) in stats.Where(kv => kv.Value > 0))
                Log.Info($"  [{count,4}x]  «{word}»");

            return new CensorResult
            {
                CensoredText = string.Join('\n', resultLines),
                ReplacedCount = totalReplaced,
                Elapsed = stopwatch.Elapsed,
                WordStats = stats.Select(kv => (kv.Key, kv.Value)).ToList()
            };
        }

        // ── Построение цензурной маски ────────────────────────────────
        // Первая буква остаётся, остальные → '*'
        private static string BuildCensor(string word)
        {
            if (word.Length <= 1) return "*";
            return word[0] + new string('*', word.Length - 1);
        }

        // ── Парсинг текстового словаря ────────────────────────────────
        public static IEnumerable<string> ParseWordList(string raw)
        {
            return raw
                .Split(new[] { '\n', '\r', ',', ';' },
                    StringSplitOptions.RemoveEmptyEntries)
                .Select(w => w.Trim())
                .Where(w => w.Length > 0);
        }
    }
}