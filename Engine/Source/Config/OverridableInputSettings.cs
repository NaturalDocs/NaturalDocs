/* 
 * Class: CodeClear.NaturalDocs.Engine.Config.OverridableInputSettings
 * ____________________________________________________________________________
 * 
 * A set of properties that apply to the input and can be set either globally or to a single output target.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2021 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;


namespace CodeClear.NaturalDocs.Engine.Config
	{
	public class OverridableInputSettings
		{
		
		// Group: Functions
		// __________________________________________________________________________
		
		public OverridableInputSettings ()
			{
			characterEncodingRules = null;
			}

		public OverridableInputSettings (OverridableInputSettings toCopy)
			{
			if (toCopy.HasCharacterEncodingRules)
				{  
				// This is a shallow copy as CharacterEncodingRule is a class and not a struct.  However, CharacterEncodingRule's
				// properties are only set when it's created so it should be okay to share the member objects.
				characterEncodingRules = toCopy.characterEncodingRules.GetRange(0, toCopy.characterEncodingRules.Count);
				}
			else
				{  characterEncodingRules = null;  }
			}			

		/* Function: AddCharacterEncodingRule
		 * Adds a <CharacterEncodingRule> to the list.
		 */
		public void AddCharacterEncodingRule (CharacterEncodingRule rule)
			{
			if (characterEncodingRules == null)
				{  characterEncodingRules = new List<CharacterEncodingRule>();  }

			characterEncodingRules.Add(rule);
			}

		/* Function: AddCharacterEncodingRules
		 * Appends a list of <CharacterEncodingRules> to the list.
		 */
		public void AddCharacterEncodingRules (IList<CharacterEncodingRule> rules)
			{
			if (characterEncodingRules == null)
				{  characterEncodingRules = new List<CharacterEncodingRule>(rules);  }
			else
				{  characterEncodingRules.AddRange(rules);  }
			}



		// Group: Properties
		// __________________________________________________________________________
		
		/* Property: CharacterEncodingRules
		 * A list of <CharacterEncodingRule> objects that apply, or null if there aren't any.
		 */
		public IList<CharacterEncodingRule> CharacterEncodingRules
			{
			get
				{  return characterEncodingRules; }
			}

		/* Property: HasCharacterEncodingRules
		 * Whether any <CharacterEncodingRules> are defined.
		 */
		public bool HasCharacterEncodingRules
			{
			get
				{  return (characterEncodingRules != null);  }
			}
	
		
		// Group: Variables
		// __________________________________________________________________________
		
		protected List<CharacterEncodingRule> characterEncodingRules;
		
		}
	}