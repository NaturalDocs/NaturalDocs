/*
 * Class: CodeClear.NaturalDocs.Engine.Output.HTML.Paths.Other
 * ____________________________________________________________________________
 *
 * Path functions relating to files in the "other" folder in HTML output.
 *
 */

// This file is part of Natural Docs, which is Copyright © 2003-2026 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Text;


namespace CodeClear.NaturalDocs.Engine.Output.HTML.Paths
	{
	static public class Other
		{

		/* Function: HomePageFile
		 * Returns the output file name of the home page.
		 */
		static public AbsolutePath HomePageFile (AbsolutePath targetOutputFolder)
			{
			return OutputFile(targetOutputFolder, "home.html");
			}


		/* Function: OutputFile
		 * Returns the output file name of the file in the "other" folder.
		 */
		static public AbsolutePath OutputFile (AbsolutePath targetOutputFolder, RelativePath relativePath)
			{
			return targetOutputFolder + "/other/" + Paths.Utilities.Sanitize(relativePath);
			}


		/* Function: OutputFolder
		 *
		 * Returns the output folder for "other" files.
		 *
		 * Examples:
		 *
		 *		targetOutputFolder - C:\Project\Documentation\other
		 */
		static public AbsolutePath OutputFolder (AbsolutePath targetOutputFolder)
			{
			return targetOutputFolder + "/other";
			}

		}
	}
