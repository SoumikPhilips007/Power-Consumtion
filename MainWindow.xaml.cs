using System.Windows;
using TimestampCalculator.Services;
using TimestampCalculator.ViewModels;

namespace TimestampCalculator;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel(
            new FileDialogService(),
            new PowerConsumptionCalculatorService(new FileOperation()));
    }
}
