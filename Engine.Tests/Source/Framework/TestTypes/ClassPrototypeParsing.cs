/* 
 * Class: CodeClear.NaturalDocs.Engine.Tests.Framework.TestTypes.ClassPrototypeParsing
 * ____________________________________________________________________________
 * 
 * File-based tests to make sure Natural Docs can parse class prototypes correctly.
 * 
 */

// This file is part of Natural Docs, which is Copyright © 2003-2017 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using CodeClear.NaturalDocs.Engine;
using CodeClear.NaturalDocs.Engine.Tokenization;
using CodeClear.NaturalDocs.Engine.Tests.Framework;
using CodeClear.NaturalDocs.Engine.Topics;


namespace CodeClear.NaturalDocs.Engine.Tests.Framework.TestTypes
	{
	public class ClassPrototypeParsing : Framework.SourceToTopics
		{

		public override string OutputOf (IList<Topic> topics)
			{
			if (topics == null || topics.Count == 0)
				{  return "(No topics found)";  }

			StringBuilder output = new StringBuilder();

			for (int topicIndex = 0; topicIndex < topics.Count; topicIndex++)
				{
				if (topicIndex != 0)
					{
					output.AppendLine();  
					output.AppendLine("-----");  
					output.AppendLine();
					}

				if (topics[topicIndex].ParsedClassPrototype == null)
					{  output.AppendLine("(No class prototype detected)");  }
				else
					{  
					var parsedPrototype = topics[topicIndex].ParsedClassPrototype;
					TokenIterator start, end;

					output.AppendLine(topics[topicIndex].Prototype);
					output.AppendLine();

					int numberOfLines = parsedPrototype.NumberOfPrePrototypeLines;

					for (int i = 0; i < numberOfLines; i++)
						{
						parsedPrototype.GetPrePrototypeLine(i, out start, out end);
						output.Append("  - Pre-Prototype Line: ");
						parsedPrototype.Tokenizer.AppendTextBetweenTo(start, end, output);
						output.AppendLine();
						}

					if (parsedPrototype.GetName(out start, out end))
						{  output.AppendLine("  - Name: " + parsedPrototype.Tokenizer.TextBetween(start, end));  }
					else
						{  output.AppendLine("  - Name: (none)");  }

					if (parsedPrototype.GetTemplateSuffix(out start, out end))
						{  output.AppendLine("  - Template Suffix: " + parsedPrototype.Tokenizer.TextBetween(start, end));  }
					if (parsedPrototype.GetKeyword(out start, out end))
						{  output.AppendLine("  - Keyword: " + parsedPrototype.Tokenizer.TextBetween(start, end));  }
					if (parsedPrototype.GetModifiers(out start, out end))
						{  output.AppendLine("  - Modifiers: " + parsedPrototype.Tokenizer.TextBetween(start, end));  }
					output.AppendLine("  - Access Level: " + parsedPrototype.GetAccessLevel());

					numberOfLines = parsedPrototype.NumberOfPostPrototypeLines;

					for (int i = 0; i < numberOfLines; i++)
						{
						parsedPrototype.GetPostPrototypeLine(i, out start, out end);
						output.Append("  - Post-Prototype Line: ");
						parsedPrototype.Tokenizer.AppendTextBetweenTo(start, end, output);
						output.AppendLine();
						}

					int numberOfParents = parsedPrototype.NumberOfParents;

					if (numberOfParents == 0)
						{
						output.AppendLine("  - No parents");
						}
					else
						{
						for (int i = 0; i < numberOfParents; i++)
							{
							output.AppendLine();

							parsedPrototype.GetParent(i, out start, out end);
							output.AppendLine("  - Parent " + (i + 1) + ": " + parsedPrototype.Tokenizer.TextBetween(start, end));

							if (parsedPrototype.GetParentName(i, out start, out end))
								{  output.AppendLine("    - Name: " + parsedPrototype.Tokenizer.TextBetween(start, end));  }
							else
								{  output.AppendLine("    - Name: (none)");  }

							if (parsedPrototype.GetParentTemplateSuffix(i, out start, out end))
								{  output.AppendLine("    - Template Suffix: " + parsedPrototype.Tokenizer.TextBetween(start, end));  }
							if (parsedPrototype.GetParentModifiers(i, out start, out end))
								{  output.AppendLine("    - Modifiers: " + parsedPrototype.Tokenizer.TextBetween(start, end));  }
							}
						}
					}
				}

			return output.ToString();
			}
		}
	}