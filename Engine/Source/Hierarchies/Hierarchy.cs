/* 
 * Class: CodeClear.NaturalDocs.Engine.Hierarchies.Hierarchy
 * ____________________________________________________________________________
 * 
 * Information about a single hierarchy within Natural Docs.
 * 
 */

// This file is part of Natural Docs, which is Copyright © 2003-2021 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;


namespace CodeClear.NaturalDocs.Engine.Hierarchies
	{
	public class Hierarchy
		{
		
		// Group: Functions
		// __________________________________________________________________________
		
		
		/* Constructor: Hierarchy
		 */
		public Hierarchy (string name, string pluralName, string simpleIdentifier, string pluralSimpleIdentifier,
								 HierarchyType type, bool isLanguageSpecific, bool isCaseSensitive = true)
			{
			this.name = name;
			this.pluralName = pluralName;
			this.simpleIdentifier = simpleIdentifier;
			this.pluralSimpleIdentifier = pluralSimpleIdentifier;
			this.type = type;
			this.isLanguageSpecific = isLanguageSpecific;
			this.isCaseSensitive = isCaseSensitive;
			}



		// Group: Properties
		// __________________________________________________________________________


		/* Property: ID
		 * The numeric ID of the hierarchy.
		 */
		public int ID
			{
			get
				{  return (int)type;  }
			}

		/* Property: Name
		 * The name of the hierarchy.
		 */
		public string Name
			{
			get
				{  return name;  }
			}

		/* Property: PluralName
		 * The plural name of the hierarchy.
		 */
		public string PluralName
			{
			get
				{  return pluralName;  }
			}

		/* Property: SimpleIdentifier
		 * The hierarchy's name using only the letters A to Z.
		 */
		public string SimpleIdentifier
			{
			get
				{  return simpleIdentifier;  }
			set
				{  simpleIdentifier = value;  }
			}
			
		/* Property: PluralSimpleIdentifier
		 * The hierarchy's plural name using only the letters A to Z.
		 */
		public string PluralSimpleIdentifier
			{
			get
				{  return pluralSimpleIdentifier;  }
			set
				{  pluralSimpleIdentifier = value;  }
			}
			
		/* Property: Type
		 * The <HierarchyType> of the hierarchy.
		 */
		public HierarchyType Type
			{
			get
				{  return type;  }
			}

		/* Property: IsLanguageSpecific
		 * Whether members of the hierarchy should be separated by language.
		 */
		public bool IsLanguageSpecific
			{
			get
				{  return isLanguageSpecific;  }
			}

		/* Property: IsLanguageAgnostic
		 * Whether members of the hierarchy don't need to be separated by language.
		 */
		public bool IsLanguageAgnostic
			{
			get
				{  return !IsLanguageSpecific;  }
			}

		/* Property: IsCaseSensitive
		 * Whether members of the hierarchy are case-sensitive.  Note that this is only relevant if the hierarchy is language-agnostic.
		 * If it language-specific you should get the case-sensitivity setting from the language associated with it instead.  In debug
		 * builds attempting to access this property will throw an exception if the hierarchy is language-specific.
		 */
		public bool IsCaseSensitive
			{
			get
				{  
				#if DEBUG
				if (IsLanguageSpecific)
					{  throw new InvalidOperationException("Shouldn't read Hierarchy.IsCaseSensitive for language-specific hierarchies.");  }
				#endif

				return isCaseSensitive;  
				}
			}

		/* Property: SortValue
		 * A number to determine the hierarchy's preferred position in a list of hierarchies, lower numbers appearing first.
		 */
		public int SortValue
			{
			get
				{  return (int)type;  }
			}



		// Group: Variables
		// __________________________________________________________________________


		/* var: name
		 * The name of the hierarchy.
		 */
		protected string name;

		/* var: pluralName
		 * The plural name of the hierarchy.
		 */
		protected string pluralName;

		/* var: simpleIdentifier
		 * The hierarchy's name using only the letters A to Z.
		 */
		protected string simpleIdentifier;

		/* var: pluralSimpleIdentifier
		 * The hierarchy's plural name using only the letters A to Z.
		 */
		protected string pluralSimpleIdentifier;

		/* var: type
		 * The <HierarchyType> associated with this hierarchy.
		 */
		protected HierarchyType type;

		/* var: isLanguageSpecific
		 * Whether hierarchy members should be separated by language.
		 */
		protected bool isLanguageSpecific;

		/* var: isCaseSensitive
		 * Whether the hierarchy members are case-sensitive.  This is only relevant if the hierarchy is language-agnostic.  If the
		 * hierarchy is language-specific you should get this setting from the language instead.
		 */
		protected bool isCaseSensitive;

		}
	}