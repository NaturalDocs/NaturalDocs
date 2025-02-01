/*
 * Class: CodeClear.NaturalDocs.Engine.Tests.Framework.TestTypes.Symbols
 * ____________________________________________________________________________
 *
 * File-based tests to make sure Natural Docs can generate symbols correctly.
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
 */

// This file is part of Natural Docs, which is Copyright © 2003-2025 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using System.Text;
using CodeClear.NaturalDocs.Engine.Symbols;
using CodeClear.NaturalDocs.Engine.Topics;


namespace CodeClear.NaturalDocs.Engine.Tests.Framework.TestTypes
	{
	public class Symbols : Framework.BaseTestTypes.SourceToTopics
		{

		public override string OutputOf (IList<Topic> topics)
			{
			if (topics == null || topics.Count == 0)
				{  return "(No topics found)";  }

			StringBuilder output = new StringBuilder();

			for (int i = 0; i < topics.Count; i++)
				{
				if (i != 0)
					{  output.AppendLine("-----");  }

				output.AppendLine("Symbol: " + topics[i].Symbol.FormatWithSeparator('|'));
				output.AppendLine("Ending Symbol: " + topics[i].Symbol.EndingSymbol.ToString());

				if (topics[i].TitleParameters != null)
					{  output.AppendLine("Title Parameters: " + topics[i].TitleParameters.ToString().Replace(ParameterString.SeparatorChar, '|'));  }
				else
					{  output.AppendLine("Title Parameters: (none)");  }

				if (topics[i].PrototypeParameters != null)
					{  output.AppendLine("Prototype Parameters: " + topics[i].PrototypeParameters.ToString().Replace(ParameterString.SeparatorChar, '|'));  }
				else
					{  output.AppendLine("Prototype Parameters: (none)");  }
				}

			return output.ToString();
			}

		}
	}
