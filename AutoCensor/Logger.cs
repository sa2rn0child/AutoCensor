using System;
using System.IO;
using System.Text;

namespace AutoCensor
{
    // ══════════════════════════════════════════════════════════════════
    //  Logger — запись событий в autocensor.log
    //  Используется всеми компонентами через статический синглтон.
    // ══════════════════════════════════════════════════════════════════
    public enum LogLevel { Info, Warning, Error, Success }

    public sealed class Logger : IDisposable
    {
        private static Logger? _instance;
        private static readonly object _lock = new();

        private readonly StreamWriter _writer;
        private readonly string _logPath;

        public string LogPath => _logPath;

        // ── Синглтон ─────────────────────────────────────────────────
        public static Logger Instance
        {
            get
            {
                lock (_lock)
                {
                    _instance ??= new Logger();
                    return _instance;
                }
            }
        }

        private Logger()
        {
            _logPath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "autocensor.log");

            _writer = new StreamWriter(_logPath, append: true, Encoding.UTF8)
            {
                AutoFlush = true
            };

            WriteHeader();
        }

        // ── Запись заголовка сессии ───────────────────────────────────
        private void WriteHeader()
        {
            string sep = new string('═', 60);
            _writer.WriteLine();
            _writer.WriteLine(sep);
            _writer.WriteLine($"  AutoCensor  |  Сессия начата: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            _writer.WriteLine(sep);
        }

        // ── Публичные методы логирования ──────────────────────────────
        public void Info(string message) => Write(LogLevel.Info, message);
        public void Warning(string message) => Write(LogLevel.Warning, message);
        public void Error(string message) => Write(LogLevel.Error, message);
        public void Success(string message) => Write(LogLevel.Success, message);

        public void Error(string message, Exception ex)
            => Write(LogLevel.Error, $"{message} | Exception: {ex.GetType().Name}: {ex.Message}");

        private void Write(LogLevel level, string message)
        {
            lock (_lock)
            {
                string prefix = level switch
                {
                    LogLevel.Info => "[INFO   ]",
                    LogLevel.Warning => "[WARN   ]",
                    LogLevel.Error => "[ERROR  ]",
                    LogLevel.Success => "[SUCCESS]",
                    _ => "[INFO   ]"
                };

                string line = $"{DateTime.Now:HH:mm:ss.fff}  {prefix}  {message}";
                _writer.WriteLine(line);
            }
        }

        // ── Итоговый блок сессии ──────────────────────────────────────
        public void WriteSessionSummary(int replacements, string sourceFile, string outputFile)
        {
            lock (_lock)
            {
                string sep = new string('─', 60);
                _writer.WriteLine(sep);
                _writer.WriteLine($"  ИТОГ СЕССИИ");
                _writer.WriteLine($"  Источник:    {sourceFile}");
                _writer.WriteLine($"  Результат:   {outputFile}");
                _writer.WriteLine($"  Замен:       {replacements}");
                _writer.WriteLine($"  Завершено:   {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                _writer.WriteLine(sep);
            }
        }

        public void Dispose()
        {
            lock (_lock)
            {
                _writer?.Flush();
                _writer?.Dispose();
            }
        }
    }
}