/*
 * Class: CodeClear.NaturalDocs.Engine.Tests.Framework.SourceToHTML
 * ____________________________________________________________________________
 *
 * A base class for automated tests where sample source files are run through Natural Docs normally, generating
 * HTML output, and then portions of the HTML are extracted, saved to files, and compared to other files containing
 * the expected output.
 *
 *	 The benefit of this approach is that you never have to hand code the output.  You can run the tests without
 *	 an expected output file, look over the actual output file, and if it's acceptable rename it to become the
 *	 expected output file.
 *
 * Usage:
 *
 *		- Derive a class that has the [TestFixture] attribute.
 *		- Create a function with the [Test] attribute that calls <TestFolder()>, pointing it to the input files and
 *		   specifying which parts of the generated HTML should be extracted.
 *		- All files in the test folder in the format "[Test Name] - Input.[extension]" will have portions of its HTML
 *		  extracted when NUnit runs.
 *		- A corresponding file "[Test Name] - Actual Output.txt" will be created for each one.
 *		- If it matches the contents of the file "[Test Name] - Expected Output.txt", the test will pass.  If it doesn't or
 *		  that file doesn't exist, the test will fail.
 *		- The full generated output will remain in a folder called "HTML Output" if you want to look at it.
 *
 */

// This file is part of Natural Docs, which is Copyright © 2003-2024 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using NUnit.Framework;
using CodeClear.NaturalDocs.Engine;
using CodeClear.NaturalDocs.Engine.Collections;


namespace CodeClear.NaturalDocs.Engine.Tests.Framework
	{
	public abstract class SourceToHTML
		{

		// Group: Functions
		// __________________________________________________________________________

		/* Constructor: SourceToHTML
		 */
		public SourceToHTML ()
			{
			engineInstanceManager = null;
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
				int endOfClosingTag = FindEndOfClosingTag(html, tagIndex, tagName);
				string tag = html.Substring(tagIndex, endOfClosingTag - tagIndex);

				// Filter out Topic# tags.
				if (tag.StartsWith("<a name=\"Topic"))
					{
					// Ignore
					}
				else
					{
					if (output.Length != 0)
						{  output.Append("\r\n-----\r\n");  }

					output.Append(tag);
					}

				tagIndex = FindNextTag(html, endOfClosingTag, tagName, className);
				}

			return output.ToString();
			}


		/* Function: TestFolder
		 *
		 * Tests all the input files contained in this folder.  See <EngineInstanceManager.Start()> for how relative paths are handled.
		 *
		 * Unless you override <ExtractHTML()>, the output will be all the tags that match the passed tag name and, if specified, the
		 * passed class name.
		 */
		public void TestFolder (Path testDataFolder, Path projectConfigFolder, string tagName, string className = null,
									   bool reformatHTML = false, string outputTitle = null, string outputSubtitle = null,
									   string outputStyle = null)
			{
			TestList allTests = new TestList();

			engineInstanceManager = new EngineInstanceManager();
			engineInstanceManager.Start(testDataFolder, projectConfigFolder, true, outputTitle, outputSubtitle, outputStyle);

			// Store this so we can still use it for error messages after the engine is disposed of.
			Path inputFolder = engineInstanceManager.InputFolder;

			try
				{
				engineInstanceManager.Run();

				// Build a test for each input file we find
				string[] files = System.IO.Directory.GetFiles(inputFolder);

				foreach (string file in files)
					{
					if (Test.IsInputFile(file))
						{
						Test test = Test.FromInputFile(file);

						try
							{
							var fileInfo = EngineInstance.Files.FromPath(file);

							if (fileInfo == null)
								{  throw new Exception("Could not get file info of " + file);  }

							var fileContext = new Engine.Output.HTML.Context(engineInstanceManager.HTMLBuilder, fileInfo.ID);

							Path htmlFile = fileContext.OutputFile;

							string html = System.IO.File.ReadAllText(htmlFile);
							html = ExtractHTML(html, tagName, className);

							if (reformatHTML)
								{  html = ReformatHTML(html);  }

							test.SetActualOutput(html);
							}
						catch (Exception e)
							{  test.TestException = e;  }

						test.Run();
						allTests.Add(test);
						}
					}
				}

			finally
				{
				engineInstanceManager.Dispose();
				engineInstanceManager = null;
				}


			if (allTests.Count == 0)
				{  Assert.Fail("There were no tests found in " + inputFolder);  }
			else if (allTests.Passed == false)
				{  Assert.Fail(allTests.BuildFailureMessage());  }
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


		/* Function: ReformatHTML
		 * - Pretty-prints tables to make it more human readable.
		 * - Removes numbered IDs like NDPrototype364 so changing ID numbers don't affect the expected output.
		 */
		protected string ReformatHTML (string input)
			{
			StringBuilder output = new StringBuilder();

			int textPosition = 0;
			int lastNewSectionPosition = 0;
			int afterLastClosingTagPosition = -1;

			int indentLevel = 0;
			int spacesPerIndentLevel = 3;

			for (;;)
				{
				// Find next relevant tag
				var tagMatch = TagsToFormatRegex.Match(input, textPosition);

				if (tagMatch.Success == false)
					{  break;  }

				// Append text between the current position and the next relevant tag
				if (tagMatch.Index > textPosition)
					{  output.Append(input, textPosition, tagMatch.Index - textPosition);  }

				// Section separator
				if (tagMatch.Value.StartsWith("-----"))
					{
					output.Append(tagMatch.Value);  // will include newline

					indentLevel = 0;

					textPosition = tagMatch.Index + tagMatch.Length;
					lastNewSectionPosition = textPosition;
					}

				// Relevant closing tags
				else if (tagMatch.Value.StartsWith("</"))
					{
					// Only put it on a new line if it immediately follows another closing tag
					if (textPosition == afterLastClosingTagPosition)
						{
						output.AppendLine();
						output.Append(' ', indentLevel * spacesPerIndentLevel);
						}

					output.Append(tagMatch.Value);

					// Safety check since the HTML could be invalid
					if (indentLevel > 0)
						{  indentLevel--;  }

					textPosition = tagMatch.Index + tagMatch.Length;
					afterLastClosingTagPosition = textPosition;
					}

				// Relevant opening tags
				else
					{
					if (textPosition != lastNewSectionPosition)
						{
						output.AppendLine();
						indentLevel++;
						}

					output.Append(' ', indentLevel * spacesPerIndentLevel);
					output.Append(tagMatch.Value);

					textPosition = tagMatch.Index + tagMatch.Length;
					}
				}

			// Append remaining text after the last tag
			if (textPosition < input.Length)
				{  output.Append(input, textPosition, input.Length - textPosition);  }

			string outputString = output.ToString();

			// Remove ID numbers from tags
			outputString = IDNumbersRegex.Replace(outputString, "");

			return outputString;
			}


		// Group: Properties
		// __________________________________________________________________________

		public NaturalDocs.Engine.Instance EngineInstance
			{
			get
				{
				if (engineInstanceManager != null)
					{  return engineInstanceManager.EngineInstance;  }
				else
					{  return null;  }
				}
			}


		// Group: Variables
		// __________________________________________________________________________

		protected EngineInstanceManager engineInstanceManager;


		// Group: Static Variables
		// __________________________________________________________________________

		static protected System.Text.RegularExpressions.Regex TagsToFormatRegex = new System.Text.RegularExpressions.Regex("(?:</?div[^>]*>|-----\r\n)",
																						 RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.CultureInvariant);
		static protected System.Text.RegularExpressions.Regex IDNumbersRegex = new System.Text.RegularExpressions.Regex(" id=\"ND(?:Class)?Prototype[0-9]+\"",
																					   RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.CultureInvariant);

		}
	}
