/*
 * Class: CodeClear.NaturalDocs.Engine.Tests.Framework.TestTypes.LinkInterpretations
 * ____________________________________________________________________________
 *
 * A class to test Natural Docs' link interpreting.
 *
 * Commands:
 *
 *		> // text
 *		Comment.
 *
 *		> <Link text>
 *		The link to show the interpretations of.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2022 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using CodeClear.NaturalDocs.Engine;
using CodeClear.NaturalDocs.Engine.Links;
using CodeClear.NaturalDocs.Engine.Tests.Framework;


namespace CodeClear.NaturalDocs.Engine.Tests.Framework.TestTypes
	{
	public class LinkInterpretations : Framework.TextCommands
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
