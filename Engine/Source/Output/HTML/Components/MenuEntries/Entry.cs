/* 
 * Class: CodeClear.NaturalDocs.Engine.Output.HTML.Components.MenuEntries.Entry
 * ____________________________________________________________________________
 * 
 * A base class for all entries in <Menu>.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2020 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;


namespace CodeClear.NaturalDocs.Engine.Output.HTML.Components.MenuEntries
	{
	abstract public class Entry
		{

		// Group: Functions
		// __________________________________________________________________________

		/* Function: Entry
		 */
		public Entry (Hierarchy hierarchy)
			{
			title = null;
			parent = null;

			this.hierarchy = hierarchy;
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
		public Container Parent
			{
			get
				{  return parent;  }
			set
				{  parent = value;  }
			}

		/* Property: Hierarchy
		 * The <Engine.Hierarchy> this entry appears in.
		 */
		public Hierarchy Hierarchy
			{
			get
				{  return hierarchy;  }
			set
				{  hierarchy = value;  }
			}


		// Group: Variables
		// __________________________________________________________________________

		/* var: title
		 */
		protected string title;

		/* var: parent
		 */
		protected Container parent;

		/* var: hierarchy
		 */
		protected Hierarchy hierarchy;

		}
	}