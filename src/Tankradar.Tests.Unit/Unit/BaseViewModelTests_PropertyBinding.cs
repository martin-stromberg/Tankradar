using System.ComponentModel;
using Tankradar.MAUI.ViewModels;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft die MVVM-Grundfunktionalität von <see cref="BaseViewModel"/> (Titel, IsBusy/IsNotBusy, PropertyChanged) anhand von <see cref="TankbookViewModel"/>.
/// </summary>
public class BaseViewModelTests_PropertyBinding : BaseTest
{
    /// <summary>
    /// Prüft, dass der Seitentitel bereits im Konstruktor gesetzt wird.
    /// </summary>
    [Fact]
    public void Constructor_SetsTitle()
    {
        var viewModel = new TankbookViewModel();

        Assert.Equal("Tankbuch", viewModel.Title);
    }

    /// <summary>
    /// Prüft, dass ein neues ViewModel im Ruhezustand startet.
    /// </summary>
    [Fact]
    public void Constructor_StartsNotBusy()
    {
        var viewModel = new TankbookViewModel();

        Assert.False(viewModel.IsBusy);
        Assert.True(viewModel.IsNotBusy);
    }

    /// <summary>
    /// Prüft, dass das Setzen von <see cref="BaseViewModel.IsBusy"/> sowohl für <c>IsBusy</c> als auch für <c>IsNotBusy</c> das <see cref="INotifyPropertyChanged.PropertyChanged"/>-Ereignis auslöst.
    /// </summary>
    [Fact]
    public void SetIsBusy_RaisesPropertyChangedForIsBusyAndIsNotBusy()
    {
        var viewModel = new TankbookViewModel();
        var raisedProperties = new List<string?>();
        viewModel.PropertyChanged += (_, e) => raisedProperties.Add(e.PropertyName);

        viewModel.IsBusy = true;

        Assert.Contains(nameof(TankbookViewModel.IsBusy), raisedProperties);
        Assert.Contains(nameof(TankbookViewModel.IsNotBusy), raisedProperties);
        Assert.True(viewModel.IsBusy);
        Assert.False(viewModel.IsNotBusy);
    }

    /// <summary>
    /// Prüft, dass ein erneutes Setzen desselben Werts kein <see cref="INotifyPropertyChanged.PropertyChanged"/>-Ereignis auslöst.
    /// </summary>
    [Fact]
    public void SetTitle_SameValue_DoesNotRaisePropertyChanged()
    {
        var viewModel = new TankbookViewModel();
        var raisedCount = 0;
        viewModel.PropertyChanged += (_, _) => raisedCount++;

        viewModel.Title = viewModel.Title;

        Assert.Equal(0, raisedCount);
    }
}
