/*
 * Class: CodeClear.NaturalDocs.Tests.TestRunners.NDMarkup
 * ____________________________________________________________________________
 *
 * Tests to make sure Natural Docs can convert comments to <NDMarkup> correctly.
 *
 */

// This file is part of Natural Docs, which is Copyright © 2003-2026 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using System.Text;
using CodeClear.NaturalDocs.Engine.Topics;


namespace CodeClear.NaturalDocs.Tests.TestRunners
	{
	public class NDMarkup : TestRunner
		{

		public NDMarkup ()
			: base (InputMode.Topics, EngineMode.InstanceOnly)
			{  	}

		protected override string RunTest (IList<Topic> topics)
			{
			if (topics == null || topics.Count == 0)
				{  return "(No topics found)";  }

			StringBuilder output = new StringBuilder();

			bool isFirst = true;
			for (int i = 0; i < topics.Count; i++)
				{
				if (topics[i].IsEmbedded == false)
					{
					if (isFirst)
						{  isFirst = false;  }
					else
						{  output.AppendLine("-----");  }

					if (topics[i].Body == null)
						{  output.AppendLine("(No body detected)");  }
					else
						{  output.AppendLine(topics[i].Body);  }
					}
				}

			return output.ToString();
			}

		}
	}
