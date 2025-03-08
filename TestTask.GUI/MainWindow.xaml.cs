using System.Windows;
using System.Windows.Controls;
using TestTask.GUI.ViewModel;

namespace TestTask.GUI;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }
    
    private async void GetTradesButton_OnClick(object sender, RoutedEventArgs e)
    {
        var viewModel = (MainViewModel)DataContext;
        await viewModel.LoadTradesAsync();
    }
    
    private async void GetCandlesButton_OnClick(object sender, RoutedEventArgs e)
    {
        var viewModel = (MainViewModel)DataContext;
        await viewModel.LoadCandlesAsync();
    }

    private void ToggleButton_OnChecked(object sender, RoutedEventArgs e)
    {
        if (sender is not RadioButton radioButton)
            return;
        
        var viewModel = (MainViewModel)DataContext;

        switch (radioButton.Name)
        {
            case "UseAmount":
                viewModel.TimeTextBoxesVisibility = Visibility.Collapsed;
                viewModel.CandlesAmountTextBoxVisibility = Visibility.Visible;
                break;
            case "UseTime":
                viewModel.TimeTextBoxesVisibility = Visibility.Visible;
                viewModel.CandlesAmountTextBoxVisibility = Visibility.Collapsed;
                break;
        }
    }

    private void GetSocketTradesButton_OnClick(object sender, RoutedEventArgs e)
    {
        var viewModel = (MainViewModel)DataContext;
        viewModel.LoadSocketTradesAsync();
    }

    private void GetSocketCandlesButton_OnClick(object sender, RoutedEventArgs e)
    {
        var viewModel = (MainViewModel)DataContext;
        viewModel.LoadSocketCandlesAsync();
    }

    private async void GetPortfoliosButton_OnClick(object sender, RoutedEventArgs e)
    {
        var viewModel = (MainViewModel)DataContext;
        await viewModel.LoadPortfoliosAsync();
    }
}