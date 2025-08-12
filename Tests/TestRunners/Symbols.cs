﻿/*
 * Class: CodeClear.NaturalDocs.Tests.TestRunners.Symbols
 * ____________________________________________________________________________
 *
 * Tests to make sure Natural Docs can generate symbols correctly.
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


namespace CodeClear.NaturalDocs.Tests.TestRunners
	{
	public class Symbols : TestRunner
		{

		public Symbols ()
			: base (InputMode.Topics, EngineMode.InstanceOnly)
			{  	}

		protected override string RunTest (IList<Topic> topics)
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
