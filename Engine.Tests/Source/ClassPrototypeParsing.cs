/* 
 * Class: GregValure.NaturalDocs.Engine.Tests.ClassPrototypeParsing
 * ____________________________________________________________________________
 * 
 * File-based tests to make sure Natural Docs can parse class prototypes correctly.
 * 
 */

// This file is part of Natural Docs, which is Copyright © 2003-2012 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using GregValure.NaturalDocs.Engine;
using GregValure.NaturalDocs.Engine.Tokenization;
using GregValure.NaturalDocs.Engine.Tests.Framework;
using GregValure.NaturalDocs.Engine.Topics;


namespace GregValure.NaturalDocs.Engine.Tests
	{
	[TestFixture]
	public class ClassPrototypeParsing : Framework.SourceToTopics
		{

		// Group: Tests
		// __________________________________________________________________________

		[Test, Category("Basic Language Support")]
		public void BasicSupport ()
			{
			TestFolder("Languages/Basic Support/Class Prototype Parsing", "Shared ND Config/Basic Language Support");
			}


		// Group: Output
		// __________________________________________________________________________

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

					if (parsedPrototype.GetName(out start, out end))
						{  output.AppendLine("  - Name: " + parsedPrototype.Tokenizer.TextBetween(start, end));  }
					else
						{  output.AppendLine("  - Name: (none)");  }

					if (parsedPrototype.GetTemplateSuffix(out start, out end))
						{  output.AppendLine("  - Template Suffix: " + parsedPrototype.Tokenizer.TextBetween(start, end));  }
					if (parsedPrototype.GetModifiers(out start, out end))
						{  output.AppendLine("  - Modifiers: " + parsedPrototype.Tokenizer.TextBetween(start, end));  }
					if (parsedPrototype.GetPostModifiers(out start, out end))
						{  output.AppendLine("  - Post Modifiers: " + parsedPrototype.Tokenizer.TextBetween(start, end));  }

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
							if (parsedPrototype.GetParentPostModifiers(i, out start, out end))
								{  output.AppendLine("    - Post Modifiers: " + parsedPrototype.Tokenizer.TextBetween(start, end));  }
							}
						}
					}
				}

			return output.ToString();
			}
		}
	}