/* 
 * Class: CodeClear.NaturalDocs.Engine.Output.HTML.Paths.Menu
 * ____________________________________________________________________________
 * 
 * Path functions relating to menu data in HTML output.
 * 
 */

// This file is part of Natural Docs, which is Copyright © 2003-2020 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Text;
using CodeClear.NaturalDocs.Engine.Symbols;


namespace CodeClear.NaturalDocs.Engine.Output.HTML.Paths
	{
	static public class Menu
		{

		/* Function: OutputFile
		 * 
		 * Returns the file name of the JavaScript data file with the passed type and ID number.
		 * 
		 * Examples:
		 * 
		 *		targetOutputFolder + type + id - C:\Project\Documentation\menu\files3.js
		 *		targetOutputFolder + type + id + fileNameOnly - files3.js
		 */
		static public Path OutputFile (Path targetOutputFolder, string type, int id = 1, bool fileNameOnly = false)
			{
			StringBuilder result = new StringBuilder();

			if (!fileNameOnly)
				{
				result.Append(targetOutputFolder);
				result.Append("/menu/");
				}

			result.Append(type);

			if (id != 1)
				{  result.Append(id);  }

			result.Append(".js");

			return result.ToString();
			}


		/* Function: OutputFolder
		 * Returns the output folder for menu data files.
		 */
		static public Path OutputFolder (Path targetOutputFolder)
			{
			return targetOutputFolder + "/menu";
			}

		}
	}
