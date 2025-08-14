/*
 * Class: CodeClear.NaturalDocs.Tests.TestRunners.ClassCombining
 * ____________________________________________________________________________
 *
 * Tests to make sure Natural Docs can merge topics from multiple files into a single coherent list for a class view.
 *
 * Since the input files and output files will not match 1:1, the generated output files will be in the format
 *	"[Class Name] - Actual Output.txt".
 *
 */

// This file is part of Natural Docs, which is Copyright © 2003-2025 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using System.Text;
using CodeClear.NaturalDocs.Engine.Topics;


namespace CodeClear.NaturalDocs.Tests.TestRunners
	{
	public class ClassCombining : TestRunner
		{

		public ClassCombining ()
			: base (InputMode.ClassTopics, EngineMode.InstanceAndGeneratedDocs)
			{  	}

		protected override string RunTest (IList<Topic> topics)
			{
			if (topics == null || topics.Count == 0)
				{  return "(No topics generated)";  }

			StringBuilder output = new StringBuilder();
			bool inGroup = false;

			for (int i = 0; i < topics.Count; i++)
				{
				if (topics[i].IsEmbedded)
					{  continue;  }

				int indent;
				char decoration = '\0';

				if (i == 0)
					{
					indent = 0;
					decoration = '_';
					}
				else if (topics[i].IsGroup)
					{
					indent = 3;
					decoration = '-';
					inGroup = true;
					}
				else if (!inGroup)
					{  indent = 3;  }
				else
					{  indent = 6;  }

				if (decoration != '\0')
					{
					output.Append(' ', indent);
					output.Append(decoration, 20);
					output.AppendLine();

					if (decoration == '_')
						{  output.AppendLine();  }
					}

				output.Append(' ', indent);
				output.AppendLine(topics[i].Title);

				if (topics[i].Prototype != null)
					{
					output.Append(' ', indent);
					output.AppendLine('[' + topics[i].Prototype + ']');
					}

				if (topics[i].Body != null)
					{
					output.Append(' ', indent);
					output.AppendLine(topics[i].Body);
					}

				if (decoration != '\0')
					{
					output.Append(' ', indent);
					output.Append(decoration, 20);
					output.AppendLine();

					if (decoration == '_')
						{  output.AppendLine();  }
					}

				if (i < topics.Count - 1)
					{  output.AppendLine();  }
				}

			return output.ToString();
			}

		}
	}
