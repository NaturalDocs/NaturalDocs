/* 
 * Class: CodeClear.NaturalDocs.Engine.Languages.ShebangStringComparer
 * ____________________________________________________________________________
 * 
 * An implementation of IComparer that incorporates string length.  Longer strings are less than shorter strings, and if 
 * two strings are equal lengths it does a regular string comparison.  This is done so when you iterate through a
 * <Collections.SortedStringTable> of shebang strings, the longer strings come first.  This is important because someone
 * could conceivably define one language with shebang string "php5" and another with just "php".  We want the longer
 * one to be tested against first.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2021 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;


namespace CodeClear.NaturalDocs.Engine.Languages
	{
	public class ShebangStringComparer : System.Collections.Generic.IComparer<string>
		{
		public int Compare (string a, string b)
			{
			int aLength, bLength;
			
			if (a == null)
				{  aLength = 0;  }
			else
				{  aLength = a.Length;  }
				
			if (b == null)
				{  bLength = 0;  }
			else
				{  bLength = b.Length;  }
				
			if (aLength != bLength)
				{  return bLength - aLength;  }
			else if (aLength == 0)  // Both null
				{  return 0;  }
			else
				{  return a.CompareTo(b);  }
			}
		}
	}