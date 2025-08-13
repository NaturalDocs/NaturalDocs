/*
 * Class: CodeClear.NaturalDocs.Tests.TestRunners.HTML
 * ____________________________________________________________________________
 *
 * A base class for <TestRunners> that extract portions of the HTML output.
 *
 * Usage:
 *
 *		- Inherit from <TestRunners.HTML> instead of <TestRunner>.
 *
 *		- Override <TestRunner.RunTest(string)>.
 *
 *			- Call <ExtractHTML()> with the tag, class, and formatting parameters you want.  Return its contents from RunTest().
 *
 *		- That's it.  You're done.
 *
 */

// This file is part of Natural Docs, which is Copyright © 2003-2025 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Text;
using System.Text.RegularExpressions;


namespace CodeClear.NaturalDocs.Tests.TestRunners
	{
	public abstract class HTML : TestRunner
		{

		protected HTML ()
			: base (InputMode.HTML, EngineMode.InstanceAndGeneratedDocs)
			{  	}


		protected string ExtractHTML (string html, string tagName, string className = null, bool reformatHTML = false)
			{
			StringBuilder outputBuilder = new StringBuilder();
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
					if (outputBuilder.Length != 0)
						{  outputBuilder.Append("\r\n-----\r\n");  }

					outputBuilder.Append(tag);
					}

				tagIndex = FindNextTag(html, endOfClosingTag, tagName, className);
				}

			string output = outputBuilder.ToString();

			if (reformatHTML)
				{  output = ReformatHTML(output);  }

			return output;
			}



		// Group: Support Functions
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
		 *
		 * Pretty-prints certain tags to make the HTML more human readable.
		 *
		 * Numbered IDs like "NDPrototype364" will be removed so if they change from one test run to the next it won't affect the
		 * expected output.
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



		// Group: Static Variables
		// __________________________________________________________________________

		static protected Regex TagsToFormatRegex = new Regex("(?:</?div[^>]*>|-----\r\n)",
																						 RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.CultureInvariant);
		static protected Regex IDNumbersRegex = new Regex(" id=\"ND(?:Class)?Prototype[0-9]+\"",
																					   RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.CultureInvariant);

		}
	}
