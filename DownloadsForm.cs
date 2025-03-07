using System;
using System.Windows.Forms;
using System.Drawing;
using System.Data.SQLite;
using System.Data;
using System.Diagnostics;
using System.IO;

namespace DeltaBrowser
{
    public class DownloadsForm : Form
    {
        private DataGridView downloadsGrid;
        private Button clearButton;
        private Button openFolderButton;
        private Button deleteButton;
        private SQLiteConnection downloadsDb;
        private System.Windows.Forms.Timer updateTimer;

        public DownloadsForm(SQLiteConnection db)
        {
            downloadsDb = db;
            InitializeComponent();
            LoadDownloads();
            
            // Таймер для обновления статуса загрузок
            updateTimer = new System.Windows.Forms.Timer();
            updateTimer.Interval = 1000; // 1 секунда
            updateTimer.Tick += (s, e) => LoadDownloads();
            updateTimer.Start();
        }

        private void InitializeComponent()
        {
            this.Text = "Загрузки";
            this.Size = new Size(800, 600);
            this.BackColor = Color.FromArgb(45, 45, 48);
            this.ForeColor = Color.White;

            // Кнопки
            clearButton = new Button
            {
                Text = "Очистить историю",
                Width = 130,
                Location = new Point(10, 10),
                BackColor = Color.FromArgb(60, 60, 60),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            clearButton.Click += ClearButton_Click;

            openFolderButton = new Button
            {
                Text = "Открыть папку",
                Width = 130,
                Location = new Point(150, 10),
                BackColor = Color.FromArgb(60, 60, 60),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            openFolderButton.Click += OpenFolderButton_Click;

            deleteButton = new Button
            {
                Text = "Удалить",
                Width = 130,
                Location = new Point(290, 10),
                BackColor = Color.FromArgb(60, 60, 60),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            deleteButton.Click += DeleteButton_Click;

            // Таблица загрузок
            downloadsGrid = new DataGridView
            {
                Location = new Point(10, 50),
                Size = new Size(765, 500),
                BackgroundColor = Color.FromArgb(30, 30, 30),
                ForeColor = Color.Black,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true
            };
            downloadsGrid.DoubleClick += DownloadsGrid_DoubleClick;

            this.Controls.AddRange(new Control[] {
                clearButton, openFolderButton, deleteButton, downloadsGrid
            });
        }

        private void LoadDownloads()
        {
            string sql = @"SELECT Id, FileName, Url, Path, StartDate, CompletedDate, 
                          FileSize, Status FROM Downloads ORDER BY StartDate DESC";

            using (SQLiteCommand command = new SQLiteCommand(sql, downloadsDb))
            using (SQLiteDataAdapter adapter = new SQLiteDataAdapter(command))
            {
                DataTable table = new DataTable();
                adapter.Fill(table);
                downloadsGrid.DataSource = table;
            }

            // Настройка отображения колонок
            downloadsGrid.Columns["Id"].Visible = false;
            downloadsGrid.Columns["FileName"].HeaderText = "Имя файла";
            downloadsGrid.Columns["Url"].HeaderText = "URL";
            downloadsGrid.Columns["Path"].HeaderText = "Путь";
            downloadsGrid.Columns["StartDate"].HeaderText = "Начало загрузки";
            downloadsGrid.Columns["CompletedDate"].HeaderText = "Завершено";
            downloadsGrid.Columns["FileSize"].HeaderText = "Размер";
            downloadsGrid.Columns["Status"].HeaderText = "Статус";

            // Форматирование размера файла
            foreach (DataGridViewRow row in downloadsGrid.Rows)
            {
                if (row.Cells["FileSize"].Value != DBNull.Value)
                {
                    long size = Convert.ToInt64(row.Cells["FileSize"].Value);
                    row.Cells["FileSize"].Value = FormatFileSize(size);
                }
            }
        }

        private string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            int order = 0;
            double size = bytes;
            while (size >= 1024 && order < sizes.Length - 1)
            {
                order++;
                size = size / 1024;
            }
            return $"{size:0.##} {sizes[order]}";
        }

        private void ClearButton_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Очистить историю загрузок?", "Подтверждение",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                string sql = "DELETE FROM Downloads";
                using (SQLiteCommand command = new SQLiteCommand(sql, downloadsDb))
                {
                    command.ExecuteNonQuery();
                }
                LoadDownloads();
            }
        }

        private void OpenFolderButton_Click(object sender, EventArgs e)
        {
            if (downloadsGrid.SelectedRows.Count == 1)
            {
                string path = downloadsGrid.SelectedRows[0].Cells["Path"].Value.ToString();
                if (File.Exists(path))
                {
                    Process.Start("explorer.exe", $"/select,\"{path}\"");
                }
            }
        }

        private void DeleteButton_Click(object sender, EventArgs e)
        {
            if (downloadsGrid.SelectedRows.Count > 0)
            {
                if (MessageBox.Show("Удалить выбранные записи?", "Подтверждение",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    foreach (DataGridViewRow row in downloadsGrid.SelectedRows)
                    {
                        string sql = "DELETE FROM Downloads WHERE Id = @id";
                        using (SQLiteCommand command = new SQLiteCommand(sql, downloadsDb))
                        {
                            command.Parameters.AddWithValue("@id", row.Cells["Id"].Value);
                            command.ExecuteNonQuery();
                        }

                        // Удаляем файл, если он существует
                        string path = row.Cells["Path"].Value.ToString();
                        if (File.Exists(path))
                        {
                            try
                            {
                                File.Delete(path);
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show($"Не удалось удалить файл: {ex.Message}",
                                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            }
                        }
                    }
                    LoadDownloads();
                }
            }
        }

        private void DownloadsGrid_DoubleClick(object sender, EventArgs e)
        {
            if (downloadsGrid.SelectedRows.Count == 1)
            {
                string path = downloadsGrid.SelectedRows[0].Cells["Path"].Value.ToString();
                if (File.Exists(path))
                {
                    try
                    {
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = path,
                            UseShellExecute = true
                        });
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Не удалось открыть файл: {ex.Message}",
                            "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
            updateTimer.Stop();
        }
    }
}