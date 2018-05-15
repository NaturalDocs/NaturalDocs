/* 
 * Class: CodeClear.NaturalDocs.Engine.Tests.Framework.TestTypes.VersionStrings
 * ____________________________________________________________________________
 * 
 * A class to test Natural Docs' link interpreting.
 * 
 * Commands:
 * 
 *		> // text
 *		Comment.
 *		
 *		> Version string
 *		The version string to parse.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2018 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using CodeClear.NaturalDocs.Engine;
using CodeClear.NaturalDocs.Engine.Tests.Framework;


namespace CodeClear.NaturalDocs.Engine.Tests.Framework.TestTypes
	{
	public class VersionStrings : Framework.TextCommands
		{

		public override string OutputOf (IList<string> commands)
			{
			StringBuilder output = new StringBuilder();

			foreach (var command in commands)
				{
				output.AppendLine(command);

				if (command != null && command.Trim() != "" && command.StartsWith("//") == false)
					{
					try
						{
						string versionString = command.Trim();
						Engine.Version version = new Version(versionString);

						output.AppendLine("- Major: " + version.MajorVersion + ", Minor: " + version.MinorVersion + ", Bugfix: " + version.BugFixVersion);
						output.AppendLine("- Release Type: " + version.Type.ToString());

						if (version.Count != 0)
							{  output.AppendLine("- Count: " + version.Count);  }
						if (version.Year != 0)
							{  output.AppendLine("- Year: " + version.Year + ", Month: " + version.Month + ", Day: " + version.Day);  }

						output.Append("- Primary: " + version.PrimaryVersionString);
						
						if (version.SecondaryVersionString != null)
							{  output.AppendLine(", Secondary: " + version.SecondaryVersionString);  }
						else
							{  output.AppendLine(", no secondary");  }

						if (string.Compare(version.ToString(), versionString, true) == 0)
							{  output.AppendLine("- Round trip successful");  }
						else
							{  output.AppendLine("- Round trip failed: \"" + versionString + "\" != \"" + version.ToString() + "\"");  }
						}
					catch (Exception e)
						{
						output.AppendLine("- Exception: " + e.Message);
						}
					}
				}

			return output.ToString();
			}

		}
	}