using System;
using System.Windows.Forms;
using System.Drawing;
using System.Data.SQLite;
using System.Data;

namespace DeltaBrowser
{
    public class BookmarksForm : Form
    {
        private DataGridView bookmarksGrid;
        private Button addButton;
        private Button deleteButton;
        private Button editButton;
        private SQLiteConnection bookmarksDb;
        private TextBox searchBox;

        public BookmarksForm(SQLiteConnection db)
        {
            bookmarksDb = db;
            InitializeComponent();
            LoadBookmarks();
        }

        private void InitializeComponent()
        {
            this.Text = "Закладки";
            this.Size = new Size(800, 600);
            this.BackColor = Color.FromArgb(45, 45, 48);
            this.ForeColor = Color.White;

            // Поиск
            searchBox = new TextBox
            {
                Width = 200,
                Height = 25,
                Location = new Point(10, 10),
                BackColor = Color.FromArgb(70, 70, 70),
                ForeColor = Color.White
            };
            searchBox.TextChanged += SearchBox_TextChanged;

            // Кнопки
            addButton = new Button
            {
                Text = "Добавить",
                Width = 100,
                Location = new Point(220, 10),
                BackColor = Color.FromArgb(60, 60, 60),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            addButton.Click += AddButton_Click;

            deleteButton = new Button
            {
                Text = "Удалить",
                Width = 100,
                Location = new Point(330, 10),
                BackColor = Color.FromArgb(60, 60, 60),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            deleteButton.Click += DeleteButton_Click;

            editButton = new Button
            {
                Text = "Изменить",
                Width = 100,
                Location = new Point(440, 10),
                BackColor = Color.FromArgb(60, 60, 60),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            editButton.Click += EditButton_Click;

            // Таблица закладок
            bookmarksGrid = new DataGridView
            {
                Location = new Point(10, 45),
                Size = new Size(765, 505),
                BackgroundColor = Color.FromArgb(30, 30, 30),
                ForeColor = Color.Black,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true
            };

            this.Controls.AddRange(new Control[] { 
                searchBox, addButton, deleteButton, editButton, bookmarksGrid 
            });
        }

        private void LoadBookmarks(string searchTerm = "")
        {
            string sql = @"SELECT Id, Title, Url, Folder, AddedDate 
                          FROM Bookmarks 
                          WHERE Title LIKE @search OR Url LIKE @search
                          ORDER BY AddedDate DESC";

            using (SQLiteCommand command = new SQLiteCommand(sql, bookmarksDb))
            {
                command.Parameters.AddWithValue("@search", $"%{searchTerm}%");
                using (SQLiteDataAdapter adapter = new SQLiteDataAdapter(command))
                {
                    DataTable table = new DataTable();
                    adapter.Fill(table);
                    bookmarksGrid.DataSource = table;
                }
            }

            // Настройка отображения колонок
            bookmarksGrid.Columns["Id"].Visible = false;
            bookmarksGrid.Columns["Title"].HeaderText = "Название";
            bookmarksGrid.Columns["Url"].HeaderText = "Адрес";
            bookmarksGrid.Columns["Folder"].HeaderText = "Папка";
            bookmarksGrid.Columns["AddedDate"].HeaderText = "Дата добавления";
        }

        private void SearchBox_TextChanged(object sender, EventArgs e)
        {
            LoadBookmarks(searchBox.Text);
        }

        private void AddButton_Click(object sender, EventArgs e)
        {
            using (var form = new BookmarkEditForm())
            {
                if (form.ShowDialog() == DialogResult.OK)
                {
                    string sql = @"INSERT INTO Bookmarks (Title, Url, Folder) 
                                 VALUES (@title, @url, @folder)";

                    using (SQLiteCommand command = new SQLiteCommand(sql, bookmarksDb))
                    {
                        command.Parameters.AddWithValue("@title", form.BookmarkTitle);
                        command.Parameters.AddWithValue("@url", form.BookmarkUrl);
                        command.Parameters.AddWithValue("@folder", form.BookmarkFolder);
                        command.ExecuteNonQuery();
                    }

                    LoadBookmarks();
                }
            }
        }

        private void DeleteButton_Click(object sender, EventArgs e)
        {
            if (bookmarksGrid.SelectedRows.Count > 0)
            {
                if (MessageBox.Show("Удалить выбранные закладки?", "Подтверждение",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    foreach (DataGridViewRow row in bookmarksGrid.SelectedRows)
                    {
                        string sql = "DELETE FROM Bookmarks WHERE Id = @id";
                        using (SQLiteCommand command = new SQLiteCommand(sql, bookmarksDb))
                        {
                            command.Parameters.AddWithValue("@id", row.Cells["Id"].Value);
                            command.ExecuteNonQuery();
                        }
                    }
                    LoadBookmarks();
                }
            }
        }

        private void EditButton_Click(object sender, EventArgs e)
        {
            if (bookmarksGrid.SelectedRows.Count == 1)
            {
                var row = bookmarksGrid.SelectedRows[0];
                using (var form = new BookmarkEditForm(
                    row.Cells["Title"].Value.ToString(),
                    row.Cells["Url"].Value.ToString(),
                    row.Cells["Folder"].Value.ToString()))
                {
                    if (form.ShowDialog() == DialogResult.OK)
                    {
                        string sql = @"UPDATE Bookmarks 
                                     SET Title = @title, Url = @url, Folder = @folder 
                                     WHERE Id = @id";

                        using (SQLiteCommand command = new SQLiteCommand(sql, bookmarksDb))
                        {
                            command.Parameters.AddWithValue("@id", row.Cells["Id"].Value);
                            command.Parameters.AddWithValue("@title", form.BookmarkTitle);
                            command.Parameters.AddWithValue("@url", form.BookmarkUrl);
                            command.Parameters.AddWithValue("@folder", form.BookmarkFolder);
                            command.ExecuteNonQuery();
                        }

                        LoadBookmarks();
                    }
                }
            }
        }
    }

    public class BookmarkEditForm : Form
    {
        private TextBox titleBox;
        private TextBox urlBox;
        private ComboBox folderBox;
        private Button saveButton;
        private Button cancelButton;

        public string BookmarkTitle => titleBox.Text;
        public string BookmarkUrl => urlBox.Text;
        public string BookmarkFolder => folderBox.Text;

        public BookmarkEditForm(string title = "", string url = "", string folder = "General")
        {
            InitializeComponent();
            titleBox.Text = title;
            urlBox.Text = url;
            folderBox.Text = folder;
        }

        private void InitializeComponent()
        {
            this.Text = "Редактирование закладки";
            this.Size = new Size(400, 200);
            this.BackColor = Color.FromArgb(45, 45, 48);
            this.ForeColor = Color.White;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;

            var titleLabel = new Label
            {
                Text = "Название:",
                Location = new Point(10, 10),
                AutoSize = true
            };

            titleBox = new TextBox
            {
                Location = new Point(10, 30),
                Width = 360,
                BackColor = Color.FromArgb(70, 70, 70),
                ForeColor = Color.White
            };

            var urlLabel = new Label
            {
                Text = "Адрес:",
                Location = new Point(10, 60),
                AutoSize = true
            };

            urlBox = new TextBox
            {
                Location = new Point(10, 80),
                Width = 360,
                BackColor = Color.FromArgb(70, 70, 70),
                ForeColor = Color.White
            };

            var folderLabel = new Label
            {
                Text = "Папка:",
                Location = new Point(10, 110),
                AutoSize = true
            };

            folderBox = new ComboBox
            {
                Location = new Point(10, 130),
                Width = 360,
                BackColor = Color.FromArgb(70, 70, 70),
                ForeColor = Color.White
            };
            folderBox.Items.AddRange(new string[] { "General", "Work", "Personal", "Other" });

            saveButton = new Button
            {
                Text = "Сохранить",
                DialogResult = DialogResult.OK,
                Location = new Point(190, 130),
                BackColor = Color.FromArgb(60, 60, 60),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };

            cancelButton = new Button
            {
                Text = "Отмена",
                DialogResult = DialogResult.Cancel,
                Location = new Point(280, 130),
                BackColor = Color.FromArgb(60, 60, 60),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };

            this.Controls.AddRange(new Control[] {
                titleLabel, titleBox,
                urlLabel, urlBox,
                folderLabel, folderBox,
                saveButton, cancelButton
            });

            this.AcceptButton = saveButton;
            this.CancelButton = cancelButton;
        }
    }
} 