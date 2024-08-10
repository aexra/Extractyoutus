using System.ComponentModel;
using System.Runtime.CompilerServices;
using Extractyoutus.ViewModels;

using Microsoft.UI.Xaml.Controls;
using Windows.ApplicationModel.Email.DataProvider;
using Windows.Storage;
using Windows.Storage.Pickers;

namespace Extractyoutus.Views;

// TODO: Set the URL for your privacy policy by updating SettingsPage_PrivacyTermsLink.NavigateUri in Resources.resw.
public sealed partial class SettingsPage : Page, INotifyPropertyChanged
{
    public event PropertyChangedEventHandler PropertyChanged;

    private string _description;
    public string Description
    {
        get => _description;
        set
        {
            if (_description != value)
            {
                _description = value;
                NotifyPropertyChanged();
            }
        }
    }

    public SettingsViewModel ViewModel
    {
        get;
    }

    public SettingsPage()
    {
        ViewModel = App.GetService<SettingsViewModel>();
        InitializeComponent();

        Description = (string)ApplicationData.Current.LocalSettings.Values["extractor_folder"];
    }

    private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private async void settingsCard_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        var picker = new FolderPicker();
        picker.SuggestedStartLocation = PickerLocationId.Downloads;
        picker.FileTypeFilter.Add("*");

        var hwnd = App.MainWindow.GetWindowHandle();
        WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

        var folder = await picker.PickSingleFolderAsync();

        ApplicationData.Current.LocalSettings.Values["extractor_folder"] = folder.Path;

        Description = folder.Path;
    }
}
