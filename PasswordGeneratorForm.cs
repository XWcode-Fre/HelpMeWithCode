using System;
using System.Windows.Forms;
using System.Drawing;

namespace DeltaBrowser
{
    public class PasswordGeneratorForm : Form
    {
        private NumericUpDown lengthInput;
        private CheckBox lowercaseCheckbox;
        private CheckBox uppercaseCheckbox;
        private CheckBox numbersCheckbox;
        private CheckBox specialCheckbox;
        private TextBox generatedPasswordBox;
        private Button generateButton;
        private Button copyButton;

        public PasswordGeneratorForm()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "Генератор паролей";
            this.Size = new Size(400, 300);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;

            var lengthLabel = new Label
            {
                Text = "Длина пароля:",
                Location = new Point(20, 20),
                AutoSize = true
            };

            lengthInput = new NumericUpDown
            {
                Location = new Point(150, 18),
                Minimum = 8,
                Maximum = 64,
                Value = 16
            };

            lowercaseCheckbox = new CheckBox
            {
                Text = "Строчные буквы (a-z)",
                Location = new Point(20, 50),
                Checked = true
            };

            uppercaseCheckbox = new CheckBox
            {
                Text = "Заглавные буквы (A-Z)",
                Location = new Point(20, 80),
                Checked = true
            };

            numbersCheckbox = new CheckBox
            {
                Text = "Цифры (0-9)",
                Location = new Point(20, 110),
                Checked = true
            };

            specialCheckbox = new CheckBox
            {
                Text = "Специальные символы (!@#$%^&*)",
                Location = new Point(20, 140),
                Checked = true
            };

            generatedPasswordBox = new TextBox
            {
                Location = new Point(20, 180),
                Width = 340,
                ReadOnly = true,
                Font = new Font("Consolas", 12)
            };

            generateButton = new Button
            {
                Text = "Сгенерировать",
                Location = new Point(20, 220),
                Width = 120
            };
            generateButton.Click += GenerateButton_Click;

            copyButton = new Button
            {
                Text = "Копировать",
                Location = new Point(150, 220),
                Width = 120
            };
            copyButton.Click += CopyButton_Click;

            this.Controls.AddRange(new Control[] {
                lengthLabel,
                lengthInput,
                lowercaseCheckbox,
                uppercaseCheckbox,
                numbersCheckbox,
                specialCheckbox,
                generatedPasswordBox,
                generateButton,
                copyButton
            });
        }

        private void GenerateButton_Click(object sender, EventArgs e)
        {
            try
            {
                string password = PasswordGenerator.GeneratePassword(
                    (int)lengthInput.Value,
                    lowercaseCheckbox.Checked,
                    uppercaseCheckbox.Checked,
                    numbersCheckbox.Checked,
                    specialCheckbox.Checked
                );
                generatedPasswordBox.Text = password;
            }
            catch (ArgumentException ex)
            {
                MessageBox.Show(ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void CopyButton_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(generatedPasswordBox.Text))
            {
                Clipboard.SetText(generatedPasswordBox.Text);
                MessageBox.Show("Пароль скопирован в буфер обмена!", "Успешно", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
    }
} 