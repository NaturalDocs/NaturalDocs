/* 
 * Class: GregValure.NaturalDocs.EngineTests.PrototypeParsing
 * ____________________________________________________________________________
 * 
 * File-based tests to make sure Natural Docs can parse prototypes correctly.
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
using GregValure.NaturalDocs.Engine.Tokenization;
using GregValure.NaturalDocs.EngineTests.Framework;


namespace GregValure.NaturalDocs.EngineTests
	{
	[TestFixture]
	public class PrototypeParsing : Framework.SourceToTopics
		{

		[Test]
		public void BasicLanguageSupport ()
			{
			TestFolder("Prototype Parsing/Basic Language Support");
			}

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
					var language = Engine.Instance.Languages.FromID(topics[topicIndex].LanguageID);
					var parsedPrototype = language.ParsePrototype(topics[topicIndex].Prototype, topics[topicIndex].TopicTypeID);
					
					TokenIterator start, end, extensionStart, extensionEnd;
					int numberOfParameters = parsedPrototype.NumberOfParameters;

					if (numberOfParameters == 0)
						{
						parsedPrototype.GetCompletePrototype(out start, out end);
						output.AppendLine("- No Parameters: " + parsedPrototype.Tokenizer.TextBetween(start, end));
						}
					else
						{
						parsedPrototype.GetBeforeParameters(out start, out end);
						output.AppendLine("- Before Parameters: " + parsedPrototype.Tokenizer.TextBetween(start, end));
						output.AppendLine();

						for (int paramIndex = 0; paramIndex < numberOfParameters; paramIndex++)
							{
							parsedPrototype.GetParameter(paramIndex, out start, out end);
							output.AppendLine("  - Parameter " + (paramIndex + 1) + ": " + parsedPrototype.Tokenizer.TextBetween(start, end));

							if (parsedPrototype.GetParameterName(paramIndex, out start, out end))
								{  output.AppendLine("    - Name: " + parsedPrototype.Tokenizer.TextBetween(start, end));  }
							else
								{  output.AppendLine("    - Name: (not detected)");  }

							if (parsedPrototype.GetFullParameterType(paramIndex, out start, out end, out extensionStart, out extensionEnd))
								{  
								output.Append("    - Full Type: " + parsedPrototype.Tokenizer.TextBetween(start, end));

								if (extensionEnd > extensionStart)
									{  output.Append(parsedPrototype.Tokenizer.TextBetween(extensionStart, extensionEnd));  }

								output.AppendLine();
								}
							else
								{  output.AppendLine("    - Full Type: (not detected)");  }

							if (parsedPrototype.GetBaseParameterType(paramIndex, out start, out end))
								{  output.AppendLine("    - Base Type: " + parsedPrototype.Tokenizer.TextBetween(start, end));  }
							else
								{  output.AppendLine("    - Base Type: (not detected)");  }

							parsedPrototype.GetParameter(paramIndex, out start, out end);
							int linkableTypes = 0;

							output.Append("    - Link Candidates: ");
							TokenIterator iterator = start;
							TokenIterator linkableTypeStart = start;

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

								parsedPrototype.Tokenizer.AppendTextBetweenTo(linkableTypeStart, iterator, output);
								linkableTypes++;
								}

							if (linkableTypes == 0)
								{  output.Append("(not detected)");  }

							output.AppendLine();

							if (parsedPrototype.GetDefaultValue(paramIndex, out start, out end))
								{  output.AppendLine("    - Default Value: " + parsedPrototype.Tokenizer.TextBetween(start, end));  }
							else
								{  output.AppendLine("    - Default Value: (not detected)");  }

							output.AppendLine();
							}

						parsedPrototype.GetAfterParameters(out start, out end);
						output.AppendLine("- After Parameters: " + parsedPrototype.Tokenizer.TextBetween(start, end));
						}
					}
				}

			return output.ToString();
			}

		}
	}