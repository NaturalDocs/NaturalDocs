/* 
 * Class: CodeClear.NaturalDocs.Engine.Tests.Framework.TestTypes.Grouping
 * ____________________________________________________________________________
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
using CodeClear.NaturalDocs.Engine.Tests.Framework;
using CodeClear.NaturalDocs.Engine.Topics;


namespace CodeClear.NaturalDocs.Engine.Tests.Framework.TestTypes
	{
	public class Grouping : Framework.SourceToTopics
		{

		public override string OutputOf (IList<Topic> topics)
			{
			if (topics == null || topics.Count == 0)
				{  return "(No topics found)";  }

			StringBuilder output = new StringBuilder();
			bool inClass = false;
			bool inGroup = false;

			for (int i = 0; i < topics.Count; i++)
				{
				if (topics[i].DefinesClass)
					{
					inClass = false;
					inGroup = false;

					if (i != 0)
						{  output.AppendLine();  }
					}
				else if (topics[i].IsGroup)
					{
					inGroup = false;
					}

				if (inClass)
					{  output.Append(' ', 3);  }
				if (inGroup)
					{  output.Append(' ', 3);  }

				output.Append(EngineInstance.CommentTypes.FromID(topics[i].CommentTypeID).DisplayName + ": ");

				if (topics[i].Title == null)
					{  output.AppendLine("(No title detected)");  }
				else
					{  output.AppendLine(topics[i].Title);  }

				if (topics[i].DefinesClass)
					{
					inClass = true;
					inGroup = false;
					}
				else if (topics[i].IsGroup)
					{
					inGroup = true;
					}
				}

			return output.ToString();
			}

		}
	}