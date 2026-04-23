using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace TimestampCalculator
{
    internal class FileOperation
    {
        /// <summary>
        /// Reads the time of a log entry containing a target string based on its occurrence
        /// </summary>
        /// <param name="filePath">Full path to the log file</param>
        /// <param name="targetString">Target string to search for in the log entries</param>
        /// <param name="occurrence">Occurrence of the target string to find (1 for first, 2 for second, -1 for last, etc.)</param>
        /// <returns>Returns the time (HH:mm:ss.ff) of the log entry if found, otherwise null</returns>
        public string? ReadDevLogCurrent(string filePath, string targetString, int occurrence)
        {
            if (!File.Exists(filePath) || occurrence == 0) return null;

            var lines = File.ReadLines(filePath).Where(l => l.Contains(targetString)).ToList();
            int index = occurrence > 0 ? occurrence - 1 : lines.Count + occurrence;

            return (index >= 0 && index < lines.Count) ? ExtractTimeFromLine(lines[index]) : null;
        }

        /// <summary>
        /// Searches for a log entry containing the target string that occurs after the last occurrence of an anchor string, and returns its time
        /// </summary>
        /// <param name="filePath">Full path to the log file</param>
        /// <param name="anchorString">Anchor string to identify the reference point in the log (e.g., "Scan completed")</param>
        /// <param name="targetString">Target string to search for in the log entries following the anchor</param>
        /// <returns>Returns the time (HH:mm:ss.ff) of the first log entry containing the target string that occurs after the last occurrence of the anchor string, otherwise null</returns>
        public string? GetSubsequentLog(string filePath, string anchorString, string targetString)
        {
            if (!File.Exists(filePath)) return null;

            var lines = File.ReadAllLines(filePath);
            int anchorIndex = Array.FindLastIndex(lines, l => l.Contains(anchorString));

            return anchorIndex < 0
                ? null
                : ExtractTimeFromLine(lines.Skip(anchorIndex + 1)
                                          .FirstOrDefault(l => l.Contains(targetString)));
        }

        private static string? ExtractTimeFromLine(string? line)
        {
            return line?.Split('\t').ElementAtOrDefault(2)?.Trim();
        }
        /// <summary>
        /// Method to create a csv file if it does not exist
        /// </summary>
        /// <param name="fileName">Input file name with extension</param>
        /// <param name="filePath">Path where the file needs to be created</param>
        public void CreateCSV(string fileName, string filePath)
        {
            Directory.CreateDirectory(filePath);
            string fullPath = Path.Combine(filePath, fileName);
            if (!File.Exists(fullPath)) File.WriteAllText(fullPath, "");
        }
        /// <summary>
        /// Method to read a cell value from csv file. Cell value should be in format A1, B2 etc.
        /// </summary>
        /// <param name="fileName">Input file name with extension</param>
        /// <param name="filePath">Path where the file is located</param>
        /// <param name="cell">Cell value in format A1, B2 etc.</param>
        /// <returns>String value read from the cell. Returns empty string if file or cell does not exist</returns>
        public string ReadCSV(string fileName, string filePath, string cell)
        {
            string fullPath = Path.Combine(filePath, fileName);
            if (!File.Exists(fullPath)) return "";

            int row, col; GetCellPosition(cell, out row, out col);
            var lines = File.ReadAllLines(fullPath);

            return row < lines.Length && col < lines[row].Split(',').Length
                ? lines[row].Split(',')[col]
                : "";
        }
        /// <summary>
        /// Method to write a value to a cell in csv file. Cell value should be in format A1, B2 etc. If the file or cell does not exist, it will be created.
        /// </summary>
        /// <param name="fileName">File name with extension</param>
        /// <param name="filePath">Path where the file is located</param>
        /// <param name="cell">Cell value in format A1, B2 etc.</param>
        /// <param name="value">Value to be written in the cell</param>
        public void WriteCSV(string fileName, string filePath, string cell, string value)
        {
            string fullPath = Path.Combine(filePath, fileName);
            int row, col; GetCellPosition(cell, out row, out col);

            var lines = EnsureRowSize(File.Exists(fullPath) ? File.ReadAllLines(fullPath) : new string[0], row);
            var columns = EnsureColumnSize(lines[row], col);

            columns[col] = value;
            lines[row] = string.Join(",", columns);
            File.WriteAllLines(fullPath, lines);
        }

        private void GetCellPosition(string cell, out int row, out int col)
        {
            row = int.Parse(Regex.Match(cell, @"\d+").Value) - 1;
            string column = Regex.Match(cell, @"[A-Za-z]+").Value.ToUpper();
            col = 0; foreach (char c in column) col = col * 26 + (c - 'A' + 1);
            col -= 1;
        }

        private string[] EnsureRowSize(string[] lines, int row)
        {
            if (lines.Length <= row) Array.Resize(ref lines, row + 1);
            if (lines[row] == null) lines[row] = "";
            return lines;
        }

        private string[] EnsureColumnSize(string line, int col)
        {
            var columns = line.Split(',');
            if (columns.Length <= col) Array.Resize(ref columns, col + 1);
            return columns;
        }
        /// <summary>
        /// Exports Dictionary<string,double> data to CSV with Time and Power columns
        /// </summary>
        public void ExportDataToCSV(string fileName, string filePath, Dictionary<string, double> data)
        {
            // Create CSV file
            CreateCSV(fileName, filePath);

            // Write headers
            WriteCSV(fileName, filePath, "A1", "Time");
            WriteCSV(fileName, filePath, "B1", "Power");

            int row = 2;

            foreach (var item in data)
            {
                WriteCSV(fileName, filePath, "A" + row, item.Key);          // Time
                WriteCSV(fileName, filePath, "B" + row, item.Value.ToString()); // Power

                row++;
            }
        }
        /// <summary>
        /// Fast method to export Dictionary<string,double> to CSV
        /// </summary>
        public void ExportDataToCSVFast(
    string fileName,
    string filePath,
    Dictionary<string, double> wFundData,
    Dictionary<string, double> activePowerData)
        {
            Directory.CreateDirectory(filePath);
            string fullPath = Path.Combine(filePath, fileName);

            List<string> lines = new List<string>();

            // Header
            lines.Add("Time,W Fund Total Avg,Active Power Total Avg");

            foreach (var item in wFundData)
            {
                string time = item.Key;
                double wFundValue = item.Value;

                double activePowerValue = 0;

                if (activePowerData.ContainsKey(time))
                {
                    activePowerValue = activePowerData[time];
                }

                lines.Add($"{time},{wFundValue},{activePowerValue}");
            }

            File.WriteAllLines(fullPath, lines);
        }
    }
}
