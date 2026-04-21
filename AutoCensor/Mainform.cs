using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace AutoCensor
{
    // ╔══════════════════════════════════════════════════════════════════╗
    // ║  AutoCensor — Главная форма                                      ║
    // ║  Milestone 4: UI Design        (Apr-20)                          ║
    // ║  Milestone 5: Core Implementation (Apr-27)                       ║
    // ║  Milestone 6: File Handling    (May-4)                           ║
    // ║  Milestone 8: Async & UX       (May-18)                          ║
    // ║  Milestone 9: Output & Reporting (May-25)                        ║
    // ╚══════════════════════════════════════════════════════════════════╝
    public partial class MainForm : Form
    {
        // Milestone 4: UI Design — состояние темы
        private bool isDarkTheme = false;

        // ── Панели ────────────────────────────────────────────────────
        private Panel pnlTop;
        private Panel pnlMain;
        private Panel pnlLeft;
        private Panel pnlRight;
        private Panel pnlBottom;

        // ── Шапка ─────────────────────────────────────────────────────
        private Label lblTitle;
        private Label lblSubtitle;
        private Button btnTheme;

        // ── Левая колонка ─────────────────────────────────────────────
        private Panel pnlDropZone;
        private Label lblDropIcon;
        private Label lblDropText;
        private Label lblFileLabel;
        private TextBox txtFilePath;
        private Button btnBrowseFile;
        private Label lblWordsLabel;
        private TextBox txtWordList;
        private Button btnBrowseDict;
        private Button btnClear;

        // ── Правая колонка ────────────────────────────────────────────
        private Label lblResultLabel;
        private TextBox txtResult;
        private Button btnSaveResult;
        private Label lblReplacedCount;  // Milestone 9: счётчик замен

        // ── Нижняя панель ─────────────────────────────────────────────
        private Button btnStart;
        private Button btnStop;
        private ProgressBar progressBar;
        private Label lblStatus;
        private Label lblError;      // US3: сообщение об ошибке файла

        public MainForm()
        {
            InitializeComponents();
            ApplyTheme();
        }

        // ══════════════════════════════════════════════════════════════
        //  Milestone 4 + 5: UI Design + Core Implementation
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
            // Milestone 4: UI Design — шапка с заголовком и кнопкой темы
            pnlTop = new Panel
            {
                Dock = DockStyle.Top,
                Height = 86,
                Padding = new Padding(24, 0, 24, 0)
            };

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
            pnlMain = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(20, 16, 20, 8)
            };

            // ── LEFT COLUMN ───────────────────────────────────────────
            // Milestone 5: Core Implementation — левая панель
            pnlLeft = new Panel
            {
                Dock = DockStyle.Left,
                Width = 400,
                Padding = new Padding(0, 0, 16, 0)
            };

            // Milestone 6: File Handling — drag-and-drop зона
            pnlDropZone = new Panel
            {
                Height = 90,
                Dock = DockStyle.Top,
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 0, 0, 12)
            };
            pnlDropZone.Paint += PnlDropZone_Paint;
            pnlDropZone.Click += (s, e) => { /* TODO Milestone 6: BrowseFile */ };

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

            // Milestone 6: поле пути + кнопка
            var pnlFileLine = new Panel { Height = 62, Dock = DockStyle.Top };

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

            btnBrowseFile = MakeIconButton("📂", new Point(318, 19), new Size(66, 30));
            btnBrowseFile.Text = "📂  Обзор";
            btnBrowseFile.Font = new Font("Segoe UI", 8.5f);
            // TODO Milestone 6: btnBrowseFile.Click += BtnBrowseFile_Click;

            // US3: метка ошибки — появляется если файл повреждён/не найден
            lblError = new Label
            {
                Text = "",
                Font = new Font("Segoe UI", 8.5f),
                AutoSize = true,
                Location = new Point(0, 52),
                ForeColor = Color.FromArgb(220, 53, 69),
                Visible = false
            };
            // TODO Milestone 6: lblError.Text = "⚠ Файл не найден или повреждён"; lblError.Visible = true;

            pnlFileLine.Height = 76;
            pnlFileLine.Controls.AddRange(new Control[] { lblFileLabel, txtFilePath, btnBrowseFile, lblError });

            // Milestone 7: Censorship Engine — поле слов
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
                Anchor = AnchorStyles.Top | AnchorStyles.Left |
                             AnchorStyles.Right | AnchorStyles.Bottom,
                Location = new Point(0, 30),
                Size = new Size(384, 130)
            };

            // Milestone 6 + 8: кнопки действий под полем слов
            var pnlBtns = new Panel
            {
                Height = 44,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                Location = new Point(0, 168)
            };

            btnBrowseDict = MakeOutlineButton("📖  Загрузить словарь", new Point(0, 4), new Size(188, 34));
            // TODO Milestone 6: btnBrowseDict.Click += BtnBrowseDict_Click;

            btnClear = MakeOutlineButton("✕  Очистить", new Point(196, 4), new Size(120, 34));
            // TODO Milestone 8: btnClear.Click += BtnClear_Click;

            pnlBtns.Controls.AddRange(new Control[] { btnBrowseDict, btnClear });
            pnlWordsBlock.Controls.AddRange(new Control[] { lblWordsLabel, txtWordList, pnlBtns });

            pnlLeft.Controls.Add(pnlWordsBlock);
            pnlLeft.Controls.Add(pnlFileLine);
            pnlLeft.Controls.Add(pnlDropZone);

            // ── RIGHT COLUMN ──────────────────────────────────────────
            // Milestone 9: Output & Reporting
            pnlRight = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(16, 0, 0, 0)
            };

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
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom |
                             AnchorStyles.Left | AnchorStyles.Right,
                Location = new Point(16, 20)
            };

            // Milestone 9: сохранение в result.txt
            btnSaveResult = new Button
            {
                Text = "💾  Сохранить результат",
                Size = new Size(200, 36),
                FlatStyle = FlatStyle.Flat,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left,
                Font = new Font("Segoe UI", 9f),
                Cursor = Cursors.Hand
            };
            btnSaveResult.FlatAppearance.BorderSize = 1;
            // TODO Milestone 9: btnSaveResult.Click += BtnSaveResult_Click;

            // Milestone 9: US4 — счётчик замен рядом с кнопкой сохранения
            lblReplacedCount = new Label
            {
                Text = "Замен: —",
                Font = new Font("Segoe UI", 8.5f),
                AutoSize = true,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left,
                ForeColor = Color.FromArgb(99, 102, 241)
            };
            // TODO Milestone 9: lblReplacedCount.Text = $"Замен: {count}";

            pnlRight.Controls.AddRange(new Control[] { lblResultLabel, txtResult, btnSaveResult, lblReplacedCount });

            pnlMain.Controls.Add(pnlRight);
            pnlMain.Controls.Add(pnlLeft);

            // ── BOTTOM BAR ────────────────────────────────────────────
            // Milestone 8: Async & UX — панель запуска
            pnlBottom = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 64,
                Padding = new Padding(20, 0, 20, 0)
            };

            // Milestone 7+8: запуск асинхронной обработки
            btnStart = new Button
            {
                Text = "▶   Запустить цензуру",
                Size = new Size(200, 42),
                FlatStyle = FlatStyle.Flat,
                Location = new Point(20, 11),
                Font = new Font("Segoe UI Semibold", 10f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnStart.FlatAppearance.BorderSize = 0;
            // TODO Milestone 7+8: btnStart.Click += BtnStart_Click;

            // Milestone 8: отмена через CancellationToken
            btnStop = new Button
            {
                Text = "⏹   Стоп",
                Size = new Size(110, 42),
                FlatStyle = FlatStyle.Flat,
                Location = new Point(230, 11),
                Font = new Font("Segoe UI", 9.5f),
                Enabled = false,
                Cursor = Cursors.Hand
            };
            btnStop.FlatAppearance.BorderSize = 1;
            // TODO Milestone 8: btnStop.Click += (s, e) => _cts?.Cancel();

            // Milestone 8: прогресс-бар
            progressBar = new ProgressBar
            {
                Size = new Size(300, 8),
                Location = new Point(360, 28),
                Style = ProgressBarStyle.Continuous
            };

            // Milestone 8: строка статуса
            lblStatus = new Label
            {
                Text = "Готово. Замен: —",
                AutoSize = true,
                Location = new Point(676, 22),
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Color.Gray
            };

            pnlBottom.Controls.AddRange(new Control[]
            {
                btnStart, btnStop, progressBar, lblStatus
            });

            // ── ФИНАЛЬНАЯ СБОРКА ──────────────────────────────────────
            this.Controls.Add(pnlMain);
            this.Controls.Add(pnlBottom);
            this.Controls.Add(pnlTop);

            this.Resize += (s, e) => { RepositionTopRight(); LayoutRightPanel(); };
            this.Load += (s, e) => { RepositionTopRight(); LayoutRightPanel(); };
            pnlRight.Resize += (s, e) => LayoutRightPanel();
            pnlMain.Resize += (s, e) => LayoutRightPanel();

            ResumeLayout(false);
        }

        // ── Позиционирование кнопки темы при ресайзе ──────────────────
        private void RepositionTopRight()
        {
            btnTheme.Location = new Point(pnlTop.Width - btnTheme.Width - 24, 25);
        }

        // ── Растягивание правой колонки ───────────────────────────────
        private void LayoutRightPanel()
        {
            if (pnlRight == null || txtResult == null || btnSaveResult == null) return;

            int w = pnlRight.ClientSize.Width - 32;
            int h = pnlRight.ClientSize.Height - 72;
            txtResult.Size = new Size(w, h);
            btnSaveResult.Location = new Point(16, h + 28);

            // US4: счётчик замен — правее кнопки сохранения
            if (lblReplacedCount != null)
                lblReplacedCount.Location = new Point(226, h + 37);
        }

        // ── Вспомогательные фабрики кнопок ────────────────────────────
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
        //  Milestone 4: UI Design — переключение тем
        // ══════════════════════════════════════════════════════════════
        private void BtnTheme_Click(object? sender, EventArgs e)
        {
            isDarkTheme = !isDarkTheme;
            btnTheme.Text = isDarkTheme ? "☀  Светлая тема" : "🌙  Тёмная тема";
            ApplyTheme();
        }

        private void ApplyTheme()
        {
            Color bg, surface, surfaceHigh, fg, fgMuted, accent, accentFg,
                  border, inputBg, inputFg;

            if (isDarkTheme)
            {
                // Milestone 4: тёмная тема
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
                // Milestone 4: светлая тема
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

            // Фоны
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

            // Заголовок
            lblTitle.ForeColor = accent;
            lblTitle.BackColor = surface;
            lblSubtitle.ForeColor = fgMuted;
            lblSubtitle.BackColor = surface;

            // Лейблы
            foreach (var l in new[] { lblFileLabel, lblWordsLabel, lblResultLabel })
            { l.ForeColor = fgMuted; l.BackColor = Color.Transparent; }

            lblDropIcon.ForeColor = accent;
            lblDropIcon.BackColor = Color.Transparent;
            lblDropText.ForeColor = fgMuted;
            lblDropText.BackColor = Color.Transparent;
            lblStatus.ForeColor = fgMuted;
            lblStatus.BackColor = Color.Transparent;

            // Поля ввода
            foreach (var tb in new[] { txtFilePath, txtWordList, txtResult })
            {
                tb.BackColor = inputBg;
                tb.ForeColor = inputFg;
                tb.BorderStyle = BorderStyle.FixedSingle;
            }

            // Кнопка запуска — заливка акцентом
            btnStart.BackColor = accent;
            btnStart.ForeColor = accentFg;
            btnStart.FlatAppearance.BorderColor = accent;
            btnStart.FlatAppearance.MouseOverBackColor =
                Color.FromArgb(Math.Min(accent.R + 20, 255),
                               Math.Min(accent.G + 18, 255),
                               Math.Min(accent.B + 30, 255));

            // Остальные кнопки — outline
            foreach (var b in new[] { btnBrowseFile, btnBrowseDict, btnClear,
                                       btnStop, btnSaveResult })
            {
                b.BackColor = surfaceHigh;
                b.ForeColor = fg;
                b.FlatAppearance.BorderColor = border;
                b.FlatAppearance.MouseOverBackColor =
                    isDarkTheme ? Color.FromArgb(44, 44, 60)
                                : Color.FromArgb(238, 236, 255);
            }

            // Кнопка темы — акцентный бордер
            btnTheme.BackColor = surface;
            btnTheme.ForeColor = accent;
            btnTheme.FlatAppearance.BorderColor = accent;
            btnTheme.FlatAppearance.MouseOverBackColor =
                isDarkTheme ? Color.FromArgb(30, 28, 50)
                            : Color.FromArgb(242, 240, 255);

            progressBar.BackColor = isDarkTheme
                ? Color.FromArgb(40, 40, 56)
                : Color.FromArgb(220, 218, 240);

            // US3: цвет ошибки одинаков в обеих темах — красный
            if (lblError != null)
                lblError.ForeColor = Color.FromArgb(220, 53, 69);

            // US4: счётчик замен — акцентный цвет темы
            if (lblReplacedCount != null)
                lblReplacedCount.ForeColor = accent;
        }

        // ══════════════════════════════════════════════════════════════
        //  Milestone 6: File Handling
        //  Кастомная отрисовка drop-зоны со скруглёнными углами
        // ══════════════════════════════════════════════════════════════
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