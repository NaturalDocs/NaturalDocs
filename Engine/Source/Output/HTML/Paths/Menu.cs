/* 
 * Class: CodeClear.NaturalDocs.Engine.Output.HTML.Paths.Menu
 * ____________________________________________________________________________
 * 
 * Path functions relating to menu data in HTML output.
 * 
 */

// This file is part of Natural Docs, which is Copyright © 2003-2021 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Text;
using CodeClear.NaturalDocs.Engine.Hierarchies;


namespace CodeClear.NaturalDocs.Engine.Output.HTML.Paths
	{
	static public class Menu
		{

		/* Function: OutputFile
		 * 
		 * Returns the file name of the JavaScript data file with the passed hierarchy and ID number.
		 * 
		 * Examples:
		 * 
		 *		targetOutputFolder + hierarchy + id - C:\Project\Documentation\menu\files3.js
		 *		targetOutputFolder + hierarchy + id + fileNameOnly - files3.js
		 */
		static public Path OutputFile (Path targetOutputFolder, HierarchyType hierarchy, int id = 1, bool fileNameOnly = false)
			{
			StringBuilder result = new StringBuilder();

			if (!fileNameOnly)
				{
				result.Append(targetOutputFolder);
				result.Append("/menu/");
				}

			switch (hierarchy)
				{
				case HierarchyType.File:
					result.Append("files");
					break;
				case HierarchyType.Class:
					result.Append("classes");
					break;
				case HierarchyType.Database:
					result.Append("database");
					break;
				default:
					throw new NotImplementedException();
				}

			if (id != 1)
				{  result.Append(id);  }

			result.Append(".js");

			return result.ToString();
			}


		/* Function: TabOutputFile
		 * 
		 * Returns the file name of the JavaScript data file which stores the tab information.
		 * 
		 * Examples:
		 * 
		 *		targetOutputFolder - C:\Project\Documentation\menu\tabs.js
		 *		targetOutputFolder + fileNameOnly - tabs.js
		 */
		static public Path TabOutputFile (Path targetOutputFolder, bool fileNameOnly = false)
			{
			if (fileNameOnly)
				{  return "tabs.js";  }
			else
				{  return (targetOutputFolder + "/menu/tabs.js");  }
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
