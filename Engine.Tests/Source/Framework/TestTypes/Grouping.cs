/* 
 * Class: GregValure.NaturalDocs.Engine.Tests.Framework.TestTypes.Grouping
 * ____________________________________________________________________________
 * 
 */

// This file is part of Natural Docs, which is Copyright © 2003-2013 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using GregValure.NaturalDocs.Engine;
using GregValure.NaturalDocs.Engine.Tests.Framework;
using GregValure.NaturalDocs.Engine.Topics;


namespace GregValure.NaturalDocs.Engine.Tests.Framework.TestTypes
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
			int groupID = Engine.Instance.TopicTypes.IDFromKeyword("group");

			for (int i = 0; i < topics.Count; i++)
				{
				if (topics[i].DefinesClass)
					{
					inClass = false;
					inGroup = false;

					if (i != 0)
						{  output.AppendLine();  }
					}
				else if (topics[i].TopicTypeID == groupID)
					{
					inGroup = false;
					}

				if (inClass)
					{  output.Append(' ', 3);  }
				if (inGroup)
					{  output.Append(' ', 3);  }

				output.Append(Engine.Instance.TopicTypes.FromID(topics[i].TopicTypeID).DisplayName + ": ");

				if (topics[i].Title == null)
					{  output.AppendLine("(No title detected)");  }
				else
					{  output.AppendLine(topics[i].Title);  }

				if (topics[i].DefinesClass)
					{
					inClass = true;
					inGroup = false;
					}
				else if (topics[i].TopicTypeID == groupID)
					{
					inGroup = true;
					}
				}

			return output.ToString();
			}

		}
	}