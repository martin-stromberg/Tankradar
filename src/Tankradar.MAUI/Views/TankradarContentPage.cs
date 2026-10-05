using Tankradar.MAUI.ViewModels;

namespace Tankradar.MAUI.Views;

/// <summary>
/// Gemeinsame Basisklasse für alle Content-Seiten der Tankatlas-App. Leitet <c>OnAppearing</c>
/// einmalig an das per <see cref="BindableObject.BindingContext"/> gesetzte <see cref="BaseViewModel"/> weiter.
/// </summary>
public abstract class TankradarContentPage : ContentPage
{
    /// <inheritdoc />
    protected override void OnAppearing()
    {
        base.OnAppearing();
        (BindingContext as BaseViewModel)?.OnAppearing();
    }

    /// <inheritdoc />
    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        (BindingContext as BaseViewModel)?.OnDisappearing();
    }
}
