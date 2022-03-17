using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using CefSharp;
using Newtonsoft.Json;
using sxlib;
using sxlib.Specialized;
using SynapseX.CrackerSussyAssets;
using SynapseX.Properties;
using Application = System.Windows.Application;
using Brushes = System.Windows.Media.Brushes;
using ContextMenu = System.Windows.Controls.ContextMenu;
using MenuItem = System.Windows.Controls.MenuItem;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using Orientation = System.Windows.Controls.Orientation;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;
using FontDialog = System.Windows.Forms.FontDialog;
using FontFamily = System.Windows.Media.FontFamily;

// ReSharper disable ArrangeMethodOrOperatorBody

namespace SynapseX
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private SxLibWPF lib;
        private Storyboard RainbowStoryboard;

        public MainWindow()
        {
            Settings.Default.PropertyChanged += (_, _) => Settings.Default.Save();
            AppDomain.CurrentDomain.UnhandledException += (sender, args) => File.AppendAllText("bs_bin/error.log", args.ExceptionObject + Environment.NewLine + Environment.NewLine);
            InitializeComponent();
            lib = SxLib.InitializeWPF(this, AppDomain.CurrentDomain.BaseDirectory);

            Closed += (sender, args) => Process.GetCurrentProcess().Kill();
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            
            WebClient wec = new WebClient();
            MaxHeight = SystemParameters.MaximizedPrimaryScreenHeight - 2;
            string dir = AppDomain.CurrentDomain.BaseDirectory;
            border.Visibility = Visibility.Visible;
            RainbowStoryboard = (Storyboard)TryFindResource("RainbowBorderStoryboard");
            if (Settings.Default.RainbowBorder) RainbowStoryboard.Begin();
            else RainbowStoryboard.Stop();
            if(!File.Exists(dir + @"\bin\SLInjector.dll"))
            {
                try
                {
                    await wec.DownloadFileTaskAsync(new Uri("https://raw.githubusercontent.com/L1ghtingBolt/BestSynapse/master/SLInjector.dll"), AppDomain.CurrentDomain.BaseDirectory + @"\bin\SLInjector.dll");
                    Process.Start(Assembly.GetEntryAssembly().Location);
                    Application.Current.Shutdown();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                    Application.Current.Shutdown();
                }
            }
            lib.LoadEvent += SynapseLoadEvent;

            lib.AttachEvent += SynapseAttachEvent;
            lib.Load();
            if (!File.Exists(dir + @"\bs_bin\editor_files\rosploco.html"))
            {
                await Task.Run(async () =>
                {

                    try
                    {
                        await wec.DownloadFileTaskAsync(new Uri("https://raw.githubusercontent.com/L1ghtingBolt/FraktureSS/master/editor_files.zip"), AppDomain.CurrentDomain.BaseDirectory + @"\bs_bin\editor_files.zip");
                        ZipFile.ExtractToDirectory(AppDomain.CurrentDomain.BaseDirectory + @"\bs_bin\editor_files.zip", AppDomain.CurrentDomain.BaseDirectory + @"\bs_bin");
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "Monaco download failed!");
                    }
                });
            }

            Directory.CreateDirectory("scripts");
            Directory.CreateDirectory("bs_bin/tabs");
            Editor.InitializeDirectory("bs_bin/tabs");
            // UpdateEditorBar();

            // script tree
            await PopulateScriptTree(ScriptList, "scripts");
            var watcher = new FileSystemWatcher("scripts")
            {
                NotifyFilter = NotifyFilters.Attributes
                               | NotifyFilters.CreationTime
                               | NotifyFilters.DirectoryName
                               | NotifyFilters.FileName
                               | NotifyFilters.LastAccess
                               | NotifyFilters.LastWrite
                               | NotifyFilters.Security
                               | NotifyFilters.Size
            };


            async void OnFileEvent(object obj, FileSystemEventArgs eventArgs)
            {
                await Dispatcher.InvokeAsync(async delegate
                {
                    await PopulateScriptTree(ScriptList, "scripts");
                });
            }

            watcher.Changed += OnFileEvent;
            watcher.Created += OnFileEvent;
            watcher.Deleted += OnFileEvent;
            watcher.Renamed += OnFileEvent;

            watcher.Filter = "*.*";
            watcher.IncludeSubdirectories = true;
            watcher.EnableRaisingEvents = true;

            // script hub
            ScriptHubPanel.Children.Clear();

            var scripts = await Rbxscripts.FetchScriptsAsync();
            ScriptHubTitle.Text = $"Script Hub ({scripts.Length})";
            foreach (var scriptObject in scripts)
            {
                var obj = new ScriptItem
                {
                    Width = 150,
                    Height = 150,
                    Margin = new Thickness(5),
                    Script = scriptObject
                };
                obj.Executed += (_, _) => lib.Execute(obj.Script.Script);

                ScriptHubPanel.Children.Add(obj);

                ScriptHubSearchBar.TextChanged += delegate
                {
                    if (string.IsNullOrWhiteSpace(ScriptHubSearchBar.Text))
                    {
                        obj.Visibility = Visibility.Visible;
                        return;
                    }

                    var search = ScriptHubSearchBar.Text.ToLower();
                    var name = obj.Script.Title.ToLower();
                    var tags = obj.Script.Tags.ToLower().Split('-');

                    obj.Visibility = name.Contains(search) || Array.Exists(tags, str => str.Contains(search))
                        ? Visibility.Visible
                        : Visibility.Collapsed;
                };
            }

            // load lib
            
        }
        
        private async void SynapseLoadEvent(SxLibBase.SynLoadEvents e, object _)
        {
            
            switch (e)
            {
                case SxLibBase.SynLoadEvents.CHECKING_WL:
                    LoadingBar.Value = 25;
                    LoadingText.Text = "Checking Whitelist...";
                    break;

                case SxLibBase.SynLoadEvents.DOWNLOADING_DATA:
                    LoadingBar.Value = 50;
                    LoadingText.Text = "Downloading Data...";
                    
                    break;

                case SxLibBase.SynLoadEvents.CHECKING_DATA:
                    LoadingBar.Value = 75;
                    LoadingText.Text = "Checking Data...";
                    break;

                case SxLibBase.SynLoadEvents.READY:
                    LoadingBar.Value = 100;
                    LoadingText.Text = "Ready! Welcome!";
                    lib.SetWindow(this);

                    TopMost.IsChecked = Topmost = lib.GetOptions().TopMost;
                    AutoAttach.IsChecked = lib.GetOptions().AutoAttach;
                    AutoLaunch.IsChecked = lib.GetOptions().AutoLaunch;
                    InternalUI.IsChecked = lib.GetOptions().InternalUI;
                    UnlockFPS.IsChecked = lib.GetOptions().UnlockFPS;
                    RainbowBorder.IsChecked = Settings.Default.RainbowBorder;
                    await Task.Delay(2000);

                    // Hide loading screen
                    ((Storyboard) TryFindResource("LoadCompletedStoryboard")).Begin();
                    break;
                case SxLibBase.SynLoadEvents.NOT_LOGGED_IN:
                    LoadingBar.Foreground = Brushes.Red;
                    LoadingText.Text = "Not logged in!";
                    break;
                case SxLibBase.SynLoadEvents.FAILED_TO_DOWNLOAD:
                    LoadingBar.Foreground = Brushes.Red;
                    LoadingText.Text = "Failed to Download!";
                    break;
                case SxLibBase.SynLoadEvents.DOWNLOADING_DLLS:
                    LoadingBar.Foreground = Brushes.Green;
                    LoadingText.Text = "Downloading DLLs";
                    break;
                case SxLibBase.SynLoadEvents.FAILED_TO_VERIFY:
                    LoadingBar.Foreground = Brushes.Red;
                    LoadingText.Text = "Failed to verify!";
                    break;
                case SxLibBase.SynLoadEvents.NOT_ENOUGH_TIME:
                    LoadingBar.Foreground = Brushes.Red;
                    LoadingText.Text = "Not enough time!!";
                    break;
                case SxLibBase.SynLoadEvents.NOT_UPDATED:
                    LoadingBar.Foreground = Brushes.Red;
                    LoadingText.Text = "Not Updated!";
                    break;
                case SxLibBase.SynLoadEvents.UNAUTHORIZED_HWID:
                    LoadingBar.Foreground = Brushes.Red;
                    LoadingText.Text = "Unauthorized HWID!";
                    break;
                case SxLibBase.SynLoadEvents.ALREADY_EXISTING_WL:
                    LoadingBar.Foreground = Brushes.Red;
                    LoadingText.Text = "Already exising WL!";
                    break;
                case SxLibBase.SynLoadEvents.UNKNOWN:
                    LoadingBar.Foreground = Brushes.Orange;
                    LoadingBar.Value = 50;
                    LoadingText.Text = "Error! SLInjector may be not found. Downloading!";
                    WebClient wc = new WebClient();
                    try
                    {
                        await wc.DownloadFileTaskAsync(new Uri("https://raw.githubusercontent.com/L1ghtingBolt/BestSynapse/master/SLInjector.dll"), AppDomain.CurrentDomain.BaseDirectory + @"\bin\SLInjector.dll");
                        Process.Start(Assembly.GetEntryAssembly().Location);
                        Application.Current.Shutdown();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                        Application.Current.Shutdown();
                    }
                    break;
            }
        }

        private void SynapseAttachEvent(SxLibBase.SynAttachEvents evnt, object _)
        {
            File.AppendAllText("logs.txt", evnt + Environment.NewLine);

            switch (evnt)
            {
                case SxLibBase.SynAttachEvents.ALREADY_INJECTED:
                    InjectState.Content = "Attached. (Already attached!)";
                    StateColor.Fill = Brushes.Lime;
                    break;

                case SxLibBase.SynAttachEvents.CHECKING:
                    InjectState.Content = "Attaching... (Checking)";
                    StateColor.Fill = Brushes.Orange;
                    break;

                case SxLibBase.SynAttachEvents.READY:
                    InjectState.Content = "Attached.";
                    StateColor.Fill = Brushes.Lime;
                    break;

                case SxLibBase.SynAttachEvents.CHECKING_WHITELIST:
                    InjectState.Content = "Attaching... (Checking whitelist)";
                    StateColor.Fill = Brushes.Orange;
                    break;

                case SxLibBase.SynAttachEvents.SCANNING:
                    InjectState.Content = "Attaching... (Scanning)";
                    StateColor.Fill = Brushes.Orange;
                    break;

                case SxLibBase.SynAttachEvents.PROC_CREATION:
                    InjectState.Content = "Roblox detected.";
                    StateColor.Fill = Brushes.Orange;
                    break;

                case SxLibBase.SynAttachEvents.FAILED_TO_ATTACH:
                    InjectState.Content = "Not attached (Failed to attach...)";
                    StateColor.Fill = Brushes.Red;
                    break;

                case SxLibBase.SynAttachEvents.FAILED_TO_FIND:
                    InjectState.Content = "Not Attached (Roblox not found...)";
                    StateColor.Fill = Brushes.Red;
                    break;

                case SxLibBase.SynAttachEvents.FAILED_TO_UPDATE:
                    InjectState.Content = "Not Attached (Failed to update...)";
                    StateColor.Fill = Brushes.Red;
                    break;

                case SxLibBase.SynAttachEvents.INJECTING:
                    InjectState.Content = "Attaching...";
                    StateColor.Fill = Brushes.Orange;
                    break;

                case SxLibBase.SynAttachEvents.NOT_INJECTED:
                    InjectState.Content = "Not attached.";
                    StateColor.Fill = Brushes.Red;
                    break;

                case SxLibBase.SynAttachEvents.NOT_RUNNING_LATEST_VER_UPDATING:
                    InjectState.Content = "Updating...";
                    StateColor.Fill = Brushes.Orange;
                    break;

                case SxLibBase.SynAttachEvents.REINJECTING:
                    InjectState.Content = "Re-attaching...";
                    StateColor.Fill = Brushes.Orange;
                    break;

                case SxLibBase.SynAttachEvents.UPDATING_DLLS:
                    InjectState.Content = "Updating Dlls...";
                    StateColor.Fill = Brushes.Orange;
                    break;

                case SxLibBase.SynAttachEvents.NOT_UPDATED:
                    InjectState.Content = "Not updated.";
                    StateColor.Fill = Brushes.Red;
                    break;

                case SxLibBase.SynAttachEvents.PROC_DELETION:
                    InjectState.Content = "Not attached.";
                    StateColor.Fill = Brushes.Red;
                    break;

                default:
                    InjectState.Content = "Unknown";
                    StateColor.Fill = Brushes.White;
                    break;
            }
        }

        private void DraggableBorder_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                DragMove();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();
        private void StateButton_Click(object sender, RoutedEventArgs e) => WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        private void MinimizeButton_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

        private void Window_StateChanged(object sender, EventArgs e)
        {
            StateButton.Content = WindowState == WindowState.Maximized ? '\xe923' : '\xe922';
            Main.Margin = WindowState == WindowState.Maximized ? new Thickness(7) : new Thickness(0);
        }

        //public void UpdateEditorBar()
        //{
        //    UndoButton.IsEnabled = Editor.SelectedEditor.CanUndo;
        //    RedoButton.IsEnabled = Editor.SelectedEditor.CanRedo;
        //}

        private async Task PopulateScriptTree(ItemsControl treeView, string directory)
        {
            // this code is pretty bad but it'll work for now.
            treeView.Items.Clear();

            foreach (var path in Directory.GetDirectories(directory))
            {
                var info = new DirectoryInfo(path);

                void OnDeleteDirectoryClick(object sender, RoutedEventArgs e)
                {
                    if (Messages.ShowGenericWarningMessage())
                        Directory.Delete(path, true);
                }

                var panel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(2) };
                var icon = new TextBlock
                {
                    FontFamily = TryFindResource("Segoe Fluent Icons") as FontFamily,
                    FontSize = 14,
                    Text = "\xe8b7",
                    Margin = new Thickness(0, 0, 3, 0)
                };

                var content = new TextBlock
                {
                    FontSize = 12,
                    Text = info.Name,
                    VerticalAlignment = VerticalAlignment.Center
                };

                panel.Children.Add(icon);
                panel.Children.Add(content);

                var item = new TreeViewItem { Header = panel };

                var menu = new ContextMenu();

                var deleteDirectory = new MenuItem { Header = "Delete Directory" };
                deleteDirectory.Click += OnDeleteDirectoryClick;

                menu.Items.Add(deleteDirectory);
                item.ContextMenu = menu;
                treeView.Items.Add(item);
                await PopulateScriptTree(item, path);
            }

            foreach (var file in Directory.GetFiles(directory))
            {
                var info = new FileInfo(file);

                void OnExecuteClick(object sender, RoutedEventArgs e)
                {
                    var script = File.ReadAllText(file);
                    lib.Execute(script);
                }

                void OnLoadScriptClick(object sender, RoutedEventArgs e)
                {
                    if (Messages.ShowGenericQuestionMessage("Open in a new tab?"))
                        Editor.CreateTab(info.Name, File.ReadAllText(file));
                    else
                        Editor.SelectedEditor.ExecuteScriptAsync($"setText({JsonConvert.SerializeObject(File.ReadAllText(file))})");
                }

                void OnSaveScriptClick(object sender, RoutedEventArgs e) => File.WriteAllText(file, Editor.SelectedEditor.EvaluateScript("getText()"));

                void OnDeleteScriptClick(object sender, RoutedEventArgs e)
                {
                    if (Messages.ShowGenericWarningMessage())
                        File.Delete(file);
                }

                var panel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(2) };
                var icon = new TextBlock
                {
                    FontFamily = TryFindResource("Codicon") as FontFamily,
                    FontSize = 14,
                    Text = "\xea7b",
                    Margin = new Thickness(0, 0, 3, 0)
                };

                var content = new TextBlock
                {
                    FontSize = 12,
                    Text = info.Name,
                    VerticalAlignment = VerticalAlignment.Center
                };

                panel.Children.Add(icon);
                panel.Children.Add(content);

                var item = new TreeViewItem { Header = panel };

                var menu = new ContextMenu();

                var executeScript = new MenuItem { Header = "Execute" };
                executeScript.Click += OnExecuteClick;

                var loadScript = new MenuItem { Header = "Load on Editor" };
                loadScript.Click += OnLoadScriptClick;

                var saveScript = new MenuItem { Header = "Save File" };
                saveScript.Click += OnSaveScriptClick;

                var deleteScript = new MenuItem { Header = "Delete File" };
                deleteScript.Click += OnDeleteScriptClick;

                menu.Items.Add(executeScript);
                menu.Items.Add(loadScript);
                menu.Items.Add(saveScript);
                menu.Items.Add(deleteScript);
                item.ContextMenu = menu;
                treeView.Items.Add(item);
            }
        }

        public static readonly string FileFilter = FilterInstance.ToString(new[]
        {
            new FilterInstance
            {
                Title = "LUA Script",
                Filter = "*.lua",
                IncludeFilter = true
            },
            new FilterInstance
            {
                Title = "Text Document",
                Filter = "*.txt",
                IncludeFilter = true
            }
        });

        private void Panels_Click(object sender, RoutedEventArgs e) => ((Storyboard)TryFindResource("PanelClosedStoryboard")).Begin();
        private void SettingsButton_Click(object sender, RoutedEventArgs e) => ((Storyboard)TryFindResource("SettingsOpenStoryboard")).Begin();
        private void ScriptHubButton_Click(object sender, RoutedEventArgs e) => ((Storyboard)TryFindResource("ScriptHubOpenStoryboard")).Begin();
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            var saveDialog = new SaveFileDialog
            {
                Title = "Save File",
                Filter = FileFilter,
                FilterIndex = 1,
                RestoreDirectory = true
            };

            if (saveDialog.ShowDialog() == true) File.WriteAllText(saveDialog.FileName, Editor.SelectedEditor.EvaluateScript("getText()"));
        }

        private void OpenButton_Click(object sender, RoutedEventArgs e)
        {
            var openDialog = new OpenFileDialog
            {
                Title = "Open File",
                Filter = FileFilter,
                FilterIndex = 1,
                RestoreDirectory = true
            };

            if (openDialog.ShowDialog() != true) return;
            if (Messages.ShowGenericQuestionMessage("Open in a new tab?"))
                Editor.CreateTab(Path.GetFileName(openDialog.FileName), File.ReadAllText(openDialog.FileName));
            else Editor.SelectedEditor.ExecuteScriptAsync("setText", File.ReadAllText(openDialog.FileName));
        }

        private void TopMost_Checked(object sender, RoutedEventArgs e)
        {
            var options = lib.GetOptions();
            options.TopMost = Topmost = TopMost.IsChecked.GetValueOrDefault();
            lib.SetOptions(options);
        }

        private void AutoAttach_Checked(object sender, RoutedEventArgs e)
        {
            var options = lib.GetOptions();
            options.AutoAttach = AutoAttach.IsChecked.GetValueOrDefault();
            lib.SetOptions(options);
        }

        private void AutoLaunch_Checked(object sender, RoutedEventArgs e)
        {
            var options = lib.GetOptions();
            options.AutoLaunch = AutoLaunch.IsChecked.GetValueOrDefault();
            lib.SetOptions(options);
        }

        private void InternalUI_Checked(object sender, RoutedEventArgs e)
        {
            var options = lib.GetOptions();
            options.InternalUI = InternalUI.IsChecked.GetValueOrDefault();
            lib.SetOptions(options);
        }

        private void UnlockFPS_Checked(object sender, RoutedEventArgs e)
        {
            var options = lib.GetOptions();
            options.UnlockFPS = UnlockFPS.IsChecked.GetValueOrDefault();
            lib.SetOptions(options);
        }

        private void AttachButton_Click(object sender, RoutedEventArgs e)
        {
            lib.SetWindow(this);
            if(!lib.Ready())
                lib.Attach();
        }
        private void ExecuteButton_Click(object sender, RoutedEventArgs e) => lib.Execute(Editor.SelectedEditor.EvaluateScript("getText()"));

        //private void FontButton_Click(object sender, RoutedEventArgs e)
        //{
        //    var fontDialog = new FontDialog
        //    {
        //        ShowColor = false,
        //        ShowEffects = false,
        //        ShowApply = false,
        //        Font = new Font(
        //            new System.Drawing.FontFamily(Settings.Default.FontName),
        //            (float)Settings.Default.FontSize)
        //    };

        //    if (fontDialog.ShowDialog() != System.Windows.Forms.DialogResult.OK) return;

        //    Settings.Default.FontName = fontDialog.Font.Name;
        //    Settings.Default.FontSize = fontDialog.Font.Size;
        //}

        private void KillRobloxButton_Click(object sender, RoutedEventArgs e)
        {
            foreach (var process in Process.GetProcessesByName("RobloxPlayerBeta"))
                process.Kill();
        }

        private async void ReinstallRobloxButton_Click(object sender, RoutedEventArgs e)
        {
            WebClient wec = new WebClient();
            if (!File.Exists(AppDomain.CurrentDomain.BaseDirectory + @"\bin\RobloxPlayerLauncher.exe"))
            {
                try
                {
                    await wec.DownloadFileTaskAsync(new Uri("https://raw.githubusercontent.com/L1ghtingBolt/BestSynapse/master/RobloxPlayerLauncher.exe"), AppDomain.CurrentDomain.BaseDirectory + @"\bin\RobloxPlayerLauncher.exe");
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Failed to download Roblox installer.");
                }
            }
            Process.Start(AppDomain.CurrentDomain.BaseDirectory + @"\bin\RobloxPlayerLauncher.exe");

    }

        private void RainbowBorder_Checked(object sender, RoutedEventArgs e)
        {
            Settings.Default.RainbowBorder = RainbowBorder.IsChecked.GetValueOrDefault();
            if (Settings.Default.RainbowBorder) RainbowStoryboard.Begin();
            else RainbowStoryboard.Stop();
        }

        private async void ObfuscateButton_Click(object sender, RoutedEventArgs e)
        {
            var currenttext = await Editor.SelectedEditor.EvaluateScriptAsync("getText()");
            var obfus = await new Obfuscator().ObfuscateVariables(currenttext.Result.ToString());
            // mEditor.SelectedEditor.ShowDevTools();
            //var scr = "setText(\"" + obfus + ""
            Editor.SelectedEditor.EvaluateScriptAsync($"setText(\"{obfus.Replace("\"", "\\\"")}\");");
        }
    }
}
