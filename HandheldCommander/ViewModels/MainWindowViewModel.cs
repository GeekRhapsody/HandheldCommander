using System.Collections.ObjectModel;
using HandheldCommander.Models;
using System.IO;
using System.Diagnostics;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Windows.Input;
using System.Threading.Tasks;

namespace HandheldCommander.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    public ObservableCollection<FileSystemItem> LeftPanelItems { get; } = new();
    public ObservableCollection<FileSystemItem> RightPanelItems { get; } = new();

    private int _leftPanelSelectedIndex;
    public int LeftPanelSelectedIndex
    {
        get => _leftPanelSelectedIndex;
        set => SetProperty(ref _leftPanelSelectedIndex, value);
    }

    private int _rightPanelSelectedIndex;
    public int RightPanelSelectedIndex
    {
        get => _rightPanelSelectedIndex;
        set => SetProperty(ref _rightPanelSelectedIndex, value);
    }

    private bool _leftPanelSelected = true;
    public bool LeftPanelSelected
    {
        get => _leftPanelSelected;
        set
        {
            if (SetProperty(ref _leftPanelSelected, value))
            {
                OnPropertyChanged(nameof(RightPanelSelected));
            }
        }
    }

    public bool RightPanelSelected => !_leftPanelSelected;

    private static string GetDocumentsPath()
    {
        return Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
    }

    private static string[] GetDrives()
    {
        return Directory.GetLogicalDrives();
    }

    private bool _showingDrivesLeft = false;
    private bool _showingDrivesRight = false;

    private string _leftPanelPath = GetDocumentsPath();
    public string LeftPanelPath
    {
        get => _leftPanelPath;
        set
        {
            if (SetProperty(ref _leftPanelPath, value))
            {
                _showingDrivesLeft = false;
                LoadDirectory(LeftPanelItems, value);
                OnLeftPanelPathChanged(value);
            }
        }
    }

    private string _rightPanelPath = GetDocumentsPath();
    public string RightPanelPath
    {
        get => _rightPanelPath;
        set
        {
            if (SetProperty(ref _rightPanelPath, value))
            {
                _showingDrivesRight = false;
                LoadDirectory(RightPanelItems, value);
                OnRightPanelPathChanged(value);
            }
        }
    }

    public MainWindowViewModel()
    {
        LoadDirectory(LeftPanelItems, LeftPanelPath);
        LoadDirectory(RightPanelItems, RightPanelPath);
    }

    private void LoadDirectory(ObservableCollection<FileSystemItem> target, string path)
    {
        target.Clear();
        // Add parent directory entry if not root
        var parent = Directory.GetParent(path);
        if (parent != null)
        {
            target.Add(new FileSystemItem { Name = "..", Path = parent.FullName, IsDirectory = true, Icon = "📁" });
        }
        // Add directories
        foreach (var dir in Directory.GetDirectories(path))
        {
            target.Add(new FileSystemItem { Name = System.IO.Path.GetFileName(dir), Path = dir, IsDirectory = true, Icon = "📁" });
        }
        // Add files
        foreach (var file in Directory.GetFiles(path))
        {
            target.Add(new FileSystemItem { Name = System.IO.Path.GetFileName(file), Path = file, IsDirectory = false, Icon = "📄" });
        }
    }

    private void ShowDrives(ObservableCollection<FileSystemItem> target)
    {
        target.Clear();
        foreach (var drive in GetDrives())
        {
            target.Add(new FileSystemItem { Name = drive, Path = drive, IsDirectory = true, Icon = "💾" });
        }
    }

    public void EnterSelectedItem(bool leftPanel)
    {
        var items = leftPanel ? LeftPanelItems : RightPanelItems;
        var idx = leftPanel ? LeftPanelSelectedIndex : RightPanelSelectedIndex;
        if (idx < 0 || idx >= items.Count) return;
        var item = items[idx];
        if (item.IsDirectory)
        {
            if (item.Name == "..")
            {
                NavigateUp(leftPanel);
                return;
            }
            if (leftPanel)
                LeftPanelPath = item.Path;
            else
                RightPanelPath = item.Path;
            if (leftPanel) LeftPanelSelectedIndex = 0; else RightPanelSelectedIndex = 0;
        }
        else
        {
            try
            {
                Process.Start(new ProcessStartInfo(item.Path) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to open file: {ex.Message}");
            }
        }
    }

    public void NavigateUp(bool leftPanel)
    {
        if (leftPanel)
        {
            if (_showingDrivesLeft)
                return;
            var parent = Directory.GetParent(LeftPanelPath);
            if (parent == null)
            {
                ShowDrives(LeftPanelItems);
                _showingDrivesLeft = true;
                LeftPanelSelectedIndex = 0;
            }
            else
            {
                LeftPanelPath = parent.FullName;
                LeftPanelSelectedIndex = 0;
            }
        }
        else
        {
            if (_showingDrivesRight)
                return;
            var parent = Directory.GetParent(RightPanelPath);
            if (parent == null)
            {
                ShowDrives(RightPanelItems);
                _showingDrivesRight = true;
                RightPanelSelectedIndex = 0;
            }
            else
            {
                RightPanelPath = parent.FullName;
                RightPanelSelectedIndex = 0;
            }
        }
    }

    public void RefreshPanels()
    {
        LoadDirectory(LeftPanelItems, LeftPanelPath);
        LoadDirectory(RightPanelItems, RightPanelPath);
    }

    public IEnumerable<string> LeftPanelBreadcrumbs
    {
        get
        {
            if (_showingDrivesLeft)
                return new[] { "Drives" };
            return GetBreadcrumbs(LeftPanelPath);
        }
    }
    public IEnumerable<string> RightPanelBreadcrumbs
    {
        get
        {
            if (_showingDrivesRight)
                return new[] { "Drives" };
            return GetBreadcrumbs(RightPanelPath);
        }
    }
    private IEnumerable<string> GetBreadcrumbs(string path)
    {
        if (string.IsNullOrEmpty(path))
            yield break;
        var parts = path.Split(System.IO.Path.DirectorySeparatorChar, System.StringSplitOptions.RemoveEmptyEntries);
        if (System.IO.Path.IsPathRooted(path))
        {
            yield return System.IO.Path.GetPathRoot(path);
        }
        foreach (var part in parts)
        {
            if (part != System.IO.Path.GetPathRoot(path).TrimEnd(System.IO.Path.DirectorySeparatorChar))
                yield return part;
        }
    }
    // Notify property changed for breadcrumbs when path changes
    private void OnLeftPanelPathChanged(string value)
    {
        OnPropertyChanged(nameof(LeftPanelBreadcrumbs));
    }
    private void OnRightPanelPathChanged(string value)
    {
        OnPropertyChanged(nameof(RightPanelBreadcrumbs));
    }
}
