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
		 */
		static public AbsolutePath FileMenuOutputFile (AbsolutePath targetOutputFolder, int number = 1)
			{
			return MenuOutputFile(targetOutputFolder, FileMenuDataFileIdentifier, number);
			}


		/* Function: FileMenuOutputFileName
		 *
		 * Returns the pathless file name of the JavaScript menu data file with the passed number.
		 *
		 * Examples:
		 *
		 *		number - files3.js
		 */
		static public string FileMenuOutputFileName (int number = 1)
			{
			return MenuOutputFileName(FileMenuDataFileIdentifier, number);
			}


		/* Function: HierarchyMenuOutputFile
		 *
		 * Returns the file name of the JavaScript menu data file for the passed hierarchy and number.
		 *
		 * Examples:
		 *
		 *		targetOutputFolder + hierarchy - C:\Project\Documentation\menu\classes.js
		 *		targetOutputFolder + hierarchy + number - C:\Project\Documentation\menu\classes3.js
		 */
		static public AbsolutePath HierarchyMenuOutputFile (AbsolutePath targetOutputFolder, Hierarchy hierarchy, int number = 1)
			{
			return MenuOutputFile(targetOutputFolder, HierarchyMenuDataFileIdentifier(hierarchy), number);
			}


		/* Function: HierarchyMenuOutputFileName
		 *
		 * Returns the pathless file name of the JavaScript menu data file for the passed hierarchy and number.
		 *
		 * Examples:
		 *
		 *		hierarchy + number - classes3.js
		 */
		static public string HierarchyMenuOutputFileName (Hierarchy hierarchy, int number = 1)
			{
			return MenuOutputFileName(HierarchyMenuDataFileIdentifier(hierarchy), number);
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
		 */
		static public AbsolutePath MenuOutputFile (AbsolutePath targetOutputFolder, string dataFileIdentifier, int number = 1)
			{
			StringBuilder result = new StringBuilder();

			result.Append(targetOutputFolder);
			result.Append("/menu/");
			result.Append(dataFileIdentifier);

			if (number != 1)
				{  result.Append(number);  }

			result.Append(".js");

			return result.ToString();
			}


		/* Function: MenuOutputFileName
		 *
		 * Returns the pathless file name of the JavaScript menu data file with the passed identifier and number.  Generally you
		 * should use <FileMenuOutputFileName()> and <HierarchyMenuOutputFileName()> instead, but this works if you have
		 * the identifier (such as "files" or "classes") instead.
		 *
		 * Examples:
		 *
		 *		identifier + number - classes3.js
		 */
		static public string MenuOutputFileName (string dataFileIdentifier, int number = 1)
			{
			StringBuilder result = new StringBuilder(dataFileIdentifier.Length + 5);

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
		 */
		static public AbsolutePath TabOutputFile (AbsolutePath targetOutputFolder)
			{
			return targetOutputFolder + "/menu/tabs.js";
			}


		/* Function: OutputFolder
		 * Returns the output folder for menu data files.
		 */
		static public AbsolutePath OutputFolder (AbsolutePath targetOutputFolder)
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
