#if WINDOWS
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Tankradar.MAUI.Services.Map;

namespace Tankradar.MAUI.Views.Controls;

/// <summary>
/// Windows-Teil der Karte: Ziehen mit der Maus verschiebt die Karte, das Mausrad zoomt.
/// </summary>
public partial class StationMapView
{
    private readonly MapPointerTracker _pointer = new();
    private UIElement? _pointerTarget;

    partial void AttachPointerInput()
    {
        if (_pointerTarget is not null)
        {
            _pointerTarget.PointerPressed -= OnPointerPressed;
            _pointerTarget.PointerMoved -= OnPointerMoved;
            _pointerTarget.PointerReleased -= OnPointerReleased;
            _pointerTarget.PointerCanceled -= OnPointerReleased;
            _pointerTarget.PointerCaptureLost -= OnPointerCaptureLost;
            _pointerTarget.PointerWheelChanged -= OnPointerWheelChanged;
            _pointerTarget = null;
        }

        if (Surface.Handler?.PlatformView is UIElement target)
        {
            target.PointerPressed += OnPointerPressed;
            target.PointerMoved += OnPointerMoved;
            target.PointerReleased += OnPointerReleased;
            target.PointerCanceled += OnPointerReleased;
            target.PointerCaptureLost += OnPointerCaptureLost;
            target.PointerWheelChanged += OnPointerWheelChanged;
            _pointerTarget = target;
        }
    }

    private static bool IsMouse(PointerRoutedEventArgs e)
    {
        return e.Pointer.PointerDeviceType == PointerDeviceType.Mouse;
    }

    private void OnPointerPressed(object sender, PointerRoutedEventArgs e)
    {
        if (!IsMouse(e) || sender is not UIElement target)
        {
            return;
        }

        var point = e.GetCurrentPoint(target);
        if (!point.Properties.IsLeftButtonPressed)
        {
            return;
        }

        _pointer.Press(point.Position.X, point.Position.Y);
        target.CapturePointer(e.Pointer);
        e.Handled = true;
    }

    private void OnPointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (!IsMouse(e) || sender is not UIElement target)
        {
            return;
        }

        var point = e.GetCurrentPoint(target);
        if (!point.Properties.IsLeftButtonPressed)
        {
            _pointer.Release();
            return;
        }

        if (_pointer.Move(point.Position.X, point.Position.Y) is { } delta)
        {
            PanByPixels(delta.X, delta.Y);
            e.Handled = true;
        }
    }

    private void OnPointerReleased(object sender, PointerRoutedEventArgs e)
    {
        if (!IsMouse(e) || sender is not UIElement target)
        {
            return;
        }

        _pointer.Release();
        target.ReleasePointerCapture(e.Pointer);
    }

    private void OnPointerCaptureLost(object sender, PointerRoutedEventArgs e)
    {
        _pointer.Release();
    }

    private void OnPointerWheelChanged(object sender, PointerRoutedEventArgs e)
    {
        if (sender is not UIElement target)
        {
            return;
        }

        var steps = _pointer.Wheel(e.GetCurrentPoint(target).Properties.MouseWheelDelta);

        // Das Rad gehört der Karte: Die umgebende Seite scrollt nicht mit.
        e.Handled = true;
        if (steps != 0)
        {
            Zoom(steps);
        }
    }
}
#endif
