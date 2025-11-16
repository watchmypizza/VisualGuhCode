using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Security.AccessControl;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using static System.Net.Mime.MediaTypeNames;

namespace VisualGuhCode
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            this.ContentRendered += Window_ContentRendered;
            CommandPalette.Opacity = 0;
            TabName.Text = "New Tab";

            var OpenDirectorySelectorOnLaunch = new OpenFolderDialog();
            OpenDirectorySelectorOnLaunch.Title = "Open Project location";
            OpenDirectorySelectorOnLaunch.DefaultDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            OpenDirectorySelectorOnLaunch.Multiselect = false;
            OpenDirectorySelectorOnLaunch.ShowDialog();

            try
            {
                _fileSystemResult.currentDir = OpenDirectorySelectorOnLaunch.FolderName;
                Directory.SetCurrentDirectory(OpenDirectorySelectorOnLaunch.FolderName);
            } catch (Exception e)
            {
                _fileSystemResult.currentDir = Directory.GetCurrentDirectory();
            }
        }

        public class FileSystemResult
        {
            public List<FileSystemItem> Items { get; set; }
            public List<string> RawPaths { get; set; }

            public string curPath;

            public int charCount;

            public string prevPath;

            public bool CmdPltEnabled;

            public string currentDir;

            public bool isLoaded {  get; set; } = false;

            public int tabCount;

            public bool isSaved = true;
        }

        private List<FileSystemItem> LoadTopLevel(string path)
        {
            var result = new List<FileSystemItem>();

            foreach (var dir in Directory.GetDirectories(path))
            {
                result.Add(new FileSystemItem
                {
                    Name = System.IO.Path.GetFileName(dir),
                    FullPath = dir,
                    isFolder = true,
                    SubItems = { new FileSystemItem { Name = "Loading.." } }
                });
            }

            foreach (var file in Directory.GetFiles(path))
            {
                result.Add(new FileSystemItem
                {
                    Name = System.IO.Path.GetFileName(file),
                    FullPath = file,
                    isFolder = false,
                    Extension = System.IO.Path.GetExtension(file)
                });
            }

            return result;
        }

        private void FolderTree_Expanded(object sender, RoutedEventArgs e)
        {
            var item = (TreeViewItem)e.OriginalSource;
            var fsItem = (FileSystemItem)item.DataContext;

            if (!fsItem.isFolder || fsItem.IsLoaded) return;

            fsItem.SubItems.Clear();

            foreach (var dir in Directory.GetDirectories(fsItem.FullPath))
            {
                fsItem.SubItems.Add(new FileSystemItem
                {
                    Name = System.IO.Path.GetFileName(dir),
                    FullPath = dir,
                    isFolder = true,
                    SubItems = { new FileSystemItem { Name = "Loading.." } }
                });
            }

            foreach (var file in Directory.GetFiles(fsItem.FullPath))
            {
                fsItem.SubItems.Add(new FileSystemItem
                {
                    Name = System.IO.Path.GetFileName(file),
                    FullPath = file,
                    isFolder = false,
                    Extension = System.IO.Path.GetExtension(file)
                });
            }

            fsItem.IsLoaded = true;
        }

        // Old function which slowed everything down before
        private FileSystemResult GetFileSystemItems(string path)
        {
            var items = new List<FileSystemItem>();
            var rawPaths = new List<string>();
            var curPath = new List<string>();

            var folders = Directory.GetDirectories(path);
            foreach (var folderPath in folders)
            {

                var sub = GetFileSystemItems(folderPath);

                items.Add(new FileSystemItem
                {
                    Name = System.IO.Path.GetFileName(folderPath),
                    isFolder = true,
                    Extension = "",
                    SubItems = GetFileSystemItems(folderPath).Items
                });

                rawPaths.AddRange(sub.RawPaths);
            }

            var files = Directory.GetFiles(path);
            foreach (var filePath in files)
            {
                items.Add(new FileSystemItem
                {
                    Name = System.IO.Path.GetFileName(filePath),
                    isFolder = false,
                    Extension = System.IO.Path.GetExtension(filePath),
                });

                rawPaths.Add(filePath);
            }

            return new FileSystemResult { Items = items, RawPaths = rawPaths};
        }

        private void Window_ContentRendered(object sender, EventArgs e)
        {
            try
            {
                var CurrentDirectory = _fileSystemResult.currentDir;

                if (string.IsNullOrEmpty(CurrentDirectory) || !Directory.Exists(CurrentDirectory))
                    throw new DirectoryNotFoundException();

                var rootItems = LoadTopLevel(CurrentDirectory);
                FolderTree.ItemsSource = rootItems;
            }
            catch (Exception ex)
            {
                // fallback to current working dir if the selected folder is invalid
                var CurrentDirectory = Directory.GetCurrentDirectory();
                var rootItems = LoadTopLevel(CurrentDirectory);
                FolderTree.ItemsSource = rootItems;

                MessageBox.Show(
                    $"Error loading selected directory. Loaded current directory instead.\n{ex}",
                    "Warning",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }

        private FileSystemResult _fileSystemResult = new FileSystemResult();

        private void SaveCurrentFile()
        {
            if (string.IsNullOrEmpty(_fileSystemResult.curPath))
                return;

            var content = new TextRange(CodeBox.Document.ContentStart, CodeBox.Document.ContentEnd);
            string text = content.Text;

            try
            {
                File.WriteAllText(_fileSystemResult.curPath, text);
            } catch (System.IO.IOException e)
            {
                var errBox = MessageBox.Show(
                    $"There was an error saving your file!\n{e}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }

        private void CodeBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _fileSystemResult.charCount++;

            if (_fileSystemResult.charCount >= 50)
            {
                _fileSystemResult.charCount = 0;
                SaveCurrentFile();
            }
        }

        private void FolderTree_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var fileItem = FolderTree.SelectedItem as FileSystemItem;
            if (fileItem == null || fileItem.isFolder)
                return;

            if (_fileSystemResult.currentDir != null)
            {
                Directory.SetCurrentDirectory(_fileSystemResult.currentDir);
            }
            var CurrentDirectory = Directory.GetCurrentDirectory();
            var rawPaths = LoadTopLevel(CurrentDirectory);

            // find full path
            string newPath = fileItem.FullPath;
            if (newPath == null)
                return;

            if (!_fileSystemResult.isSaved)
            {
                var msg = MessageBox.Show(
                    Title = "You didn't save your file yet, your changes will be discarded.\nContinue?",
                    "Info",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);
                if (msg != MessageBoxResult.Yes)
                {
                    return;
                }
                _fileSystemResult.isSaved = true;
            }

            // save previous file
            SaveCurrentFile();

            // update current file
            _fileSystemResult.curPath = newPath;
            _fileSystemResult.prevPath = newPath;

            // load file
            try
            {
                string text = File.ReadAllText(newPath);
                CodeBox.Document.Blocks.Clear();
                CodeBox.Document.Blocks.Add(new Paragraph(new Run(text)));
                CodeBox.Focus();

                AddNewTab(newPath);
            } catch (System.IO.IOException ex)
            {
                MessageBox.Show(
                    Title = $"The file you want to open may contain a virus (or something went wrong opening the file). View error below.\n{ex}",
                    "Error",
                    MessageBoxButton.OKCancel,
                    MessageBoxImage.Error);

                using (var reader = new StreamReader(newPath))
                {
                    string text = reader.ReadToEnd();

                    CodeBox.Document.Blocks.Clear();
                    CodeBox.Document.Blocks.Add(new Paragraph(new Run(text)));
                    CodeBox.Focus();

                    AddNewTab(newPath);
                };
            }
        }

        public void FadeIn(UIElement element, double durationSeconds = 0.3)
        {
            var fade = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromSeconds(durationSeconds),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };

            element.BeginAnimation(UIElement.OpacityProperty, fade);
        }
        public void FadeOut(UIElement element, double durationSeconds = 0.3)
        {
            var fade = new DoubleAnimation
            {
                From = 1,
                To = 0,
                Duration = TimeSpan.FromSeconds(durationSeconds),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
            };

            element.BeginAnimation(UIElement.OpacityProperty, fade);
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.S)
            {
                if (!_fileSystemResult.isSaved)
                {
                    var fileSavingDialog = new SaveFileDialog();
                    fileSavingDialog.Title = "Save file";
                    fileSavingDialog.InitialDirectory = Directory.GetCurrentDirectory();
                    fileSavingDialog.ShowDialog();
                    using (var writer = new StreamWriter(fileSavingDialog.FileName))
                    {
                        var CBT = new TextRange(CodeBox.Document.ContentStart, CodeBox.Document.ContentEnd);
                        writer.Write(CBT.Text);
                        _fileSystemResult.isSaved = true;
                        return;
                    }
                }
                SaveCurrentFile();
            }

            // Command Palette
            if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.P)
            {
                if (_fileSystemResult.CmdPltEnabled == true)
                {
                    return;
                }
                _fileSystemResult.CmdPltEnabled = true;
                FadeIn(CommandPalette, 0.5);
                System.Diagnostics.Debug.WriteLine("Showing command palette");
                CmdPlt.Focus();
            }

            if ((Keyboard.Modifiers & ModifierKeys.Shift) != 0 && (Keyboard.Modifiers & ModifierKeys.Control) != 0 && e.Key == Key.S)
            {
                var fileSavingDialog = new SaveFileDialog();
                fileSavingDialog.InitialDirectory = Directory.GetCurrentDirectory();

                bool? result = fileSavingDialog.ShowDialog();
                if (result != true) return;

                var content = new TextRange(CodeBox.Document.ContentStart, CodeBox.Document.ContentEnd);
                var text = content.Text;

                var filePath = fileSavingDialog.FileName;

                try
                {
                    File.WriteAllText(filePath, text);

                    MessageBox.Show(
                        $"File successfully saved to:\n{filePath}",
                        "Info",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information
                    );
                }
                catch (IOException err)
                {
                    MessageBox.Show(
                        $"There was an error saving your file:\n{err.Message}",
                        "Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error
                    );
                }
            }

            if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.O)
            {
                var folderOpenDialog = new OpenFolderDialog();
                folderOpenDialog.Title = "Open Folder";
                folderOpenDialog.Multiselect = false;
                folderOpenDialog.RootDirectory = System.Environment.SpecialFolder.UserProfile.ToString();
                folderOpenDialog.ShowDialog();
                Directory.SetCurrentDirectory(folderOpenDialog.FolderName);

                FolderTree.ItemsSource = null;
                var rootItems = LoadTopLevel(folderOpenDialog.FolderName);
                FolderTree.ItemsSource = rootItems;
            }

            if (e.Key == Key.Escape)
            {
                if (_fileSystemResult.CmdPltEnabled)
                {
                    _fileSystemResult.CmdPltEnabled = false;
                    FadeOut(CommandPalette, 0.5);
                    CodeBox.Focus();
                }
            }

            if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.N)
            {
                if (!_fileSystemResult.isSaved)
                {
                    var msg = MessageBox.Show(
                        Title = "You didn't save your file yet, your changes will be discarded.\nContinue?",
                        "Info",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);
                    if (msg != MessageBoxResult.Yes)
                    {
                        return;
                    }
                }
                CodeBox.Document.Blocks.Clear();
                _fileSystemResult.curPath = null;
                _fileSystemResult.isSaved = false;
            }

            _fileSystemResult.CmdPltEnabled = false;
        }

        private void CmdPlt_KeyDown(object sender, KeyEventArgs e)
        {
            string text = CmdPlt.Text.ToLower();
            var charToRem = new string[] { "\"", "'" };
            foreach (var c in charToRem)
            {
                text = text.Replace(c, string.Empty);
            }

            if (text.Contains("./"))
            {
                if (text.Contains('~'))
                {
                    return;
                }
                text = text.Replace("./", $"{Directory.GetCurrentDirectory()}\\");
            }

            if (text.Contains('~'))
            {
                System.Diagnostics.Debug.WriteLine("Text contains ~");
                text = text.Replace("~", System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ""));
                System.Diagnostics.Debug.WriteLine(text);
            }

            if (e.Key == Key.Enter)
            {
                
                if (Directory.Exists(text))
                {
                    if (!_fileSystemResult.isSaved)
                    {
                        var msg = MessageBox.Show(
                            Title = "You didn't save your file yet, your changes will be discarded.\nContinue?",
                            "Info",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Question);
                        if (msg != MessageBoxResult.Yes)
                        {
                            return;
                        }

                        _fileSystemResult.isSaved = true;
                    }

                    FolderTree.ItemsSource = null;
                    Directory.SetCurrentDirectory(text);
                    var newDir = System.IO.Path.GetFullPath(text);
                    var rootItems = LoadTopLevel(newDir);

                    FolderTree.ItemsSource = rootItems;
                    FadeOut(CommandPalette, 0.25);
                    CodeBox.Focus();

                    _fileSystemResult.currentDir = newDir;
                }

                if (File.Exists(text))
                {
                    if (!_fileSystemResult.isSaved)
                    {
                        var msg = MessageBox.Show(
                            Title = "You didn't save your file yet, your changes will be discarded.\nContinue?",
                            "Info",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Question);
                        if (msg != MessageBoxResult.Yes)
                        {
                            return;
                        }
                    }

                    _fileSystemResult.isSaved = true;
                    var CBT = File.ReadAllText(text);

                    CodeBox.Document.Blocks.Clear();
                    CodeBox.Document.Blocks.Add(new Paragraph(new Run(CBT)));
                    FadeOut(CommandPalette, 0.25);
                    CodeBox.Focus();
                }
            }
        }

        private void CmdPlt_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            
        }

        private void Close_MouseLeftButtonDown(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Minimize_MouseLeftButtonDown(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void Maximize_MouseLeftButtonDown(object sender, RoutedEventArgs e)
        {
            if (WindowState == WindowState.Maximized)
            {
                WindowState = WindowState.Normal;
            } else
            {
                WindowState = WindowState.Maximized;
            }
        }

        private void NavBar_MouseLeftButtonDown(object sender, RoutedEventArgs e)
        {
            DragMove();
        }

        private void MenuBar_Quit_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void MenuBar_File_Save(object sender, RoutedEventArgs e)
        {

        }

        private void MenuBar_File_SaveAs(object sender, RoutedEventArgs e)
        {

        }

        private void MenuBar_File_NewFile(object sender, RoutedEventArgs e)
        {

        }
        private void AddNewTab(string filePath)
        {
            int existingTabs = FileTabs.Children.OfType<Rectangle>()
                                  .Count(r => r != NewTab && r != Tab);

            if (_fileSystemResult.tabCount >= 6)
            {
                NewTab.Opacity = 0;
                NewTabText.Opacity = 0;
                return;
            }

            double newLeft = existingTabs * 105;

            var newTabRect = new Rectangle
            {
                Width = 100,
                Height = 25,
                Fill = (Brush)new BrushConverter().ConvertFromString("#27272B"),
                RadiusX = 5,
                RadiusY = 5,
                HorizontalAlignment = HorizontalAlignment.Left,
                Margin = new Thickness(newLeft, 1, 0, 0),
                Tag = filePath
            };
            newTabRect.MouseLeftButtonDown += TabClick;

            var newTabText = new TextBlock
            {
                Text = $"Tab {existingTabs + 1}",
                Width = 100,
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Left,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(newLeft, 1, 0, 0),
                Tag = filePath
            };
            newTabText.MouseLeftButtonDown += TabClick;

            double plusLeft = (existingTabs + 1) * 105;

            NewTab.Margin = new Thickness(plusLeft, NewTab.Margin.Top, 0, 0);
            NewTabText.Margin = new Thickness(plusLeft, NewTabText.Margin.Top, 0, 0);

            FileTabs.Children.Add(newTabRect);
            FileTabs.Children.Add(newTabText);

            _fileSystemResult.tabCount++;
        }

        private void TabClick(object sender, MouseButtonEventArgs e)
        {
            string filePath = null;

            if (!_fileSystemResult.isSaved)
            {
                var msg = MessageBox.Show(
                    Title = "You didn't save your file yet, your changes will be discarded.\nContinue?",
                    "Info",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);
                if (msg != MessageBoxResult.Yes)
                {
                    return;
                }
                _fileSystemResult.isSaved = true;
            }

            if (sender is Rectangle rect && rect.Tag is string rectPath)
            {
                filePath = rectPath;
            }
            else if (sender is TextBlock txt && txt.Tag is string txtPath)
            {
                filePath = txtPath;
            }
            else
            {
                Debug.WriteLine("Clicked unknown sender type");
                return;
            }

            if (filePath == null || !File.Exists(filePath))
            {
                Debug.WriteLine($"Invalid path: {filePath}");
                return;
            }

            string fileContents = File.ReadAllText(filePath);
            CodeBox.Document.Blocks.Clear();
            CodeBox.Document.Blocks.Add(new Paragraph(new Run(fileContents)));
            _fileSystemResult.curPath = filePath;
        }

        private void newTab(object sender, MouseButtonEventArgs e)
        {

            _fileSystemResult.tabCount++;

            if (_fileSystemResult.tabCount >= 6)
            {
                NewTab.Opacity = 0;
                NewTabText.Opacity = 0;
                return;
            }

            if (!_fileSystemResult.isSaved)
            {
                var msg = MessageBox.Show(
                    Title = "You didn't save your file yet, your changes will be discarded.\nContinue?",
                    "Info",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);
                if (msg != MessageBoxResult.Yes)
                {
                    return;
                }
                _fileSystemResult.isSaved = true;
            }

            var fileOpenDialog = new OpenFileDialog();
            fileOpenDialog.Title = "Select a file to open";
            fileOpenDialog.Multiselect = false;
            fileOpenDialog.InitialDirectory = Directory.GetCurrentDirectory();
            fileOpenDialog.ShowDialog();

            if (fileOpenDialog.FileName == null || fileOpenDialog.FileName == "")
            {
                return;
            }

            AddNewTab(fileOpenDialog.FileName);

            using (var reader = new StreamReader(fileOpenDialog.FileName))
            {
                var text = reader.ReadToEnd();
                CodeBox.Document.Blocks.Clear();
                CodeBox.Document.Blocks.Add(new Paragraph(new Run(text)));
            }
        }
    }
}