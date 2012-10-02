/* 
 * Class: GregValure.NaturalDocs.Engine.Output.MenuEntries.Class.Scope
 * ____________________________________________________________________________
 * 
 * Represents a scope in <Menu>, aka a container for all classes appearing at the same level of the
 * hierarchy.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2012 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;


namespace GregValure.NaturalDocs.Engine.Output.MenuEntries.Class
	{
	public class Scope : Base.Container
		{

		// Group: Functions
		// __________________________________________________________________________

		/* Function: Scope
		 */
		public Scope (Symbols.SymbolString scopeString) : base ()
			{
			this.scopeString = scopeString;
			this.Title = scopeString.LastSegment;
			}

		override public void Condense ()
			{ // xxx
			//CondenseContainersInMembers();

			//if (Members.Count == 1 && Members[0] is Folder)
			//   {
			//   Folder subFolder = (Members[0] as Folder);

			//   Members = subFolder.Members;

			//   if (CondensedTitles == null)
			//      {  CondensedTitles = new List<string>();  }

			//   CondensedTitles.Add(subFolder.Title);

			//   if (subFolder.CondensedTitles != null)
			//      {  CondensedTitles.AddRange(subFolder.CondensedTitles);  }

			//   pathFromFileSource = subFolder.pathFromFileSource;
			//   }
			}

		// Group: Properties
		// __________________________________________________________________________

		/* Property: WrappedScopeString
		 * The scope <Symbols.SymbolString> associated with this entry.
		 */
		public Symbols.SymbolString WrappedScopeString
			{
			get
				{  return scopeString;  }
			}


		// Group: Variables
		// __________________________________________________________________________

		protected Symbols.SymbolString scopeString;

		}
	}