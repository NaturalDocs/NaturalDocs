/*
 * Class: CodeClear.NaturalDocs.Engine.Tests.Framework.TestTypes.HTML
 * ____________________________________________________________________________
 *
 * File-based tests to make sure Natural Docs can convert files into HTML correctly.
 *
 *
 * Deriving a Test Class:
 *
 *		- Derive a class and add the [TestFixture] attribute.
 *
 *		- Create a function with the [Test] attribute that calls TestFolder(), pointing it to the input files.
 *
 *
 * Input and Output Files:
 *
 *		- All files in the test folder in the format "[Test Name] - Input.[extension]" will have portions of its HTML
 *		  extracted when NUnit runs.
 *
 *		- A corresponding file "[Test Name] - Actual Output.txt" will be created for each one.
 *
 *		- If it matches the contents of the file "[Test Name] - Expected Output.txt", the test will pass.  If it doesn't or
 *		  that file doesn't exist, the test will fail.
 *
 *		- The full generated output will remain in a folder called "HTML Output" if you want to look at it in a browser.
 *
 */

// This file is part of Natural Docs, which is Copyright © 2003-2025 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Text;


namespace CodeClear.NaturalDocs.Engine.Tests.Framework.TestTypes
	{
	public class HTML : Framework.BaseTestTypes.SourceToHTML
		{

		public override string OutputOf (string html, string tagName, string className = null)
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

		}
	}
