/* 
 * Class: GregValure.NaturalDocs.Engine.Output.MenuEntries.Base.Entry
 * ____________________________________________________________________________
 * 
 * A base class for all entries in <Menu>.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2012 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;


namespace GregValure.NaturalDocs.Engine.Output.MenuEntries.Base
	{
	abstract public class Entry //: IComparable<Entry>
		{

		// Group: Functions
		// __________________________________________________________________________

		/* Function: Entry
		 */
		public Entry ()
			{
			title = null;
			parent = null;
			extraData = null;
			}


		// Group: Properties
		// __________________________________________________________________________

		/* Property: Title
		 * The title of the menu entry, or null if none.  This can only be null if the entry is not expected to be displayed,
		 * such as a container that will be collapsed.
		 */
		public string Title
			{
			get
				{  return title;  }
			set
				{  title = value;  }
			}

		/* Property: Parent
		 * The parent of this entry, or null if none.
		 */
		public Base.Container Parent
			{
			get
				{  return parent;  }
			set
				{  parent = value;  }
			}

		/* Property: ExtraData
		 * A object reference that can be used to attach arbitrary data to an individual entry.  This can be used to aid 
		 * code that converts the menu to an output format.
		 */
		public object ExtraData
			{
			get
				{  return extraData;  }
			set
				{  extraData = value;  }
			}


		// Group: Variables
		// __________________________________________________________________________

		/* var: title
		 */
		protected string title;

		/* var: parent
		 */
		protected Base.Container parent;

		/* var: extraData
		 */
		protected object extraData;

		}
	}