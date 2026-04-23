using System.IO;
using System.Globalization;
using System.Text;
using System.Windows.Media;

namespace TimestampCalculator.Services;

internal sealed class PowerConsumptionCalculatorService
{
    private const string AvgScanPowerStartString = "Performance Logging [ScanExec] 0 begin";
    private const string AxisEnablePowerStartString = "Performance Logging [ScanExec] 0 event Scan completed";
    private const string AxisEnableStopStringNormal = "DAS(Device): GCI2Serial. Set gradamp standby, no check";
    private const string AxisEnableStopStringNg = "Q_GRDR_Gramp_NG(DeviceLog): /GRADIENT/GRAMP:State: { \"DeviceName\" : \"NG2250-XP\" , \"FromState\" : \"OperEnableActive\" , \"ToState\" : \"OperDisabled\" }";
    private const string ReadyToScanStopStringNormal = "DAS(Device): GCI2Serial. Set gradamp power save, no check";
    private const string ReadyToScanStopStringNg = "Q_GRDR_Gramp_NG(DeviceLog): /GRADIENT/GRAMP:State: { \"DeviceName\" : \"NG2250-XP\" , \"FromState\" : \"StandbyInTransit\" , \"ToState\" : \"StandbyActive\"";
    private const string StandbyPowerStopString = "AESCSTATE: Cryocompressor fault since";
    private const string CryoCompressorOffStopString = "AESCSTATE: Cryocompressor fault SOLVED";
    private const string StandbyPowerStartString = "Q_GRDR_GDDFamily(DeviceLog): /COOLING/LCC:State: { \"DeviceName\" : \"LCC\" , \"Status\" : \"LCC mode changed to \" , \"Standby\" }";
    private const string ResultFileName = "PowerConsumptionResults.csv";

    private static readonly Brush SuccessBrush = new SolidColorBrush(Color.FromRgb(0x16, 0xA3, 0x4A));
    private static readonly Brush ErrorBrush = new SolidColorBrush(Color.FromRgb(0xDC, 0x26, 0x26));
    private static readonly Brush SuccessBackground = new SolidColorBrush(Color.FromRgb(0xC0, 0xFF, 0xC0));
    private static readonly Brush ErrorBackground = new SolidColorBrush(Color.FromRgb(0xFF, 0xC0, 0xC0));

    private readonly FileOperation _fileOperation;

    public PowerConsumptionCalculatorService(FileOperation fileOperation)
    {
        _fileOperation = fileOperation;
    }

    public CalculationResponse Calculate(CalculationRequest request)
    {
        CalculationResponse? failureResponse = null;
        var axisEnableStopString = request.IsNgAmplifier ? AxisEnableStopStringNg : AxisEnableStopStringNormal;
        var readyToScanStopString = request.IsNgAmplifier ? ReadyToScanStopStringNg : ReadyToScanStopStringNormal;

        var avgScanPowerStart = request.Timestamp1;
        var axisEnableStart = request.Timestamp2;
        var readyToScanStart = request.Timestamp3;
        var amplifierStateStart = request.Timestamp4;
        var standbyPowerStart = request.Timestamp5;
        var cryoCompressorOffStart = request.Timestamp6;
        var cryoCompressorOffStop = request.Timestamp7;

        var avgScanPowerStop = axisEnableStart;
        var axisEnableStop = readyToScanStart;
        var readyToScanStop = amplifierStateStart;
        var amplifierStateStop = standbyPowerStart;
        var standbyPowerStop = cryoCompressorOffStart;

        if (!TryResolveTimestamp(ref avgScanPowerStart, () => ReadAndFormatLog(request.DevLogPath, AvgScanPowerStartString, 1), "Timestamp1",
                $"Could not find log entry for '{AvgScanPowerStartString}'. Please enter the Average Scan Power Start Time manually."))
        {
            return failureResponse!;
        }

        if (!TryResolveTimestamp(ref avgScanPowerStop, () => ReadAndFormatLog(request.DevLogPath, AxisEnablePowerStartString, -1), "Timestamp2",
                $"Could not find log entry for '{AxisEnablePowerStartString}'. Please enter the Axis Enable Power Start Time manually."))
        {
            return failureResponse!;
        }

        if (!TryResolveTimestamp(ref axisEnableStart, () => ReadAndFormatLog(request.DevLogPath, AxisEnablePowerStartString, -1), "Timestamp2",
                $"Could not find log entry for '{AxisEnablePowerStartString}'. Please enter the Axis Enable Power Start Time manually."))
        {
            return failureResponse!;
        }

        if (!TryResolveTimestamp(ref axisEnableStop, () => ReadAndFormatSubsequentLog(request.DevLogPath, AxisEnablePowerStartString, axisEnableStopString), "Timestamp3",
                $"Could not find log entry for '{axisEnableStopString}' following '{AxisEnablePowerStartString}'. Please enter the Ready to Scan Power Start Time manually."))
        {
            return failureResponse!;
        }

        if (!TryResolveTimestamp(ref readyToScanStart, () => ReadAndFormatSubsequentLog(request.DevLogPath, AxisEnablePowerStartString, axisEnableStopString), "Timestamp3",
                $"Could not find log entry for '{axisEnableStopString}' following '{AxisEnablePowerStartString}'. Please enter the Ready to Scan Power Start Time manually."))
        {
            return failureResponse!;
        }

        if (!TryResolveTimestamp(ref readyToScanStop, () => ReadAndFormatSubsequentLog(request.DevLogPath, AxisEnablePowerStartString, readyToScanStopString), "Timestamp4",
                $"Could not find log entry for '{readyToScanStopString}' following '{AxisEnablePowerStartString}'. Please enter the Amplifier State Start Time manually."))
        {
            return failureResponse!;
        }

        if (!TryResolveTimestamp(ref amplifierStateStart, () => ReadAndFormatSubsequentLog(request.DevLogPath, AxisEnablePowerStartString, readyToScanStopString), "Timestamp4",
                $"Could not find log entry for '{readyToScanStopString}' following '{AxisEnablePowerStartString}'. Please enter the Amplifier State Start Time manually."))
        {
            return failureResponse!;
        }

        if (!TryResolveTimestamp(ref amplifierStateStop, () => ReadAndFormatLog(request.DevLogPath, StandbyPowerStartString, 1), "Timestamp5",
                $"Could not find log entry for '{StandbyPowerStartString}'. Please enter the Standby Power Start Time manually."))
        {
            return failureResponse!;
        }

        if (!TryResolveTimestamp(ref standbyPowerStart, () => ReadAndFormatLog(request.DevLogPath, StandbyPowerStartString, 1), "Timestamp5",
                $"Could not find log entry for '{StandbyPowerStartString}'. Please enter the Standby Power Start Time manually."))
        {
            return failureResponse!;
        }

        if (!TryResolveTimestamp(ref standbyPowerStop, () => ReadAndFormatLog(request.DevLogPath, StandbyPowerStopString, 1), "Timestamp6",
                $"Could not find log entry for '{StandbyPowerStopString}'. Please enter the Cryo Compressor Off Power Start Time manually."))
        {
            return failureResponse!;
        }

        if (!TryResolveTimestamp(ref cryoCompressorOffStart, () => ReadAndFormatLog(request.DevLogPath, StandbyPowerStopString, 1), "Timestamp6",
                $"Could not find log entry for '{StandbyPowerStopString}'. Please enter the Cryo Compressor Off Power Start Time manually."))
        {
            return failureResponse!;
        }

        if (!TryResolveTimestamp(ref cryoCompressorOffStop, () => ReadAndFormatLog(request.DevLogPath, CryoCompressorOffStopString, 1), "Timestamp7",
                $"Could not find log entry for '{CryoCompressorOffStopString}'. Please enter the Cryo On Power Time manually."))
        {
            return failureResponse!;
        }

        var avgScanPower = GetTimeDifference(avgScanPowerStart, avgScanPowerStop);
        var axisEnable = GetTimeDifference(axisEnableStart, axisEnableStop);
        var readyToScan = GetTimeDifference(readyToScanStart, readyToScanStop);
        var amplifierState = GetTimeDifference(amplifierStateStart, amplifierStateStop);
        var standbyPower = GetTimeDifference(standbyPowerStart, standbyPowerStop);
        var cryoCompressorOff = GetTimeDifference(cryoCompressorOffStart, cryoCompressorOffStop);

        var wFundData = ReadTimeAndPower(request.PowerLogPath);
        var activePowerData = ReadTimeAndActivePower(request.PowerLogPath);

        var avgScanPowerValue = GetAverage(activePowerData, avgScanPowerStart, avgScanPowerStop);
        var axisEnablePowerValue = GetAverage(activePowerData, axisEnableStart, axisEnableStop);
        var readyToScanPowerValue = GetAverage(activePowerData, readyToScanStart, readyToScanStop);
        var amplifierStatePowerValue = GetAverage(activePowerData, amplifierStateStart, amplifierStateStop);
        var standbyPowerValue = GetAverage(activePowerData, standbyPowerStart, standbyPowerStop);
        var cryoCompressorOffPowerValue = GetAverage(activePowerData, cryoCompressorOffStart, cryoCompressorOffStop);
        var offModePower = standbyPowerValue - cryoCompressorOffPowerValue;
        var lccPower = amplifierStatePowerValue - standbyPowerValue;
        var standbyPowerStopWasAutoResolved = string.IsNullOrWhiteSpace(request.Timestamp6);

        var outputFileName = $"{request.ValueE}_{ResultFileName}";
        var outputDirectory = Directory.GetCurrentDirectory();

        _fileOperation.ExportDataToCSVFast(outputFileName, outputDirectory, wFundData, activePowerData);
        _fileOperation.WriteCSV(outputFileName, outputDirectory, "E1", request.ValueE);
        _fileOperation.WriteCSV(outputFileName, outputDirectory, "F1", "Start cal time");
        _fileOperation.WriteCSV(outputFileName, outputDirectory, "G1", "Stop cal time");
        _fileOperation.WriteCSV(outputFileName, outputDirectory, "H1", "Period");
        _fileOperation.WriteCSV(outputFileName, outputDirectory, "I1", "Average Power");
        _fileOperation.WriteCSV(outputFileName, outputDirectory, "J1", "Requirement");
        _fileOperation.WriteCSV(outputFileName, outputDirectory, "K1", "Result");
        _fileOperation.WriteCSV(outputFileName, outputDirectory, "E2", "Average scan power before correction");
        _fileOperation.WriteCSV(outputFileName, outputDirectory, "E3", "Axis enable power");
        _fileOperation.WriteCSV(outputFileName, outputDirectory, "E4", "Ready to scan power");
        _fileOperation.WriteCSV(outputFileName, outputDirectory, "E5", "Amplifier: Idle, LCC:Run,Cryo:Run");
        _fileOperation.WriteCSV(outputFileName, outputDirectory, "E6", "Standby power");
        _fileOperation.WriteCSV(outputFileName, outputDirectory, "E7", "Cryo compressor off power");
        _fileOperation.WriteCSV(outputFileName, outputDirectory, "E8", "Off mode power =  standby power - Cryo compressor off power =");
        _fileOperation.WriteCSV(outputFileName, outputDirectory, "E9", "LCC power =  Amplifier: Idle LCC:Run Cryo:Run - StandbyPower");
        _fileOperation.WriteCSV(outputFileName, outputDirectory, "E10", "P_EC");
        _fileOperation.WriteCSV(outputFileName, outputDirectory, "E11", "T_EC");
        _fileOperation.WriteCSV(outputFileName, outputDirectory, "E12", "Corrected Power = (T_EC * P_EC + 180*P_axis enable  + 39 * (P_axis enable + 0.3)) / (T_EC + 180 + 39)");
        _fileOperation.WriteCSV(outputFileName, outputDirectory, "F2", CsvFormatted(avgScanPowerStart));
        _fileOperation.WriteCSV(outputFileName, outputDirectory, "G2", ReduceOneSecond(CsvFormatted(avgScanPowerStop)));
        _fileOperation.WriteCSV(outputFileName, outputDirectory, "F3", CsvFormatted(axisEnableStart));
        _fileOperation.WriteCSV(outputFileName, outputDirectory, "G3", ReduceOneSecond(CsvFormatted(axisEnableStop)));
        _fileOperation.WriteCSV(outputFileName, outputDirectory, "F4", CsvFormatted(readyToScanStart));
        _fileOperation.WriteCSV(outputFileName, outputDirectory, "G4", ReduceOneSecond(CsvFormatted(readyToScanStop)));
        _fileOperation.WriteCSV(outputFileName, outputDirectory, "F5", CsvFormatted(amplifierStateStart));
        _fileOperation.WriteCSV(outputFileName, outputDirectory, "G5", ReduceOneSecond(CsvFormatted(amplifierStateStop)));
        _fileOperation.WriteCSV(outputFileName, outputDirectory, "F6", CsvFormatted(standbyPowerStart));

        if (standbyPowerStopWasAutoResolved)
        {
            _fileOperation.WriteCSV(outputFileName, outputDirectory, "G6", ReduceOneMinute(ReduceOneSecond(CsvFormatted(standbyPowerStop))));
            _fileOperation.WriteCSV(outputFileName, outputDirectory, "F7", ReduceOneMinute(CsvFormatted(cryoCompressorOffStart)));
        }
        else
        {
            _fileOperation.WriteCSV(outputFileName, outputDirectory, "G6", ReduceOneSecond(CsvFormatted(standbyPowerStop)));
            _fileOperation.WriteCSV(outputFileName, outputDirectory, "F7", CsvFormatted(cryoCompressorOffStart));
        }

        _fileOperation.WriteCSV(outputFileName, outputDirectory, "G7", CsvFormatted(cryoCompressorOffStop));
        _fileOperation.WriteCSV(outputFileName, outputDirectory, "H2", avgScanPower);
        _fileOperation.WriteCSV(outputFileName, outputDirectory, "H3", axisEnable);
        _fileOperation.WriteCSV(outputFileName, outputDirectory, "H4", readyToScan);
        _fileOperation.WriteCSV(outputFileName, outputDirectory, "H5", amplifierState);
        _fileOperation.WriteCSV(outputFileName, outputDirectory, "H6", standbyPower);
        _fileOperation.WriteCSV(outputFileName, outputDirectory, "H7", cryoCompressorOff);
        _fileOperation.WriteCSV(outputFileName, outputDirectory, "I2", ((double)avgScanPowerValue / 1000).ToString(CultureInfo.InvariantCulture));
        _fileOperation.WriteCSV(outputFileName, outputDirectory, "I3", ((double)axisEnablePowerValue / 1000).ToString(CultureInfo.InvariantCulture));
        _fileOperation.WriteCSV(outputFileName, outputDirectory, "I4", ((double)readyToScanPowerValue / 1000).ToString(CultureInfo.InvariantCulture));
        _fileOperation.WriteCSV(outputFileName, outputDirectory, "I5", ((double)amplifierStatePowerValue / 1000).ToString(CultureInfo.InvariantCulture));
        _fileOperation.WriteCSV(outputFileName, outputDirectory, "I6", ((double)standbyPowerValue / 1000).ToString(CultureInfo.InvariantCulture));
        _fileOperation.WriteCSV(outputFileName, outputDirectory, "I7", ((double)cryoCompressorOffPowerValue / 1000).ToString(CultureInfo.InvariantCulture));
        _fileOperation.WriteCSV(outputFileName, outputDirectory, "I8", ((double)offModePower / 1000).ToString(CultureInfo.InvariantCulture));
        _fileOperation.WriteCSV(outputFileName, outputDirectory, "I9", ((double)lccPower / 1000).ToString(CultureInfo.InvariantCulture));
        _fileOperation.WriteCSV(outputFileName, outputDirectory, "F10", ((double)avgScanPowerValue / 1000).ToString(CultureInfo.InvariantCulture));
        _fileOperation.WriteCSV(outputFileName, outputDirectory, "F11", ConvertToSeconds(avgScanPower).ToString(CultureInfo.InvariantCulture));

        var correctedPower = (ConvertToSeconds(avgScanPower) * avgScanPowerValue + 180 * axisEnablePowerValue + 39 * (axisEnablePowerValue + 300))
            / (double)(ConvertToSeconds(avgScanPower) + 180 + 39);

        _fileOperation.WriteCSV(outputFileName, outputDirectory, "I12", (correctedPower / 1000).ToString(CultureInfo.InvariantCulture));
        _fileOperation.WriteCSV(outputFileName, outputDirectory, "J2", request.ExpectedAverageScanPower.ToString(CultureInfo.InvariantCulture));
        _fileOperation.WriteCSV(outputFileName, outputDirectory, "K2", avgScanPowerValue <= request.ExpectedAverageScanPower * 1000 ? "Pass" : "Fail");
        _fileOperation.WriteCSV(outputFileName, outputDirectory, "J4", request.ExpectedReadyToScanPower.ToString(CultureInfo.InvariantCulture));
        _fileOperation.WriteCSV(outputFileName, outputDirectory, "K4", readyToScanPowerValue <= request.ExpectedReadyToScanPower * 1000 ? "Pass" : "Fail");
        _fileOperation.WriteCSV(outputFileName, outputDirectory, "J6", request.ExpectedStandbyPower.ToString(CultureInfo.InvariantCulture));
        _fileOperation.WriteCSV(outputFileName, outputDirectory, "K6", standbyPowerValue <= request.ExpectedStandbyPower * 1000 ? "Pass" : "Fail");
        _fileOperation.WriteCSV(outputFileName, outputDirectory, "J8", request.ExpectedOffModePower.ToString(CultureInfo.InvariantCulture));
        _fileOperation.WriteCSV(outputFileName, outputDirectory, "K8", offModePower <= request.ExpectedOffModePower * 1000 ? "Pass" : "Fail");
        _fileOperation.WriteCSV(outputFileName, outputDirectory, "J12", request.ExpectedCorrectedPower.ToString(CultureInfo.InvariantCulture));
        _fileOperation.WriteCSV(outputFileName, outputDirectory, "K12", correctedPower <= request.ExpectedCorrectedPower * 1000 ? "Pass" : "Fail");

        return new CalculationResponse(
            true,
            Path.Combine(outputDirectory, outputFileName),
            null,
            null,
            SuccessBrush,
            SuccessBackground);

        bool TryResolveTimestamp(ref string timestamp, Func<string> resolver, string fieldKey, string errorMessage)
        {
            if (!string.IsNullOrWhiteSpace(timestamp))
            {
                return true;
            }

            try
            {
                timestamp = resolver();
                return true;
            }
            catch
            {
                failureResponse = new CalculationResponse(
                    false,
                    errorMessage,
                    fieldKey,
                    null,
                    ErrorBrush,
                    ErrorBackground);
                return false;
            }
        }
    }

    private string ReadAndFormatLog(string filePath, string target, int occurrence)
    {
        var value = _fileOperation.ReadDevLogCurrent(filePath, target, occurrence);
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException("Log entry not found.");
        }

        return DateTime.Parse(value, CultureInfo.InvariantCulture).ToString("h:mm:ss tt", CultureInfo.InvariantCulture);
    }

    private string ReadAndFormatSubsequentLog(string filePath, string anchor, string target)
    {
        var value = _fileOperation.GetSubsequentLog(filePath, anchor, target);
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException("Log entry not found.");
        }

        return DateTime.Parse(value, CultureInfo.InvariantCulture).ToString("h:mm:ss tt", CultureInfo.InvariantCulture);
    }

    private static string CsvFormatted(string input) =>
        DateTime.Parse(input, CultureInfo.InvariantCulture).ToString("h:mm:ss tt", CultureInfo.InvariantCulture);

    private static string GetTimeDifference(string startTimeStr, string stopTimeStr)
    {
        var start = DateTime.Parse(startTimeStr, CultureInfo.InvariantCulture);
        var stop = DateTime.Parse(stopTimeStr, CultureInfo.InvariantCulture);
        var difference = stop - start;

        return string.Format(CultureInfo.InvariantCulture, "{0:00}:{1:00}:{2:00}",
            (int)difference.TotalHours,
            difference.Minutes,
            difference.Seconds);
    }

    private static int ConvertToSeconds(string time)
    {
        if (string.IsNullOrWhiteSpace(time))
        {
            return 0;
        }

        var parts = time.Split(':');
        return int.Parse(parts[0], CultureInfo.InvariantCulture) * 3600
            + int.Parse(parts[1], CultureInfo.InvariantCulture) * 60
            + int.Parse(parts[2], CultureInfo.InvariantCulture);
    }

    private static string ReduceOneSecond(string inputTime)
    {
        if (!DateTime.TryParseExact(inputTime, "h:mm:ss tt", CultureInfo.InvariantCulture, DateTimeStyles.None, out var time))
        {
            throw new FormatException("Invalid time format. Expected format: h:mm:ss tt.");
        }

        return time.AddSeconds(-1).ToString("h:mm:ss tt", CultureInfo.InvariantCulture);
    }

    private static string ReduceOneMinute(string inputTime)
    {
        if (!DateTime.TryParseExact(inputTime, "h:mm:ss tt", CultureInfo.InvariantCulture, DateTimeStyles.None, out var time))
        {
            throw new FormatException("Invalid time format. Expected format: h:mm:ss tt.");
        }

        return time.AddMinutes(-1).ToString("h:mm:ss tt", CultureInfo.InvariantCulture);
    }

    private static int GetAverage(Dictionary<string, double> data, string startTime, string endTime)
    {
        var start = DateTime.Parse(startTime, CultureInfo.InvariantCulture);
        var end = DateTime.Parse(endTime, CultureInfo.InvariantCulture);

        var values = data
            .Where(x => DateTime.Parse(x.Key, CultureInfo.InvariantCulture) >= start && DateTime.Parse(x.Key, CultureInfo.InvariantCulture) <= end)
            .Select(x => x.Value)
            .ToList();

        return values.Count == 0 ? 0 : (int)values.Average();
    }

    private static Dictionary<string, double> ReadTimeAndPower(string filePath) =>
        ReadTimeAndColumn(filePath, "W Fund Total Avg");

    private static Dictionary<string, double> ReadTimeAndActivePower(string filePath) =>
        ReadTimeAndColumn(filePath, "Active Power Total Avg");

    private static Dictionary<string, double> ReadTimeAndColumn(string filePath, string columnName)
    {
        var dict = new Dictionary<string, double>();

        if (!File.Exists(filePath))
        {
            return dict;
        }

        var lines = ReadAllLinesRobust(filePath);
        if (lines.Length == 0)
        {
            return dict;
        }

        lines = lines.Where(l => !string.IsNullOrWhiteSpace(l)).ToArray();
        if (lines.Length == 0)
        {
            return dict;
        }

        var headerLine = CleanText(lines[0]);
        var delimiter = DetectDelimiter(headerLine);
        var headers = SplitLine(headerLine, delimiter);
        var timeIndex = FindColumnIndex(headers, "Time");
        var powerIndex = FindColumnIndex(headers, columnName);

        if (timeIndex == -1 || powerIndex == -1)
        {
            return dict;
        }

        for (var i = 1; i < lines.Length; i++)
        {
            var line = CleanText(lines[i]);
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var cols = SplitLine(line, delimiter);
            if (cols.Length <= Math.Max(timeIndex, powerIndex))
            {
                continue;
            }

            var time = cols[timeIndex].Trim().Trim('"');
            var powerText = cols[powerIndex].Trim().Trim('"');
            if (string.IsNullOrWhiteSpace(time) || string.IsNullOrWhiteSpace(powerText))
            {
                continue;
            }

            var extractedTime = ExtractTime(time);
            if (TryParseDoubleRobust(powerText, out var power) && !dict.ContainsKey(extractedTime))
            {
                dict.Add(extractedTime, Math.Abs(power));
            }
        }

        return dict;
    }

    private static string[] ReadAllLinesRobust(string filePath)
    {
        var encodings = new[]
        {
            Encoding.UTF8,
            Encoding.Unicode,
            Encoding.BigEndianUnicode,
            Encoding.UTF32,
            Encoding.ASCII,
            Encoding.Default,
        };

        foreach (var encoding in encodings)
        {
            try
            {
                var lines = File.ReadAllLines(filePath, encoding);
                if (lines.Length > 0 && CleanText(lines[0]).IndexOf("Time", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return lines;
                }
            }
            catch
            {
            }
        }

        using var reader = new StreamReader(filePath, true);
        var content = reader.ReadToEnd();
        return string.IsNullOrWhiteSpace(content)
            ? Array.Empty<string>()
            : content.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.None);
    }

    private static string CleanText(string input) =>
        string.IsNullOrEmpty(input) ? string.Empty : input.Replace("\uFEFF", string.Empty).Replace("\0", string.Empty).Trim();

    private static char DetectDelimiter(string line)
    {
        var tabCount = line.Count(c => c == '\t');
        var commaCount = line.Count(c => c == ',');
        var semicolonCount = line.Count(c => c == ';');

        if (tabCount >= commaCount && tabCount >= semicolonCount && tabCount > 0)
        {
            return '\t';
        }

        if (commaCount >= semicolonCount && commaCount > 0)
        {
            return ',';
        }

        if (semicolonCount > 0)
        {
            return ';';
        }

        return '\t';
    }

    private static string[] SplitLine(string line, char delimiter) =>
        line.Split(delimiter).Select(x => CleanText(x).Trim().Trim('"')).ToArray();

    private static int FindColumnIndex(string[] headers, string targetHeader)
    {
        for (var i = 0; i < headers.Length; i++)
        {
            if (headers[i].IndexOf(targetHeader, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return i;
            }
        }

        return -1;
    }

    private static string ExtractTime(string time)
    {
        if (string.IsNullOrWhiteSpace(time))
        {
            return string.Empty;
        }

        time = time.Trim();
        var amIndex = time.IndexOf("AM", StringComparison.OrdinalIgnoreCase);
        var pmIndex = time.IndexOf("PM", StringComparison.OrdinalIgnoreCase);

        if (amIndex >= 0)
        {
            return time[..(amIndex + 2)].Trim();
        }

        if (pmIndex >= 0)
        {
            return time[..(pmIndex + 2)].Trim();
        }

        return time;
    }

    private static bool TryParseDoubleRobust(string input, out double value)
    {
        value = 0;

        if (string.IsNullOrWhiteSpace(input))
        {
            return false;
        }

        input = input.Trim().Trim('"').Replace(" ", string.Empty);

        return double.TryParse(input, NumberStyles.Any, CultureInfo.InvariantCulture, out value)
            || double.TryParse(input, NumberStyles.Any, CultureInfo.CurrentCulture, out value)
            || double.TryParse(input.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out value);
    }
}

public sealed record CalculationRequest(
    string Timestamp1,
    string Timestamp2,
    string Timestamp3,
    string Timestamp4,
    string Timestamp5,
    string Timestamp6,
    string Timestamp7,
    string ValueA,
    string ValueB,
    string ValueC,
    string ValueD,
    string ValueE,
    string PowerLogPath,
    string DevLogPath,
    bool IsNgAmplifier,
    double ExpectedAverageScanPower,
    double ExpectedReadyToScanPower,
    double ExpectedStandbyPower,
    double ExpectedOffModePower,
    double ExpectedCorrectedPower);

public sealed record CalculationResponse(
    bool IsSuccess,
    string StatusMessage,
    string? InvalidFieldKey,
    string? OutputPath,
    Brush StatusBrush,
    Brush BackgroundBrush);
