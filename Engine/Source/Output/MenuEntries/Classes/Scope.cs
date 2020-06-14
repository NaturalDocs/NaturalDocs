/* 
 * Class: CodeClear.NaturalDocs.Engine.Output.MenuEntries.Classes.Scope
 * ____________________________________________________________________________
 * 
 * Represents a scope in <Menu>, aka a container for all classes appearing at the same level of the
 * hierarchy.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2020 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;


namespace CodeClear.NaturalDocs.Engine.Output.MenuEntries.Classes
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
			{
			CondenseContainersInMembers();

			if (Members.Count == 1 && Members[0] is Scope)
				{
				Scope childScope = (Members[0] as Scope);

				Members = childScope.Members;

				foreach (var member in Members)
					{  member.Parent = this;  }

				if (CondensedTitles == null)
					{  CondensedTitles = new List<string>();  }

				CondensedTitles.Add(childScope.Title);

				if (childScope.CondensedTitles != null)
					{  CondensedTitles.AddRange(childScope.CondensedTitles);  }

				scopeString = childScope.scopeString;
				}
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