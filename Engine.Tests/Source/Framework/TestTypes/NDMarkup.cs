/*
 * Class: CodeClear.NaturalDocs.Engine.Tests.Framework.TestTypes.NDMarkup
 * ____________________________________________________________________________
 *
 * File-based tests to make sure Natural Docs can convert comments to <NDMarkup> correctly.
 *
 */

// This file is part of Natural Docs, which is Copyright © 2003-2022 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using CodeClear.NaturalDocs.Engine;
using CodeClear.NaturalDocs.Engine.Tests.Framework;
using CodeClear.NaturalDocs.Engine.Topics;


namespace CodeClear.NaturalDocs.Engine.Tests.Framework.TestTypes
	{
	public class NDMarkup : Framework.SourceToTopics
		{

		public override string OutputOf (IList<Topic> topics)
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
