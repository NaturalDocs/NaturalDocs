/*
 * Class: CodeClear.NaturalDocs.Engine.Output.HTML.Paths.SearchIndex
 * ____________________________________________________________________________
 *
 * Path functions relating to search index data in HTML output.
 *
 */

// This file is part of Natural Docs, which is Copyright © 2003-2026 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Text;


namespace CodeClear.NaturalDocs.Engine.Output.HTML.Paths
	{
	static public class SearchIndex
		{

		/* Function: IndexOutputFile
		 *
		 * Returns the file name of the JavaScript data file which stores the index of all the prefix data files.
		 *
		 * Examples:
		 *
		 *		targetOutputFolder - C:\Project\Documentation\search\index.js
		 */
		static public AbsolutePath IndexOutputFile (Path targetOutputFolder)
			{
			return targetOutputFolder + "/search/index.js";
			}


		/* Function: PrefixOutputFile
		 *
		 * Returns the file name of the JavaScript data file for the passed prefix.
		 *
		 * Examples:
		 *
		 *		targetOutputFolder + prefix - C:\Project\Documentation\search\keywords\006a0073006f.js
		 */
		static public AbsolutePath PrefixOutputFile (Path targetOutputFolder, string prefix)
			{
			#if DEBUG
			if (prefix.Length > 3)
				{  throw new Exception ("PrefixOutputFile assumes the prefix will be 3 characters or less.");  }
			#endif

			StringBuilder result = new StringBuilder(targetOutputFolder.Length + 17 + 15);

			result.Append(targetOutputFolder);
			result.Append("/search/keywords/");

			for (int i = 0; i < prefix.Length; i++)
				{
				uint intValue = char.ToLower(prefix[i]);
				result.Append(intValue.ToString("x4"));
				}

			result.Append(".js");
			return result.ToString();
			}


		/* Function: OutputFolder
		 * Returns the root output folder for search index data files.
		 */
		static public AbsolutePath OutputFolder (AbsolutePath targetOutputFolder)
			{
			return targetOutputFolder + "/search";
			}

		}
	}
