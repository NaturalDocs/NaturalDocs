/* 
 * Class: CodeClear.NaturalDocs.Engine.Styles.PageTypeUtilities
 * ____________________________________________________________________________
 * 
 * A static class of functions related to <PageType>.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2020 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Text;


namespace CodeClear.NaturalDocs.Engine.Styles
	{
	static public class PageTypeUtilities
		{

		/* Function: ToString
		 * Converts the <PageType> to a string.
		 */
		public static string ToString (PageType type)
			{
			switch (type)
				{
				case PageType.All:
					return "All";
				case PageType.Content:
					return "Content";
				case PageType.Frame:
					return "Frame";
				case PageType.Home:
					return "Home";
				default:
					throw new NotImplementedException();
				}
			}

		/* Function: ToPageType
		 * Converts the string to a <PageType> if it can, or null if it can't.
		 */
		public static PageType? ToPageType (string value)
			{
			value = value.ToLower();

			if (value == "all")
				{  return PageType.All;  }
			else if (value == "content")
				{  return PageType.Content;  }
			else if (value == "frame")
				{  return PageType.Frame;  }
			else if (value == "home")
				{  return PageType.Home;  }
			else
				{  return null;  }
			}
			
		}
	}