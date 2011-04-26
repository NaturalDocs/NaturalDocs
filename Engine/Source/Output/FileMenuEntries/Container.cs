/* 
 * Class: GregValure.NaturalDocs.Engine.Output.FileMenuEntries.Container
 * ____________________________________________________________________________
 * 
 * A base class for entries in <FileMenu> which can contain other entries.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2011 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;


namespace GregValure.NaturalDocs.Engine.Output.FileMenuEntries
	{
	public abstract class Container : Entry
		{

		// Group: Functions
		// __________________________________________________________________________

		public Container () : base ()
			{
			members = new List<Entry>();
			}


		/* Function: Sort
		 * Sorts the members of this container, and recursively sorts the members of any containers found within in.
		 */
		public void Sort ()
			{
			members.Sort();

			foreach (Entry member in members)
				{
				if (member is Container)
					{  (member as Container).Sort();  }
				}
			}


		// Group: Properties
		// __________________________________________________________________________

		/* Property: Members
		 * Returns the list of members this entry contains.  The entire list may be replaced with another one.
		 */
		public List<Entry> Members
			{  
			get
				{  return members;  }
			set
				{  members = value;  }
			}


		// Group: Variables
		// __________________________________________________________________________

		protected List<Entry> members;
		}
	}