using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Threading;

namespace ImageSorterApp
{
    public class ImageSorterForm : Form
    {
        private string[] _imageFiles;
        private int _currentIndex = 0;
        private string _outputFolderPath;
        private string _sourceFolderPath;
        private PictureBox _pictureBox;
        private Label _notificationLabel;

        public ImageSorterForm()
        {
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

            SelectFolders();
            LoadImage();

            KeyDown += OnKeyDown;
            Resize += (s, e) => LoadImage();
        }

        private void ShowNotification(string message)
        {
            _notificationLabel.Text = message;
            _notificationLabel.Visible = true;
            System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer { Interval = 2000 };
            timer.Tick += (s, e) =>
            {
                _notificationLabel.Visible = false;
                timer.Stop();
            };
            timer.Start();
        }

        private void SelectFolders()
        {
            using (var folderDialog = new FolderBrowserDialog())
            {
                MessageBox.Show("Выберите папку с изображениями.");
                if (folderDialog.ShowDialog() == DialogResult.OK)
                {
                    _sourceFolderPath = folderDialog.SelectedPath;
                    _imageFiles = Directory.GetFiles(_sourceFolderPath, "*.*")
                        .Where(f => f.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
                                    f.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                        .ToArray();
                }
                
                MessageBox.Show("Выберите папку для сохранения изображений.");
                if (folderDialog.ShowDialog() == DialogResult.OK)
                {
                    _outputFolderPath = folderDialog.SelectedPath;
                }
            }

            if (_imageFiles == null || _imageFiles.Length == 0)
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
                _pictureBox.Image = Image.FromFile(_imageFiles[_currentIndex]);
                Text = $"Image {(_currentIndex + 1)} of {_imageFiles.Length}";
            }
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Left:
                    if (_currentIndex > 0)
                    {
                        _currentIndex--;
                        LoadImage();
                    }
                    break;

                case Keys.Right:
                    if (_currentIndex < _imageFiles.Length - 1)
                    {
                        _currentIndex++;
                        LoadImage();
                    }
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
            string sourceFile = _imageFiles[_currentIndex];
            string destinationFile = Path.Combine(_outputFolderPath, Path.GetFileName(sourceFile));

            if (!File.Exists(destinationFile))
            {
                File.Copy(sourceFile, destinationFile);
                ShowNotification($"Сохранено: {Path.GetFileName(sourceFile)}");
            }
            else
            {
                ShowNotification("Файл уже существует в выходной папке.");
            }
        }

        private void DeleteSavedImage()
        {
            string savedFile = Path.Combine(_outputFolderPath, Path.GetFileName(_imageFiles[_currentIndex]));

            if (File.Exists(savedFile))
            {
                File.Delete(savedFile);
                ShowNotification($"Удалено: {Path.GetFileName(savedFile)}");
            }
            else
            {
                ShowNotification("Файл отсутствует в выходной папке.");
            }
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
                    ShowNotification($"Ошибка удаления {Path.GetFileName(_imageFiles[i])}");
                }
            }
            ShowNotification("Просмотренные изображения удалены.");
        }

        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new ImageSorterForm());
        }
    }
}