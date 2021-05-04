/* 
 * Class: CodeClear.NaturalDocs.Engine.Output.HTML.Components.MenuEntries.Class
 * ____________________________________________________________________________
 * 
 * Represents a class in <Menu>.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2021 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;


namespace CodeClear.NaturalDocs.Engine.Output.HTML.Components.MenuEntries
	{
	public class Class : Entry
		{

		// Group: Functions
		// __________________________________________________________________________

		/* Function: Class
		 */
		public Class (Symbols.ClassString classString) : base (classString.HierarchyType)
			{
			this.classString = classString;
			this.Title = classString.Symbol.LastSegment;
			}


		// Group: Properties
		// __________________________________________________________________________

		/* Property: WrappedClassString
		 * The <Symbols.ClassString> associated with this entry.
		 */
		public Symbols.ClassString WrappedClassString
			{
			get
				{  return classString;  }
			}


		// Group: Variables
		// __________________________________________________________________________

		protected Symbols.ClassString classString;

		}
	}