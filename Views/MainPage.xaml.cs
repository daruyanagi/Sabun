using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.Xaml.Controls;
using System;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Linq;
using Windows.ApplicationModel.DataTransfer;
using Windows.UI.Xaml.Navigation;
using Sabun.Helpers;

namespace Sabun.Views
{
    public sealed partial class MainPage : Page, INotifyPropertyChanged
    {
        public MainPage()
        {
            InitializeComponent();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            await InitializeAsync();
        }

        private async Task InitializeAsync()
        {
            Path1 = await ApplicationData.Current.LocalSettings.ReadAsync<string>(nameof(Path1));
            Path2 = await ApplicationData.Current.LocalSettings.ReadAsync<string>(nameof(Path2));

            var file1 = await ApplicationData.Current.LocalFolder.CreateFileAsync("file1", CreationCollisionOption.OpenIfExists);
            Text1 = await FileIO.ReadTextAsync(file1);
            var file2 = await ApplicationData.Current.LocalFolder.CreateFileAsync("file2", CreationCollisionOption.OpenIfExists);
            Text2 = await FileIO.ReadTextAsync(file2);

            ContentArea.DataContext = this;

            Refresh();
        }

        private string path1 = null;
        public string Path1
        {
            get { return path1; }
            set { Set(ref path1, value); }
        }

        private string path2 = null;
        public string Path2
        {
            get { return path2; }
            set { Set(ref path2, value); }
        }

        private string text1 = string.Empty;
        public string Text1
        {
            get { return text1; }
            set { Set(ref text1, value); }
        }

        private string text2 = string.Empty;
        public string Text2
        {
            get { return text2; }
            set { Set(ref text2, value); }
        }

        private void refreshButton_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            Refresh();
        }

        private async void openFile1Button_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            var filePicker = new Windows.Storage.Pickers.FileOpenPicker();
            filePicker.FileTypeFilter.Add(".txt");

            var file1 = await filePicker.PickSingleFileAsync();
            if (file1 == null) return;
            
            await SetFile1Async(file1);
            Refresh();
        }

        private async Task SetFile1Async(StorageFile file1)
        {
            Path1 = file1.Path;
            await ApplicationData.Current.LocalSettings.SaveAsync(nameof(Path1), Path1);

            try
            {
                Text1 = await FileIO.ReadTextAsync(file1);
            }
            catch
            {
                var buffer = await FileIO.ReadBufferAsync(file1);

                System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
                var enc = System.Text.Encoding.GetEncoding("Shift_JIS");
                Text1 = enc.GetString(buffer.ToArray());
            }
            finally
            {
                var file = await ApplicationData.Current.LocalFolder.CreateFileAsync("file1", CreationCollisionOption.OpenIfExists);
                await FileIO.WriteTextAsync(file, text1);
            }
        }

        private async void openFile2Button_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            var filePicker = new Windows.Storage.Pickers.FileOpenPicker();
            filePicker.FileTypeFilter.Add(".txt");

            var file2 = await filePicker.PickSingleFileAsync();
            if (file2 == null) return;

            await SetFile2Async(file2);
            Refresh();
        }

        private async Task SetFile2Async(StorageFile file2)
        {
            Path2 = file2.Path;
            await ApplicationData.Current.LocalSettings.SaveAsync(nameof(Path2), Path2);

            try
            {
                Text2 = await FileIO.ReadTextAsync(file2);
            }
            catch
            {
                var buffer = await FileIO.ReadBufferAsync(file2);

                System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
                var enc = System.Text.Encoding.GetEncoding("Shift_JIS");
                Text2 = enc.GetString(buffer.ToArray());
            }
            finally
            {
                var file = await ApplicationData.Current.LocalFolder.CreateFileAsync("file2", CreationCollisionOption.OpenIfExists);
                await FileIO.WriteTextAsync(file, text2);
            }
        }

        private void Refresh()
        {
            var results = FastDiff.FastDiff.Diff(text1, text2);

            var fore_color = Services.ThemeSelectorService.IsLightThemeEnabled ? "black" : "white";
            var back_color = Services.ThemeSelectorService.IsLightThemeEnabled ? "white" : "black";

            var block_blank_color = Services.ThemeSelectorService.IsLightThemeEnabled ? "WhiteSmoke" : "#404040";
            var block_modified_color = Services.ThemeSelectorService.IsLightThemeEnabled ? "#fff7e6" : "#cc8b00";
            var block_inserted_color = Services.ThemeSelectorService.IsLightThemeEnabled ? "#ebfaeb" : "#29a329";
            var block_deleted_color = Services.ThemeSelectorService.IsLightThemeEnabled ? "#ffebe6" : "#cc2900";

            var inline_modified_color = Services.ThemeSelectorService.IsLightThemeEnabled ? "#ffe7b3" : "#996900";
            var inline_inserted_color = Services.ThemeSelectorService.IsLightThemeEnabled ? "#c2f0c2" : "#196619";
            var inline_deleted_color = Services.ThemeSelectorService.IsLightThemeEnabled ? "#ffc2b3" : "#991f00";

            var temp =
$@"
<style>
    body {{ color: {fore_color}; background-color: {back_color}; }}
    table {{ border-spacing: 0; table-layout: fixed; word-wrap: break-word; width: 100%; }}
    td, th {{ vertical-align: top; }}
    th {{ padding-right: 0.5em; padding-left: 0.5em; color: Gray; font-family: consolas; font-weight: normal; width: 4em; }}
    .blank {{ background-color: {block_blank_color}; }}

    td.modified {{ background-color: {block_modified_color}; }}
    td.inserted {{ background-color: {block_inserted_color}; }}
    td.deleted {{ background-color: {block_deleted_color}; }}

    span.modified {{ background-color: {inline_modified_color}; }}
    span.inserted {{ background-color: {inline_inserted_color}; }}
    span.deleted {{ background-color: {inline_deleted_color}; }}
</style>
";

            temp += "<table>\n";

            var lines1 = text1.Replace("&", "&amp;")
                .Replace("<", "&lt;").Replace(">", "&gt;")
                .Replace("\r\n", "\r").Replace("\r", "\n").Split('\n');
            var lines2 = text2.Replace("&", "&amp;")
                .Replace("<", "&lt;").Replace(">", "&gt;")
                .Replace("\r\n", "\r").Replace("\r", "\n").Split('\n');

            foreach (var result in results)
            {
                var o = result.OriginalStart;
                var m = result.ModifiedStart;

                // 変更なし
                if (!result.Modified)
                {
                    for (int i = 0; i < result.OriginalLength /* == result.ModifiedLength */; i++)
                    {
                        temp += "<tr>";
                        temp += $"<th>{o + i:0000}</th><td class=''>{lines1[o + i]}</td>";
                        temp += $"<th>{m + i:0000}</th><td class=''>{lines2[m + i]}</td>";
                        temp += "</tr>";
                    }

                    continue;
                }

                // 行の削除
                if (result.Modified && (result.ModifiedLength == 0))
                {
                    for (int i = 0; i < result.OriginalLength; i++)
                    {
                        temp += "<tr>";
                        temp += $"<th>{o + i:0000}</th><td class='deleted'>{lines1[o + i]}</td>";
                        temp += "<th></th><td class='blank'></td>";
                        temp += "</tr>";
                    }

                    continue;
                }

                // 行の挿入
                if (result.Modified && (result.OriginalLength == 0))
                {
                    for (int i = 0; i < result.ModifiedLength; i++)
                    {
                        temp += "<tr>";
                        temp += "<th></th><td class='blank'></td>";
                        temp += $"<th>{m + i:0000}</th><td class='inserted'>{lines2[m + i]}</td>";
                        temp += "</tr>";
                    }

                    continue;
                }

                // 変更
                var length = Math.Max(result.ModifiedLength, result.OriginalLength);

                for (int i = 0; i < length; i++)
                {
                    var line1 = i < result.OriginalLength ? lines1[o + i] : string.Empty;
                    var line2 = i < result.ModifiedLength ? lines2[m + i] : string.Empty;

                    var r = FastDiff.FastDiff.DiffChar(line1, line2);

                    line1 = r.Select(_ =>
                    {
                        var sub = line1.Substring(_.OriginalStart, _.OriginalLength);
                        if (_.Modified && _.ModifiedLength == 0) sub = $"<span class='deleted'>{sub}</span>";
                        else if (_.Modified) sub = $"<span class='modified'>{sub}</span>";
                        return sub;
                    }).LinesToString();

                    line2 = r.Select(_ =>
                    {
                        var sub = line2.Substring(_.ModifiedStart, _.ModifiedLength);
                        if (_.Modified && _.OriginalLength == 0) sub = $"<span class='inserted'>{sub}</span>";
                        else if (_.Modified) sub = $"<span class='modified'>{sub}</span>";
                        return sub;
                    }).LinesToString();

                    temp += "<tr>";

                    temp += i < result.OriginalLength
                        ? $"<th>{o + i:0000}</th><td class='modified'>{line1}</td>"
                        : "<th></th><td class='blank'></td>";

                    temp += i < result.ModifiedLength
                        ? $"<th>{m + i:0000}</th><td class='modified'>{line2}</td>"
                        : "<th></th><td class='blank'></td>";

                    temp += "</tr>";
                }
            }

            temp += "</table>";
            
            webView1.NavigateToString(temp);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void Set<T>(ref T storage, T value, [CallerMemberName]string propertyName = null)
        {
            if (Equals(storage, value))
            {
                return;
            }

            storage = value;
            OnPropertyChanged(propertyName);
        }

        private void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));



        private void Grid_DragOver(object sender, Windows.UI.Xaml.DragEventArgs e)
        {
            if (!e.DataView.Contains(StandardDataFormats.StorageItems)) return;

            e.AcceptedOperation = DataPackageOperation.Copy;
            e.Handled = true;
        }

        private async void Grid_Drop(object sender, Windows.UI.Xaml.DragEventArgs e)
        {
            if (!e.DataView.Contains(StandardDataFormats.StorageItems)) return;

            var d = e.GetDeferral();
            var files = await e.DataView.GetStorageItemsAsync();

            if (files.Count == 1)
            {
                var grid = sender as Grid;
                var point = e.GetPosition(grid);

                if (point.X < grid.ActualWidth / 2)
                {
                    await SetFile1Async(files.First() as StorageFile);
                }
                else
                {
                    await SetFile2Async(files.First() as StorageFile);
                }

                Refresh();
            }
            else if (files.Count > 1)
            {
                await SetFile1Async(files[0] as StorageFile);
                await SetFile2Async(files[1] as StorageFile);

                Refresh();
            }

            d.Complete();
        }
    }
}
