﻿/* 
 * Class: CodeClear.NaturalDocs.Engine.Output.HTML.Components.MenuEntries.Entry
 * ____________________________________________________________________________
 * 
 * A base class for all entries in <Menu>.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2021 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using CodeClear.NaturalDocs.Engine.Hierarchies;


namespace CodeClear.NaturalDocs.Engine.Output.HTML.Components.MenuEntries
	{
	abstract public class Entry
		{

		// Group: Functions
		// __________________________________________________________________________

		/* Function: Entry
		 */
		public Entry (HierarchyType hierarchyType, int hierarchyID)
			{
			title = null;
			parent = null;

			this.hierarchyType = hierarchyType;
			this.hierarchyID = hierarchyID;
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

		/* Property: HierarchyType
		 * The <HierarchyType> this entry appears in.
		 */
		public HierarchyType HierarchyType
			{
			get
				{  return hierarchyType;  }
			set
				{  hierarchyType = value;  }
			}

		/* Property: HierarchyID
		 * The ID of the hierarchy this entry is a member of, or zero if it is part of the file hierarchy.
		 */
		public int HierarchyID
			{
			get
				{  return hierarchyID;  }
			set
				{  hierarchyID = value;  }
			}


		// Group: Variables
		// __________________________________________________________________________

		/* var: title
		 */
		protected string title;

		/* var: parent
		 */
		protected Container parent;

		/* var: hierarchyType
		 */
		protected HierarchyType hierarchyType;

		/* var: hierarchyID
		 * The ID of the hierarchy this entry is a member of, or zero if it is part of the file hierarchy.
		 */
		protected int hierarchyID;

		}
	}