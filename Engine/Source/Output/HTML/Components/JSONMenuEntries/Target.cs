/*
 * Class: CodeClear.NaturalDocs.Engine.Output.HTML.Components.JSONMenuEntries.Target
 * ____________________________________________________________________________
 *
 * A base class for non-container entries in <JSONMenu>.
 *
 */

// This file is part of Natural Docs, which is Copyright © 2003-2026 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;


namespace CodeClear.NaturalDocs.Engine.Output.HTML.Components.JSONMenuEntries
	{
	public class Target : Entry
		{

		// Group: Functions
		// __________________________________________________________________________


		/* Constructor: Target
		 */
		public Target (MenuEntries.Entry menuEntry) : base (menuEntry)
			{
			#if DEBUG
			if (menuEntry is MenuEntries.Container)
				{  throw new Exception("Tried to create a JSON Target with a Container menu entry.");  }
			#endif

			json = null;
			}



		// Group: Properties
		// __________________________________________________________________________


		/* Property: JSON
		 * The generated JSON associated with this entry, or null if it hasn't been generated yet.
		 */
		public string JSON
			{
			get
				{  return json;  }
			set
				{  json = value;  }
			}



		// Group: Variables
		// __________________________________________________________________________


		/* var: json
		 */
		protected string json;

		}
	}
