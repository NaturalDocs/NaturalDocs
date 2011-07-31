/* 
 * Class: GregValure.NaturalDocs.EngineTests.Framework.SourceToHTML
 * ____________________________________________________________________________
 * 
 * A base class for unit tests where sample source files are run through Natural Docs normally, generating HTML
 * output, and then portions of the HTML are extracted, saved to files, and compared to other files containing
 * the expected output.
 * 
 * 
 * Usage:
 * 
 *		- Derive a class that has the [TestFixture] attribute.
 *		- Create a function with the [Test] attribute that calls <TestFolder()>, pointing it to the input files.
 *		- 
 *		- All files in the test folder in the format "[Test Name] - Input.[extension]" will have portions of its HTML
 *		  extracted when NUnit runs.
 *		- A corresponding file "[Test Name] - Actual Output.txt" will be created for each one.
 *		- If it matches the contents of the file "[Test Name] - Expected Output.txt", the test will pass.  If it doesn't or
 *		  that file doesn't exist, the test will fail.
 *		- The full generated output will remain in a folder called "HTML Output" if you want to look at it.
 *	
 * Tips:
 * 
 *		- If you need to define project config files for when Natural Docs runs, create a folder called "ND Config" in
 *		  the test folder.
 *		  
 *		- The benefit of this approach is that you never have to hand code the output.  You can run the tests without
 *		  an expected output file, look over the actual output file, and if it's acceptable rename it to become the
 *		  expected output file.
 *		
 */

// This file is part of Natural Docs, which is Copyright © 2003-2011 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using GregValure.NaturalDocs.Engine;


namespace GregValure.NaturalDocs.EngineTests.Framework
	{
	public abstract class SourceToHTML
		{

		// Group: Functions
		// __________________________________________________________________________

		/* Constructor: SourceToHTML
		 */
		public SourceToHTML ()
			{
			}


		/* Function: ExtractHTML
		 * Takes the HTML output data and returns the parts of it being tested.  The default implementation extracts all tags matching the
		 * passed tag name and, if specified, the passed class name.  You can also override this to provide your own implementation.
		 */
		virtual public string ExtractHTML (string html, string tagName, string className = null)
			{
			StringBuilder output = new StringBuilder();
			int tagIndex = FindNextTag(html, 0, tagName, className);

			while (tagIndex != -1)
				{
				if (output.Length != 0)
					{  output.Append("\r\n-----\r\n");  }

				int endOfClosingTag = FindEndOfClosingTag(html, tagIndex, tagName);
				output.Append(html, tagIndex, endOfClosingTag - tagIndex);

				tagIndex = FindNextTag(html, endOfClosingTag, tagName, className);
				}

			return output.ToString();
			}


		/* Function: TestFolder
		 * 
		 * Tests all the input files contained in this folder.
		 * 
		 * If you pass a relative path it will take the executing assembly path, skip up until it passes "bin", move into the "Test Data"
		 * subfolder, and then make the path relative to that.  This is because it's meant to be run from a Visual Studio source tree, 
		 * so from C:\Project\bin\debug\EngineTests.dll it will look for C:\Project\Test Data\[test folder].
		 * 
		 * Unless you override <ExtractHTML()>, the output will be all the tags that match the passed tag name and, if specified, the
		 * passed class name.
		 */
		public void TestFolder (Path testFolder, string tagName, string className = null)
			{
			List<TestResult> testResults = new List<TestResult>();
			int failureCount = 0;

			TestEngine.Start(testFolder, true);

			try
				{
				TestEngine.Run();


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
							var fileInfo = Engine.Instance.Files.FromPath(file);

							if (fileInfo == null)
								{  throw new Exception("Could not get file info of " + file);  }

							Path htmlFile = TestEngine.HTMLBuilder.Source_OutputFile(fileInfo.ID);

							string html = System.IO.File.ReadAllText(htmlFile);

							test.ActualOutput = ExtractHTML(html, tagName, className); 
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



		// Group: Parsing Functions
		// __________________________________________________________________________


		/* Function: FindNextTag
		 * Returns the index of the next tag of the specified type.  If a class name is passed, it will only match tags which have that
		 * class set.  The index will point to the opening bracket.
		 */
		protected int FindNextTag (string content, int startingIndex, string tagName, string className = null)
			{
			string searchString = '<' + tagName;
			int index = content.IndexOf(searchString, startingIndex);

			while (index != -1)
				{
				if ( (content[index + searchString.Length] == ' ' || content[index + searchString.Length] == '>') &&
					  (className == null || HasClass(content, index, className)) )
					{  return index;  }

				index = content.IndexOf(searchString, index + searchString.Length);
				}

			return -1;
			}

		/* Function: FindEndOfClosingTag
		 * Returns the index to the end of the tag and all its contents.  This will always return a value; if there's no closing tag then
		 * the end of the string will be returned.  The passed index should be at the opening bracket of the opening tag, and the returned 
		 * index will be one past the closing bracket of the closing tag.
		 */
		protected int FindEndOfClosingTag (string content, int tagIndex, string tagName)
			{
			string openingTagString = '<' + tagName;
			string closingTagString = "</" + tagName + '>';

			tagIndex += openingTagString.Length;
			int closingTagsToSkip = 0;

			int nextOpeningIndex = content.IndexOf(openingTagString, tagIndex);
			int nextClosingIndex = content.IndexOf(closingTagString, tagIndex);

			for (;;)
				{			
				// If the tags aren't matched correctly, just return the end of the string.
				if (nextClosingIndex == -1)
					{  return content.Length;  }

				// If we hit another opening index, we have to factor in the nested tag.
				if (nextOpeningIndex != -1 && nextOpeningIndex < nextClosingIndex)
					{
					closingTagsToSkip++;
					nextOpeningIndex = content.IndexOf(openingTagString, nextOpeningIndex + openingTagString.Length);
					continue;
					}

				// Skip the closing index if it's nested.
				if (closingTagsToSkip > 0)
					{
					closingTagsToSkip--;
					nextClosingIndex = content.IndexOf(closingTagString, nextClosingIndex + closingTagString.Length);
					continue;
					}

				// We found it!
				return nextClosingIndex + closingTagString.Length;
				}
			}


		/* Function: HasClass
		 * Returns whether the tag being pointed to by the passed index has the passed class name.  The index should point to the
		 * tag's opening bracket.
		 */
		protected bool HasClass (string content, int tagIndex, string className)
			{
			tagIndex++;  // Move past the <
			int endTagIndex = content.IndexOf('>', tagIndex);

			if (endTagIndex == -1)
				{  return false;  }

			int startClassIndex = content.IndexOf(" class=\"", tagIndex, endTagIndex - tagIndex);

			if (startClassIndex == -1)
				{  return false;  }

			startClassIndex += 8;  // Move past class=" and the leading space
			int endClassIndex = content.IndexOf('"', startClassIndex, endTagIndex - startClassIndex);

			if (endClassIndex == -1)
				{  return false;  }

			int classIndex = content.IndexOf(className, startClassIndex, endClassIndex - startClassIndex);

			if (classIndex == -1)
				{  return false;  }

			// Check that there's a space or quote on either side of it so we know it's not a substring of another class.
			// We don't have to worry about bounds as we know there's content to each side.
			return ( (content[classIndex - 1] == '"' || content[classIndex - 1] == ' ') &&
							(content[classIndex + className.Length] == '"' || content[classIndex + className.Length] == ' ') );
			}
			
		}
	}