/* 
 * Class: CodeClear.NaturalDocs.Engine.Tests.Framework.TestTypes.PrototypeParsing
 * ____________________________________________________________________________
 * 
 * File-based tests to make sure Natural Docs can parse prototypes correctly.
 * 
 */

// This file is part of Natural Docs, which is Copyright © 2003-2018 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System.Collections.Generic;
using System.Text;
using CodeClear.NaturalDocs.Engine.Prototypes;
using CodeClear.NaturalDocs.Engine.Tokenization;
using CodeClear.NaturalDocs.Engine.Topics;


namespace CodeClear.NaturalDocs.Engine.Tests.Framework.TestTypes
	{
	public class PrototypeParsing : Framework.SourceToTopics
		{

		public override string OutputOf (IList<Topic> topics)
			{
			if (topics == null || topics.Count == 0)
				{  return "(No topics found)";  }

			StringBuilder output = new StringBuilder();

			for (int topicIndex = 0; topicIndex < topics.Count; topicIndex++)
				{
				if (topicIndex != 0)
					{  output.AppendLine("-----");  }

				if (topics[topicIndex].Prototype == null)
					{  output.AppendLine("(No prototype detected)");  }
				else
					{  
					var parsedPrototype = topics[topicIndex].ParsedPrototype;
					
					TokenIterator start, end, extraModifierStart, extraModifierEnd, prefixStart, prefixEnd, suffixStart, suffixEnd;
					int numberOfParameters = parsedPrototype.NumberOfParameters;

					if (numberOfParameters == 0)
						{
						parsedPrototype.GetCompletePrototype(out start, out end);
						output.AppendLine("- No Parameters: " + parsedPrototype.Tokenizer.TextBetween(start, end));
						output.AppendLine("  - Access Level: " + parsedPrototype.GetAccessLevel());
						output.Append("  - Link Candidates: ");
						AppendLinkCandidates(start, end, output);
						output.AppendLine();
						AppendPrePrototypeLines(parsedPrototype, output);
						}
					else
						{
						parsedPrototype.GetBeforeParameters(out start, out end);
						output.AppendLine("- Before Parameters: " + parsedPrototype.Tokenizer.TextBetween(start, end));
						output.AppendLine("  - Access Level: " + parsedPrototype.GetAccessLevel());
						output.Append("  - Link Candidates: ");
						AppendLinkCandidates(start, end, output);
						output.AppendLine();
						AppendPrePrototypeLines(parsedPrototype, output);
						output.AppendLine();

						for (int paramIndex = 0; paramIndex < numberOfParameters; paramIndex++)
							{
							parsedPrototype.GetParameter(paramIndex, out start, out end);
							output.AppendLine("  - Parameter " + (paramIndex + 1) + ": " + parsedPrototype.Tokenizer.TextBetween(start, end));

							if (parsedPrototype.GetParameterName(paramIndex, out start, out end))
								{  output.AppendLine("    - Name: " + parsedPrototype.Tokenizer.TextBetween(start, end));  }
							else
								{  output.AppendLine("    - Name: (not detected)");  }

							string fullType = null;
							if (parsedPrototype.GetFullParameterType(paramIndex, out start, out end, 
																						out extraModifierStart, out extraModifierEnd, 
																						out prefixStart, out prefixEnd, 
																						out suffixStart, out suffixEnd, false))
								{  
								StringBuilder fullTypeBuilder = new StringBuilder();
								
								if (extraModifierEnd > extraModifierStart)
									{  fullTypeBuilder.Append(parsedPrototype.Tokenizer.TextBetween(extraModifierStart, extraModifierEnd) + " ");  }
								
								fullTypeBuilder.Append(parsedPrototype.Tokenizer.TextBetween(start, end));

								if (prefixEnd > prefixStart)
									{  fullTypeBuilder.Append(parsedPrototype.Tokenizer.TextBetween(prefixStart, prefixEnd));  }
								if (suffixEnd > suffixStart)
									{  fullTypeBuilder.Append(parsedPrototype.Tokenizer.TextBetween(suffixStart, suffixEnd));  }

								fullType = fullTypeBuilder.ToString();
								}

							string impliedType = null;
							if (parsedPrototype.GetFullParameterType(paramIndex, out start, out end, 
																					   out extraModifierStart, out extraModifierEnd, 
																					   out prefixStart, out prefixEnd, 
																					   out suffixStart, out suffixEnd, true))
								{  
								StringBuilder impliedTypeBuilder = new StringBuilder();

								if (extraModifierEnd > extraModifierStart)
									{  impliedTypeBuilder.Append(parsedPrototype.Tokenizer.TextBetween(extraModifierStart, extraModifierEnd) + " ");  }
								
								impliedTypeBuilder.Append(parsedPrototype.Tokenizer.TextBetween(start, end));

								if (prefixEnd > prefixStart)
									{  impliedTypeBuilder.Append(parsedPrototype.Tokenizer.TextBetween(prefixStart, prefixEnd));  }
								if (suffixEnd > suffixStart)
									{  impliedTypeBuilder.Append(parsedPrototype.Tokenizer.TextBetween(suffixStart, suffixEnd));  }

								impliedType = impliedTypeBuilder.ToString();
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

							if (parsedPrototype.GetBaseParameterType(paramIndex, out start, out end, false))
								{  output.AppendLine("    - Base Type: " + parsedPrototype.Tokenizer.TextBetween(start, end));  }
							else if (parsedPrototype.GetBaseParameterType(paramIndex, out start, out end, true))
								{  output.AppendLine("    - Base Type (implied): " + parsedPrototype.Tokenizer.TextBetween(start, end));  }
							else
								{  output.AppendLine("    - Base Type: (not detected)");  }

							parsedPrototype.GetParameter(paramIndex, out start, out end);

							output.Append("    - Link Candidates: ");
							AppendLinkCandidates(start, end, output);
							output.AppendLine();

							if (parsedPrototype.GetDefaultValue(paramIndex, out start, out end))
								{  output.AppendLine("    - Default Value: " + parsedPrototype.Tokenizer.TextBetween(start, end));  }
							else
								{  output.AppendLine("    - Default Value: (not detected)");  }

							output.AppendLine();
							}

						if (parsedPrototype.GetAfterParameters(out start, out end))
							{  output.AppendLine("- After Parameters: " + parsedPrototype.Tokenizer.TextBetween(start, end));  }
						output.Append("  - Link Candidates: ");
						AppendLinkCandidates(start, end, output);
						output.AppendLine();
						AppendPostPrototypeLines(parsedPrototype, output);
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

				start.Tokenizer.AppendTextBetweenTo(linkableTypeStart, iterator, output);
				linkableTypes++;
				}

			if (linkableTypes == 0)
				{  output.Append("(none)");  }
			}

		void AppendPrePrototypeLines (ParsedPrototype prototype, StringBuilder output)
			{
			int numberOfLines = prototype.NumberOfPrePrototypeLines;
			TokenIterator start, end;

			for (int i = 0; i < numberOfLines; i++)
				{
				prototype.GetPrePrototypeLine(i, out start, out end);
				output.Append("  - Pre-Prototype Line: ");
				prototype.Tokenizer.AppendTextBetweenTo(start, end, output);
				output.AppendLine();
				}
			}

		void AppendPostPrototypeLines (ParsedPrototype prototype, StringBuilder output)
			{
			int numberOfLines = prototype.NumberOfPostPrototypeLines;
			TokenIterator start, end;

			for (int i = 0; i < numberOfLines; i++)
				{
				prototype.GetPostPrototypeLine(i, out start, out end);
				output.Append("  - Post-Prototype Line: ");
				prototype.Tokenizer.AppendTextBetweenTo(start, end, output);
				output.AppendLine();
				}
			}
		}
	}