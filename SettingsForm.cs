using System;
using System.Windows.Forms;
using System.Drawing;
using Microsoft.Web.WebView2.Core;

namespace DeltaBrowser
{
    public partial class SettingsForm : Form
    {
        private CoreWebView2 webView;
        private CheckBox javascriptEnabled;
        private CheckBox popupsEnabled;
        private CheckBox statusBarEnabled;
        private CheckBox devToolsEnabled;
        private CheckBox contextMenuEnabled;
        private CheckBox zoomEnabled;
        private Button clearCacheButton;
        private Button clearCookiesButton;
        private Button saveButton;
        private Button cancelButton;
        private Label creditLabel;
        private ComboBox themeSelector;
        private Label themeLabel;

        public SettingsForm(CoreWebView2 webView)
        {
            this.webView = webView;
            InitializeComponent();
            LoadSettings();
            AddCredits();
        }

        private void InitializeComponent()
        {
            this.Text = "Настройки";
            this.Size = new Size(400, 500);
            this.BackColor = Color.FromArgb(45, 45, 48);
            this.ForeColor = Color.White;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;

            var panel = new Panel
            {
                AutoScroll = true,
                Dock = DockStyle.Fill,
                Padding = new Padding(10)
            };

            // Настройки JavaScript
            javascriptEnabled = new CheckBox
            {
                Text = "Включить JavaScript",
                AutoSize = true,
                Location = new Point(20, 20),
                ForeColor = Color.White
            };

            // Настройки всплывающих окон
            popupsEnabled = new CheckBox
            {
                Text = "Разрешить всплывающие окна",
                AutoSize = true,
                Location = new Point(20, 50),
                ForeColor = Color.White
            };

            // Строка состояния
            statusBarEnabled = new CheckBox
            {
                Text = "Показывать строку состояния",
                AutoSize = true,
                Location = new Point(20, 80),
                ForeColor = Color.White
            };

            // Инструменты разработчика
            devToolsEnabled = new CheckBox
            {
                Text = "Включить инструменты разработчика",
                AutoSize = true,
                Location = new Point(20, 110),
                ForeColor = Color.White
            };

            // Контекстное меню
            contextMenuEnabled = new CheckBox
            {
                Text = "Включить контекстное меню",
                AutoSize = true,
                Location = new Point(20, 140),
                ForeColor = Color.White
            };

            // Масштабирование
            zoomEnabled = new CheckBox
            {
                Text = "Разрешить масштабирование",
                AutoSize = true,
                Location = new Point(20, 170),
                ForeColor = Color.White
            };

            // Кнопки очистки
            clearCacheButton = new Button
            {
                Text = "Очистить кэш",
                Width = 150,
                Location = new Point(20, 220),
                BackColor = Color.FromArgb(60, 60, 60),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            clearCacheButton.Click += ClearCacheButton_Click;

            clearCookiesButton = new Button
            {
                Text = "Очистить cookies",
                Width = 150,
                Location = new Point(20, 260),
                BackColor = Color.FromArgb(60, 60, 60),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            clearCookiesButton.Click += ClearCookiesButton_Click;

            // Добавляем выбор темы
            themeLabel = new Label
            {
                Text = "Тема оформления:",
                AutoSize = true,
                Location = new Point(20, 300),
                ForeColor = Color.White
            };

            themeSelector = new ComboBox
            {
                Width = 150,
                Location = new Point(20, 330),
                BackColor = Color.FromArgb(60, 60, 60),
                ForeColor = Color.White,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            themeSelector.Items.AddRange(new string[] { "Тёмная", "Светлая", "Персиковая", "Голубая" });
            themeSelector.SelectedIndex = 0;
            themeSelector.SelectedIndexChanged += ThemeSelector_SelectedIndexChanged;

            // Кнопки сохранения/отмены
            saveButton = new Button
            {
                Text = "Сохранить",
                DialogResult = DialogResult.OK,
                Width = 100,
                Location = new Point(180, 400),
                BackColor = Color.FromArgb(60, 60, 60),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            saveButton.Click += SaveButton_Click;

            cancelButton = new Button
            {
                Text = "Отмена",
                DialogResult = DialogResult.Cancel,
                Width = 100,
                Location = new Point(290, 400),
                BackColor = Color.FromArgb(60, 60, 60),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };

            panel.Controls.AddRange(new Control[] {
                javascriptEnabled,
                popupsEnabled,
                statusBarEnabled,
                devToolsEnabled,
                contextMenuEnabled,
                zoomEnabled,
                clearCacheButton,
                clearCookiesButton,
                themeLabel,
                themeSelector,
                saveButton,
                cancelButton
            });

            this.Controls.Add(panel);

            // Добавляем подпись разработчика
            creditLabel = new Label
            {
                Text = "made by killmeqq",
                Font = new Font("Segoe UI", 8),
                ForeColor = Color.FromArgb(150, 150, 150),
                AutoSize = true,
                Dock = DockStyle.Bottom,
                TextAlign = ContentAlignment.MiddleCenter,
                Padding = new Padding(0, 0, 0, 10)
            };
            this.Controls.Add(creditLabel);
        }

        private void LoadSettings()
        {
            javascriptEnabled.Checked = webView.Settings.IsScriptEnabled;
            popupsEnabled.Checked = webView.Settings.AreDefaultScriptDialogsEnabled;
            statusBarEnabled.Checked = webView.Settings.IsStatusBarEnabled;
            devToolsEnabled.Checked = webView.Settings.AreDevToolsEnabled;
            contextMenuEnabled.Checked = webView.Settings.AreDefaultContextMenusEnabled;
            zoomEnabled.Checked = webView.Settings.IsZoomControlEnabled;
        }

        private async void ClearCacheButton_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Очистить кэш браузера?", "Подтверждение",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                await webView.Profile.ClearBrowsingDataAsync();
                MessageBox.Show("Кэш очищен", "Успешно",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private async void ClearCookiesButton_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Очистить все cookies?", "Подтверждение",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                await webView.Profile.ClearBrowsingDataAsync(
                    CoreWebView2BrowsingDataKinds.Cookies);
                MessageBox.Show("Cookies очищены", "Успешно",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void SaveButton_Click(object sender, EventArgs e)
        {
            webView.Settings.IsScriptEnabled = javascriptEnabled.Checked;
            webView.Settings.AreDefaultScriptDialogsEnabled = popupsEnabled.Checked;
            webView.Settings.IsStatusBarEnabled = statusBarEnabled.Checked;
            webView.Settings.AreDevToolsEnabled = devToolsEnabled.Checked;
            webView.Settings.AreDefaultContextMenusEnabled = contextMenuEnabled.Checked;
            webView.Settings.IsZoomControlEnabled = zoomEnabled.Checked;
        }

        private void ThemeSelector_SelectedIndexChanged(object sender, EventArgs e)
        {
            var mainForm = this.Owner as MainForm;
            if (mainForm != null)
            {
                switch (themeSelector.SelectedIndex)
                {
                    case 0: // Тёмная
                        ApplyTheme(Color.FromArgb(45, 45, 48), Color.FromArgb(30, 30, 30), Color.White);
                        break;
                    case 1: // Светлая
                        ApplyTheme(Color.White, Color.FromArgb(240, 240, 240), Color.Black);
                        break;
                    case 2: // Персиковая
                        ApplyTheme(Color.FromArgb(255, 218, 185), Color.FromArgb(255, 228, 196), Color.Black);
                        break;
                    case 3: // Голубая
                        ApplyTheme(Color.FromArgb(176, 224, 230), Color.FromArgb(173, 216, 230), Color.Black);
                        break;
                }
            }
        }

        private void ApplyTheme(Color mainBackground, Color navBackground, Color textColor)
        {
            var mainForm = this.Owner as MainForm;
            if (mainForm != null)
            {
                mainForm.ApplyTheme(mainBackground, navBackground, textColor);
                this.BackColor = mainBackground;
                this.ForeColor = textColor;
                
                foreach (Control control in this.Controls.Find("panel", true)[0].Controls)
                {
                    if (control is CheckBox || control is Label)
                    {
                        control.ForeColor = textColor;
                    }
                    else if (control is Button)
                    {
                        control.BackColor = navBackground;
                        control.ForeColor = textColor;
                    }
                    else if (control is ComboBox)
                    {
                        control.BackColor = navBackground;
                        control.ForeColor = textColor;
                    }
                }
            }
        }

        private void AddCredits()
        {
            // Убеждаемся, что подпись всегда внизу
            this.Controls.SetChildIndex(creditLabel, this.Controls.Count - 1);
        }
    }
} 