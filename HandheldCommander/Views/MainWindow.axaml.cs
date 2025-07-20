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

    private void PollGamepad(object state)
    {
        if (_controller == null)
        {
            _controller = FindConnectedController();
            if (_controller != null)
                Console.WriteLine($"[Gamepad] Controller found: {_controller.UserIndex}");
            else
                return;
        }
        if (!_controller.IsConnected || _vm == null)
        {
            Console.WriteLine("[Gamepad] Not connected or VM null");
            return;
        }
        var stateResult = _controller.GetState();
        var gamepad = stateResult.Gamepad;
        Console.WriteLine($"[Gamepad] Poll: Buttons={gamepad.Buttons}");

        // D-Pad Up
        if ((gamepad.Buttons & GamepadButtonFlags.DPadUp) != 0 && _canMove)
        {
            Dispatcher.UIThread.Post(() =>
            {
                if (_vm.LeftPanelSelected)
                {
                    if (_vm.LeftPanelSelectedIndex > 0)
                        _vm.LeftPanelSelectedIndex--;
                }
                else
                {
                    if (_vm.RightPanelSelectedIndex > 0)
                        _vm.RightPanelSelectedIndex--;
                }
            });
            _canMove = false;
        }
        // D-Pad Down
        else if ((gamepad.Buttons & GamepadButtonFlags.DPadDown) != 0 && _canMove)
        {
            Dispatcher.UIThread.Post(() =>
            {
                if (_vm.LeftPanelSelected)
                {
                    if (_vm.LeftPanelSelectedIndex < _vm.LeftPanelItems.Count - 1)
                        _vm.LeftPanelSelectedIndex++;
                }
                else
                {
                    if (_vm.RightPanelSelectedIndex < _vm.RightPanelItems.Count - 1)
                        _vm.RightPanelSelectedIndex++;
                }
            });
            _canMove = false;
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

        // X Button (Show context menu)
        if ((gamepad.Buttons & GamepadButtonFlags.X) != 0 && _canX)
        {
            Dispatcher.UIThread.Post(() =>
            {
                if (_vm.LeftPanelSelected)
                {
                    var listBox = this.FindControl<ListBox>("LeftPanelListBox");
                    listBox.ContextMenu?.Open(listBox);
                }
                else
                {
                    var listBox = this.FindControl<ListBox>("RightPanelListBox");
                    listBox.ContextMenu?.Open(listBox);
                }
            });
            _canX = false;
        }
        else if ((gamepad.Buttons & GamepadButtonFlags.X) == 0)
        {
            _canX = true;
        }
    }
}