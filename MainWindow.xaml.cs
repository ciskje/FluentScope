using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Win32;
using USBScope.Live;
using USBScope.Usb;

namespace USBScope;

public partial class MainWindow : Window
{
    private readonly ObservableCollection<USBScopeInfo> _devices = new();
    private readonly ObservableCollection<UsbEventInfo> _events = new();
    private readonly LiveMonitor _monitor = new();
    private readonly DispatcherTimer _timer;
    private bool _refreshing;

    public MainWindow()
    {
        InitializeComponent();
        DevicesGrid.ItemsSource = _devices;
        EventsGrid.ItemsSource = _events;

        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
        _timer.Tick += async (_, _) => await RefreshAsync(logEvents: true);

        Loaded += async (_, _) => await RefreshAsync(logEvents: false);
    }

    private async Task RefreshAsync(bool logEvents)
    {
        if (_refreshing)
            return;
        _refreshing = true;
        RefreshButton.IsEnabled = false;
        StatusText.Text = "Reading USB devices…";

        try
        {
            // Blocking enumeration (P/Invoke): runs off the UI thread.
            List<USBScopeInfo> current = await Task.Run(UsbEnumerator.Enumerate);

            if (logEvents)
            {
                foreach (var e in _monitor.Refresh(current))
                {
                    _events.Insert(0, e);
                    EventLogWriter.Append(e);
                }
            }
            else
            {
                _monitor.SyncOnly(current);
            }

            _devices.Clear();
            foreach (var d in current
                         .OrderByDescending(d => d.SpeedRank)
                         .ThenBy(d => d.DeviceName, StringComparer.CurrentCulture))
            {
                _devices.Add(d);
            }

            int unreadable = current.Count(d => !d.Available);
            StatusText.Text = unreadable > 0
                ? $"{_devices.Count} rows. {unreadable} unreadable hubs: if devices are missing, run the program as administrator."
                : $"{_devices.Count} USB devices read successfully.";
        }
        catch (Exception ex)
        {
            StatusText.Text = "Error: " + ex.Message;
        }
        finally
        {
            _refreshing = false;
            RefreshButton.IsEnabled = true;
        }
    }

    private void ExportHtml_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new SaveFileDialog
        {
            Filter = "HTML file (*.html)|*.html",
            FileName = $"USBScope-{DateTime.Now:yyyyMMdd-HHmm}.html",
        };
        if (dlg.ShowDialog(this) != true)
            return;
        try
        {
            File.WriteAllText(dlg.FileName, HtmlReport.Build(_devices, DateTime.Now));
            StatusText.Text = $"HTML report saved to {dlg.FileName}";
        }
        catch (Exception ex)
        {
            StatusText.Text = "HTML save error: " + ex.Message;
        }
    }

    private void Exit_Click(object sender, RoutedEventArgs e) => Close();

    private void About_Click(object sender, RoutedEventArgs e)
        => new AboutWindow { Owner = this }.ShowDialog();

    private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        => await RefreshAsync(logEvents: false);

    private void LiveCheck_Changed(object sender, RoutedEventArgs e)
    {
        if (LiveCheck.IsChecked == true)
            _timer.Start();
        else
            _timer.Stop();
    }

    /// <summary>
    /// Clicking a row copies it to the clipboard (tab-separated, pastes into Excel).
    /// Header clicks (sorting) and empty areas have no DataGridRow ancestor and are ignored.
    /// </summary>
    private void CopyRow_Click(object sender, MouseButtonEventArgs e)
    {
        if (RowItem(e.OriginalSource as DependencyObject) is not { } item)
            return;
        string text = item switch
        {
            USBScopeInfo d => string.Join("\t",
                d.DeviceName, d.Vendor, d.VidPid, d.Standard, d.SpeedText, d.Location, d.Status),
            UsbEventInfo ev => string.Join("\t",
                ev.TimeText, ev.EventKind, ev.DeviceName, ev.Vendor, ev.Standard, ev.SpeedText),
            _ => "",
        };
        if (text.Length == 0)
            return;
        try
        {
            Clipboard.SetText(text);
            StatusText.Text = "Row copied to clipboard.";
        }
        catch
        {
            StatusText.Text = "Copy failed: clipboard busy, try again.";
        }
    }

    private static object? RowItem(DependencyObject? el)
    {
        while (el is not null && el is not DataGridRow)
            el = VisualTreeHelper.GetParent(el);
        return (el as DataGridRow)?.Item;
    }
}
