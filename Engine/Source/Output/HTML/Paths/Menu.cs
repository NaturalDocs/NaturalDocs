/*
 * Class: CodeClear.NaturalDocs.Engine.Output.HTML.Paths.Menu
 * ____________________________________________________________________________
 *
 * Path functions relating to menu data in HTML output.
 *
 */

// This file is part of Natural Docs, which is Copyright © 2003-2026 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Text;
using CodeClear.NaturalDocs.Engine.Hierarchies;


namespace CodeClear.NaturalDocs.Engine.Output.HTML.Paths
	{
	static public class Menu
		{

		/* Function: FileMenuOutputFile
		 *
		 * Returns the file name of the JavaScript menu data file with the passed number.
		 *
		 * Examples:
		 *
		 *		targetOutputFolder - C:\Project\Documentation\menu\files.js
		 *		targetOutputFolder + number - C:\Project\Documentation\menu\files3.js
		 *		targetOutputFolder + number + fileNameOnly - files3.js
		 */
		static public Path FileMenuOutputFile (Path targetOutputFolder, int number = 1, bool fileNameOnly = false)
			{
			return MenuOutputFile(targetOutputFolder, FileMenuDataFileIdentifier, number, fileNameOnly);
			}


		/* Function: HierarchyMenuOutputFile
		 *
		 * Returns the file name of the JavaScript menu data file for the passed hierarchy and number.
		 *
		 * Examples:
		 *
		 *		targetOutputFolder + hierarchy - C:\Project\Documentation\menu\classes.js
		 *		targetOutputFolder + hierarchy + number - C:\Project\Documentation\menu\classes3.js
		 *		targetOutputFolder + hierarchy + number + fileNameOnly - classes3.js
		 */
		static public Path HierarchyMenuOutputFile (Path targetOutputFolder, Hierarchy hierarchy, int number = 1, bool fileNameOnly = false)
			{
			return MenuOutputFile(targetOutputFolder, HierarchyMenuDataFileIdentifier(hierarchy), number, fileNameOnly);
			}


		/* Function: MenuOutputFile
		 *
		 * Returns the file name of the JavaScript menu data file with the passed identifier and number.  Generally you should use
		 * <FileMenuOutputFile()> and <HierarchyMenuOutputFile()> instead, but this works if you have the identifier (such as
		 * "files" or "classes") instead.
		 *
		 * Examples:
		 *
		 *		targetOutputFolder + identifier - C:\Project\Documentation\menu\classes.js
		 *		targetOutputFolder + identifier + number - C:\Project\Documentation\menu\classes3.js
		 *		targetOutputFolder + identifier + number + fileNameOnly - classes3.js
		 */
		static public Path MenuOutputFile (Path targetOutputFolder, string dataFileIdentifier, int number = 1, bool fileNameOnly = false)
			{
			StringBuilder result = new StringBuilder();

			if (!fileNameOnly)
				{
				result.Append(targetOutputFolder);
				result.Append("/menu/");
				}

			result.Append(dataFileIdentifier);

			if (number != 1)
				{  result.Append(number);  }

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


		/* Property: FileMenuDataFileIdentifier
		 * The identifier that gets used in file menu data files, such as "files".
		 */
		static public string FileMenuDataFileIdentifier
			{
			get
				{  return "files";  }
			}


		/* Function: HierarchyMenuDataFileIdentifier
		 * The identifier that gets used in the menu data files of the passed hierarchy, such as "classes" or "database".
		 */
		static public string HierarchyMenuDataFileIdentifier (Hierarchy hierarchy)
			{
			return hierarchy.PluralSimpleIdentifier.ToLowerInvariant();
			}

		}
	}
