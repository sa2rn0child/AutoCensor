using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AutoCensor
{
    public partial class MainForm : Form
    {
        // ── Состояние ──────────────────────────────────────────────────
        private bool isDarkTheme = false;
        private CancellationTokenSource? _cts;
        private string? _loadedSourcePath;
        private string? _lastSavedResultPath;
        private string? _lastCensoredText;
        private int _lastReplacedCount;

        private static readonly Logger Log = Logger.Instance;

        // ══════════════════════════════════════════════════════════════
        //  Панели, лейблы, контролы — объявлены здесь
        // ══════════════════════════════════════════════════════════════
        private Panel pnlTop, pnlMain, pnlLeft, pnlRight, pnlBottom;
        private Label lblTitle, lblSubtitle;
        private Button btnTheme;
        private Button btnClean;
        private Panel pnlDropZone;
        private Label lblDropIcon, lblDropText;
        private Label lblFileLabel;
        private TextBox txtFilePath;
        private Button btnBrowseFile;
        private Label lblWordsLabel;
        private TextBox txtWordList;
        private Button btnBrowseDict, btnClear;
        private Label lblResultLabel;
        private TextBox txtResult;
        private Button btnSaveResult;
        private Label lblReplacedCount;
        private Button btnStart, btnStop;
        private ProgressBar progressBar;
        private Label lblStatus, lblError;

        public MainForm()
        {
            Log.Info("MainForm: инициализация.");
            InitializeComponents();
            ApplyTheme();
            Log.Info("MainForm: готов.");
        }

        // ══════════════════════════════════════════════════════════════
        //  InitializeComponents — вся вёрстка (без изменений кроме
        //  подключения обработчиков TODO)
        // ══════════════════════════════════════════════════════════════
        private void InitializeComponents()
        {
            SuspendLayout();

            this.Text = "AutoCensor";
            this.Size = new Size(1000, 700);
            this.MinimumSize = new Size(860, 600);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Font = new Font("Segoe UI", 9.5f);
            this.FormBorderStyle = FormBorderStyle.Sizable;

            // ── TOP BAR ───────────────────────────────────────────────
            pnlTop = new Panel { Dock = DockStyle.Top, Height = 86, Padding = new Padding(24, 0, 24, 0) };

            lblTitle = new Label
            {
                Text = "AutoCensor",
                Font = new Font("Segoe UI Semibold", 20f, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(24, 12)
            };

            lblSubtitle = new Label
            {
                Text = "Автоматическая цензура текстовых файлов",
                Font = new Font("Segoe UI", 9f),
                AutoSize = true,
                Location = new Point(26, 52)
            };

            btnTheme = new Button
            {
                Text = "🌙  Тёмная тема",
                Size = new Size(148, 36),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 9f),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btnTheme.FlatAppearance.BorderSize = 1;
            btnTheme.Click += BtnTheme_Click;

            pnlTop.Controls.AddRange(new Control[] { lblTitle, lblSubtitle, btnTheme });

            // ── MAIN AREA ─────────────────────────────────────────────
            pnlMain = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20, 16, 20, 8) };

            // ── LEFT COLUMN ───────────────────────────────────────────
            pnlLeft = new Panel { Dock = DockStyle.Left, Width = 400, Padding = new Padding(0, 0, 16, 0) };

            // Drop-zone
            pnlDropZone = new Panel
            {
                Height = 90,
                Dock = DockStyle.Top,
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 0, 0, 12)
            };
            pnlDropZone.Paint += PnlDropZone_Paint;
            pnlDropZone.Click += (s, e) => BtnBrowseFile_Click(s, e);
            pnlDropZone.AllowDrop = true;
            pnlDropZone.DragEnter += PnlDropZone_DragEnter;
            pnlDropZone.DragDrop += PnlDropZone_DragDrop;

            lblDropIcon = new Label
            {
                Text = "📄",
                Font = new Font("Segoe UI", 18f),
                AutoSize = false,
                Size = new Size(40, 40),
                Location = new Point(16, 24),
                TextAlign = ContentAlignment.MiddleCenter
            };

            lblDropText = new Label
            {
                Text = "Перетащите .txt файл сюда\nили нажмите для выбора",
                Font = new Font("Segoe UI", 9f),
                AutoSize = false,
                Size = new Size(300, 40),
                Location = new Point(60, 24),
                TextAlign = ContentAlignment.MiddleLeft
            };

            pnlDropZone.Controls.AddRange(new Control[] { lblDropIcon, lblDropText });

            // File line
            var pnlFileLine = new Panel { Height = 76, Dock = DockStyle.Top };

            lblFileLabel = new Label
            {
                Text = "Путь к файлу",
                Font = new Font("Segoe UI", 8.5f),
                AutoSize = true,
                Location = new Point(0, 0)
            };

            txtFilePath = new TextBox
            {
                ReadOnly = true,
                Font = new Font("Segoe UI", 9.5f),
                Size = new Size(310, 30),
                Location = new Point(0, 20),
                TabStop = false
            };

            btnBrowseFile = MakeIconButton("📂  Обзор", new Point(318, 19), new Size(66, 30));
            btnBrowseFile.Font = new Font("Segoe UI", 8.5f);
            btnBrowseFile.Click += BtnBrowseFile_Click;

            lblError = new Label
            {
                Text = "",
                Font = new Font("Segoe UI", 8.5f),
                AutoSize = true,
                Location = new Point(0, 52),
                ForeColor = Color.FromArgb(220, 53, 69),
                Visible = false
            };

            pnlFileLine.Controls.AddRange(new Control[]
                { lblFileLabel, txtFilePath, btnBrowseFile, lblError });

            // Words block
            var pnlWordsBlock = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 8, 0, 0) };

            lblWordsLabel = new Label
            {
                Text = "Слова для цензуры",
                Font = new Font("Segoe UI", 8.5f),
                AutoSize = true,
                Location = new Point(0, 8)
            };

            txtWordList = new TextBox
            {
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                Font = new Font("Segoe UI", 9.5f),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom,
                Location = new Point(0, 30),
                Size = new Size(384, 130)
            };

            var pnlBtns = new Panel
            {
                Height = 44,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                Location = new Point(0, 168)
            };

            btnBrowseDict = MakeOutlineButton("📖  Загрузить словарь", new Point(0, 4), new Size(188, 34));
            btnBrowseDict.Click += BtnBrowseDict_Click;

            btnClear = MakeOutlineButton("✕  Очистить", new Point(196, 4), new Size(120, 34));
            btnClear.Click += BtnClear_Click;

            pnlBtns.Controls.AddRange(new Control[] { btnBrowseDict, btnClear });
            pnlWordsBlock.Controls.AddRange(new Control[] { lblWordsLabel, txtWordList, pnlBtns });

            pnlLeft.Controls.Add(pnlWordsBlock);
            pnlLeft.Controls.Add(pnlFileLine);
            pnlLeft.Controls.Add(pnlDropZone);

            // ── RIGHT COLUMN ──────────────────────────────────────────
            pnlRight = new Panel { Dock = DockStyle.Fill, Padding = new Padding(16, 0, 0, 0) };

            lblResultLabel = new Label
            {
                Text = "Результат",
                Font = new Font("Segoe UI", 8.5f),
                AutoSize = true,
                Location = new Point(16, 0)
            };

            txtResult = new TextBox
            {
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Both,
                WordWrap = false,
                Font = new Font("Consolas", 9.5f),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                Location = new Point(16, 20)
            };

            btnSaveResult = new Button
            {
                Text = "💾  Сохранить результат",
                Size = new Size(200, 36),
                FlatStyle = FlatStyle.Flat,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left,
                Font = new Font("Segoe UI", 9f),
                Cursor = Cursors.Hand,
                Enabled = false
            };
            

            lblReplacedCount = new Label
            {
                Text = "Замен: —",
                Font = new Font("Segoe UI", 8.5f),
                AutoSize = true,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left,
                ForeColor = Color.FromArgb(99, 102, 241)
            };

            pnlRight.Controls.AddRange(new Control[]
                { lblResultLabel, txtResult, btnSaveResult, lblReplacedCount });

            pnlMain.Controls.Add(pnlRight);
            pnlMain.Controls.Add(pnlLeft);

            // ── BOTTOM BAR ────────────────────────────────────────────
            pnlBottom = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 64,
                Padding = new Padding(20, 0, 20, 0)
            };
            btnClean = new Button
            {
                Text = "Очистить",
                Size = new Size(110, 42),
                FlatStyle = FlatStyle.Flat,
                Location = new Point(20, 11),
                Font = new Font("Segoe UI Semibold", 10f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnSaveResult.FlatAppearance.BorderSize = 1;
            btnClean.Click += BtnClean_Click;

            btnStart = new Button
            {
                Text = "▶   Запустить цензуру",
                Size = new Size(200, 42),
                FlatStyle = FlatStyle.Flat,
                Location = new Point(140, 11),
                Font = new Font("Segoe UI Semibold", 10f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnStart.FlatAppearance.BorderSize = 0;
            btnStart.Click += BtnStart_Click;

            btnStop = new Button
            {
                Text = "⏹   Стоп",
                Size = new Size(110, 42),
                FlatStyle = FlatStyle.Flat,
                Location = new Point(350, 11),
                Font = new Font("Segoe UI", 9.5f),
                Enabled = false,
                Cursor = Cursors.Hand
            };
            btnStop.FlatAppearance.BorderSize = 1;
            btnStop.Click += (s, e) =>
            {
                _cts?.Cancel();
                Log.Info("Пользователь нажал «Стоп».");
                SetStatus("Отменяется…");
            };

            progressBar = new ProgressBar
            {
                Size = new Size(300, 8),
                Location = new Point(470, 28),
                Style = ProgressBarStyle.Continuous
            };

            lblStatus = new Label
            {
                Text = "Готово.",
                AutoSize = true,
                Location = new Point(780, 22),
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Color.Gray
            };

            pnlBottom.Controls.AddRange(new Control[]
                { btnStart, btnStop, progressBar, lblStatus, btnClean });

            // ── СБОРКА ────────────────────────────────────────────────
            this.Controls.Add(pnlMain);
            this.Controls.Add(pnlBottom);
            this.Controls.Add(pnlTop);

            this.Resize += (s, e) => { RepositionTopRight(); LayoutRightPanel(); };
            this.Load += (s, e) => { RepositionTopRight(); LayoutRightPanel(); };
            pnlRight.Resize += (s, e) => LayoutRightPanel();
            pnlMain.Resize += (s, e) => LayoutRightPanel();

            ResumeLayout(false);
        }

        // ══════════════════════════════════════════════════════════════
        //  MILESTONE 6 — File Handling
        // ══════════════════════════════════════════════════════════════
        private async void BtnBrowseFile_Click(object? sender, EventArgs e)
        {
            string? path = FileHandler.BrowseTextFile();
            if (path != null)
                await LoadSourceFileAsync(path);
        }

        private async void BtnBrowseDict_Click(object? sender, EventArgs e)
        {
            string? path = FileHandler.BrowseDictFile();
            if (path == null) return;

            string? content = await FileHandler.ReadFileAsync(path);
            if (content == null)
            {
                ShowFileError("Не удалось загрузить словарь.");
                return;
            }

            // Добавляем к существующим словам
            string existing = txtWordList.Text.Trim();
            txtWordList.Text = existing.Length > 0
                ? existing + Environment.NewLine + content
                : content;

            Log.Info($"Словарь загружен: {path}");
        }

        private void BtnClear_Click(object? sender, EventArgs e)
        {
            txtWordList.Clear();
            Log.Info("Список слов очищен.");
        }

        private void BtnClean_Click(object? sender, EventArgs e)
        {
            txtFilePath.Clear();
            txtWordList.Clear();
            txtResult.Clear();
        }

        // ── Drag & Drop ───────────────────────────────────────────────
        private void PnlDropZone_DragEnter(object? sender, DragEventArgs e)
        {
            e.Effect = e.Data!.GetDataPresent(DataFormats.FileDrop)
                ? DragDropEffects.Copy
                : DragDropEffects.None;
        }

        private async void PnlDropZone_DragDrop(object? sender, DragEventArgs e)
        {
            if (FileHandler.ValidateDrop(e, out string? path) && path != null)
                await LoadSourceFileAsync(path);
            else
                ShowFileError("Поддерживаются только .txt файлы.");
        }

        // ── Загрузка исходного файла ──────────────────────────────────
        private async Task LoadSourceFileAsync(string path)
        {
            HideFileError();
            txtFilePath.Text = path;
            _loadedSourcePath = path;

            string? content = await FileHandler.ReadFileAsync(path);
            if (content == null)
            {
                ShowFileError("⚠ Файл не найден или повреждён.");
                _loadedSourcePath = null;
                txtFilePath.Text = "";
            }
        }

        // ── Сохранение результата ─────────────────────────────────────
        private async void BtnSaveResult_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_lastCensoredText)) return;

            string? dir = _loadedSourcePath != null
                ? Path.GetDirectoryName(_loadedSourcePath)
                : null;

            bool saved = await FileHandler.SaveResultAsync(_lastCensoredText, dir);

            if (saved)
            {
                // Генерируем отчёт после сохранения
                var report = ReportGenerator.Build(
                    sourceFile: _loadedSourcePath ?? "—",
                    outputFile: "result.txt",
                    totalCharsIn: _loadedSourcePath != null
                                       ? (await FileHandler.ReadFileAsync(_loadedSourcePath))?.Length ?? 0
                                       : 0,
                    totalCharsOut: _lastCensoredText.Length,
                    replacedCount: _lastReplacedCount,
                    elapsed: TimeSpan.Zero,
                    wordStats: new System.Collections.Generic.List<(string, int)>());

                SetStatus($"Сохранено. Замен: {_lastReplacedCount}");
            }
        }

        // ══════════════════════════════════════════════════════════════
        //  MILESTONE 7+8 — Async censorship
        // ══════════════════════════════════════════════════════════════
        private async void BtnStart_Click(object? sender, EventArgs e)
        {
            // Валидация
            if (string.IsNullOrWhiteSpace(_loadedSourcePath))
            {
                ShowFileError("⚠ Выберите файл для цензуры.");
                Log.Warning("Запуск отменён: файл не выбран.");
                return;
            }

            string? sourceText = await FileHandler.ReadFileAsync(_loadedSourcePath);
            if (sourceText == null)
            {
                ShowFileError("⚠ Не удалось прочитать файл.");
                return;
            }

            var words = CensorEngine.ParseWordList(txtWordList.Text);
            if (!words.Any())
            {
                MessageBox.Show(
                    "Список слов для цензуры пуст.\nДобавьте слова или загрузите словарь.",
                    "AutoCensor", MessageBoxButtons.OK, MessageBoxIcon.Information);
                Log.Warning("Запуск отменён: список слов пуст.");
                return;
            }

            // ── Настройка UI на время обработки ──────────────────────
            SetProcessingState(true);
            txtResult.Clear();
            progressBar.Value = 0;
            SetStatus("Обработка…");
            HideFileError();

            _cts = new CancellationTokenSource();

            var progress = new Progress<int>(pct =>
            {
                progressBar.Value = pct;
                SetStatus($"Обработка… {pct}%");
            });

            try
            {
                var engine = new CensorEngine();
                var result = await engine.ProcessAsync(
                    sourceText,
                    words,
                    progress,
                    _cts.Token);

                _lastCensoredText = result.CensoredText;
                _lastReplacedCount = result.ReplacedCount;

                txtResult.Text = result.CensoredText;
                lblReplacedCount.Text = $"Замен: {result.ReplacedCount}";
                progressBar.Value = 100;
                btnSaveResult.Enabled = true;

                SetStatus($"Готово. Замен: {result.ReplacedCount}  ({result.Elapsed.TotalSeconds:F2}s)");
                Log.Success($"UI обновлён. Замен: {result.ReplacedCount}.");
            }
            catch (OperationCanceledException)
            {
                SetStatus("Отменено пользователем.");
                Log.Warning("Операция отменена через CancellationToken.");
            }
            catch (Exception ex)
            {
                SetStatus("Ошибка обработки.");
                Log.Error("Необработанная ошибка во время цензуры.", ex);
                MessageBox.Show($"Ошибка: {ex.Message}", "AutoCensor",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                SetProcessingState(false);
                _cts.Dispose();
                _cts = null;
            }
        }

        // ── Переключение UI между «в процессе» / «готово» ────────────
        private void SetProcessingState(bool processing)
        {
            btnStart.Enabled = !processing;
            btnStop.Enabled = processing;
            btnClean.Enabled = !processing;
            btnBrowseFile.Enabled = !processing;
            btnBrowseDict.Enabled = !processing;
        }

        // ══════════════════════════════════════════════════════════════
        //  Вспомогательные методы
        // ══════════════════════════════════════════════════════════════
        private void SetStatus(string text)
        {
            if (lblStatus.InvokeRequired)
                lblStatus.Invoke(() => lblStatus.Text = text);
            else
                lblStatus.Text = text;
        }

        private void ShowFileError(string message)
        {
            lblError.Text = message;
            lblError.Visible = true;
        }

        private void HideFileError()
        {
            lblError.Text = "";
            lblError.Visible = false;
        }

        // ══════════════════════════════════════════════════════════════
        //  Layout helpers (без изменений)
        // ══════════════════════════════════════════════════════════════
        private void RepositionTopRight()
            => btnTheme.Location = new Point(pnlTop.Width - btnTheme.Width - 24, 25);

        private void LayoutRightPanel()
        {
            if (pnlRight == null || txtResult == null || btnSaveResult == null) return;
            int w = pnlRight.ClientSize.Width - 32;
            int h = pnlRight.ClientSize.Height - 72;
            txtResult.Size = new Size(w, h);
            btnSaveResult.Location = new Point(16, h + 28);
            if (lblReplacedCount != null)
                lblReplacedCount.Location = new Point(226, h + 37);
        }

        private Button MakeIconButton(string text, Point loc, Size size)
        {
            var b = new Button
            {
                Text = text,
                Location = loc,
                Size = size,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 9.5f)
            };
            b.FlatAppearance.BorderSize = 1;
            return b;
        }

        private Button MakeOutlineButton(string text, Point loc, Size size)
        {
            var b = new Button
            {
                Text = text,
                Location = loc,
                Size = size,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 9f)
            };
            b.FlatAppearance.BorderSize = 1;
            return b;
        }

        // ══════════════════════════════════════════════════════════════
        //  Milestone 4: Темы (без изменений)
        // ══════════════════════════════════════════════════════════════
        private void BtnTheme_Click(object? sender, EventArgs e)
        {
            isDarkTheme = !isDarkTheme;
            btnTheme.Text = isDarkTheme ? "☀  Светлая тема" : "🌙  Тёмная тема";
            ApplyTheme();
        }

        private void ApplyTheme()
        {
            Color bg, surface, surfaceHigh, fg, fgMuted, accent, accentFg, border, inputBg, inputFg;

            if (isDarkTheme)
            {
                bg = Color.FromArgb(15, 15, 20);
                surface = Color.FromArgb(24, 24, 32);
                surfaceHigh = Color.FromArgb(34, 34, 46);
                fg = Color.FromArgb(225, 225, 235);
                fgMuted = Color.FromArgb(130, 128, 160);
                accent = Color.FromArgb(99, 102, 241);
                accentFg = Color.White;
                border = Color.FromArgb(50, 50, 70);
                inputBg = Color.FromArgb(30, 30, 42);
                inputFg = Color.FromArgb(220, 218, 240);
            }
            else
            {
                bg = Color.FromArgb(244, 244, 250);
                surface = Color.White;
                surfaceHigh = Color.FromArgb(248, 247, 255);
                fg = Color.FromArgb(25, 25, 45);
                fgMuted = Color.FromArgb(120, 115, 150);
                accent = Color.FromArgb(79, 70, 229);
                accentFg = Color.White;
                border = Color.FromArgb(210, 206, 235);
                inputBg = Color.White;
                inputFg = Color.FromArgb(25, 25, 45);
            }

            this.BackColor = bg;
            pnlTop.BackColor = surface;
            pnlMain.BackColor = bg;
            pnlLeft.BackColor = bg;
            pnlRight.BackColor = bg;
            pnlBottom.BackColor = surface;
            pnlDropZone.BackColor = isDarkTheme
                ? Color.FromArgb(22, 22, 36)
                : Color.FromArgb(248, 246, 255);
            pnlDropZone.Invalidate();

            lblTitle.ForeColor = accent;
            lblTitle.BackColor = surface;
            lblSubtitle.ForeColor = fgMuted;
            lblSubtitle.BackColor = surface;

            foreach (var l in new[] { lblFileLabel, lblWordsLabel, lblResultLabel })
            { l.ForeColor = fgMuted; l.BackColor = Color.Transparent; }

            lblDropIcon.ForeColor = accent;
            lblDropIcon.BackColor = Color.Transparent;
            lblDropText.ForeColor = fgMuted;
            lblDropText.BackColor = Color.Transparent;
            lblStatus.ForeColor = fgMuted;
            lblStatus.BackColor = Color.Transparent;

            foreach (var tb in new[] { txtFilePath, txtWordList, txtResult })
            { tb.BackColor = inputBg; tb.ForeColor = inputFg; tb.BorderStyle = BorderStyle.FixedSingle; }

            btnStart.BackColor = accent;
            btnStart.ForeColor = accentFg;
            btnStart.FlatAppearance.BorderColor = accent;
            btnStart.FlatAppearance.MouseOverBackColor =
                Color.FromArgb(Math.Min(accent.R + 20, 255), Math.Min(accent.G + 18, 255), Math.Min(accent.B + 30, 255));

            foreach (var b in new[] { btnBrowseFile, btnBrowseDict, btnClear, btnStop, btnClean, btnSaveResult })
            {
                b.BackColor = surfaceHigh;
                b.ForeColor = fg;
                b.FlatAppearance.BorderColor = border;
                b.FlatAppearance.MouseOverBackColor =
                    isDarkTheme ? Color.FromArgb(44, 44, 60) : Color.FromArgb(238, 236, 255);
            }

            btnTheme.BackColor = surface;
            btnTheme.ForeColor = accent;
            btnTheme.FlatAppearance.BorderColor = accent;
            btnTheme.FlatAppearance.MouseOverBackColor =
                isDarkTheme ? Color.FromArgb(30, 28, 50) : Color.FromArgb(242, 240, 255);

            progressBar.BackColor = isDarkTheme
                ? Color.FromArgb(40, 40, 56)
                : Color.FromArgb(220, 218, 240);

            if (lblError != null) lblError.ForeColor = Color.FromArgb(220, 53, 69);
            if (lblReplacedCount != null) lblReplacedCount.ForeColor = accent;
        }

        // ── Drop-zone paint ───────────────────────────────────────────
        private void PnlDropZone_Paint(object? sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var r = new Rectangle(2, 2, pnlDropZone.Width - 5, pnlDropZone.Height - 5);
            int rad = 12;

            using var pen = new Pen(
                isDarkTheme ? Color.FromArgb(70, 68, 120) : Color.FromArgb(170, 162, 230), 1.5f);
            pen.DashStyle = DashStyle.Dash;
            pen.DashOffset = 4f;

            using var path = RoundedRect(r, rad);
            g.DrawPath(pen, path);
        }

        private static GraphicsPath RoundedRect(Rectangle r, int rad)
        {
            int d = rad * 2;
            var path = new GraphicsPath();
            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}