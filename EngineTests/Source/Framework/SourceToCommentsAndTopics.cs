/* 
 * Class: GregValure.NaturalDocs.EngineTests.Framework.SourceToCommentsAndTopics
 * ____________________________________________________________________________
 * 
 * A base class for automated tests where sample source files are loaded from a folder, converted to 
 * <PossibleDocumentationComments> and <Topics>, and the portions of them being tested are saved to files and 
 * compared to other files containing the expected result.
 * 
 *	 The benefit of this approach is that you never have to hand code the output.  You can run the tests without
 *	 an expected output file, look over the actual output file, and if it's acceptable rename it to become the
 * expected output file.
 * 
 * Usage:
 * 
 *		- Derive a class that has the [TestFixture] attribute.
 *		- Create a function with the [Test] attribute that calls <TestFolder()>, pointing it to the input files.
 *		- Define <OutputOf()> to convert some facet of the data to string output.
 *		- All files in the test folder in the format "[Test Name] - Input.[extension]" will be tested when NUnit runs.
 *		- A corresponding file "[Test Name] - Actual Output.txt" will be created for each one.
 *		- If it matches the contents of the file "[Test Name] - Expected Output.txt", the test will pass.  If it doesn't,
 *		  that file doesn't exist, or an exception was thrown, the test will fail.
 *		
 */

// This file is part of Natural Docs, which is Copyright © 2003-2012 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using GregValure.NaturalDocs.Engine;
using GregValure.NaturalDocs.Engine.Comments;
using GregValure.NaturalDocs.Engine.Languages;
using GregValure.NaturalDocs.Engine.Links;


namespace GregValure.NaturalDocs.EngineTests.Framework
	{
	public abstract class SourceToCommentsAndTopics
		{

		// Group: Functions
		// __________________________________________________________________________

		/* Constructor: SourceToCommentsAndTopics
		 */
		public SourceToCommentsAndTopics ()
			{
			}


		/* Function: OutputOf
		 * 
		 * Override this function to generate the output for the passed data.  The output should be whatever you're
		 * testing, so if you want to test prototype detection, return the prototype.  You have to account for the possibility
		 * of there being more than one topic in an input file, or none at all.
		 * 
		 * You do not need to worry about catching exceptions unless the test is supposed to trigger them.  Uncaught exceptions
		 * will be handled automatically and cause the test to fail.  If the exception was intended as part of correct operation then 
		 * you must catch it to prevent this.
		 * 
		 * This function should not return null or an empty string as part of a successful test.  Doing so will cause the test to fail.
		 * If a test is supposed to generated no output, return a string such as "test successful" instead.
		 */
		public abstract string OutputOf (IList<PossibleDocumentationComment> comments, IList<Topic> topics);


		/* Function: TestFolder
		 * 
		 * Tests all the input files contained in this folder.
		 * 
		 * If the test data folder is relative it will take the executing assembly path, skip up until it finds "Components", move 
		 * into the "EngineTests\Test Data" subfolder, and then make the path relative to that.  This is because it assumes all 
		 * the Natural Docs components will be subfolders of a shared Components folder, and Visual Studio or any other IDE
		 * is running an executable found inside a component's subfolder.
		 */
		public void TestFolder (Path testFolder, Path projectConfigFolder = default(Path))
			{
			List<TestResult> testResults = new List<TestResult>();
			int failureCount = 0;

			TestEngine.Start(testFolder, projectConfigFolder);

			try
				{
				// Iterate through files

				string[] files = System.IO.Directory.GetFiles(TestEngine.InputFolder);
				Test test = new Test();

				foreach (string file in files)
					{
					if (Test.IsInputFile(file))
						{
						test.Load(file);

						try
							{  
							Language language = Engine.Instance.Languages.FromExtension(test.InputFile.Extension);

							if (language == null)
								{  throw new Exception("Extension " + test.InputFile.Extension + " did not resolve to a language.");  }

							string code = System.IO.File.ReadAllText(test.InputFile);

							IList<PossibleDocumentationComment> comments = language.GetComments(code);

							IList<Topic> topics;
							LinkSet classParentLinks;
							language.Parse(code, -1, Engine.Delegates.NeverCancel, out topics, out classParentLinks);

							test.ActualOutput = OutputOf(comments, topics);  
							}
						catch (Exception e)
							{  test.TestException = e;  }

						test.SaveOutput();  // Even if an exception was thrown.
						testResults.Add(test.ToTestResult());

						if (test.Passed == false)
							{  failureCount++;  }
						}
					}
				}

			finally
				{
				TestEngine.Dispose();
				}


			// Build status message

			if (testResults.Count == 0)
				{
				Assert.Fail("There were no tests found in " + TestEngine.InputFolder);
				}
			else if (failureCount > 0)
				{
				StringBuilder message = new StringBuilder();
				message.Append(failureCount.ToString() + " out of " + testResults.Count + " test" + (testResults.Count == 1 ? "" : "s") + 
												  " failed for " + testFolder + ':');

				foreach (TestResult testResult in testResults)
					{  
					if (testResult.Passed == false)
						{  message.Append("\n - " + testResult.Name);  }
					}

				Assert.Fail(message.ToString());
				}
			}

		}

	}