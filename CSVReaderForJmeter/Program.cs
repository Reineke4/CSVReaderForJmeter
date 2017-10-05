using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CSVReaderForJmeter
{
    class Program
    {
        private static double _errorPercent;
        static int Main(string[] args)
        {
            string userPath;
            if (args.Length != 0)
            {
                userPath = args[0];
            }
            else
            {
                userPath = Directory.GetFiles(Directory.GetCurrentDirectory()).ToList().FirstOrDefault(file => file.EndsWith(".csv"));
                if (userPath == null)
                {
                    throw new FileNotFoundException("No csv files in program directory");
                }
            }
            string path = GetPathToFile(userPath);
            string[] separators = new[] { ",", "\r\n" };
            string[] columnHeaders = File.ReadAllLines(path)[0].Split(',');
            int headersCount = columnHeaders.Length;
            List<string> columnsContent = File.ReadAllText(path).Split(separators, StringSplitOptions.None).ToList();
            columnsContent.RemoveAt(columnsContent.Count - 1);
            columnsContent.RemoveRange(0, headersCount);
            Dictionary<string, string[]> normalizedCSV = WriteCsvToDictionary(columnHeaders, columnsContent.ToArray());
            if (!ValidateErrors(normalizedCSV, 1))
                Environment.Exit(1);
            return 0;
        }

        /// <summary>
        /// Get path for the CSV file
        /// </summary>
        /// <param name="userPath">Filename in program directory or fully qualified path name</param>
        private static string GetPathToFile(string userPath)
        {
            var currentDirectory = Directory.GetCurrentDirectory();
            if (userPath.Contains("/") || userPath.Contains("\\"))
            {
                return userPath;
            }
            else
            {
                return $"{currentDirectory}/{userPath}";
            }
        }

        /// <summary>
        /// Set full CSV data to the dictionary 
        /// </summary>
        /// <param name="columnsHeaders">CSV Headers</param>
        /// <param name="columnsContent">CSV Data Withot headers</param>
        /// <returns>CSV headers are keys, CSV columns body are string arrays</returns>
        private static Dictionary<string, string[]> WriteCsvToDictionary(string[] columnsHeaders, string[] columnsContent)
        {
            int topElementIndex = 0;
            Dictionary<string, string[]> result = new Dictionary<string, string[]>();
            foreach (var header in columnsHeaders)
            {
                string[] columnContent = GetSingleColumn(columnsContent, topElementIndex, columnsHeaders.Length);
                result.Add(header, columnContent);
                topElementIndex++;
            }
            return result;
        }

        /// <summary>
        /// Get appropriate data from CSV 
        /// </summary>
        /// <param name="columnsContent">Inner text of column except header</param>
        /// <param name="firstColumnIndex">Index of the top element in column</param>
        /// <param name="headerLenght">Count of headers</param>
        /// <returns>Appropriate column content, based on index</returns>
        private static string[] GetSingleColumn(string[] columnsContent, int firstColumnIndex, int headerLenght)
        {
            List<string> my = new List<string>();
            for (int i = 0; i < (columnsContent.Length / headerLenght); i++)
            {
                my.Add(columnsContent.ElementAt(firstColumnIndex));
                firstColumnIndex = firstColumnIndex + headerLenght;
            }
            return my.ToArray();
        }

        /// <summary>
        /// Check if the 'Failure Message' column contains any message
        /// </summary>
        [System.Obsolete("ValidateErrorMessages is deprecated, please use ValidateErrors instead")]
        private static bool ValidateErrorMessages(Dictionary<string, string[]> columnsFromFile)
        {
            string[] errors = columnsFromFile["failureMessage"];
            if (errors.Any(t => t.Trim() != ""))
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Check if the 'Satus' column contains failed records
        /// </summary>
        [System.Obsolete("ValidateStatus is deprecated, please use ValidateErrors instead")]
        private static bool ValidateStatus(Dictionary<string, string[]> columnsFromFile)
        {
            string[] errors = columnsFromFile["success"];
            if (errors.Any(t => t.ToLower().Trim() != "true"))
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Check the percent of server and client errors from test run
        /// </summary>
        /// <param name="countOfResponses">The full count of received responses</param>
        /// <param name="errorCodes">List of all selected error codes</param>
        private static void ValidateErrorPercents(int countOfResponses, List<string> errorCodes)
        {
            List<string> serverErrorsList = errorCodes.Select(el => el).Where(el => el.StartsWith("5")).ToList();
            List<string> clientErrorsList = errorCodes.Select(el => el).Where(el => el.StartsWith("4")).ToList();
            string percentOfServerErrors = null;
            string percentOfClientErrors = null;
            double serverErrors = 0;
            double clientErrors = 0;

            if (serverErrorsList.Count > 0)
            {
                serverErrors = serverErrorsList.Count * 100.0/countOfResponses;
                serverErrors = Math.Round(serverErrors, 2);
                percentOfServerErrors = $"The test run finished with { serverErrors } % of server errors";
            }
            if (serverErrorsList.Count > 0)
            {
                clientErrors = clientErrorsList.Count * 100/countOfResponses;
                clientErrors = Math.Round(clientErrors, 2);
                percentOfClientErrors = $"The test run finished with { clientErrors } % of client errors";
            }
            _errorPercent = serverErrors + clientErrors;
            File.WriteAllText($"{Directory.GetCurrentDirectory()}\\TestRunErrorInfo.txt", $"{percentOfServerErrors}\r\n{percentOfClientErrors}");
        }

        /// <summary>
        /// Check the percent of error status codes from dictionary 
        /// </summary>
        /// <param name="columnsFromFile">Target dictionary wiht 'responseCode' column</param>
        /// <param name="acceptableErrorPercent">Acceptable percent of failed responses</param>
        /// <returns>Return false if actual percent of error responses more than acceptable</returns>
        private static bool ValidateErrors(Dictionary<string, string[]> columnsFromFile, double acceptableErrorPercent)
        {
            string[] responses = columnsFromFile["responseCode"];
            List<string> errorsList = responses.Select(el => el).Where(el => el.StartsWith("5") || el.StartsWith("4")).ToList();

            if (errorsList.Count == 0)
            {
                return true;
            }
            ValidateErrorPercents(responses.Length, errorsList);
            if (_errorPercent > acceptableErrorPercent)
            {
                return false;
            }
            return true;
        }
    }
}
