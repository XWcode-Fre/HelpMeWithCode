using System;
using System.Windows.Forms;
using System.Drawing;
using System.Data.SQLite;
using System.Data;

namespace DeltaBrowser
{
    public class HistoryForm : Form
    {
        private DataGridView historyGrid;
        private Button clearButton;
        private Button deleteButton;
        private TextBox searchBox;
        private DateTimePicker fromDate;
        private DateTimePicker toDate;
        private SQLiteConnection historyDb;

        public HistoryForm(SQLiteConnection db)
        {
            historyDb = db;
            InitializeComponent();
            LoadHistory();
        }

        private void InitializeComponent()
        {
            this.Text = "История";
            this.Size = new Size(900, 600);
            this.BackColor = Color.FromArgb(45, 45, 48);
            this.ForeColor = Color.White;

            // Поиск
            var searchLabel = new Label
            {
                Text = "Поиск:",
                Location = new Point(10, 15),
                AutoSize = true
            };

            searchBox = new TextBox
            {
                Width = 200,
                Height = 25,
                Location = new Point(70, 12),
                BackColor = Color.FromArgb(70, 70, 70),
                ForeColor = Color.White
            };
            searchBox.TextChanged += SearchBox_TextChanged;

            // Фильтр по датам
            var fromLabel = new Label
            {
                Text = "От:",
                Location = new Point(290, 15),
                AutoSize = true
            };

            fromDate = new DateTimePicker
            {
                Location = new Point(320, 12),
                Format = DateTimePickerFormat.Short,
                BackColor = Color.FromArgb(70, 70, 70),
                ForeColor = Color.White
            };
            fromDate.ValueChanged += DateFilter_ValueChanged;

            var toLabel = new Label
            {
                Text = "До:",
                Location = new Point(450, 15),
                AutoSize = true
            };

            toDate = new DateTimePicker
            {
                Location = new Point(480, 12),
                Format = DateTimePickerFormat.Short,
                BackColor = Color.FromArgb(70, 70, 70),
                ForeColor = Color.White
            };
            toDate.ValueChanged += DateFilter_ValueChanged;

            // Кнопки
            clearButton = new Button
            {
                Text = "Очистить историю",
                Width = 130,
                Location = new Point(640, 10),
                BackColor = Color.FromArgb(60, 60, 60),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            clearButton.Click += ClearButton_Click;

            deleteButton = new Button
            {
                Text = "Удалить выбранное",
                Width = 130,
                Location = new Point(780, 10),
                BackColor = Color.FromArgb(60, 60, 60),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            deleteButton.Click += DeleteButton_Click;

            // Таблица истории
            historyGrid = new DataGridView
            {
                Location = new Point(10, 45),
                Size = new Size(865, 505),
                BackgroundColor = Color.FromArgb(30, 30, 30),
                ForeColor = Color.Black,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true
            };
            historyGrid.DoubleClick += HistoryGrid_DoubleClick;

            this.Controls.AddRange(new Control[] {
                searchLabel, searchBox,
                fromLabel, fromDate,
                toLabel, toDate,
                clearButton, deleteButton,
                historyGrid
            });
        }

        private void LoadHistory()
        {
            string sql = @"SELECT Id, Title, Url, VisitDate 
                          FROM History 
                          WHERE (Title LIKE @search OR Url LIKE @search)
                          AND VisitDate BETWEEN @fromDate AND @toDate
                          ORDER BY VisitDate DESC";

            using (SQLiteCommand command = new SQLiteCommand(sql, historyDb))
            {
                command.Parameters.AddWithValue("@search", $"%{searchBox.Text}%");
                command.Parameters.AddWithValue("@fromDate", fromDate.Value.Date);
                command.Parameters.AddWithValue("@toDate", toDate.Value.Date.AddDays(1));

                using (SQLiteDataAdapter adapter = new SQLiteDataAdapter(command))
                {
                    DataTable table = new DataTable();
                    adapter.Fill(table);
                    historyGrid.DataSource = table;
                }
            }

            // Настройка отображения колонок
            historyGrid.Columns["Id"].Visible = false;
            historyGrid.Columns["Title"].HeaderText = "Название";
            historyGrid.Columns["Url"].HeaderText = "Адрес";
            historyGrid.Columns["VisitDate"].HeaderText = "Дата посещения";
        }

        private void SearchBox_TextChanged(object sender, EventArgs e)
        {
            LoadHistory();
        }

        private void DateFilter_ValueChanged(object sender, EventArgs e)
        {
            LoadHistory();
        }

        private void ClearButton_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Вы уверены, что хотите очистить всю историю?",
                "Подтверждение", MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                string sql = "DELETE FROM History";
                using (SQLiteCommand command = new SQLiteCommand(sql, historyDb))
                {
                    command.ExecuteNonQuery();
                }
                LoadHistory();
            }
        }

        private void DeleteButton_Click(object sender, EventArgs e)
        {
            if (historyGrid.SelectedRows.Count > 0)
            {
                if (MessageBox.Show("Удалить выбранные записи?",
                    "Подтверждение", MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    foreach (DataGridViewRow row in historyGrid.SelectedRows)
                    {
                        string sql = "DELETE FROM History WHERE Id = @id";
                        using (SQLiteCommand command = new SQLiteCommand(sql, historyDb))
                        {
                            command.Parameters.AddWithValue("@id", row.Cells["Id"].Value);
                            command.ExecuteNonQuery();
                        }
                    }
                    LoadHistory();
                }
            }
        }

        private void HistoryGrid_DoubleClick(object sender, EventArgs e)
        {
            if (historyGrid.SelectedRows.Count == 1)
            {
                var url = historyGrid.SelectedRows[0].Cells["Url"].Value.ToString();
                if (Owner is MainForm mainForm)
                {
                    mainForm.NavigateToUrl(url);
                }
            }
        }
    }
} 