using System;
using System.IO;
using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace inwdwhg
{
    public partial class MainWindow : Window
    {
        private DispatcherTimer _startupTimer;
        private int _startupTicks = 0;
        private bool _isProductSelected = false;

        private string _rootCloudPath = @"C:\Program Files (x86)\Steam\steamapps\common\csgo legacy\nl_cloud";
        private string _currentCloudPath = "";

        public MainWindow()
        {
            InitializeComponent();

            ((Storyboard)FindResource("SpinAnimation")).Begin();
            ((Storyboard)FindResource("FadeInAnimation")).Begin();

            _startupTimer = new DispatcherTimer();
            _startupTimer.Interval = TimeSpan.FromMilliseconds(400);
            _startupTimer.Tick += StartupTimer_Tick;
            _startupTimer.Start();
        }

        private void StartupTimer_Tick(object sender, EventArgs e)
        {
            _startupTicks++;

            if (_startupTicks == 2)
                TxtLoadingStatus.Text = "Connecting to servers...";
            else if (_startupTicks == 4)
                TxtLoadingStatus.Text = "Loading configurations...";
            else if (_startupTicks >= 7)
            {
                _startupTimer.Stop();

                DoubleAnimation hideLoading = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(150));
                hideLoading.Completed += (s, ev) =>
                {
                    LoadingGrid.Visibility = Visibility.Collapsed;
                    MainGrid.Visibility = Visibility.Visible;
                    MainGrid.Opacity = 0;
                    MainGrid.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(250)));
                };
                LoadingGrid.BeginAnimation(UIElement.OpacityProperty, hideLoading);
            }
        }

        private void ProductCard_Click(object sender, MouseButtonEventArgs e)
        {
            if (_isProductSelected)
            {
                _isProductSelected = false;
                ProductCard.Background = (Brush)new BrushConverter().ConvertFrom("#08080A");
                ProductCard.BorderBrush = (Brush)new BrushConverter().ConvertFrom("#1E1E22");

                BtnAction.IsEnabled = false;
                BtnAction.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(1.0, 0.3, TimeSpan.FromMilliseconds(200)));
            }
            else
            {
                _isProductSelected = true;
                ProductCard.Background = (Brush)new BrushConverter().ConvertFrom("#161616");
                ProductCard.BorderBrush = (Brush)new BrushConverter().ConvertFrom("#3A3A3A");

                BtnAction.IsEnabled = true;
                BtnAction.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(0.3, 1.0, TimeSpan.FromMilliseconds(200)));
            }
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                DragMove();
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            CloseWithFadeOut();
        }

        private void CloseWithFadeOut()
        {
            ((Storyboard)FindResource("FadeOutAnimation")).Begin();
        }

        private void FadeOutAnimation_Completed(object sender, EventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void CheckCloudPath()
        {
            if (!Directory.Exists(_rootCloudPath))
            {
                _rootCloudPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "nl_cloud");
                if (!Directory.Exists(_rootCloudPath))
                    Directory.CreateDirectory(_rootCloudPath);
            }
            if (string.IsNullOrEmpty(_currentCloudPath))
            {
                _currentCloudPath = _rootCloudPath;
            }
        }

        private void BtnCloudFolder_Click(object sender, RoutedEventArgs e)
        {
            CheckCloudPath();
            _currentCloudPath = _rootCloudPath;
            LoadCloudFiles();

            CloudBrowserGrid.Visibility = Visibility.Visible;

            DoubleAnimation fadeOutMain = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(150));
            fadeOutMain.Completed += (s, args) => MainGrid.Visibility = Visibility.Collapsed;
            MainGrid.BeginAnimation(UIElement.OpacityProperty, fadeOutMain);

            DoubleAnimation fadeInCloud = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(250));
            CloudBrowserGrid.BeginAnimation(UIElement.OpacityProperty, fadeInCloud);

            CloudTransform.BeginAnimation(TranslateTransform.XProperty, new DoubleAnimation(30, 0, TimeSpan.FromMilliseconds(250)));
        }

        private void BtnBackFromCloud_Click(object sender, RoutedEventArgs e)
        {
            CheckCloudPath();

            if (!string.Equals(_currentCloudPath, _rootCloudPath, StringComparison.OrdinalIgnoreCase))
            {
                DirectoryInfo parent = Directory.GetParent(_currentCloudPath);
                if (parent != null && parent.FullName.Length >= _rootCloudPath.Length)
                {
                    NavigateBackFolder(parent.FullName);
                    return;
                }
            }

            MainGrid.Visibility = Visibility.Visible;
            MainGrid.Opacity = 0;
            MainGrid.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(250)));

            DoubleAnimation fadeOutCloud = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(150));
            fadeOutCloud.Completed += (s, args) => CloudBrowserGrid.Visibility = Visibility.Collapsed;
            CloudBrowserGrid.BeginAnimation(UIElement.OpacityProperty, fadeOutCloud);
        }

        private void NavigateToFolder(string newPath)
        {
            DoubleAnimation slideOut = new DoubleAnimation(0, -15, TimeSpan.FromMilliseconds(120));
            DoubleAnimation fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(120));

            fadeOut.Completed += (s, e) =>
            {
                _currentCloudPath = newPath;
                LoadCloudFiles();

                FilesTransform.X = 15;
                FilesListBox.Opacity = 0;

                DoubleAnimation slideIn = new DoubleAnimation(15, 0, TimeSpan.FromMilliseconds(180))
                {
                    EasingFunction = new ExponentialEase { EasingMode = EasingMode.EaseOut, Exponent = 7 }
                };
                DoubleAnimation fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(180));

                FilesTransform.BeginAnimation(TranslateTransform.XProperty, slideIn);
                FilesListBox.BeginAnimation(UIElement.OpacityProperty, fadeIn);
            };

            FilesTransform.BeginAnimation(TranslateTransform.XProperty, slideOut);
            FilesListBox.BeginAnimation(UIElement.OpacityProperty, fadeOut);
        }

        private void NavigateBackFolder(string newPath)
        {
            DoubleAnimation slideOut = new DoubleAnimation(0, 15, TimeSpan.FromMilliseconds(120));
            DoubleAnimation fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(120));

            fadeOut.Completed += (s, e) =>
            {
                _currentCloudPath = newPath;
                LoadCloudFiles();

                FilesTransform.X = -15;
                FilesListBox.Opacity = 0;

                DoubleAnimation slideIn = new DoubleAnimation(-15, 0, TimeSpan.FromMilliseconds(180))
                {
                    EasingFunction = new ExponentialEase { EasingMode = EasingMode.EaseOut, Exponent = 7 }
                };
                DoubleAnimation fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(180));

                FilesTransform.BeginAnimation(TranslateTransform.XProperty, slideIn);
                FilesListBox.BeginAnimation(UIElement.OpacityProperty, fadeIn);
            };

            FilesTransform.BeginAnimation(TranslateTransform.XProperty, slideOut);
            FilesListBox.BeginAnimation(UIElement.OpacityProperty, fadeOut);
        }

        private void LoadCloudFiles()
        {
            FilesListBox.Items.Clear();
            CheckCloudPath();

            try
            {
                TxtBrowserTitle.Text = Path.GetFileName(_currentCloudPath);
                if (string.IsNullOrEmpty(TxtBrowserTitle.Text) || string.Equals(_currentCloudPath, _rootCloudPath, StringComparison.OrdinalIgnoreCase))
                    TxtBrowserTitle.Text = "nl_cloud";

                string[] directories = Directory.GetDirectories(_currentCloudPath);
                string[] files = Directory.GetFiles(_currentCloudPath);

                if (directories.Length == 0 && files.Length == 0)
                {
                    FilesListBox.Items.Add("Folder is empty");
                }
                else
                {
                    foreach (string dir in directories)
                    {
                        FilesListBox.Items.Add($"📁  {Path.GetFileName(dir)}");
                    }

                    foreach (string file in files)
                    {
                        FilesListBox.Items.Add($"📄  {Path.GetFileName(file)}");
                    }
                }
            }
            catch (Exception ex)
            {
                FilesListBox.Items.Add($"Error: {ex.Message}");
            }
        }

        private void FilesListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (FilesListBox.SelectedItem == null) return;
            string selectedText = FilesListBox.SelectedItem.ToString();

            if (selectedText.StartsWith("📁"))
            {
                string folderName = selectedText.Substring(3).Trim();
                string newPath = Path.Combine(_currentCloudPath, folderName);
                if (Directory.Exists(newPath))
                {
                    NavigateToFolder(newPath);
                }
            }
        }

        private void FilesListBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete)
            {
                DeleteSelectedTarget();
            }
        }

        private void DeleteSelectedTarget()
        {
            if (FilesListBox.SelectedItem == null) return;
            string selectedText = FilesListBox.SelectedItem.ToString();

            if (selectedText.StartsWith("📁") || selectedText.StartsWith("📄"))
            {
                string name = selectedText.Substring(3).Trim();
                string targetPath = Path.Combine(_currentCloudPath, name);

                try
                {
                    if (Directory.Exists(targetPath))
                    {
                        Directory.Delete(targetPath, true);
                    }
                    else if (File.Exists(targetPath))
                    {
                        File.Delete(targetPath);
                    }
                    LoadCloudFiles();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to delete: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ShareSelectedFile()
        {
            if (FilesListBox.SelectedItem == null) return;
            string selectedText = FilesListBox.SelectedItem.ToString();

            if (selectedText.StartsWith("📄"))
            {
                string fileName = selectedText.Substring(3).Trim();
                string sourcePath = Path.Combine(_currentCloudPath, fileName);

                if (File.Exists(sourcePath))
                {
                    Microsoft.Win32.SaveFileDialog saveFileDialog = new Microsoft.Win32.SaveFileDialog();
                    saveFileDialog.FileName = fileName;
                    saveFileDialog.Filter = "All files (*.*)|*.*";

                    if (saveFileDialog.ShowDialog() == true)
                    {
                        try
                        {
                            File.Copy(sourcePath, saveFileDialog.FileName, true);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Failed to export file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
            }
        }

        private void ImportFile()
        {
            Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog();
            openFileDialog.Title = "Select a file to import";
            openFileDialog.Filter = "All files (*.*)|*.*";

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    CheckCloudPath();
                    string sourceFile = openFileDialog.FileName;
                    string destFile = Path.Combine(_currentCloudPath, Path.GetFileName(sourceFile));

                    File.Copy(sourceFile, destFile, true);
                    LoadCloudFiles();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to import file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void CtxImport_Click(object sender, RoutedEventArgs e)
        {
            ImportFile();
        }

        private void CtxShare_Click(object sender, RoutedEventArgs e)
        {
            ShareSelectedFile();
        }

        private void CtxDelete_Click(object sender, RoutedEventArgs e)
        {
            DeleteSelectedTarget();
        }

        // Метод извлечения зашитой DLL во временную папку
        private string ExtractEmbeddedDll()
        {
            string resourceName = "inwdwhg.neverlose.dll";
            string tempDllPath = Path.Combine(Path.GetTempPath(), "neverlose.dll");

            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                    throw new Exception($"Embedded resource '{resourceName}' not found!");

                using (FileStream fileStream = new FileStream(tempDllPath, FileMode.Create, FileAccess.Write))
                {
                    stream.CopyTo(fileStream);
                }
            }

            return tempDllPath;
        }

        private void BtnAction_Click(object sender, RoutedEventArgs e)
        {
            if (!_isProductSelected) return;

            DoubleAnimation hideCard = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(200));
            hideCard.Completed += (s, args) =>
            {
                ProductCard.Visibility = Visibility.Collapsed;
                BtnAction.Visibility = Visibility.Collapsed;
                BtnCloudFolder.Visibility = Visibility.Collapsed;

                WaitingGrid.Visibility = Visibility.Visible;
                WaitingGrid.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(250)));
                ((Storyboard)FindResource("WaitSpinAnimation")).Begin();
            };
            ProductCard.BeginAnimation(UIElement.OpacityProperty, hideCard);
            BtnAction.BeginAnimation(UIElement.OpacityProperty, hideCard);
            BtnCloudFolder.BeginAnimation(UIElement.OpacityProperty, hideCard);

            TxtWaitStatus.Text = "Waiting for csgo.exe...";

            DispatcherTimer injectTimer = new DispatcherTimer();
            injectTimer.Interval = TimeSpan.FromSeconds(1);
            injectTimer.Tick += (s, args) =>
            {
                Process[] processes = Process.GetProcessesByName("csgo");
                if (processes.Length > 0)
                {
                    injectTimer.Stop();
                    int pid = processes[0].Id;
                    TxtWaitStatus.Text = $"csgo.exe found (PID: {pid}). Injecting...";

                    try
                    {
                        string dllPath = ExtractEmbeddedDll();

                        var result = InjectorCore.InjectStandard(pid, dllPath);
                        if (!result.Success)
                        {
                            RestoreUIAfterError();
                            return;
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Injection error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        RestoreUIAfterError();
                        return;
                    }

                    TxtWaitStatus.Text = "Injection completed successfully!";

                    DispatcherTimer closeTimer = new DispatcherTimer();
                    closeTimer.Interval = TimeSpan.FromSeconds(1);
                    closeTimer.Tick += (s2, args2) =>
                    {
                        closeTimer.Stop();
                        CloseWithFadeOut();
                    };
                    closeTimer.Start();
                }
            };
            injectTimer.Start();
        }

        private void RestoreUIAfterError()
        {
            WaitingGrid.Visibility = Visibility.Collapsed;
            ProductCard.Visibility = Visibility.Visible;
            BtnAction.Visibility = Visibility.Visible;
            BtnCloudFolder.Visibility = Visibility.Visible;

            ProductCard.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(250)));
            BtnAction.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(250)));
            BtnCloudFolder.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(250)));
            BtnAction.IsEnabled = true;
        }
    }
}