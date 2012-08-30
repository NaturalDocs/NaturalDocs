/* 
 * Class: GregValure.NaturalDocs.Engine.Output.MenuEntries.Base.Container
 * ____________________________________________________________________________
 * 
 * A base class for <Entries> which can contain other entries, such as folders.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2012 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;


namespace GregValure.NaturalDocs.Engine.Output.MenuEntries.Base
	{
	public class Container : Base.Entry
		{

		// Group: Functions
		// __________________________________________________________________________

		/* Function: Container
		 */
		public Container () : base ()
			{
			members = new List<Base.Entry>();
			condensedTitles = null;
			}

		/* Function: Sort
		 * Sorts the members of this container, and recursively sorts the members of any containers found within it.
		 */
		public void Sort ()
			{
			members.Sort(CompareMembers);

			foreach (Base.Entry member in members)
				{
				if (member is Base.Container)
					{  (member as Base.Container).Sort();  }
				}
			}

		/* Function: CompareMembers
		 */
		static protected int CompareMembers (Base.Entry a, Base.Entry b)
			{
			int result = String.Compare(a.Title, b.Title, true);

			if (result != 0)
				{  return result;  }

			result = String.Compare(a.Title, b.Title, false);

			if (result != 0)
				{  return result;  }

			if (a is Base.Container)
				{  return (b is Base.Container ? 0 : 1);  }
			else if (b is Base.Container)
				{  return (a is Base.Container ? 0 : -1);  }
			else
				{  return 0;  }
			}



		// Group: Properties
		// __________________________________________________________________________

		/* Property: Members
		 * The list of members this entry contains.  The entire list may be replaced with another one, which is
		 * useful when merging containers.
		 */
		public IList<Base.Entry> Members
			{  
			get
				{  return members;  }
			}

		/* Property: CondensedTitles
		 * If another container was condensed into this one, this will be a list of their titles.  Otherwise null.
		 */
		public IList<string> CondensedTitles
			{
			get
				{  return condensedTitles;  }
			}


		// Group: Variables
		// __________________________________________________________________________

		/* var: members
		 */
		protected List<Base.Entry> members;

		/* var: condensedTitles
		 */
		protected List<string> condensedTitles;

		}
	}