
using System.Linq;
using System.Xml;

namespace CollectFailedTestsToRerun
{
    public static class UpdateXML
    {
        public static void UpdateXMLWithLatestResults(string pathToResultXML)
        {
            string pathToTestResultXML = pathToResultXML; 

            var docAfterRerun = new XmlDocument();
            docAfterRerun.Load(@"../../TestResultAfterRerun.xml");

            var originalTestXmlFile = new XmlDocument();
            originalTestXmlFile.Load(pathToTestResultXML);

            var passedTestCases = docAfterRerun.SelectNodes("//test-case[@success=\"True\"]");

            if (passedTestCases.Count > 0)
            {
                for (int i = 0; i < passedTestCases.Count; i++)
                {
                    var currentNode = originalTestXmlFile
                        .SelectSingleNode($"//test-case[@name=\"{passedTestCases[i].Attributes[0].Value}\"]");

                    var nameValueListOfNode = currentNode.Attributes.GetNamedItem("name").Value.Split('.').ToList();

                    var testClassName = nameValueListOfNode[nameValueListOfNode.Count - 2];

                    originalTestXmlFile
                        .SelectSingleNode($"//test-case[@name=\"{passedTestCases[i].Attributes[0].Value}\"]")
                        .Attributes["success"].Value = "True";
                    
                    originalTestXmlFile
                        .SelectSingleNode($"//test-case[@name=\"{passedTestCases[i].Attributes[0].Value}\"]")
                        .Attributes["result"].Value = "Success";

                    var testSuiteNode = originalTestXmlFile.SelectSingleNode($"//test-suite[@name=\"{testClassName}\"]/results");
                    var passedTestNodeInRerunTestResultFile =
                        docAfterRerun.SelectSingleNode(
                            $"//test-case[@name=\"{passedTestCases[i].Attributes[0].Value}\"]");

                    var importedNode = originalTestXmlFile.ImportNode(passedTestNodeInRerunTestResultFile, false);

                    testSuiteNode.ReplaceChild(importedNode, currentNode);

                    originalTestXmlFile.Save(pathToTestResultXML);
                }
            }
        }
    }
}
