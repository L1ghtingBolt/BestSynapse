using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using System.Xml;
using CefSharp;
using CefSharp.Wpf;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using ICSharpCode.AvalonEdit.Rendering;
using Newtonsoft.Json;
using SynapseX.Properties;

namespace SynapseX.CrackerSussyAssets
{
    /// <summary>
    /// Interaction logic for ScriptEditor.xaml
    /// </summary>
    public static class Extensions
    {
        public static string EvaluateScript(this IWebBrowser browser, string script)
        {
            var value = browser.EvaluateScriptAsync(script);
            value.Wait();
            return value.Result.Success ? value.Result.Result.ToString() : "";
        }
    }
    
    public partial class ScriptEditor : UserControl
    {
        private string TabDir;
        public ChromiumWebBrowser SelectedEditor;

        public ScriptEditor()
        {
            InitializeComponent();
        }

        private static TextEditor CreateNewEditor(string content)
        {
            var editor = new TextEditor
            {
                Background = Brushes.Transparent,
                Margin = new Thickness(1),
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                VerticalScrollBarVisibility = ScrollBarVisibility.Visible,
                ShowLineNumbers = true,
                FontFamily = new FontFamily(Settings.Default.FontName),
                FontSize = Settings.Default.FontSize,
                Text = content
            };
            
            editor.TextArea.TextView.SetResourceReference(TextView.LinkTextForegroundBrushProperty, "PrimaryTextColor");
            editor.SetResourceReference(TextEditor.ForegroundProperty, "PrimaryTextColor");
            editor.SetResourceReference(TextEditor.LineNumbersForegroundProperty, "PrimaryTextColor");

            XmlReader xml = new XmlTextReader(File.OpenRead("bs_bin/syntax.xhsd"));
            editor.SyntaxHighlighting = HighlightingLoader.Load(xml, HighlightingManager.Instance);

            editor.TextChanged += delegate
            {
                var offset = editor.CaretOffset;
                var line = editor.Document.GetLineByOffset(offset);
                editor.ScrollToLine(line.LineNumber);
            };

            Settings.Default.PropertyChanged += (_, args) =>
            {
                switch (args.PropertyName)
                {
                    case "FontName":
                        editor.FontFamily = new FontFamily(Settings.Default.FontName);
                        break;

                    case "FontSize":
                        editor.FontSize = Settings.Default.FontSize;
                        break;
                }
            };

            return editor;
        }

        private static ChromiumWebBrowser CreateNewMonacoEditor()
        {
            var cefSettings = new CefSettings
            {
                BrowserSubprocessPath = Path.GetFullPath("bs_bin/CefSharp.BrowserSubprocess.exe"),
                LocalesDirPath = Path.GetFullPath("bs_bin/locales"),
                ResourcesDirPath = Path.GetFullPath("bs_bin"),
                LogFile = Path.GetFullPath("bs_bin/cefsharp.log")
            };
            
            var editor = new ChromiumWebBrowser
            {
                Address = $"file:///{Directory.GetCurrentDirectory()}/bs_bin/editor_files/rosploco.html"
            };

            return editor;
        }

        public void AddTab(string file)
        {
            var info = new FileInfo(file);
            var content = File.ReadAllText(file);

            var editor = CreateNewMonacoEditor();
            editor.Visibility = Visibility.Hidden;

            var item = new TabItem
            {
                Header = info.Name,
                Content = editor,
                Tag = file
            };

            var panel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Focusable = false
            };

            var text = new TextBlock
            {
                Text = info.Name,
                Focusable = true,
            };

            var removeButton = new Button
            {
                Content = "\xE8BB",
                FontSize = 8,
                Width = 12,
                Height = 12,
                Margin = new Thickness(5, 0, 0, 0),
                Visibility = Visibility.Collapsed,
                FontFamily = new FontFamily("Segoe MDL2 Assets"),
                Background = null
            };

            text.SetResourceReference(TextBlock.ForegroundProperty, "PrimaryTextColor");
            removeButton.SetResourceReference(Button.ForegroundProperty, "PrimaryTextColor");

            panel.Children.Add(text);
            panel.Children.Add(removeButton);

            var timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };

            removeButton.Click += delegate
            {
                if (Tab.Items.Count == 1) return;
                timer.IsEnabled = false;
                RemoveTab(item);
            };

            item.Header = panel;
            item.Height = 24;

            Tab.Items.Add(item);

            timer.Tick += delegate
            {
                Save(item);
            };

            editor.LoadingStateChanged += (sender, args) =>
            {
                Dispatcher.Invoke(async () =>
                {
                    if (args.IsLoading) return;
                    if (!File.Exists(file)) return;

                    await editor.EvaluateScriptAsync("setText", File.ReadAllText(file));
                    await Task.Delay(500);
                    editor.Visibility = Visibility.Visible;
                    timer.IsEnabled = !args.IsLoading;
                });
            };

            Tab.SelectionChanged += delegate
            {
                removeButton.Visibility = item.IsSelected ? Visibility.Visible : Visibility.Collapsed;
                if (item.IsSelected)
                {
                    SelectedEditor = editor;
                    CurrentFileName = text.Text;
                }

                else Save(item);
                TabChanged?.Invoke(item, EventArgs.Empty);
            };
        }

        public void CreateTab(string fileName, string content = "-- Monaco and Autocomplete by Microsoft and EthanMcBloxxer on GitHub under the MIT License.")
        {
            fileName = Path.Combine(TabDir, fileName);

            if (File.Exists(fileName))
            {
                Messages.ShowGenericErrorMessage("File already exists!");
                return;
            }
            File.WriteAllText(fileName, content);
            AddTab(fileName);
        }

        public void RemoveTab(TabItem item)
        {
            var file = (string)item.Tag;
            if (File.Exists(file))
                File.Delete(file);

            Tab.Items.Remove(item);
        }

        //public void EditTab(TabItem item, string name)
        //{
        //    var oldFileName = (string)item.Tag;

        //    if (File.Exists(oldFileName))
        //    {
        //        File.Move(oldFileName, name);
        //        item.Tag = name;
        //    }

        //    else
        //        Tab.Items.Remove(item);
        //}

        public void RefreshAllTabs(bool SaveAll = true)
        {
            if (SaveAll) SaveAllTabs();
            var files = Directory.GetFiles(TabDir);
            if (files.Length == 0)
            {
                File.WriteAllText(Path.Combine(TabDir, "Script 0.lua"), "");
                files = Directory.GetFiles(TabDir);
            }

            Tab.Items.Clear();
            foreach (var file in files)
                AddTab(file);
            Tab.SelectedIndex = 0;
        }

        public void Save(TabItem item)
        {
            var file = (string)item.Tag;
            if (!File.Exists(file)) return;

            var editor = (ChromiumWebBrowser)item.Content;
            if (!editor.CanExecuteJavascriptInMainFrame) return;

            var script = editor.EvaluateScript("getText()");
            File.WriteAllText(file, script);
            Debug.WriteLine($"Saved {Path.GetFileName(item.Tag.ToString())} | {script}");
        }

        public void SaveAllTabs()
        {
            foreach (TabItem item in Tab.Items)
                Save(item);
        }
        
        public event EventHandler TabChanged;
        public string CurrentFileName = "";

        public void InitializeDirectory(string dir)
        {
            TabDir = dir;
            RefreshAllTabs(false);
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            var count = 0;
            var name = $"Script {count}.lua";
            while (File.Exists(Path.Combine(TabDir, name)))
            {
                count++;
                name = $"Script {count}.lua";
            }

            CreateTab(name);
        }
    }

}
