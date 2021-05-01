/* 
 * Class: CodeClear.NaturalDocs.Engine.CommentTypes.ConfigFiles.TextFileCommentType
 * ____________________________________________________________________________
 * 
 * A class encapsulating information about a comment type as parsed from a <ConfigFiles.TextFile>.  This differs from 
 * <CommentType> in that its meant to represent how its entry appears in the text file rather than the final combined 
 * settings.  For example, any field can be null if it's not defined.
 * 
 * 
 * Multithreading: Not Thread Safe, Supports Multiple Readers
 * 
 *		This object doesn't have any locking built in, and so it is up to the class managing it to provide thread safety if needed.
 *		However, it does support multiple concurrent readers.  This means it can be used in read-only mode with no locking or
 *		in read/write mode with a ReaderWriterLock.
 *		
 */

// This file is part of Natural Docs, which is Copyright © 2003-2021 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using CodeClear.NaturalDocs.Engine.Config;


namespace CodeClear.NaturalDocs.Engine.CommentTypes.ConfigFiles
	{
	public class TextFileCommentType
		{

		// Group: Functions
		// __________________________________________________________________________
		
		
		/* Constructor: TextFileCommentType
		 */
		public TextFileCommentType (string name, PropertyLocation propertyLocation, bool alterType = false)
			{
			this.name = name;
			this.namePropertyLocation = propertyLocation;
			this.alterType = alterType;

			displayName = null;
			displayNamePropertyLocation = default;
			displayNameFromLocale = null;
			displayNameFromLocalePropertyLocation = default;
			pluralDisplayName = null;
			pluralDisplayNamePropertyLocation = default;
			pluralDisplayNameFromLocale = null;
			pluralDisplayNameFromLocalePropertyLocation = default;

			simpleIdentifier = null;
			simpleIdentifierPropertyLocation = default;
			scope = null;
			scopePropertyLocation = default;
			hierarchyName = null;
			hierarchyNamePropertyLocation = default;
			flags = null;
			flagsPropertyLocation = default;

			keywordGroups = null;
			}


		/* Function: Duplicate
		 * Creates an independent copy of the object and all its attributes.
		 */
		public TextFileCommentType Duplicate ()
			{
			TextFileCommentType copy = new TextFileCommentType(name, namePropertyLocation, alterType);

			copy.name = name;
			copy.namePropertyLocation = namePropertyLocation;
			copy.alterType = alterType;

			copy.displayName = displayName;
			copy.displayNamePropertyLocation = displayNamePropertyLocation;
			copy.displayNameFromLocale = displayNameFromLocale;
			copy.displayNameFromLocalePropertyLocation = displayNameFromLocalePropertyLocation;
			copy.pluralDisplayName = pluralDisplayName;
			copy.pluralDisplayNamePropertyLocation = pluralDisplayNamePropertyLocation;
			copy.pluralDisplayNameFromLocale = pluralDisplayNameFromLocale;
			copy.pluralDisplayNameFromLocalePropertyLocation = pluralDisplayNameFromLocalePropertyLocation;

			copy.simpleIdentifier = simpleIdentifier;
			copy.simpleIdentifierPropertyLocation = simpleIdentifierPropertyLocation;
			copy.scope = scope;
			copy.scopePropertyLocation = scopePropertyLocation;
			copy.hierarchyName = hierarchyName;
			copy.hierarchyNamePropertyLocation = hierarchyNamePropertyLocation;
			copy.flags = flags;
			copy.flagsPropertyLocation = flagsPropertyLocation;

			if (keywordGroups != null)
				{
				copy.keywordGroups = new List<TextFileKeywordGroup>(keywordGroups.Count);
				foreach (var keywordGroup in keywordGroups)
					{  copy.keywordGroups.Add( keywordGroup.Duplicate() );  }
				}
			
			return copy;
			}


		/* Function: FixNameCapitalization
		 * Allows you to change <Name>, but only if the new value matches according to <Config.KeySettingsForCommentTypes>.
		 */
		public void FixNameCapitalization (string newName)
			{
			#if DEBUG
			if (name.NormalizeKey(Config.KeySettingsForCommentTypes) !=
				newName.NormalizeKey(Config.KeySettingsForCommentTypes))
				{  throw new Exception("Can only use FixNameCapitalization() if the resulting key is the same.");  }
			#endif

			name = newName;
			}

		/* Function: SetDisplayName
		 * Sets the comment type's display name.  This will also set <DisplayNameFromLocale> to null since they cannot both
		 * have values.
		 */
		public void SetDisplayName (string displayName, PropertyLocation propertyLocation)
			{
			this.displayName = displayName;
			this.displayNamePropertyLocation = propertyLocation;

			this.displayNameFromLocale = null;
			this.displayNameFromLocalePropertyLocation = default;
			}
			
		/* Function: SetDisplayNameFromLocale
		 * Sets the comment type's display name identifier to be retrieved from <Engine.Locale>.  This will also set <DisplayName>
		 * to null since the cannot both have values.
		 */
		public void SetDisplayNameFromLocale (string displayNameFromLocale, PropertyLocation propertyLocation)
			{
			this.displayNameFromLocale = displayNameFromLocale;
			this.displayNameFromLocalePropertyLocation = propertyLocation;

			this.displayName = null;
			this.displayNamePropertyLocation = default;
			}
			
		/* Function: SetPluralDisplayName
		 * Sets the comment type's plural display name.  This will also set <PluralDisplayNameFromLocale> to null since they
		 * cannot both have values.
		 */
		public void SetPluralDisplayName (string pluralDisplayName, PropertyLocation propertyLocation)
			{
			this.pluralDisplayName = pluralDisplayName;
			this.pluralDisplayNamePropertyLocation = propertyLocation;

			this.pluralDisplayNameFromLocale = null;
			this.pluralDisplayNameFromLocalePropertyLocation = default;
			}
			
		/* Function: SetPluralDisplayNameFromLocale
		 * Sets the comment type's plural display name identifier to be retrieved from <Engine.Locale>.  This will also set
		 * <PluralDisplayName> to null since they cannot both have values.
		 */
		public void SetPluralDisplayNameFromLocale (string pluralDisplayNameFromLocale, PropertyLocation propertyLocation)
			{
			this.pluralDisplayNameFromLocale = pluralDisplayNameFromLocale;
			this.pluralDisplayNameFromLocalePropertyLocation = propertyLocation;

			this.pluralDisplayName = null;
			this.pluralDisplayNamePropertyLocation = default;
			}
			
		/* Function: SetSimpleIdentifier
		 * Sets the comment type's name using only the letters A to Z.
		 */
		public void SetSimpleIdentifier (string simpleIdentifier, PropertyLocation propertyLocation)
			{
			this.simpleIdentifier = simpleIdentifier;
			this.simpleIdentifierPropertyLocation = propertyLocation;
			}

		/* Function: SetScope
		 * Sets the scope of the comment type.
		 */
		public void SetScope (CommentType.ScopeValue? scope, PropertyLocation propertyLocation)
			{
			this.scope = scope;
			this.scopePropertyLocation = propertyLocation;
			}

		/* Function: SetHierarchyName
		 * Sets the name of the hierarchy the comment type appears in.
		 */
		public void SetHierarchyName (string hierarchyName, PropertyLocation propertyLocation)
			{
			this.hierarchyName = hierarchyName;
			this.hierarchyNamePropertyLocation = propertyLocation;
			}

		/* Function: SetFlags
		 * Sets the flags for this comment type.
		 */
		public void SetFlags (CommentType.FlagValue? flags, PropertyLocation propertyLocation)
			{
			this.flags = flags;
			this.flagsPropertyLocation = propertyLocation;
			}

		/* Function: AddKeywordGroup
		 * Adds a keyword group to the comment type.
		 */
		public void AddKeywordGroup (TextFileKeywordGroup keywordGroup)
			{
			if (keywordGroups == null)
				{  keywordGroups = new List<TextFileKeywordGroup>();  }

			keywordGroups.Add(keywordGroup);
			}



		// Group: Properties
		// __________________________________________________________________________
			
			
		/* Property: PropertyLocation
		 * The <PropertyLocation> where the comment type is defined.
		 */
		public PropertyLocation PropertyLocation
			{
			get
				{  
				// Name is defined on the starting line of the comment type.
				return namePropertyLocation;
				}
			}

		/* Property: Name
		 * The name of the comment type, not to be confused with <DisplayName>.
		 */
		public string Name
			{
			get
				{  return name;  }
			}
			
		/* Property: NamePropertyLocation
		 * The <PropertyLocation> where <Name> is defined.
		 */
		public PropertyLocation NamePropertyLocation
			{
			get
				{  return namePropertyLocation;  }
			}

		/* Property: AlterType
		 * Whether this comment is altering an existing one or not.
		 */
		public bool AlterType
			{
			get
				{  return alterType;  }
			}
			
		/* Property: HasDisplayName
		 * Whether the comment type's display name property is defined.
		 */
		public bool HasDisplayName
			{
			get
				{  return (displayName != null);  }
			}
			
		/* Property: DisplayName
		 * The comment type's display name, or null if it's not defined.
		 */
		public string DisplayName
			{
			get
				{  return displayName;  }
			}
			
		/* Property: DisplayNamePropertyLocation
		 * The <PropertyLocation> where <DisplayName> is defined.
		 */
		public PropertyLocation DisplayNamePropertyLocation
			{
			get
				{  return displayNamePropertyLocation;  }
			}

		/* Property: HasDisplayNameFromLocale
		 * Whether the comment type's display name from locale property is defined.
		 */
		public bool HasDisplayNameFromLocale
			{
			get
				{  return (displayNameFromLocale != null);  }
			}
			
		/* Property: DisplayNameFromLocale
		 * The comment type's display name identifier to be retrieved from <Engine.Locale>, or null if it's not defined.
		 */
		public string DisplayNameFromLocale
			{
			get
				{  return displayNameFromLocale;  }
			}
			
		/* Property: DisplayNameFromLocalePropertyLocation
		 * The <PropertyLocation> where <DisplayNameFromLocale> is defined.
		 */
		public PropertyLocation DisplayNameFromLocalePropertyLocation
			{
			get
				{  return displayNameFromLocalePropertyLocation;  }
			}

		/* Property: HasPluralDisplayName
		 * Whether the comment type's plural display name property is defined.
		 */
		public bool HasPluralDisplayName
			{
			get
				{  return (pluralDisplayName != null);  }
			}
			
		/* Property: PluralDisplayName
		 * The comment type's plural display name, or null if it's not defined.
		 */
		public string PluralDisplayName
			{
			get
				{  return pluralDisplayName;  }
			}
			
		/* Property: PluralDisplayNamePropertyLocation
		 * The <PropertyLocation> where <PluralDisplayName> is defined.
		 */
		public PropertyLocation PluralDisplayNamePropertyLocation
			{
			get
				{  return pluralDisplayNamePropertyLocation;  }
			}

		/* Property: HasPluralDisplayNameFromLocale
		 * Whether the comment type's plural display name from locale property is defined.
		 */
		public bool HasPluralDisplayNameFromLocale
			{
			get
				{  return (pluralDisplayNameFromLocale != null);  }
			}
			
		/* Property: PluralDisplayNameFromLocale
		 * The comment type's plural display name identifier to be retrieved from <Engine.Locale>, or null if it's not defined.
		 */
		public string PluralDisplayNameFromLocale
			{
			get
				{  return pluralDisplayNameFromLocale;  }
			}
			
		/* Property: PluralDisplayNameFromLocalePropertyLocation
		 * The <PropertyLocation> where <PluralDisplayNameFromLocale> is defined.
		 */
		public PropertyLocation PluralDisplayNameFromLocalePropertyLocation
			{
			get
				{  return pluralDisplayNameFromLocalePropertyLocation;  }
			}

		/* Property: HasSimpleIdentifier
		 * Whether the comment type's simple identifier property is defined.
		 */
		public bool HasSimpleIdentifier
			{
			get
				{  return (simpleIdentifier != null);  }
			}

		/* Property: SimpleIdentifier
		 * The comment type's name using only the letters A to Z, or null if it's not defined.
		 */
		public string SimpleIdentifier
			{
			get
				{  return simpleIdentifier;  }
			}

		/* Property: SimpleIdentifierPropertyLocation
		 * The <PropertyLocation> where <SimpleIdentifier> is defined.
		 */
		public PropertyLocation SimpleIdentifierPropertyLocation
			{
			get
				{  return simpleIdentifierPropertyLocation;  }
			}
			
		/* Property: HasScope
		 * Whether the comment type's scope property was defined..
		 */
		public bool HasScope
			{
			get
				{  return (scope != null);  }
			}

		/* Property: Scope
		 * The scope of the comment type, or null if it is not defined.
		 */
		public CommentType.ScopeValue? Scope
			{
			get
				{  return scope;  }
			}

		/* Property: ScopePropertyLocation
		 * The <PropertyLocation> where <Scope> is defined.
		 */
		public PropertyLocation ScopePropertyLocation
			{
			get
				{  return scopePropertyLocation;  }
			}

		/* Property: HasHierarchyName
		 * Whether the comment type's hierarchy property was defined.
		 */
		public bool HasHierarchyName
			{
			get
				{  return (hierarchyName != null);  }
			}

		/* Property: HierarchyName
		 * The name of the hierarchy the comment type appears in, or null if it is not defined.
		 */
		public string HierarchyName
			{
			get
				{  return hierarchyName;  }
			}

		/* Property: HierarchyNamePropertyLocation
		 * The <PropertyLocation> where <HierarchyName> is defined.
		 */
		public PropertyLocation HierarchyNamePropertyLocation
			{
			get
				{  return hierarchyNamePropertyLocation;  }
			}

		/* Property: HasFlags
		 * Whether the comment type's flags property is defined.
		 */
		public bool HasFlags
			{
			get
				{  return (flags != null);  }
			}

		/* Property: Flags
		 * The combination of all <CommentType.FlagValues> that apply, or null if it is not defined.  Note that this does not
		 * include the <HierarchyName> even though that's defined with the flags.
		 */
		public CommentType.FlagValue? Flags
			{
			get
				{  return flags;  }
			}

		/* Property: FlagsPropertyLocation
		 * The <PropertyLocation> where <Flags> is defined.
		 */
		public PropertyLocation FlagsPropertyLocation
			{
			get
				{  return flagsPropertyLocation;  }
			}

		/* Property: HasKeywordGroups
		 * Whether the comment type has keyword groups defined.
		 */
		public bool HasKeywordGroups
			{
			get
				{  return (keywordGroups != null);  }
			}

		/* Property: KeywordGroups
		 * A list of all the keyword groups defined, or null if none.
		 */
		public IList<TextFileKeywordGroup> KeywordGroups
			{
			get
				{  return keywordGroups;  }
			}

				
		
		// Group: Variables
		// __________________________________________________________________________
		
		/* var: name
		 * The comment type name.
		 */
		protected string name;

		/* var: namePropertyLocation
		 * The <PropertyLocation> where <name> is defined, and thus where the entire comment type is defined.
		 */
		protected PropertyLocation namePropertyLocation;
		
		/* var: alterType
		 * Whether this entry is for altering a comment type instead of defining a new one.
		 */
		protected bool alterType;
		
		/* var: displayName
		 * The comment type's display name, or null if it's not defined.
		 */
		protected string displayName;

		/* var: displayNamePropertyLocation
		 * The <PropertyLocation> where <displayName> is defined.
		 */
		protected PropertyLocation displayNamePropertyLocation;
		
		/* var: displayNameFromLocale
		 * The locale identifier of the comment type's display name, or null if it's not defined.
		 */
		protected string displayNameFromLocale;
		
		/* var: displayNameFromLocalePropertyLocation
		 * The <PropertyLocation> where <displayNameFromLocale> is defined.
		 */
		protected PropertyLocation displayNameFromLocalePropertyLocation;

		/* var: pluralDisplayName
		 * The comment type's plural display name, or null if it's not defined.
		 */
		protected string pluralDisplayName;
		
		/* var: pluralDisplayNamePropertyLocation
		 * The <PropertyLocation> where <pluralDisplayName> is defined.
		 */
		protected PropertyLocation pluralDisplayNamePropertyLocation;

		/* var: pluralDisplayNameFromLocale
		 * The locale identifier of the comment type's plural display name, or null if it's not defined.
		 */
		protected string pluralDisplayNameFromLocale;
		
		/* var: pluralDisplayNameFromLocalePropertyLocation
		 * The <PropertyLocation> where <pluralDisplayNameFromLocale> is defined.
		 */
		protected PropertyLocation pluralDisplayNameFromLocalePropertyLocation;

		/* var: simpleIdentifier
		 * The comment type's name using only the letters A to Z, or null if it's not defined.
		 */
		protected string simpleIdentifier;
		
		/* var: simpleIdentifierPropertyLocation
		 * The <PropertyLocation> where <simpleIdentifier> is defined.
		 */
		protected PropertyLocation simpleIdentifierPropertyLocation;

		/* var: scope
		 * The scope of the comment type, or null if it is not defined.
		 */
		protected CommentType.ScopeValue? scope;

		/* var: scopePropertyLocation
		 * The <PropertyLocation> where <scope> is defined.
		 */
		protected PropertyLocation scopePropertyLocation;
		
		/* var: hierarchyName
		 * The name of the hierarchy the comment type appears in, or null if none.
		 */
		protected string hierarchyName;

		/* var: hierarchyNamePropertyLocation
		 * The <PropertyLocation> where <hierarchyName> is defined.
		 */
		protected PropertyLocation hierarchyNamePropertyLocation;
		
		/* var: flags
		 * The combination of all <CommentType.FlagValues> that apply, or null if it is not defined.  Note that this does not
		 * include the <hierarchyName> even though that's declared in the flags.
		 */
		protected CommentType.FlagValue? flags;

		/* var: flagsPropertyLocation
		 * The <PropertyLocation> where <flags> is defined.
		 */
		protected PropertyLocation flagsPropertyLocation;

		/* array: keywordGroups
		 * A list of all the keyword groups defined, or null if none.
		 */
		protected List<TextFileKeywordGroup> keywordGroups;

		}
	}