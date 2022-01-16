/*
 * Class: CodeClear.NaturalDocs.Engine.Styles.PageTypes
 * ____________________________________________________________________________
 *
 * A static class of functions related to <PageType>.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2022 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Text;


namespace CodeClear.NaturalDocs.Engine.Styles
	{
	static public class PageTypes
		{

		/* Function: NameOf
		 * Returns the string name of a <PageType>.
		 */
		public static string NameOf (PageType type)
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

		/* Function: FromName
		 * Converts the string name to a <PageType> if it can, or null if it can't.
		 */
		public static PageType? FromName (string value)
			{
			value = value.ToLower();

			if (value == "all")
				{  return PageType.All;  }
			else if (value == "content")
				{  return PageType.Content;  }
			else if (value == "frame")
				{  return PageType.Frame;  }
			else if (value == "home" ||
					   value == "home page" ||
					   value == "homepage")
				{  return PageType.Home;  }
			else
				{  return null;  }
			}

		}
	}
