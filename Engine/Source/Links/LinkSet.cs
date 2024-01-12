/*
 * Class: CodeClear.NaturalDocs.Engine.Links.LinkSet
 * ____________________________________________________________________________
 *
 * A sorted list of <Links> that prevents duplicates from being added as determined by <Link.CompareIdentifyingPropertiesTo()>.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2024 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using CodeClear.NaturalDocs.Engine.Links;


namespace CodeClear.NaturalDocs.Engine.Links
	{
	public class LinkSet : List<Link>
		{

		new public void Add (Link item)
			{
			// If it finds a match, position will be zero or positive.
			// If it doesn't, it will be negative and the bitwise complement of the position it should be inserted into.
			int position = BinarySearch(item, comparer);

			if (position < 0)
				{  Insert(~position, item);  }
			}

		static protected LinkSetComparer comparer = new LinkSetComparer();

		}

	public class LinkSetComparer : System.Collections.Generic.IComparer<Link>
		{
		public int Compare (Link a, Link b)
			{
			if (a == null && b == null)
				{  return 0;  }
			else if (a == null)
				{  return -1;  }
			else if (b == null)
				{  return 1;  }
			else
				{  return a.CompareIdentifyingPropertiesTo(b);  }
			}
		}
	}
