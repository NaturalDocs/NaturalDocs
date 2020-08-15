/* 
 * Class: CodeClear.NaturalDocs.Engine.Output.HTML.PageTypeUtilities
 * ____________________________________________________________________________
 * 
 * A static class of functions related to <PageType>.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2020 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;


namespace CodeClear.NaturalDocs.Engine.Output.HTML
	{
	static public class PageTypeUtilities
		{

		// Group: Functions
		// __________________________________________________________________________


		/* Function: ToString
		 * Converts the <PageType> to a string.
		 */
		public static string ToString (PageType type)
			{
			return AllPageTypeNames[(int)type];
			}


		/* Function: ToPageType
		 * Converts the string to a <PageType> if it can, or null if it can't.
		 */
		public static PageType? ToPageType (string value)
			{
			for (int i = 0; i < AllPageTypeNames.Length; i++)
				{
				if (String.Compare(value, AllPageTypeNames[i], true) == 0)
					{  return AllPageTypes[i];  }
				}

			return null;
			}



		// Group: Static Variables
		// __________________________________________________________________________


		/* var: AllPageTypes
		 * A static array of all the choices in <PageType>.  The values will be in the same order as <AllPageTypeNames>.
		 */
		public static readonly PageType[] AllPageTypes = { PageType.All, PageType.Frame, PageType.Content, PageType.Home };
		// DEPENDENCY: This depends on the indexes matching the integer values of PageType


		/* var: AllPageTypeNames
		 * A static array of simple A-Z names for each <PageType>.  The values will be in the same order as <AllPageTypes>.
		 */
		public static readonly string[] AllPageTypeNames = { "All", "Frame", "Content", "Home" };
		// DEPENDENCY: This depends on the indexes matching the integer values of PageType
			
		}
	}