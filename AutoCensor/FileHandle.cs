using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AutoCensor
{
    // ══════════════════════════════════════════════════════════════════
    //  FileHandler — Milestone 6
    //  Открытие исходного файла, словаря, сохранение результата.
    //  Поддерживает drag-and-drop и диалоги.
    // ══════════════════════════════════════════════════════════════════
    public static class FileHandler
    {
        private static readonly Logger Log = Logger.Instance;

        // ── Открыть текстовый файл через диалог ──────────────────────
        public static string? BrowseTextFile(string title = "Выберите файл")
        {
            using var dlg = new OpenFileDialog
            {
                Title = title,
                Filter = "Текстовые файлы (*.txt)|*.txt|Все файлы (*.*)|*.*"
            };

            if (dlg.ShowDialog() == DialogResult.OK)
            {
                Log.Info($"Файл выбран через диалог: {dlg.FileName}");
                return dlg.FileName;
            }

            Log.Info("Диалог выбора файла закрыт без выбора.");
            return null;
        }

        // ── Открыть файл словаря через диалог ────────────────────────
        public static string? BrowseDictFile()
            => BrowseTextFile("Выберите файл словаря");

        // ── Async чтение файла ────────────────────────────────────────
        public static async Task<string?> ReadFileAsync(string path)
        {
            if (!File.Exists(path))
            {
                Log.Error($"Файл не найден: {path}");
                return null;
            }

            try
            {
                Log.Info($"Чтение файла: {path}");
                string content = await File.ReadAllTextAsync(path, DetectEncoding(path));
                Log.Info($"Файл прочитан. Длина: {content.Length} символов.");
                return content;
            }
            catch (Exception ex)
            {
                Log.Error($"Ошибка при чтении файла: {path}", ex);
                return null;
            }
        }

        // ── Async сохранение результата ───────────────────────────────
        public static async Task<bool> SaveResultAsync(string content, string? suggestedDir = null)
        {
            using var dlg = new SaveFileDialog
            {
                Title = "Сохранить результат",
                Filter = "Текстовые файлы (*.txt)|*.txt|Все файлы (*.*)|*.*",
                FileName = "result.txt",
                InitialDirectory = suggestedDir ?? Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
            };

            if (dlg.ShowDialog() != DialogResult.OK)
            {
                Log.Info("Сохранение отменено пользователем.");
                return false;
            }

            try
            {
                await File.WriteAllTextAsync(dlg.FileName, content, Encoding.UTF8);
                Log.Success($"Результат сохранён: {dlg.FileName}");
                return true;
            }
            catch (Exception ex)
            {
                Log.Error($"Ошибка сохранения результата: {dlg.FileName}", ex);
                return false;
            }
        }

        // ── Validate drag-and-drop (только .txt) ─────────────────────
        public static bool ValidateDrop(DragEventArgs e, out string? path)
        {
            path = null;

            if (!e.Data!.GetDataPresent(DataFormats.FileDrop))
                return false;

            var files = (string[])e.Data.GetData(DataFormats.FileDrop)!;
            if (files.Length == 0) return false;

            string file = files[0];

            if (!File.Exists(file))
            {
                Log.Warning($"Drag-drop: файл не существует: {file}");
                return false;
            }

            if (!file.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
            {
                Log.Warning($"Drag-drop: неподдерживаемый формат: {Path.GetExtension(file)}");
                return false;
            }

            path = file;
            Log.Info($"Drag-drop принят: {file}");
            return true;
        }

        // ── Определение кодировки (BOM или UTF-8 fallback) ────────────
        private static Encoding DetectEncoding(string path)
        {
            try
            {
                byte[] bom = new byte[4];
                using var fs = new FileStream(path, FileMode.Open, FileAccess.Read);
                fs.Read(bom, 0, 4);

                if (bom[0] == 0xEF && bom[1] == 0xBB && bom[2] == 0xBF)
                    return Encoding.UTF8;
                if (bom[0] == 0xFF && bom[1] == 0xFE)
                    return Encoding.Unicode;
                if (bom[0] == 0xFE && bom[1] == 0xFF)
                    return Encoding.BigEndianUnicode;
            }
            catch { /* fallback */ }

            return Encoding.UTF8;
        }
    }
}