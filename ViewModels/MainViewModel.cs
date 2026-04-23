using System.Globalization;
using System.Windows.Media;
using TimestampCalculator.Infrastructure;
using TimestampCalculator.Services;

namespace TimestampCalculator.ViewModels;

internal sealed class MainViewModel : ObservableObject
{
    private static readonly Brush TransparentBrush = new SolidColorBrush(Colors.Transparent);
    private readonly IFileDialogService _fileDialogService;
    private readonly PowerConsumptionCalculatorService _calculatorService;

    private string _timestamp1 = string.Empty;
    private string _timestamp2 = string.Empty;
    private string _timestamp3 = string.Empty;
    private string _timestamp4 = string.Empty;
    private string _timestamp5 = string.Empty;
    private string _timestamp6 = string.Empty;
    private string _timestamp7 = string.Empty;
    private string _valueA = string.Empty;
    private string _valueB = string.Empty;
    private string _valueC = string.Empty;
    private string _valueD = string.Empty;
    private string _valueE = string.Empty;
    private string _powerLogPath = string.Empty;
    private string _devLogPath = string.Empty;
    private bool _isNgAmplifier;
    private bool _isTimestamp1Invalid;
    private bool _isTimestamp2Invalid;
    private bool _isTimestamp3Invalid;
    private bool _isTimestamp4Invalid;
    private bool _isTimestamp5Invalid;
    private bool _isTimestamp6Invalid;
    private bool _isTimestamp7Invalid;
    private bool _isValueAInvalid;
    private bool _isValueBInvalid;
    private bool _isValueCInvalid;
    private bool _isValueDInvalid;
    private bool _isValueEInvalid;
    private bool _isPowerLogPathInvalid;
    private bool _isDevLogPathInvalid;
    private bool _isResultVisible;
    private string _statusMessage = string.Empty;
    private Brush _statusBrush = Brushes.Transparent;
    private Brush _resultBackground = TransparentBrush;

    public MainViewModel(IFileDialogService fileDialogService, PowerConsumptionCalculatorService calculatorService)
    {
        _fileDialogService = fileDialogService;
        _calculatorService = calculatorService;

        BrowsePowerLogCommand = new RelayCommand(BrowsePowerLog);
        BrowseDevLogCommand = new RelayCommand(BrowseDevLog);
        CalculateCommand = new RelayCommand(Calculate);
        ResetCommand = new RelayCommand(Reset);
    }

    public RelayCommand BrowsePowerLogCommand { get; }
    public RelayCommand BrowseDevLogCommand { get; }
    public RelayCommand CalculateCommand { get; }
    public RelayCommand ResetCommand { get; }

    public string Timestamp1 { get => _timestamp1; set => SetProperty(ref _timestamp1, value); }
    public string Timestamp2 { get => _timestamp2; set => SetProperty(ref _timestamp2, value); }
    public string Timestamp3 { get => _timestamp3; set => SetProperty(ref _timestamp3, value); }
    public string Timestamp4 { get => _timestamp4; set => SetProperty(ref _timestamp4, value); }
    public string Timestamp5 { get => _timestamp5; set => SetProperty(ref _timestamp5, value); }
    public string Timestamp6 { get => _timestamp6; set => SetProperty(ref _timestamp6, value); }
    public string Timestamp7 { get => _timestamp7; set => SetProperty(ref _timestamp7, value); }
    public string ValueA { get => _valueA; set => SetProperty(ref _valueA, value); }
    public string ValueB { get => _valueB; set => SetProperty(ref _valueB, value); }
    public string ValueC { get => _valueC; set => SetProperty(ref _valueC, value); }
    public string ValueD { get => _valueD; set => SetProperty(ref _valueD, value); }
    public string ValueE { get => _valueE; set => SetProperty(ref _valueE, value); }
    public string PowerLogPath { get => _powerLogPath; set => SetProperty(ref _powerLogPath, value); }
    public string DevLogPath { get => _devLogPath; set => SetProperty(ref _devLogPath, value); }
    public bool IsNgAmplifier { get => _isNgAmplifier; set => SetProperty(ref _isNgAmplifier, value); }

    public bool IsTimestamp1Invalid { get => _isTimestamp1Invalid; set => SetProperty(ref _isTimestamp1Invalid, value); }
    public bool IsTimestamp2Invalid { get => _isTimestamp2Invalid; set => SetProperty(ref _isTimestamp2Invalid, value); }
    public bool IsTimestamp3Invalid { get => _isTimestamp3Invalid; set => SetProperty(ref _isTimestamp3Invalid, value); }
    public bool IsTimestamp4Invalid { get => _isTimestamp4Invalid; set => SetProperty(ref _isTimestamp4Invalid, value); }
    public bool IsTimestamp5Invalid { get => _isTimestamp5Invalid; set => SetProperty(ref _isTimestamp5Invalid, value); }
    public bool IsTimestamp6Invalid { get => _isTimestamp6Invalid; set => SetProperty(ref _isTimestamp6Invalid, value); }
    public bool IsTimestamp7Invalid { get => _isTimestamp7Invalid; set => SetProperty(ref _isTimestamp7Invalid, value); }
    public bool IsValueAInvalid { get => _isValueAInvalid; set => SetProperty(ref _isValueAInvalid, value); }
    public bool IsValueBInvalid { get => _isValueBInvalid; set => SetProperty(ref _isValueBInvalid, value); }
    public bool IsValueCInvalid { get => _isValueCInvalid; set => SetProperty(ref _isValueCInvalid, value); }
    public bool IsValueDInvalid { get => _isValueDInvalid; set => SetProperty(ref _isValueDInvalid, value); }
    public bool IsValueEInvalid { get => _isValueEInvalid; set => SetProperty(ref _isValueEInvalid, value); }
    public bool IsPowerLogPathInvalid { get => _isPowerLogPathInvalid; set => SetProperty(ref _isPowerLogPathInvalid, value); }
    public bool IsDevLogPathInvalid { get => _isDevLogPathInvalid; set => SetProperty(ref _isDevLogPathInvalid, value); }
    public bool IsResultVisible { get => _isResultVisible; set => SetProperty(ref _isResultVisible, value); }
    public string StatusMessage { get => _statusMessage; set => SetProperty(ref _statusMessage, value); }
    public Brush StatusBrush { get => _statusBrush; set => SetProperty(ref _statusBrush, value); }
    public Brush ResultBackground { get => _resultBackground; set => SetProperty(ref _resultBackground, value); }

    private void BrowsePowerLog()
    {
        var path = _fileDialogService.BrowseFile("Select Power Logs File");
        if (path is null)
        {
            return;
        }

        PowerLogPath = path;
        IsPowerLogPathInvalid = false;
    }

    private void BrowseDevLog()
    {
        var path = _fileDialogService.BrowseFile("Select Devlog File");
        if (path is null)
        {
            return;
        }

        DevLogPath = path;
        IsDevLogPathInvalid = false;
    }

    private void Calculate()
    {
        ResetValidation();

        var isValid = true;
        isValid &= ValidateRequiredText(ValueA, nameof(IsValueAInvalid));
        isValid &= ValidateRequiredText(ValueB, nameof(IsValueBInvalid));
        isValid &= ValidateRequiredText(ValueC, nameof(IsValueCInvalid));
        isValid &= ValidateRequiredText(ValueD, nameof(IsValueDInvalid));
        isValid &= ValidateRequiredText(ValueE, nameof(IsValueEInvalid));
        isValid &= ValidateRequiredText(PowerLogPath, nameof(IsPowerLogPathInvalid));
        isValid &= ValidateRequiredText(DevLogPath, nameof(IsDevLogPathInvalid));

        if (!TryParseRequiredDouble(ValueA, nameof(IsValueAInvalid), out var expectedAverageScanPower)) isValid = false;
        if (!TryParseRequiredDouble(ValueB, nameof(IsValueBInvalid), out var expectedReadyToScanPower)) isValid = false;
        if (!TryParseRequiredDouble(ValueC, nameof(IsValueCInvalid), out var expectedStandbyPower)) isValid = false;
        if (!TryParseRequiredDouble(ValueD, nameof(IsValueDInvalid), out var expectedOffModePower)) isValid = false;

        if (!isValid)
        {
            ShowResult("Please fill in all required fields with valid numeric values.", Brushes.Red, new SolidColorBrush(Color.FromRgb(0xFF, 0xC0, 0xC0)));
            return;
        }

        var request = new CalculationRequest(
            Timestamp1,
            Timestamp2,
            Timestamp3,
            Timestamp4,
            Timestamp5,
            Timestamp6,
            Timestamp7,
            ValueA,
            ValueB,
            ValueC,
            ValueD,
            ValueE,
            PowerLogPath,
            DevLogPath,
            IsNgAmplifier,
            expectedAverageScanPower,
            expectedReadyToScanPower,
            expectedStandbyPower,
            expectedOffModePower,
            expectedAverageScanPower);

        var response = _calculatorService.Calculate(request);

        if (!response.IsSuccess && !string.IsNullOrWhiteSpace(response.InvalidFieldKey))
        {
            SetInvalidFlag(response.InvalidFieldKey, true);
        }

        ShowResult(response.StatusMessage, response.StatusBrush, response.BackgroundBrush);
    }

    private void Reset()
    {
        Timestamp1 = string.Empty;
        Timestamp2 = string.Empty;
        Timestamp3 = string.Empty;
        Timestamp4 = string.Empty;
        Timestamp5 = string.Empty;
        Timestamp6 = string.Empty;
        Timestamp7 = string.Empty;
        ValueA = string.Empty;
        ValueB = string.Empty;
        ValueC = string.Empty;
        ValueD = string.Empty;
        ValueE = string.Empty;
        PowerLogPath = string.Empty;
        DevLogPath = string.Empty;
        IsNgAmplifier = false;
        StatusMessage = string.Empty;
        StatusBrush = Brushes.Transparent;
        ResultBackground = TransparentBrush;
        IsResultVisible = false;
        ResetValidation();
    }

    private void ShowResult(string message, Brush statusBrush, Brush backgroundBrush)
    {
        StatusMessage = message;
        StatusBrush = statusBrush;
        ResultBackground = backgroundBrush;
        IsResultVisible = true;
    }

    private bool ValidateRequiredText(string value, string invalidPropertyName)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        SetInvalidFlag(invalidPropertyName, true);
        return false;
    }

    private bool TryParseRequiredDouble(string value, string invalidPropertyName, out double parsed)
    {
        parsed = 0;
        if (string.IsNullOrWhiteSpace(value))
        {
            SetInvalidFlag(invalidPropertyName, true);
            return false;
        }

        if (double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out parsed)
            || double.TryParse(value, NumberStyles.Any, CultureInfo.CurrentCulture, out parsed)
            || double.TryParse(value.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out parsed))
        {
            return true;
        }

        SetInvalidFlag(invalidPropertyName, true);
        return false;
    }

    private void ResetValidation()
    {
        IsTimestamp1Invalid = false;
        IsTimestamp2Invalid = false;
        IsTimestamp3Invalid = false;
        IsTimestamp4Invalid = false;
        IsTimestamp5Invalid = false;
        IsTimestamp6Invalid = false;
        IsTimestamp7Invalid = false;
        IsValueAInvalid = false;
        IsValueBInvalid = false;
        IsValueCInvalid = false;
        IsValueDInvalid = false;
        IsValueEInvalid = false;
        IsPowerLogPathInvalid = false;
        IsDevLogPathInvalid = false;
    }

    private void SetInvalidFlag(string propertyName, bool value)
    {
        switch (propertyName)
        {
            case nameof(IsTimestamp1Invalid):
            case "Timestamp1":
                IsTimestamp1Invalid = value;
                break;
            case nameof(IsTimestamp2Invalid):
            case "Timestamp2":
                IsTimestamp2Invalid = value;
                break;
            case nameof(IsTimestamp3Invalid):
            case "Timestamp3":
                IsTimestamp3Invalid = value;
                break;
            case nameof(IsTimestamp4Invalid):
            case "Timestamp4":
                IsTimestamp4Invalid = value;
                break;
            case nameof(IsTimestamp5Invalid):
            case "Timestamp5":
                IsTimestamp5Invalid = value;
                break;
            case nameof(IsTimestamp6Invalid):
            case "Timestamp6":
                IsTimestamp6Invalid = value;
                break;
            case nameof(IsTimestamp7Invalid):
            case "Timestamp7":
                IsTimestamp7Invalid = value;
                break;
            case nameof(IsValueAInvalid):
                IsValueAInvalid = value;
                break;
            case nameof(IsValueBInvalid):
                IsValueBInvalid = value;
                break;
            case nameof(IsValueCInvalid):
                IsValueCInvalid = value;
                break;
            case nameof(IsValueDInvalid):
                IsValueDInvalid = value;
                break;
            case nameof(IsValueEInvalid):
                IsValueEInvalid = value;
                break;
            case nameof(IsPowerLogPathInvalid):
                IsPowerLogPathInvalid = value;
                break;
            case nameof(IsDevLogPathInvalid):
                IsDevLogPathInvalid = value;
                break;
        }
    }
}
