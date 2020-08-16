/* 
 * Class: CodeClear.NaturalDocs.Engine.Output.HTML.PageTypes
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
	static public class PageTypes
		{

		// Group: Functions
		// __________________________________________________________________________


		/* Function: NameOf
		 * Returns the string name of a <PageType>.
		 */
		public static string NameOf (PageType type)
			{
			return AllNames[(int)type];
			}


		/* Function: FromName
		 * Converts the string name to a <PageType> if it can, or null if it can't.
		 */
		public static PageType? FromName (string value)
			{
			for (int i = 0; i < AllNames.Length; i++)
				{
				if (String.Compare(value, AllNames[i], true) == 0)
					{  return All[i];  }
				}

			return null;
			}



		// Group: Properties
		// __________________________________________________________________________


		/* Property: Count
		 * The number of <PageTypes> there are.
		 */
		static public int Count
			{
			get
				{  return All.Length;  }
			}



		// Group: Static Variables
		// __________________________________________________________________________


		/* var: All
		 * A static array of all the choices in <PageType>.  The values will be in the same order as <AllNames>.
		 */
		public static readonly PageType[] All = { PageType.All, PageType.Frame, PageType.Content, PageType.Home };
		// DEPENDENCY: This depends on the indexes matching the integer values of PageType


		/* var: AllNames
		 * A static array of simple A-Z names for each <PageType>.  The values will be in the same order as <All>.
		 */
		public static readonly string[] AllNames = { "All", "Frame", "Content", "Home" };
		// DEPENDENCY: This depends on the indexes matching the integer values of PageType
			
		}
	}