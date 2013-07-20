/* 
 * Class: GregValure.NaturalDocs.Engine.Output.MenuEntries.Base.Container
 * ____________________________________________________________________________
 * 
 * A base class for <Entries> which can contain other entries, such as folders.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2013 Greg Valure.
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
		 * The function used for sorting members.
		 */
		static protected int CompareMembers (Base.Entry a, Base.Entry b)
			{
			string aTitle = a.Title;
			string bTitle = b.Title;


			// If we're comparing two files we want to ignore the extension so A.cpp appears before A.B.cpp.

			bool comparingFiles = (a is MenuEntries.Files.File && b is MenuEntries.Files.File);

			if (comparingFiles)
				{
				aTitle = new Path(aTitle).NameWithoutPathOrExtension;
				bTitle = new Path(bTitle).NameWithoutPathOrExtension;
				}


			// Compare in a case-insensitive way first, but fall back to a case-sensitive compare if they're equal.  This means
			// both a.cpp and A.cpp will appear before B.cpp, but they will appear consistently relative to each other.

			int result = String.Compare(aTitle, bTitle, true);

			if (result != 0)
				{  return result;  }

			result = String.Compare(aTitle, bTitle, false);

			if (result != 0)
				{  return result;  }


			// If we're comparing two files, try again with extensions so a.cpp and a.h appear consistently.

			if (comparingFiles)
				{
				result = String.Compare(a.Title, b.Title, true);

				if (result != 0)
					{  return result;  }

				result = String.Compare(a.Title, b.Title, false);

				if (result != 0)
					{  return result;  }
				}


			// At this point the titles are exactly equal.  Make sure non-containers come first so the class A will
			// appear before the container A which leads to A.B.

			if (a is Base.Container)
				{  return (b is Base.Container ? 0 : 1);  }
			else if (b is Base.Container)
				{  return (a is Base.Container ? 0 : -1);  }
			else
				{  return 0;  }
			}


		/* Function: Condense
		 * 
		 * An overridable function to handle condensing unnecessary levels into this one.  By default this will not alter the
		 * menu itself, but it will call Condense() on any subcontainers in case they implement it.
		 * 
		 * A simple way to implement this function is to call <CondenseContainersInMembers()> first, check to see if you
		 * have only one container as a member, and then implement the logic to combine them if so.
		 */
		public virtual void Condense ()
			{
			CondenseContainersInMembers();
			}


		/* Function: CondenseContainersInMembers
		 * Goes through the members of this container and calls <Condense()> on each one that is also a container.
		 */
		protected void CondenseContainersInMembers ()
			{
			foreach (Base.Entry member in members)
				{
				if (member is Base.Container)
					{  (member as Base.Container).Condense();  }
				}
			}



		// Group: Properties
		// __________________________________________________________________________

		/* Property: Members
		 * The list of members this entry contains.  The entire list may be replaced with another one, which is
		 * useful when merging containers.
		 */
		public List<Base.Entry> Members
			{  
			get
				{  return members;  }
			set
				{  members = value;  }
			}

		/* Property: CondensedTitles
		 * If another container was condensed into this one, this will be a list of their titles.  Otherwise null.
		 */
		public List<string> CondensedTitles
			{
			get
				{  return condensedTitles;  }
			set
				{  condensedTitles = value;  }
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