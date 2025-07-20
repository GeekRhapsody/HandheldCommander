using Avalonia.Controls;
using System;
using System.Threading;
using Avalonia.Threading;
using SharpDX.XInput;
using HandheldCommander.ViewModels;
using Avalonia;
using HandheldCommander.Views;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls.Primitives;

namespace HandheldCommander.Views;

public partial class MainWindow : Window
{
    private Controller _controller;
    private Timer _timer;
    private MainWindowViewModel _vm;
    private bool _canSwitchPanel = true;
    private bool _canMove = true;
    private bool _canA = true;
    private bool _canB = true;
    private bool _canX = true;
    private bool _canMoveUp = true;
    private bool _canMoveDown = true;
    private ContextMenu? _openContextMenu = null;
    private int _contextMenuSelectedIndex = 0;
    private Popup? _customPopupMenu;
    private Button[]? _popupButtons;
    private int _popupSelectedIndex = 0;
    private bool _popupIsOpen = false;
    private int _popupPanel = 0; // 0 = left, 1 = right
    private int _popupPanelIndex = 0;

    public MainWindow()
    {
        Console.WriteLine("[MainWindow] Constructor called");
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        _controller = FindConnectedController();
        if (_controller == null)
        {
            Console.WriteLine("[Gamepad] No controller found!");
        }
        else
        {
            Console.WriteLine($"[Gamepad] Controller found: {_controller.UserIndex}");
        }
        _timer = new Timer(PollGamepad, null, 0, 100);
        // Custom popup menu and buttons
        _customPopupMenu = this.FindControl<Popup>("CustomPopupMenu");
        _popupButtons = new[]
        {
            this.FindControl<Button>("PopupOption1"),
            this.FindControl<Button>("PopupOption2"),
            this.FindControl<Button>("PopupOption3"),
            this.FindControl<Button>("PopupOption4"),
        };
        foreach (var btn in _popupButtons)
            btn.IsTabStop = false;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        _vm = DataContext as MainWindowViewModel;
        Console.WriteLine($"[MainWindow] DataContextChanged: _vm is {(_vm != null ? "set" : "null")}");
    }

    private Controller FindConnectedController()
    {
        foreach (UserIndex idx in Enum.GetValues(typeof(UserIndex)))
        {
            var ctrl = new Controller(idx);
            if (ctrl.IsConnected)
                return ctrl;
        }
        return null;
    }

    private void ShowCustomPopupMenu(int panel, int itemIndex)
    {
        _popupPanel = panel;
        _popupPanelIndex = itemIndex;
        _popupSelectedIndex = 0;
        if (_customPopupMenu == null || _popupButtons == null)
            return;
        Border border = panel == 0 ? this.FindControl<Border>("LeftPanelBorder") : this.FindControl<Border>("RightPanelBorder");
        if (border == null)
            return;
        _customPopupMenu.PlacementTarget = border;
        _customPopupMenu.IsOpen = true;
        _popupIsOpen = true;
        Dispatcher.UIThread.Post(() => HighlightPopupButton(_popupSelectedIndex));
    }

    private void HideCustomPopupMenu()
    {
        if (_customPopupMenu != null)
            _customPopupMenu.IsOpen = false;
        _popupIsOpen = false;
    }

    private void HighlightPopupButton(int index)
    {
        if (_popupButtons == null) return;
        for (int i = 0; i < _popupButtons.Length; i++)
        {
            _popupButtons[i].Classes.Set("popup-selected", i == index);
        }
        _popupButtons[index].Focus();
    }

    private void PollGamepad(object state)
    {
        if (_controller == null)
        {
            _controller = FindConnectedController();
            if (_controller != null)
                ; // Console.WriteLine($"[Gamepad] Controller found: {_controller.UserIndex}");
            else
                return;
        }
        if (!_controller.IsConnected || _vm == null)
        {
            // Console.WriteLine("[Gamepad] Not connected or VM null");
            return;
        }
        var stateResult = _controller.GetState();
        var gamepad = stateResult.Gamepad;
        if (_popupIsOpen)
        {
            // D-Pad Up
            if ((gamepad.Buttons & GamepadButtonFlags.DPadUp) != 0 && _canMoveUp)
            {
                Dispatcher.UIThread.Post(() =>
                {
                    if (_popupSelectedIndex > 0)
                        _popupSelectedIndex--;
                    HighlightPopupButton(_popupSelectedIndex);
                });
                _canMoveUp = false;
            }
            else if ((gamepad.Buttons & GamepadButtonFlags.DPadUp) == 0)
            {
                _canMoveUp = true;
            }
            // D-Pad Down
            if ((gamepad.Buttons & GamepadButtonFlags.DPadDown) != 0 && _canMoveDown)
            {
                Dispatcher.UIThread.Post(() =>
                {
                    if (_popupSelectedIndex < 3)
                        _popupSelectedIndex++;
                    HighlightPopupButton(_popupSelectedIndex);
                });
                _canMoveDown = false;
            }
            else if ((gamepad.Buttons & GamepadButtonFlags.DPadDown) == 0)
            {
                _canMoveDown = true;
            }
            // B closes popup
            if ((gamepad.Buttons & GamepadButtonFlags.B) != 0 && _canB)
            {
                Dispatcher.UIThread.Post(() => HideCustomPopupMenu());
                _canB = false;
            }
            else if ((gamepad.Buttons & GamepadButtonFlags.B) == 0)
            {
                _canB = true;
            }
            return; // Suppress all other navigation when popup is open
        }
        // D-Pad Up (ContextMenu navigation)
        if (_openContextMenu != null && _openContextMenu.IsOpen)
        {
            if ((gamepad.Buttons & GamepadButtonFlags.DPadUp) != 0 && _canMoveUp)
            {
                Dispatcher.UIThread.Post(() =>
                {
                    if (_contextMenuSelectedIndex > 0)
                        _contextMenuSelectedIndex--;
                    FocusContextMenuItem(_contextMenuSelectedIndex);
                });
                _canMoveUp = false;
            }
            else if ((gamepad.Buttons & GamepadButtonFlags.DPadUp) == 0)
            {
                _canMoveUp = true;
            }
            // D-Pad Down (ContextMenu navigation)
            if ((gamepad.Buttons & GamepadButtonFlags.DPadDown) != 0 && _canMoveDown)
            {
                Dispatcher.UIThread.Post(() =>
                {
                    if (_openContextMenu != null && _contextMenuSelectedIndex < _openContextMenu.Items.Count - 1)
                        _contextMenuSelectedIndex++;
                    FocusContextMenuItem(_contextMenuSelectedIndex);
                });
                _canMoveDown = false;
            }
            else if ((gamepad.Buttons & GamepadButtonFlags.DPadDown) == 0)
            {
                _canMoveDown = true;
            }
            // B Button (close context menu)
            if ((gamepad.Buttons & GamepadButtonFlags.B) != 0 && _canB)
            {
                Dispatcher.UIThread.Post(() =>
                {
                    _openContextMenu?.Close();
                    // Return focus to the ListBox after closing
                    if (_vm.LeftPanelSelected)
                        this.FindControl<ListBox>("LeftPanelListBox").Focus();
                    else
                        this.FindControl<ListBox>("RightPanelListBox").Focus();
                });
                _canB = false;
            }
            else if ((gamepad.Buttons & GamepadButtonFlags.B) == 0)
            {
                _canB = true;
            }
            return; // Don't move file selection while menu is open
        }

        // D-Pad Up (ListBox navigation)
        if ((gamepad.Buttons & GamepadButtonFlags.DPadUp) != 0 && _canMoveUp)
        {
            Dispatcher.UIThread.Post(() =>
            {
                ListBox listBox = _vm.LeftPanelSelected ? this.FindControl<ListBox>("LeftPanelListBox") : this.FindControl<ListBox>("RightPanelListBox");
                int idx = _vm.LeftPanelSelected ? _vm.LeftPanelSelectedIndex : _vm.RightPanelSelectedIndex;
                if (idx > 0)
                {
                    if (_vm.LeftPanelSelected)
                        _vm.LeftPanelSelectedIndex--;
                    else
                        _vm.RightPanelSelectedIndex--;
                    listBox.ScrollIntoView(idx - 1);
                    var item = listBox.ItemContainerGenerator.ContainerFromIndex(idx - 1) as ListBoxItem;
                    item?.Focus();
                }
            });
            _canMoveUp = false;
        }
        else if ((gamepad.Buttons & GamepadButtonFlags.DPadUp) == 0)
        {
            _canMoveUp = true;
        }
        // D-Pad Down (ListBox navigation)
        if ((gamepad.Buttons & GamepadButtonFlags.DPadDown) != 0 && _canMoveDown)
        {
            Dispatcher.UIThread.Post(() =>
            {
                ListBox listBox = _vm.LeftPanelSelected ? this.FindControl<ListBox>("LeftPanelListBox") : this.FindControl<ListBox>("RightPanelListBox");
                int idx = _vm.LeftPanelSelected ? _vm.LeftPanelSelectedIndex : _vm.RightPanelSelectedIndex;
                int count = _vm.LeftPanelSelected ? _vm.LeftPanelItems.Count : _vm.RightPanelItems.Count;
                if (idx < count - 1)
                {
                    if (_vm.LeftPanelSelected)
                        _vm.LeftPanelSelectedIndex++;
                    else
                        _vm.RightPanelSelectedIndex++;
                    listBox.ScrollIntoView(idx + 1);
                    var item = listBox.ItemContainerGenerator.ContainerFromIndex(idx + 1) as ListBoxItem;
                    item?.Focus();
                }
            });
            _canMoveDown = false;
        }
        else if ((gamepad.Buttons & GamepadButtonFlags.DPadDown) == 0)
        {
            _canMoveDown = true;
        }
        else if ((gamepad.Buttons & (GamepadButtonFlags.DPadUp | GamepadButtonFlags.DPadDown)) == 0)
        {
            _canMove = true;
        }

        // L Shoulder
        if ((gamepad.Buttons & GamepadButtonFlags.LeftShoulder) != 0 && _canSwitchPanel)
        {
            Dispatcher.UIThread.Post(() =>
            {
                _vm.LeftPanelSelected = true;
            });
            _canSwitchPanel = false;
        }
        // R Shoulder
        else if ((gamepad.Buttons & GamepadButtonFlags.RightShoulder) != 0 && _canSwitchPanel)
        {
            Dispatcher.UIThread.Post(() =>
            {
                _vm.LeftPanelSelected = false;
            });
            _canSwitchPanel = false;
        }
        else if ((gamepad.Buttons & (GamepadButtonFlags.LeftShoulder | GamepadButtonFlags.RightShoulder)) == 0)
        {
            _canSwitchPanel = true;
        }

        // A Button (Enter directory or go up)
        if ((gamepad.Buttons & GamepadButtonFlags.A) != 0 && _canA)
        {
            Dispatcher.UIThread.Post(() =>
            {
                _vm.EnterSelectedItem(_vm.LeftPanelSelected);
            });
            _canA = false;
        }
        else if ((gamepad.Buttons & GamepadButtonFlags.A) == 0)
        {
            _canA = true;
        }
        // B Button (Navigate up)
        if ((gamepad.Buttons & GamepadButtonFlags.B) != 0 && _canB)
        {
            Dispatcher.UIThread.Post(() =>
            {
                _vm.NavigateUp(_vm.LeftPanelSelected);
            });
            _canB = false;
        }
        else if ((gamepad.Buttons & GamepadButtonFlags.B) == 0)
        {
            _canB = true;
        }

        // X Button (Show custom popup menu)
        if ((gamepad.Buttons & GamepadButtonFlags.X) != 0 && _canX)
        {
            Dispatcher.UIThread.Post(() =>
            {
                int panel = _vm.LeftPanelSelected ? 0 : 1;
                int idx = _vm.LeftPanelSelected ? _vm.LeftPanelSelectedIndex : _vm.RightPanelSelectedIndex;
                ShowCustomPopupMenu(panel, idx);
            });
            _canX = false;
        }
        else if ((gamepad.Buttons & GamepadButtonFlags.X) == 0)
        {
            _canX = true;
        }
    }

    private void FocusContextMenuItem(int index)
    {
        if (_openContextMenu != null && _openContextMenu.IsOpen && index >= 0 && index < _openContextMenu.Items.Count)
        {
            if (_openContextMenu.Items[index] is MenuItem mi)
                mi.Focus();
        }
    }
}