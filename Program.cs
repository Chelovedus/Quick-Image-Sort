using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Threading;
using System.Configuration;

namespace ImageSorterApp
{
    public class StartupForm : Form
    {
        private TextBox _sourceTextBox;
        private TextBox _outputTextBox;
        private Button _selectSourceButton;
        private Button _selectOutputButton;
        private Button _continueButton;

        public string SourceFolderPath { get; private set; }
        public string OutputFolderPath { get; private set; }

        public StartupForm()
        {
            Text = "Настройка путей";
            Width = 500;
            Height = 200;

            _sourceTextBox = new TextBox { Left = 20, Top = 20, Width = 350 };
            _selectSourceButton = new Button { Text = "...", Left = 380, Top = 18, Width = 50 };
            _selectSourceButton.Click += (s, e) => SelectFolder(_sourceTextBox);

            _outputTextBox = new TextBox { Left = 20, Top = 60, Width = 350 };
            _selectOutputButton = new Button { Text = "...", Left = 380, Top = 58, Width = 50 };
            _selectOutputButton.Click += (s, e) => SelectFolder(_outputTextBox);

            _continueButton = new Button { Text = "Далее", Left = 200, Top = 100, Width = 100 };
            _continueButton.Click += ContinueButton_Click;

            Controls.Add(_sourceTextBox);
            Controls.Add(_selectSourceButton);
            Controls.Add(_outputTextBox);
            Controls.Add(_selectOutputButton);
            Controls.Add(_continueButton);

            _sourceTextBox.Text = ConfigurationManager.AppSettings["SourceFolderPath"];
            _outputTextBox.Text = ConfigurationManager.AppSettings["OutputFolderPath"];
        }

        private void SelectFolder(TextBox textBox)
        {
            using (var folderDialog = new FolderBrowserDialog())
            {
                if (folderDialog.ShowDialog() == DialogResult.OK)
                {
                    textBox.Text = folderDialog.SelectedPath;
                }
            }
        }

        private void ContinueButton_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_sourceTextBox.Text) || string.IsNullOrEmpty(_outputTextBox.Text))
            {
                MessageBox.Show("Выберите обе папки.");
                return;
            }

            SaveSetting("SourceFolderPath", _sourceTextBox.Text);
            SaveSetting("OutputFolderPath", _outputTextBox.Text);

            SourceFolderPath = _sourceTextBox.Text;
            OutputFolderPath = _outputTextBox.Text;

            DialogResult = DialogResult.OK;
            Close();
        }

        private void SaveSetting(string key, string value)
        {
            var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            config.AppSettings.Settings[key].Value = value;
            config.Save(ConfigurationSaveMode.Modified);
            ConfigurationManager.RefreshSection("appSettings");
        }
    }

    public class ImageSorterForm : Form
    {
        private string[] _imageFiles;
        private int _currentIndex = 0;
        private string _outputFolderPath;
        private string _sourceFolderPath;
        private PictureBox _pictureBox;
        private Label _notificationLabel;

        public ImageSorterForm(string sourceFolderPath, string outputFolderPath)
        {
            _sourceFolderPath = sourceFolderPath;
            _outputFolderPath = outputFolderPath;

            Text = "Image Sorter";

            _pictureBox = new PictureBox
            {
                Dock = DockStyle.Fill,
                SizeMode = PictureBoxSizeMode.Zoom
            };
            Controls.Add(_pictureBox);

            _notificationLabel = new Label
            {
                AutoSize = true,
                ForeColor = Color.White,
                BackColor = Color.Black,
                Padding = new Padding(10),
                Visible = false,
                Top = 10,
                Left = Width - 250
            };
            Controls.Add(_notificationLabel);

            LoadImages();
            LoadImage();

            KeyDown += OnKeyDown;
            Resize += (s, e) => LoadImage();
        }

        private void LoadImages()
        {
            _imageFiles = Directory.GetFiles(_sourceFolderPath, "*.*")
                .Where(f => f.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
                            f.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                .ToArray();

            if (_imageFiles.Length == 0)
            {
                MessageBox.Show("В папке нет изображений.");
                Close();
            }
        }

        private void LoadImage()
        {
            if (_imageFiles.Length > 0 && _currentIndex >= 0 && _currentIndex < _imageFiles.Length)
            {
                _pictureBox.Image?.Dispose();
                var image = Image.FromFile(_imageFiles[_currentIndex]);
                _pictureBox.SizeMode = image.Width < 700 && image.Height < 700 ? PictureBoxSizeMode.CenterImage : PictureBoxSizeMode.Zoom;
                _pictureBox.Image = image;
                Text = $"Image {(_currentIndex + 1)} of {_imageFiles.Length}";
            }
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Left:
                    if (_currentIndex > 0) _currentIndex--;
                    LoadImage();
                    break;
                case Keys.Right:
                    if (_currentIndex < _imageFiles.Length - 1) _currentIndex++;
                    LoadImage();
                    break;
                case Keys.Up:
                    SaveCurrentImage();
                    break;
                case Keys.Down:
                    DeleteSavedImage();
                    break;
                case Keys.Escape:
                    DeleteViewedImages();
                    Close();
                    break;
            }
        }

        private void SaveCurrentImage()
        {
            string destFile = Path.Combine(_outputFolderPath, Path.GetFileName(_imageFiles[_currentIndex]));
            if (!File.Exists(destFile)) File.Copy(_imageFiles[_currentIndex], destFile);
        }

        private void DeleteSavedImage()
        {
            string destFile = Path.Combine(_outputFolderPath, Path.GetFileName(_imageFiles[_currentIndex]));
            if (File.Exists(destFile)) File.Delete(destFile);
        }

        private void DeleteViewedImages()
        {
            _pictureBox.Image?.Dispose();
            for (int i = 0; i <= _currentIndex; i++)
            {
                try
                {
                    File.Delete(_imageFiles[i]);
                }
                catch (Exception ex)
                {
                }
            }
        }

        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            using (var startupForm = new StartupForm())
            {
                if (startupForm.ShowDialog() == DialogResult.OK)
                {
                    Application.Run(new ImageSorterForm(startupForm.SourceFolderPath, startupForm.OutputFolderPath));
                }
            }
        }
    }
}
