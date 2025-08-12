/*
 * Class: CodeClear.NaturalDocs.Tests.TestRunners.PrototypeParsing
 * ____________________________________________________________________________
 *
 * Tests to make sure Natural Docs can parse prototypes correctly.
 *
 */

// This file is part of Natural Docs, which is Copyright © 2003-2025 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using System.Text;
using CodeClear.NaturalDocs.Engine.Tokenization;
using CodeClear.NaturalDocs.Engine.Topics;


namespace CodeClear.NaturalDocs.Tests.TestRunners
	{
	public class PrototypeParsing : TestRunner
		{

		public PrototypeParsing ()
			: base (InputMode.Topics, EngineMode.InstanceOnly)
			{  	}


		protected override string RunTest (IList<Topic> topics)
			{
			if (topics == null || topics.Count == 0)
				{  return "(No topics found)";  }

			StringBuilder output = new StringBuilder();

			for (int topicIndex = 0; topicIndex < topics.Count; topicIndex++)
				{
				if (topics[topicIndex].CommentTypeID == EngineInstance.CommentTypes.GroupCommentTypeID &&
					topics[topicIndex].Title != null)
					{
					if (topicIndex != 0)
						{  output.AppendLine();  }

					output.AppendLine("------------------------------");

					// Leave off the line break so the next topic ends the line instead of adding a blank line
					output.Append(topics[topicIndex].Title + ":");

					continue;
					}
				else if (topicIndex != 0)
					{
					output.AppendLine();
					output.AppendLine("------------------------------");
					output.AppendLine();
					}

				if (topics[topicIndex].Prototype == null)
					{
					output.AppendLine("(No prototype detected)");
					continue;
					}

				var parsedPrototype = topics[topicIndex].ParsedPrototype;

				// SystemVerilog modules can use multiple parameter sections to build types so we have to maintain a global parameter
				// index which starts at zero and increments across the entire prototype's parameters regardless of section.
				int globalParameterIndex = 0;

				for (int sectionIndex = 0; sectionIndex < parsedPrototype.Sections.Count; sectionIndex++)
					{
					if (sectionIndex != 0)
						{  output.AppendLine();  }

					if (parsedPrototype.Sections[sectionIndex] is Engine.Prototypes.ParameterSection)
						{
						Engine.Prototypes.ParameterSection section = (parsedPrototype.Sections[sectionIndex] as Engine.Prototypes.ParameterSection);
						output.AppendLine("- Parameter Section:");

						TokenIterator start, end;
						section.GetBeforeParameters(out start, out end);
						output.AppendLine("  - Before Parameters: " + start.TextBetween(end));
						output.AppendLine("    - Access Level: " + section.GetAccessLevel());
						output.Append("    - Link Candidates: ");
						AppendLinkCandidates(start, end, output);  output.AppendLine();

						for (int paramIndex = 0; paramIndex < section.NumberOfParameters; paramIndex++)
							{
							output.AppendLine();

							section.GetParameterBounds(paramIndex, out start, out end);
							output.AppendLine("  - Parameter " + (paramIndex + 1) + ": " + start.TextBetween(end));

							if (section.GetParameterName(paramIndex, out start, out end))
								{  output.AppendLine("    - Name: " + start.TextBetween(end));  }
							else
								{  output.AppendLine("    - Name: (not detected)");  }

							string fullType = null;
							if (parsedPrototype is Engine.Prototypes.ParsedPrototypes.SystemVerilogModule)
								{
								if (parsedPrototype.BuildFullParameterType(globalParameterIndex, out start, out end, false))
									{  fullType = start.TextBetween(end);  }
								}
							else
								{
								if (section.BuildFullParameterType(paramIndex, out start, out end, false))
									{  fullType = start.TextBetween(end);  }
								}

							string impliedType = null;
							if (parsedPrototype is Engine.Prototypes.ParsedPrototypes.SystemVerilogModule)
								{
								if (parsedPrototype.BuildFullParameterType(globalParameterIndex, out start, out end, true))
									{  impliedType = start.TextBetween(end);  }
								}
							else
								{
								if (section.BuildFullParameterType(paramIndex, out start, out end, true))
									{  impliedType = start.TextBetween(end);  }
								}

							if (fullType != null)
								{  output.AppendLine("    - Full Type: " + fullType);  }
							if (impliedType != null)
								{
								if (fullType == null)
									{  output.AppendLine("    - Full Type (implied): " + impliedType);  }
								else if (impliedType != fullType)
									{  output.AppendLine("    - Full Type (plus implied): " + impliedType);  }
								}
							if (fullType == null && impliedType == null)
								{  output.AppendLine("    - Full Type: (not detected)");  }

							if (parsedPrototype is Engine.Prototypes.ParsedPrototypes.SystemVerilogModule)
								{
								if (parsedPrototype.GetBaseParameterType(globalParameterIndex, out start, out end, false))
									{  output.AppendLine("    - Base Type: " + start.TextBetween(end));  }
								else if (parsedPrototype.GetBaseParameterType(globalParameterIndex, out start, out end, true))
									{  output.AppendLine("    - Base Type (implied): " + start.TextBetween(end));  }
								else
									{  output.AppendLine("    - Base Type: (not detected)");  }
								}
							else
								{
								if (section.GetBaseParameterType(paramIndex, out start, out end, false))
									{  output.AppendLine("    - Base Type: " + start.TextBetween(end));  }
								else if (section.GetBaseParameterType(paramIndex, out start, out end, true))
									{  output.AppendLine("    - Base Type (implied): " + start.TextBetween(end));  }
								else
									{  output.AppendLine("    - Base Type: (not detected)");  }
								}

							section.GetParameterBounds(paramIndex, out start, out end);
							output.Append("    - Link Candidates: ");
							AppendLinkCandidates(start, end, output);  output.AppendLine();

							if (section.GetParameterDefaultValue(paramIndex, out start, out end))
								{  output.AppendLine("    - Default Value: " + start.TextBetween(end));  }
							else
								{  output.AppendLine("    - Default Value: (not detected)");  }

							globalParameterIndex++;
							}

						if (section.GetAfterParameters(out start, out end))
							{
							output.AppendLine();
							output.AppendLine("  - After Parameters: " + start.TextBetween(end));
							output.Append("    - Link Candidates: ");
							AppendLinkCandidates(start, end, output);  output.AppendLine();
							}
						}

					else // Plain section
						{
						Engine.Prototypes.Section section = parsedPrototype.Sections[sectionIndex];

						output.AppendLine("- Plain Section: " + section.Start.TextBetween(section.End));
						output.AppendLine("  - Access Level: " + section.GetAccessLevel());
						output.Append("  - Link Candidates: ");
						AppendLinkCandidates(section.Start, section.End, output);  output.AppendLine();
						}
					}
				}

			return output.ToString();
			}


		void AppendLinkCandidates (TokenIterator start, TokenIterator end, StringBuilder output)
			{
			TokenIterator iterator = start;
			TokenIterator linkableTypeStart = start;
			int linkableTypes = 0;

			for (;;)
				{
				while (iterator < end &&
							iterator.PrototypeParsingType != PrototypeParsingType.TypeQualifier &&
							iterator.PrototypeParsingType != PrototypeParsingType.Type)
					{  iterator.Next();  }

				if (iterator >= end)
					{  break;  }

				linkableTypeStart = iterator;

				while (iterator < end &&
								(iterator.PrototypeParsingType == PrototypeParsingType.TypeQualifier ||
								iterator.PrototypeParsingType == PrototypeParsingType.Type) )
					{  iterator.Next();  }

				if (linkableTypes > 0)
					{  output.Append(", ");  }

				linkableTypeStart.AppendTextBetweenTo(iterator, output);
				linkableTypes++;
				}

			if (linkableTypes == 0)
				{  output.Append("(none)");  }
			}

		}
	}
