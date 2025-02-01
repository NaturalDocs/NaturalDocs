/*
 * Class: CodeClear.NaturalDocs.Engine.Tests.Framework.TestTypes.LinkInterpretations
 * ____________________________________________________________________________
 *
 * A class to test Natural Docs' link interpreting.
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
 *		- All files in the test folder in the format "[Test Name] - Input.[extension]" will be tested when NUnit runs.
 *
 *		- A corresponding file "[Test Name] - Actual Output.txt" will be created for each one.
 *
 *		- If it matches the contents of the file "[Test Name] - Expected Output.txt", the test will pass.  If it doesn't,
 *		  that file doesn't exist, or an exception was thrown, the test will fail.
 *
 *
 * Commands:
 *
 *		> // text
 *		Comment.
 *
 *		> <Link text>
 *		The link to show the interpretations of.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2025 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using System.Text;


namespace CodeClear.NaturalDocs.Engine.Tests.Framework.TestTypes
	{
	public class LinkInterpretations : Framework.BaseTestTypes.TextCommands
		{

		public override string OutputOf (IList<string> commands)
			{
			StringBuilder output = new StringBuilder();
			var parser = EngineInstance.Comments.NaturalDocsParser;

			foreach (var command in commands)
				{
				output.AppendLine(command);

				if (command.StartsWith("<"))
					{
					try
						{
						string link = command.Trim();
						string parameters = null;

						var interpretations = parser.LinkInterpretations(link,
																					   Engine.Comments.NaturalDocs.Parser.LinkInterpretationFlags.AllowNamedLinks |
																					   Engine.Comments.NaturalDocs.Parser.LinkInterpretationFlags.AllowPluralsAndPossessives |
																					   Engine.Comments.NaturalDocs.Parser.LinkInterpretationFlags.FromOriginalText,
																					   out parameters);

						if (parameters != null)
							{  output.AppendLine("- Parameters -> " + parameters);  }

						foreach (var interpretation in interpretations)
							{  output.AppendLine("- \"" + interpretation.Text + "\" -> " + interpretation.Target);  }
						}
					catch (Exception e)
						{
						output.AppendLine("ERROR: Exception " + e.Message);
						}
					}
				else if (command != null && command.Trim() != "" && command.StartsWith("//") == false)
					{
					output.AppendLine("ERROR: Unrecognized command");
					}
				}

			return output.ToString();
			}

		}
	}
