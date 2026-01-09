/*
 * Class: CodeClear.NaturalDocs.Tests.TestRunners.LinkInterpretations
 * ____________________________________________________________________________
 *
 * A class to test Natural Docs' link interpreting.
 *
 *
 * Commands:
 *
 *		The input files are a series of commands, one one each line, in one of the following formats:
 *
 *		> // text
 *		Comment.
 *
 *		> <Link text>
 *		The link to show the interpretations of.
 *
 */

// This file is part of Natural Docs, which is Copyright © 2003-2026 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Text;
using CodeClear.NaturalDocs.Engine;


namespace CodeClear.NaturalDocs.Tests.TestRunners
	{
	public class LinkInterpretations : TestRunner
		{

		public LinkInterpretations ()
			: base (InputMode.Lines, EngineMode.InstanceOnly)
			{  	}

		protected override string RunTest (string[] commands)
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
