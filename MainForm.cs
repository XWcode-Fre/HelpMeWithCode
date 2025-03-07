using System;
using System.Windows.Forms;
using System.Drawing;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using System.Data.SQLite;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
using System.Data; // Добавляем для ConnectionState

// Статический класс верхнего уровня для методов расширения
public static class GraphicsExtensions
{
    public static void AddRoundedRectangle(this System.Drawing.Drawing2D.GraphicsPath path, Rectangle rect, int radius)
    {
        int diameter = radius * 2;
        Size size = new Size(diameter, diameter);
        Rectangle arc = new Rectangle(rect.Location, size);

        // Верхний левый угол
        path.AddArc(arc, 180, 90);

        // Верхний правый угол
        arc.X = rect.Right - diameter;
        path.AddArc(arc, 270, 90);

        // Нижний правый угол
        arc.Y = rect.Bottom - diameter;
        path.AddArc(arc, 0, 90);

        // Нижний левый угол
        arc.X = rect.Left;
        path.AddArc(arc, 90, 90);

        path.CloseFigure();
    }
}

namespace DeltaBrowser
{
    public partial class MainForm : Form
    {
        private WebView2 webView;
        private TextBox urlBox;
        private Button backButton;
        private Button forwardButton;
        private Button refreshButton;
        private Button homeButton;
        private Button passwordGeneratorButton;
        private Button bookmarkButton;
        private Button historyButton;
        private Button settingsButton;
        private Button downloadButton;
        private ProgressBar loadingBar;
        private SQLiteConnection historyDb;
        private SQLiteConnection passwordsDb;
        private SQLiteConnection bookmarksDb;
        private SQLiteConnection downloadsDb;
        private Panel navigationPanel;
        private Label loadingLabel;
        private ToolTip toolTip;
        private TabControl tabControl;
        private List<TabPage> tabs;
        private Button newTabButton;

        private class BrowserTab
        {
            public WebView2 WebView { get; set; }
            public string Title { get; set; }
            public string Url { get; set; }
        }

        public MainForm()
        {
            InitializeComponent();
            
            // Установка иконки приложения
            try
            {
                string iconPath = Path.Combine(Application.StartupPath, "browser.ico");
                if (File.Exists(iconPath))
                {
                    this.Icon = new Icon(iconPath);
                }
                else
                {
                    // Создаем простую иконку программно
                    Bitmap bmp = new Bitmap(32, 32);
                    using (Graphics g = Graphics.FromImage(bmp))
                    {
                        g.Clear(Color.FromArgb(53, 54, 58));
                        using (Brush b = new SolidBrush(Color.FromArgb(26, 115, 232)))
                        {
                            g.FillEllipse(b, 4, 4, 24, 24);
                        }
                    }
                    this.Icon = Icon.FromHandle(bmp.GetHicon());
                }
            }
            catch { }
            
            tabs = new List<TabPage>();
            InitializeDatabases();
            this.Load += MainForm_Load;
        }

        private async void MainForm_Load(object sender, EventArgs e)
        {
            try
            {
                this.KeyPreview = true;
                this.KeyDown += MainForm_KeyDown;

                // Создаем новую вкладку в UI потоке
                await CreateNewTab();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при инициализации браузера: {ex.Message}",
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void InitializeComponent()
        {
            this.Text = "Delta Browser";
            this.Size = new Size(1200, 800);
            this.BackColor = Color.FromArgb(53, 54, 58);
            this.ForeColor = Color.White;
            this.WindowState = FormWindowState.Maximized;

            // Инициализация ToolTip
            toolTip = new ToolTip();
            toolTip.InitialDelay = 200;
            toolTip.ShowAlways = true;

            // Создаем главную панель
            var mainPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 2,
                ColumnCount = 1,
                Margin = new Padding(0),
                Padding = new Padding(0),
                BackColor = Color.FromArgb(53, 54, 58)
            };

            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 76)); // Увеличенная высота для навигации
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            // Создаем верхнюю панель (навигация + вкладки)
            var topPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 2,
                ColumnCount = 1,
                Margin = new Padding(0),
                Padding = new Padding(0),
                BackColor = Color.FromArgb(53, 54, 58)
            };

            topPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 36)); // Высота вкладок
            topPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40)); // Высота навигации

            // Создаем панель вкладок
            tabControl = new TabControl
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0),
                Padding = new Point(0, 0),
                DrawMode = TabDrawMode.OwnerDrawFixed,
                SizeMode = TabSizeMode.Fixed,
                ItemSize = new Size(200, 34),
                BackColor = Color.FromArgb(53, 54, 58),
                Region = new Region(new Rectangle(0, 0, Width, 34))
            };
            tabControl.DrawItem += TabControl_DrawItem;
            tabControl.Selected += TabControl_Selected;
            tabControl.MouseClick += TabControl_MouseClick;

            // Кнопка новой вкладки
            newTabButton = new Button
            {
                Text = "+",
                Size = new Size(22, 22),
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderSize = 0 },
                BackColor = Color.FromArgb(53, 54, 58),
                ForeColor = Color.FromArgb(154, 160, 166),
                Font = new Font("Segoe UI", 11),
                Cursor = Cursors.Hand,
                Location = new Point(5, 4)
            };
            newTabButton.Click += NewTabButton_Click;

            var tabPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Height = 31,
                Padding = new Padding(0),
                BackColor = Color.FromArgb(53, 54, 58)
            };
            tabPanel.Controls.Add(tabControl);
            tabPanel.Controls.Add(newTabButton);

            // Создаем панель навигации
            navigationPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Height = 40,
                BackColor = Color.FromArgb(53, 54, 58),
                Padding = new Padding(6, 3, 6, 3)
            };

            // Стиль для кнопок
            void StyleButton(Button btn, string text, string tooltip)
            {
                btn.FlatStyle = FlatStyle.Flat;
                btn.FlatAppearance.BorderSize = 0;
                btn.Size = new Size(34, 34);
                btn.Text = text;
                btn.Font = new Font("Segoe UI", 12);
                btn.BackColor = Color.Transparent;
                btn.ForeColor = Color.FromArgb(154, 160, 166);
                btn.Cursor = Cursors.Hand;
                btn.Margin = new Padding(2);
                btn.Padding = new Padding(0);
                toolTip.SetToolTip(btn, tooltip);

                btn.MouseEnter += (s, e) => {
                    btn.BackColor = Color.FromArgb(66, 67, 69);
                    btn.ForeColor = Color.White;
                };
                btn.MouseLeave += (s, e) => {
                    btn.BackColor = Color.Transparent;
                    btn.ForeColor = Color.FromArgb(154, 160, 166);
                };
            }

            // Стилизация адресной строки
            urlBox = new TextBox
            {
                Height = 36,
                Font = new Font("Segoe UI", 11),
                BackColor = Color.FromArgb(48, 49, 52),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.None,
                Margin = new Padding(0),
                Padding = new Padding(10, 8, 10, 8),
                Dock = DockStyle.Fill
            };
            urlBox.KeyPress += UrlBox_KeyPress;

            var urlBoxContainer = new Panel
            {
                Padding = new Padding(10, 8, 10, 8),
                Margin = new Padding(8, 4, 8, 4),
                BackColor = Color.FromArgb(48, 49, 52),
                Height = 36,
                Dock = DockStyle.Fill
            };
            
            urlBoxContainer.Paint += (s, e) => {
                using (var path = new System.Drawing.Drawing2D.GraphicsPath())
                {
                    var rect = new Rectangle(0, 0, urlBoxContainer.Width, urlBoxContainer.Height);
                    path.AddRoundedRectangle(rect, 8);
                    urlBoxContainer.Region = new Region(path);
                }
            };
            urlBoxContainer.Controls.Add(urlBox);

            // Размещение элементов на панели навигации
            var buttonPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 1,
                Height = 40,
                BackColor = Color.Transparent,
                Margin = new Padding(0),
                Padding = new Padding(0)
            };

            buttonPanel.ColumnStyles.Clear();
            buttonPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            buttonPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            buttonPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            // Левая группа кнопок
            var leftButtonGroup = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize = true,
                Margin = new Padding(0),
                BackColor = Color.Transparent
            };

            // Правая группа кнопок
            var rightButtonGroup = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize = true,
                Margin = new Padding(0),
                BackColor = Color.Transparent
            };

            // Инициализация кнопок
            backButton = new Button();
            StyleButton(backButton, "◀", "Назад");
            backButton.Click += BackButton_Click;
            
            forwardButton = new Button();
            StyleButton(forwardButton, "▶", "Вперед");
            forwardButton.Click += ForwardButton_Click;
            
            refreshButton = new Button();
            StyleButton(refreshButton, "↻", "Обновить");
            refreshButton.Click += RefreshButton_Click;
            
            homeButton = new Button();
            StyleButton(homeButton, "⌂", "Домой");
            homeButton.Click += HomeButton_Click;

            leftButtonGroup.Controls.AddRange(new Control[] { 
                backButton, forwardButton, refreshButton, homeButton 
            });

            // Кнопки справа
            passwordGeneratorButton = new Button();
            StyleButton(passwordGeneratorButton, "🔑", "Генератор паролей");
            passwordGeneratorButton.Click += PasswordGeneratorButton_Click;

            bookmarkButton = new Button();
            StyleButton(bookmarkButton, "☆", "Закладки");
            bookmarkButton.Click += BookmarkButton_Click;

            historyButton = new Button();
            StyleButton(historyButton, "⌚", "История");
            historyButton.Click += HistoryButton_Click;

            settingsButton = new Button();
            StyleButton(settingsButton, "⋮", "Настройки");
            settingsButton.Click += SettingsButton_Click;

            downloadButton = new Button();
            StyleButton(downloadButton, "↓", "Загрузки");
            downloadButton.Click += DownloadButton_Click;

            rightButtonGroup.Controls.AddRange(new Control[] {
                passwordGeneratorButton, bookmarkButton, historyButton,
                downloadButton, settingsButton
            });

            buttonPanel.Controls.Add(leftButtonGroup, 0, 0);
            buttonPanel.Controls.Add(urlBoxContainer, 1, 0);
            buttonPanel.Controls.Add(rightButtonGroup, 2, 0);

            navigationPanel.Controls.Add(buttonPanel);

            topPanel.Controls.Add(tabPanel, 0, 0);
            topPanel.Controls.Add(navigationPanel, 0, 1);

            mainPanel.Controls.Add(topPanel, 0, 0);

            // Создаем контейнер для веб-представления
            var browserContainer = new Panel
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0),
                BackColor = Color.White
            };
            mainPanel.Controls.Add(browserContainer, 0, 1);

            // Создаем и настраиваем индикатор загрузки
            loadingBar = new ProgressBar
            {
                Height = 2,
                Style = ProgressBarStyle.Continuous,
                MarqueeAnimationSpeed = 30,
                Visible = false,
                Dock = DockStyle.Top,
                BackColor = Color.FromArgb(53, 54, 58),
                ForeColor = Color.FromArgb(26, 115, 232)
            };

            loadingLabel = new Label
            {
                Text = "Загрузка...",
                AutoSize = true,
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.FromArgb(154, 160, 166),
                Visible = false,
                Padding = new Padding(5, 0, 5, 0),
                BackColor = Color.Transparent
            };

            // Добавляем индикаторы загрузки
            var loadingContainer = new Panel
            {
                Height = 24,
                AutoSize = true,
                Padding = new Padding(5),
                BackColor = Color.Transparent,
                Dock = DockStyle.Right
            };
            loadingContainer.Controls.Add(loadingLabel);
            rightButtonGroup.Controls.Add(loadingContainer);
            browserContainer.Controls.Add(loadingBar);

            this.Controls.Add(mainPanel);
        }

        private void TabControl_DrawItem(object sender, DrawItemEventArgs e)
        {
            var tabRect = tabControl.GetTabRect(e.Index);
            var closeRect = new Rectangle(tabRect.Right - 24, tabRect.Top + 10, 14, 14);
            var tabPage = tabControl.TabPages[e.Index];
            var isSelected = (tabControl.SelectedIndex == e.Index);
            var isHovered = tabRect.Contains(tabControl.PointToClient(Cursor.Position));

            using (var backBrush = new SolidBrush(isSelected ? Color.FromArgb(66, 67, 69) : 
                   isHovered ? Color.FromArgb(58, 59, 61) : Color.FromArgb(53, 54, 58)))
            using (var textBrush = new SolidBrush(isSelected ? Color.White : 
                   isHovered ? Color.FromArgb(220, 220, 220) : Color.FromArgb(154, 160, 166)))
            {
                // Фон вкладки с закругленными углами
                using (var path = new System.Drawing.Drawing2D.GraphicsPath())
                {
                    var radius = 6;
                    path.AddRoundedRectangle(new Rectangle(tabRect.X, tabRect.Y + 2, tabRect.Width - 2, tabRect.Height - 2), radius);
                    e.Graphics.FillPath(backBrush, path);
                }

                // Текст вкладки
                var title = tabPage.Text;
                if (title.Length > 20) title = title.Substring(0, 17) + "...";
                
                var textRect = new Rectangle(
                    tabRect.Left + 10,
                    tabRect.Top + 8,
                    tabRect.Width - 36,
                    tabRect.Height - 8
                );
                
                e.Graphics.DrawString(title, 
                    new Font("Segoe UI", 9.5f, FontStyle.Regular), 
                    textBrush, 
                    textRect,
                    new StringFormat { 
                        Trimming = StringTrimming.EllipsisCharacter,
                        Alignment = StringAlignment.Near,
                        LineAlignment = StringAlignment.Center
                    }
                );

                // Кнопка закрытия
                if (isSelected || isHovered)
                {
                    var isCloseHovered = closeRect.Contains(tabControl.PointToClient(Cursor.Position));
                    using (var closeBrush = new SolidBrush(isCloseHovered ? 
                           Color.FromArgb(200, 200, 200) : Color.FromArgb(154, 160, 166)))
                    {
                        e.Graphics.FillEllipse(closeBrush, closeRect);
                        using (var pen = new Pen(Color.FromArgb(53, 54, 58), 2))
                        {
                            // Рисуем крестик
                            e.Graphics.DrawLine(pen,
                                closeRect.X + 4, closeRect.Y + 4,
                                closeRect.X + closeRect.Width - 4, closeRect.Y + closeRect.Height - 4);
                            e.Graphics.DrawLine(pen,
                                closeRect.X + closeRect.Width - 4, closeRect.Y + 4,
                                closeRect.X + 4, closeRect.Y + closeRect.Height - 4);
                        }
                    }
                }

                // Индикатор активной вкладки
                if (isSelected)
                {
                    using (var highlightBrush = new SolidBrush(Color.FromArgb(26, 115, 232)))
                    {
                        e.Graphics.FillRectangle(highlightBrush,
                            tabRect.X, tabRect.Bottom - 2,
                            tabRect.Width - 1, 2);
                    }
                }
            }
        }

        private void TabControl_Selected(object sender, TabControlEventArgs e)
        {
            if (e.TabPage != null)
            {
                var webView = e.TabPage.Controls.OfType<WebView2>().FirstOrDefault();
                if (webView != null)
                {
                    urlBox.Text = webView.CoreWebView2?.Source ?? "";
                    this.webView = webView;
                }
            }
        }

        private void NewTabButton_Click(object sender, EventArgs e)
        {
            try
            {
                _ = CreateNewTab();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при создании новой вкладки: {ex.Message}", 
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task CreateNewTab(string url = "https://www.google.com")
        {
            try 
            {
                var tabPage = new TabPage("Новая вкладка")
                {
                    BackColor = Color.FromArgb(53, 54, 58),
                    Padding = new Padding(0),
                    Margin = new Padding(0)
                };

                var webContainer = new Panel
                {
                    Dock = DockStyle.Fill,
                    Margin = new Padding(0),
                    Padding = new Padding(0),
                    BackColor = Color.White
                };

                tabPage.Controls.Add(webContainer);
                tabControl.TabPages.Add(tabPage);
                tabControl.SelectedTab = tabPage;

                var userDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DeltaBrowser");
                Directory.CreateDirectory(userDataFolder);

                var options = new CoreWebView2EnvironmentOptions();
                var env = await CoreWebView2Environment.CreateAsync(null, userDataFolder, options);

                var newWebView = new WebView2
                {
                    Dock = DockStyle.Fill,
                    DefaultBackgroundColor = Color.White
                };

                webContainer.Controls.Add(newWebView);
                await newWebView.EnsureCoreWebView2Async(env);

                // Настройка WebView2
                newWebView.CoreWebView2.Settings.IsStatusBarEnabled = true;
                newWebView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = true;
                newWebView.CoreWebView2.Settings.IsZoomControlEnabled = true;
                newWebView.CoreWebView2.Settings.AreBrowserAcceleratorKeysEnabled = true;
                newWebView.CoreWebView2.Settings.IsPasswordAutosaveEnabled = true;
                newWebView.CoreWebView2.Settings.IsGeneralAutofillEnabled = true;
                newWebView.CoreWebView2.Settings.IsScriptEnabled = true;
                newWebView.CoreWebView2.Settings.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36";

                // Добавляем обработчики событий
                newWebView.CoreWebView2.NavigationStarting += WebView_NavigationStarting;
                newWebView.CoreWebView2.NavigationCompleted += WebView_NavigationCompleted;
                newWebView.CoreWebView2.SourceChanged += WebView_SourceChanged;
                newWebView.CoreWebView2.DownloadStarting += WebView_DownloadStarting;
                newWebView.CoreWebView2.DocumentTitleChanged += (s, e) =>
                {
                    if (this.InvokeRequired)
                    {
                        this.Invoke(new Action(() => {
                            var title = newWebView.CoreWebView2.DocumentTitle;
                            tabPage.Text = string.IsNullOrWhiteSpace(title) ? "Новая вкладка" : title;
                            tabControl.Invalidate();
                        }));
                    }
                    else
                    {
                        var title = newWebView.CoreWebView2.DocumentTitle;
                        tabPage.Text = string.IsNullOrWhiteSpace(title) ? "Новая вкладка" : title;
                        tabControl.Invalidate();
                    }
                };

                this.webView = newWebView;
                await newWebView.CoreWebView2.Navigate(url);

                // Обновляем положение кнопки новой вкладки
                newTabButton.Location = new Point(
                    tabControl.GetTabRect(tabControl.TabCount - 1).Right + 5,
                    4
                );
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при инициализации WebView2: {ex.Message}\n\nПожалуйста, убедитесь что установлен WebView2 Runtime.", 
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void InitializeDatabases()
        {
            // Инициализация базы данных истории
            if (!File.Exists("history.db"))
            {
                SQLiteConnection.CreateFile("history.db");
                historyDb = new SQLiteConnection("Data Source=history.db;Version=3;");
                historyDb.Open();
                string sql = @"CREATE TABLE History (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Url TEXT NOT NULL,
                    Title TEXT,
                    VisitDate DATETIME DEFAULT CURRENT_TIMESTAMP
                )";
                using (SQLiteCommand command = new SQLiteCommand(sql, historyDb))
                {
                    command.ExecuteNonQuery();
                }
            }
            else
            {
                historyDb = new SQLiteConnection("Data Source=history.db;Version=3;");
                historyDb.Open();
            }

            // Инициализация базы данных закладок
            if (!File.Exists("bookmarks.db"))
            {
                SQLiteConnection.CreateFile("bookmarks.db");
                bookmarksDb = new SQLiteConnection("Data Source=bookmarks.db;Version=3;");
                bookmarksDb.Open();
                string sql = @"CREATE TABLE Bookmarks (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Url TEXT NOT NULL,
                    Title TEXT,
                    AddedDate DATETIME DEFAULT CURRENT_TIMESTAMP,
                    Folder TEXT DEFAULT 'General'
                )";
                using (SQLiteCommand command = new SQLiteCommand(sql, bookmarksDb))
                {
                    command.ExecuteNonQuery();
                }
            }
            else
            {
                bookmarksDb = new SQLiteConnection("Data Source=bookmarks.db;Version=3;");
                bookmarksDb.Open();
            }

            // Инициализация базы данных паролей
            if (!File.Exists("passwords.db"))
            {
                SQLiteConnection.CreateFile("passwords.db");
                passwordsDb = new SQLiteConnection("Data Source=passwords.db;Version=3;");
                passwordsDb.Open();
                string sql = @"CREATE TABLE Passwords (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Domain TEXT NOT NULL,
                    Username TEXT,
                    Password TEXT,
                    LastModified DATETIME DEFAULT CURRENT_TIMESTAMP
                )";
                using (SQLiteCommand command = new SQLiteCommand(sql, passwordsDb))
                {
                    command.ExecuteNonQuery();
                }
            }
            else
            {
                passwordsDb = new SQLiteConnection("Data Source=passwords.db;Version=3;");
                passwordsDb.Open();
            }

            // Инициализация базы данных загрузок
            if (!File.Exists("downloads.db"))
            {
                SQLiteConnection.CreateFile("downloads.db");
                downloadsDb = new SQLiteConnection("Data Source=downloads.db;Version=3;");
                downloadsDb.Open();
                string sql = @"CREATE TABLE Downloads (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    FileName TEXT NOT NULL,
                    Url TEXT NOT NULL,
                    Path TEXT NOT NULL,
                    StartDate DATETIME DEFAULT CURRENT_TIMESTAMP,
                    CompletedDate DATETIME,
                    FileSize BIGINT,
                    Status TEXT
                )";
                using (SQLiteCommand command = new SQLiteCommand(sql, downloadsDb))
                {
                    command.ExecuteNonQuery();
                }
            }
            else
            {
                downloadsDb = new SQLiteConnection("Data Source=downloads.db;Version=3;");
                downloadsDb.Open();
            }
        }

        private void WebView_NavigationStarting(object sender, CoreWebView2NavigationStartingEventArgs e)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(() => {
                    loadingBar.Value = 0;
                    loadingBar.Visible = true;
                    loadingLabel.Visible = true;
                    
                    string url = e.Uri;
                    urlBox.Text = url;
                });
                return;
            }

            loadingBar.Value = 0;
            loadingBar.Visible = true;
            loadingLabel.Visible = true;
            
            string url = e.Uri;
            urlBox.Text = url;
        }

        private void WebView_NavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(() => {
                    loadingBar.Value = 100;
                    loadingBar.Visible = false;
                    loadingLabel.Visible = false;

                    if (e.IsSuccess)
                    {
                        var webView = (CoreWebView2)sender;
                        string url = webView.Source;
                        string title = webView.DocumentTitle;
                        
                        SaveToHistory(url, title);
                    }
                });
                return;
            }

            loadingBar.Value = 100;
            loadingBar.Visible = false;
            loadingLabel.Visible = false;

            if (e.IsSuccess)
            {
                var webView = (CoreWebView2)sender;
                string url = webView.Source;
                string title = webView.DocumentTitle;
                
                SaveToHistory(url, title);
            }
        }

        private void WebView_SourceChanged(object sender, CoreWebView2SourceChangedEventArgs e)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(() => {
                    urlBox.Text = webView.CoreWebView2.Source;
                });
                return;
            }

            urlBox.Text = webView.CoreWebView2.Source;
        }

        private void WebView_DownloadStarting(object sender, CoreWebView2DownloadStartingEventArgs e)
        {
            var downloadOperation = e.DownloadOperation;
            string fileName = downloadOperation.ResultFilePath;
            
            // Сохраняем информацию о загрузке в базу данных
            string sql = @"INSERT INTO Downloads (FileName, Url, Path, Status) 
                          VALUES (@fileName, @url, @path, @status)";
            
            using (SQLiteCommand command = new SQLiteCommand(sql, downloadsDb))
            {
                command.Parameters.AddWithValue("@fileName", Path.GetFileName(fileName));
                command.Parameters.AddWithValue("@url", downloadOperation.Uri);
                command.Parameters.AddWithValue("@path", fileName);
                command.Parameters.AddWithValue("@status", "Started");
                command.ExecuteNonQuery();
            }

            downloadOperation.StateChanged += (s, e) => {
                string updateSql = @"UPDATE Downloads 
                                   SET Status = @status, CompletedDate = @completedDate, FileSize = @fileSize 
                                   WHERE Path = @path";
                
                using (SQLiteCommand command = new SQLiteCommand(updateSql, downloadsDb))
                {
                    command.Parameters.AddWithValue("@path", fileName);
                    command.Parameters.AddWithValue("@status", downloadOperation.State.ToString());
                    command.Parameters.AddWithValue("@completedDate", 
                        downloadOperation.State == CoreWebView2DownloadState.Completed ? 
                        DateTime.Now : DBNull.Value);
                    command.Parameters.AddWithValue("@fileSize", downloadOperation.TotalBytesToReceive);
                    command.ExecuteNonQuery();
                }

                if (downloadOperation.State == CoreWebView2DownloadState.Completed)
                {
                    this.Invoke((MethodInvoker)delegate {
                        MessageBox.Show($"Загрузка завершена: {fileName}", "Загрузка", 
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                    });
                }
            };
        }

        private void SaveToHistory(string url, string title)
        {
            string sql = "INSERT INTO History (Url, Title) VALUES (@url, @title)";
            using (SQLiteCommand command = new SQLiteCommand(sql, historyDb))
            {
                command.Parameters.AddWithValue("@url", url);
                command.Parameters.AddWithValue("@title", title);
                command.ExecuteNonQuery();
            }
        }

        private void BackButton_Click(object sender, EventArgs e)
        {
            if (webView.CanGoBack)
                webView.GoBack();
        }

        private void ForwardButton_Click(object sender, EventArgs e)
        {
            if (webView.CanGoForward)
                webView.GoForward();
        }

        private void RefreshButton_Click(object sender, EventArgs e)
        {
            webView.Reload();
        }

        private void HomeButton_Click(object sender, EventArgs e)
        {
            webView.CoreWebView2.Navigate("https://www.google.com");
        }

        private void PasswordGeneratorButton_Click(object sender, EventArgs e)
        {
            using (var passwordGeneratorForm = new PasswordGeneratorForm())
            {
                passwordGeneratorForm.ShowDialog(this);
            }
        }

        private void BookmarkButton_Click(object sender, EventArgs e)
        {
            using (var bookmarksForm = new BookmarksForm(bookmarksDb))
            {
                bookmarksForm.Owner = this;
                bookmarksForm.ShowDialog();
            }
        }

        private void HistoryButton_Click(object sender, EventArgs e)
        {
            using (var historyForm = new HistoryForm(historyDb))
            {
                historyForm.Owner = this;
                historyForm.ShowDialog();
            }
        }

        private void SettingsButton_Click(object sender, EventArgs e)
        {
            using (var settingsForm = new SettingsForm(webView.CoreWebView2))
            {
                settingsForm.ShowDialog();
            }
        }

        private void DownloadButton_Click(object sender, EventArgs e)
        {
            using (var downloadsForm = new DownloadsForm(downloadsDb))
            {
                downloadsForm.ShowDialog();
            }
        }

        private void UrlBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Enter)
            {
                string url = urlBox.Text;
                if (!url.StartsWith("http://") && !url.StartsWith("https://"))
                {
                    url = "https://" + url;
                }
                webView.CoreWebView2.Navigate(url);
            }
        }

        public void NavigateToUrl(string url)
        {
            webView.CoreWebView2.Navigate(url);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
            if (historyDb != null && historyDb.State == ConnectionState.Open)
                historyDb.Close();
            if (passwordsDb != null && passwordsDb.State == ConnectionState.Open)
                passwordsDb.Close();
            if (bookmarksDb != null && bookmarksDb.State == ConnectionState.Open)
                bookmarksDb.Close();
            if (downloadsDb != null && downloadsDb.State == ConnectionState.Open)
                downloadsDb.Close();
        }

        private void MainForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control)
            {
                if (e.KeyCode == Keys.Tab)
                {
                    if (e.Shift)
                    {
                        // Ctrl + Shift + Tab - предыдущая страница
                        if (webView.CanGoBack)
                            webView.GoBack();
                    }
                    else
                    {
                        // Ctrl + Tab - следующая страница
                        if (webView.CanGoForward)
                            webView.GoForward();
                    }
                    e.Handled = true;
                }
                else if (e.KeyCode == Keys.PageUp)
                {
                    // Ctrl + PageUp - предыдущая страница
                    if (webView.CanGoBack)
                        webView.GoBack();
                    e.Handled = true;
                }
                else if (e.KeyCode == Keys.PageDown)
                {
                    // Ctrl + PageDown - следующая страница
                    if (webView.CanGoForward)
                        webView.GoForward();
                    e.Handled = true;
                }
            }
        }

        private void TabControl_MouseClick(object sender, MouseEventArgs e)
        {
            // Обработка клика по кнопке закрытия вкладки
            var tabControl = sender as TabControl;
            var tabs = tabControl.TabPages;

            for (var i = 0; i < tabs.Count; i++)
            {
                var tabRect = tabControl.GetTabRect(i);
                var closeRect = new Rectangle(tabRect.Right - 25, tabRect.Top + 10, 16, 16);

                if (closeRect.Contains(e.Location))
                {
                    if (tabs.Count > 1) // Не закрываем последнюю вкладку
                    {
                        tabs.RemoveAt(i);
                        if (i > 0)
                        {
                            tabControl.SelectedIndex = i - 1;
                        }
                        // Обновляем положение кнопки новой вкладки
                        newTabButton.Location = new Point(
                            tabControl.GetTabRect(tabControl.TabCount - 1).Right + 5,
                            0
                        );
                    }
                    break;
                }
            }
        }

        public void ApplyTheme(Color mainBackground, Color navBackground, Color textColor)
        {
            this.BackColor = mainBackground;
            navigationPanel.BackColor = navBackground;
            tabControl.BackColor = navBackground;
            
            foreach (TabPage tab in tabControl.TabPages)
            {
                tab.BackColor = Color.FromArgb(53, 54, 58);
                tab.ForeColor = Color.White;
            }

            newTabButton.BackColor = Color.FromArgb(53, 54, 58);
            newTabButton.ForeColor = Color.White;
            
            // Обновляем цвета кнопок и других элементов
            foreach (Control control in navigationPanel.Controls)
            {
                if (control is TableLayoutPanel buttonPanel)
                {
                    foreach (Control panelControl in buttonPanel.Controls)
                    {
                        if (panelControl is FlowLayoutPanel buttonGroup)
                        {
                            foreach (Control button in buttonGroup.Controls)
                            {
                                if (button is Button btn)
                                {
                                    btn.BackColor = Color.Transparent;
                                    btn.ForeColor = Color.FromArgb(200, 200, 200);
                                }
                            }
                        }
                        else if (panelControl is Panel urlPanel)
                        {
                            urlPanel.BackColor = Color.FromArgb(48, 49, 52);
                            foreach (Control urlControl in urlPanel.Controls)
                            {
                                if (urlControl is TextBox)
                                {
                                    urlControl.BackColor = urlPanel.BackColor;
                                    urlControl.ForeColor = Color.White;
                                }
                            }
                        }
                    }
                }
            }

            tabControl.Invalidate();
        }
    }
} 