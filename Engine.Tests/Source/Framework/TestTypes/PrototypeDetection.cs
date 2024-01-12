/*
 * Class: CodeClear.NaturalDocs.Engine.Tests.Framework.TestTypes.PrototypeDetection
 * ____________________________________________________________________________
 *
 * File-based tests to make sure Natural Docs can detect prototypes correctly.
 *
 */

// This file is part of Natural Docs, which is Copyright © 2003-2024 Code Clear LLC.
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
	public class PrototypeDetection : Framework.SourceToTopics
		{

		public override string OutputOf (IList<Topic> topics)
			{
			if (topics == null || topics.Count == 0)
				{  return "(No topics found)";  }

			StringBuilder output = new StringBuilder();

			for (int i = 0; i < topics.Count; i++)
				{
				// We manually use \n because calling AppendLine() will use \r\n, which conflicts with the plain
				// \n's in the prototypes.

				if (i != 0)
					{  output.Append("-----\n");  }

				if (topics[i].Prototype == null)
					{  output.Append("(No prototype detected)\n");  }
				else
					{
					output.Append(topics[i].Prototype);
					output.Append('\n');
					}
				}

			return output.ToString();
			}

		}
	}
