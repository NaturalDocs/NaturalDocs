/*
 * Class: CodeClear.NaturalDocs.Engine.Output.HTML.Components.JSONMenuEntries.Entry
 * ____________________________________________________________________________
 *
 * A base class for all entries in <JSONMenu>.
 *
 */

// This file is part of Natural Docs, which is Copyright © 2003-2026 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;


namespace CodeClear.NaturalDocs.Engine.Output.HTML.Components.JSONMenuEntries
	{
	abstract public class Entry
		{

		// Group: Functions
		// __________________________________________________________________________


		/* Constructor: Entry
		 */
		public Entry (MenuEntries.Entry menuEntry)
			{
			this.menuEntry = menuEntry;
			}



		// Group: Properties
		// __________________________________________________________________________


		/* Property: MenuEntry
		 * The <MenuEntries.Entry> associated with this one.
		 */
		public MenuEntries.Entry MenuEntry
			{
			get
				{  return menuEntry;  }
			}



		// Group: Variables
		// __________________________________________________________________________


		/* var: menuEntry
		 * The <MenuEntries.Entry> associated with this one.
		 */
		protected MenuEntries.Entry menuEntry;

		}
	}
