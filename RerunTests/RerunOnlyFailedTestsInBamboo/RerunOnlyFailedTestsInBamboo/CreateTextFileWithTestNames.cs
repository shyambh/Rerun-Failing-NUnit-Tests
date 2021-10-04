using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Xml;
using static System.String;

namespace CollectFailedTestsToRerun
{
    /// <summary>
    /// This program scans the TestResult.xml which is produced after the regular NUnit console run. After the scan, a txt file (TestsToRerun.txt) is created containing a list of all the failed tests.
    /// </summary>
    /// 
    class CreateTextFileWithTestNames
    {
        static void Main(string[] args)
        {
            var fileWithTestsToRerun = @"../../TestsToRerun.txt";
            var testXmlFileAfterRerun = @"../../TestResultAfterRerun.xml";
            XmlNodeList failedTestCases = null;

            string pathToTestResultXML = Empty; // PathToTestResultXML=..\\TestResult.xml
            string pathToDll = Empty; 
            int rerunCount = 1;
            int failedTestThreshold = 10;

            try
            {
                foreach (var fullArgument in args)
                {
                    var argumentStartsWith = fullArgument.Substring(0, fullArgument.IndexOf('=') + 1);

                    switch (argumentStartsWith)
                    {
                        case "PathToTestResultXML=":
                            pathToTestResultXML = fullArgument.TrimStart(argumentStartsWith.ToCharArray());
                            break;
                        
                        case "PathToDll=":
                            pathToDll = fullArgument.TrimStart(argumentStartsWith.ToCharArray());
                            break;
                        
                        case "RerunCount=":
                            rerunCount = Int32.Parse(fullArgument.TrimStart(argumentStartsWith.ToCharArray()));
                            break; 
                        
                        case "FailedTestThreshold=":
                            failedTestThreshold = Int32.Parse(fullArgument.TrimStart(argumentStartsWith.ToCharArray()));
                            break;

                        default:
                            throw new ArgumentException(Format($"'{fullArgument}' is an invalid argument"));
                    }
                }

                //Rerunning the failed tests for the total of rerunCount value
                for (int count = 0 ; count < rerunCount ; count++) 
                {
                    Console.WriteLine($"******************* Test Rerun Iteration ({count+1} of {rerunCount}) with a failed test threshold of maximum {failedTestThreshold} failing tests *******************");

                    var testResultXML = new XmlDocument();

                    testResultXML.Load(pathToTestResultXML);

                    failedTestCases = testResultXML.SelectNodes("//test-case[@success=\"False\"]");
                    
                    Console.WriteLine($"The number of failing tests is {failedTestCases.Count}");

                    if (failedTestCases.Count > 0 && failedTestCases.Count <= failedTestThreshold)
                    {
                        if (File.Exists(fileWithTestsToRerun))
                            File.Delete(fileWithTestsToRerun);

                        Console.WriteLine("These are the failing tests : ");

                        #region Create TextFile Containing Failed Tests

                        //Enlist all the failing tests in the txt file
                        for (int i = 0; i < failedTestCases.Count; i++)
                        {
                            File.AppendAllText(fileWithTestsToRerun,
                                failedTestCases[i].Attributes[0].Value + Environment.NewLine);

                            Console.WriteLine(failedTestCases[i].Attributes[0].Value);
                        }

                        #endregion
                        
                        #region Rerun the Failing Tests From NUnit3 Console Runner

                        string command =
                            $@"nunit3-console.exe {pathToDll} --testlist={fileWithTestsToRerun} --result={testXmlFileAfterRerun};format=nunit2";

                        Console.Out.WriteLine($"Rerunning the failed tests\n {command}");

                        var processExitCode = ExecuteCommand(command);

                        Console.Out.WriteLine($"NUnit run completed with status code {processExitCode}");

                        #endregion

                        #region Update TestResult.xml with latest test results after the re-run

                        UpdateXML.UpdateXMLWithLatestResults(pathToTestResultXML);

                        #endregion
                        
                    }

                    else
                    {
                        string reasonOfNoRerun = failedTestCases.Count == 0 ? "All the tests have passed" : $"The failed number of tests ({failedTestCases.Count}) exceeds the threshold limit of ({failedTestThreshold})";
                        Console.WriteLine($"The test is not rerun because {reasonOfNoRerun}");

                        break;
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("There is an error.\n Below is the stack trace \n" + e.StackTrace);
            }
        }

        public static int? ExecuteCommand(string command)
        {
            try
            {
                System.Diagnostics.Process process = new Process();
                System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
                startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
                startInfo.FileName = "cmd.exe";
                startInfo.Arguments = $"/c {command}";
                startInfo.UseShellExecute = false;
                startInfo.RedirectStandardError = true;
                //startInfo.WorkingDirectory = @"..\\";

                process.StartInfo = startInfo;
                process.Start();

                process.WaitForExit(30000);

                string errors = process.StandardError.ReadToEnd();

                if (errors.Length > 0)
                {
                    Console.Out.WriteLine($"Errors received during the command run : \n {errors}");
                }

                var exitCode = process.ExitCode;
                process.Close();

                return exitCode;
            }
            catch (Exception e)
            {
                Console.WriteLine("There is an error in executing the command.\n Below is the stack trace \n" + e.StackTrace);
            }

            return null;
        }
    }
}
