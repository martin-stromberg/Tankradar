#if WINDOWS
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Tankradar.MAUI.Services.Map;

namespace Tankradar.MAUI.Views.Controls;

/// <summary>
/// Windows-Teil der Karte: Ziehen mit der Maus verschiebt die Karte, das Mausrad zoomt, ein Doppelklick zentriert und vergrößert.
/// </summary>
public partial class StationMapView
{
    private readonly MapPointerTracker _pointer = new();
    private readonly MapDoubleTapDetector _doubleTap = new();
    private bool _lastPressWasMouse;
    private UIElement? _pointerTarget;
    private PointerEventHandler? _anyPointerPressed;

    partial void DetachPointerInput()
    {
        if (_pointerTarget is not null)
        {
            if (_anyPointerPressed is not null)
            {
                _pointerTarget.RemoveHandler(UIElement.PointerPressedEvent, _anyPointerPressed);
            }

            _pointerTarget.PointerPressed -= OnPointerPressed;
            _pointerTarget.PointerMoved -= OnPointerMoved;
            _pointerTarget.PointerReleased -= OnPointerReleased;
            _pointerTarget.PointerCanceled -= OnPointerReleased;
            _pointerTarget.PointerCaptureLost -= OnPointerCaptureLost;
            _pointerTarget.PointerWheelChanged -= OnPointerWheelChanged;
            _pointerTarget = null;
        }

        _pointer.Release();
    }

    partial void AttachPointerInput()
    {
        DetachPointerInput();

        if (Surface.Handler?.PlatformView is UIElement target)
        {
            // Auch für Drücke auf Schaltflächen (die das Ereignis als behandelt markieren), damit die Herkunft eines Doppeltipps immer stimmt.
            _anyPointerPressed ??= new PointerEventHandler(OnAnyPointerPressed);
            target.AddHandler(UIElement.PointerPressedEvent, _anyPointerPressed, handledEventsToo: true);
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

    partial void SuppressDoubleTap(ref bool suppress)
    {
        // Den Maus-Doppelklick wertet OnPointerPressed aus; der Doppeltipp der Gestenerkennung gilt dann nur für Berührung und Stift (kein doppeltes Zoomen).
        suppress = _lastPressWasMouse;
    }

    private void OnAnyPointerPressed(object sender, PointerRoutedEventArgs e)
    {
        _lastPressWasMouse = IsMouse(e);
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

        // Der Zeitstempel der Zeigerereignisse zählt in Mikrosekunden.
        if (_doubleTap.Register(point.Position.X, point.Position.Y, (long)(point.Timestamp / 1000)))
        {
            ZoomAndCenterAt(point.Position.X, point.Position.Y);
        }
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
