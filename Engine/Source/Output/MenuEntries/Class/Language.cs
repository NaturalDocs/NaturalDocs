/* 
 * Class: GregValure.NaturalDocs.Engine.Output.MenuEntries.Class.Language
 * ____________________________________________________________________________
 * 
 * A container that represents a language in <Menu>.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2012 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;


namespace GregValure.NaturalDocs.Engine.Output.MenuEntries.Class
	{
	public class Language : Base.Container
		{

		// Group: Functions
		// __________________________________________________________________________

		/* Function: Language
		 */
		public Language (Languages.Language language) : base ()
			{
			this.language = language;
			Title = language.Name;
			}

		override public void Condense ()
			{
			CondenseContainersInMembers();

			if (Members.Count == 1 && Members[0] is Scope)
			   {
			   Scope childScope = (Members[0] as Scope);

			   Members = childScope.Members;

			   if (CondensedTitles == null)
			      {  CondensedTitles = new List<string>();  }

			   CondensedTitles.Add(childScope.Title);

			   if (childScope.CondensedTitles != null)
			      {  CondensedTitles.AddRange(childScope.CondensedTitles);  }

			   condensedScopeString = childScope.WrappedScopeString;
				}
			}


		// Group: Properties
		// __________________________________________________________________________

		/* Property: WrappedLanguage
		 * The language associated with this entry.
		 */
		public Languages.Language WrappedLanguage
			{
			get
				{  return language;  }
			}

		/* Property: CondensedScopeString
		 * If this language had a scope condensed into it, this will be the scope <Symbols.SymbolString> associated 
		 * with that entry.
		 */
		public Symbols.SymbolString CondensedScopeString
			{
			get
				{  return condensedScopeString;  }
			}


		// Group: Variables
		// __________________________________________________________________________

		/* var: language
		 */
		protected Languages.Language language;

		/* var: condensedScopeString
		 */
		protected Symbols.SymbolString condensedScopeString;

		}
	}