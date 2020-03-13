/* 
 * Class: CodeClear.NaturalDocs.Engine.Links.ImageLinkSet
 * ____________________________________________________________________________
 * 
 * A sorted list of <ImageLinks> that prevents duplicates from being added as determined by 
 * <ImageLink.CompareIdentifyingPropertiesTo()>.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2020 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using CodeClear.NaturalDocs.Engine.Links;


namespace CodeClear.NaturalDocs.Engine.Links
	{
	public class ImageLinkSet : List<ImageLink>
		{

		new public void Add (ImageLink item)
			{
			// If it finds a match, position will be zero or positive.
			// If it doesn't, it will be negative and the bitwise complement of the position it should be inserted into.
			int position = BinarySearch(item, comparer);

			if (position < 0)
				{  Insert(~position, item);  }
			}

		static protected ImageLinkSetComparer comparer = new ImageLinkSetComparer();
				
		}

	public class ImageLinkSetComparer : System.Collections.Generic.IComparer<ImageLink>
		{
		public int Compare (ImageLink a, ImageLink b)
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
