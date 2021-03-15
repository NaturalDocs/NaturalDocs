/* 
 * Class: CodeClear.NaturalDocs.Engine.Languages.ConfigFiles.TextFileLanguage
 * ____________________________________________________________________________
 * 
 * A class encapsulating information about a language as it appears in <Languages.txt>.  This differs from <Language> in 
 * that its meant to represent its entry in a config file rather than the final combined settings of a language.
 * 
 */

// This file is part of Natural Docs, which is Copyright © 2003-2021 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using CodeClear.NaturalDocs.Engine.Config;


namespace CodeClear.NaturalDocs.Engine.Languages.ConfigFiles
	{
	public class TextFileLanguage
		{

		// Group: Types
		// __________________________________________________________________________

		/* enum: PropertyChange
		 * 
		 * Whether a property like Extensions uses Add, Replace, or neither.
		 * 
		 * None - No overriding, such as just "Extensions".  This is only valid when <AlterLanguage> is false.
		 * Add - Adds the elements to the list, such as "Add Extensions".  This is only valid when <AlterLanguage> is true.
		 * Replace - Replaces the elements on the list, such as "Replace Extensions".  This is only valid when <AlterLanguage> is true.
		 */
		public enum PropertyChange : byte
			{
			None = 0,
			Add,
			Replace
			}

		
		// Group: Functions
		// __________________________________________________________________________
		
		
		/* Constructor: TextFileLanguage
		 */
		public TextFileLanguage (string name, PropertyLocation propertyLocation, bool alterLanguage = false)
			{
			this.name = name;
			this.namePropertyLocation = propertyLocation;
			this.alterLanguage = alterLanguage;

			simpleIdentifier = null;
			simpleIdentifierPropertyLocation = default;
			aliases = null;
			aliasesPropertyChange = PropertyChange.None;
			aliasesPropertyLocation = default;
			fileExtensions = null;
			fileExtensionsPropertyChange = PropertyChange.None;
			fileExtensionsPropertyLocation = default;
			shebangStrings = null;
			shebangStringsPropertyChange = PropertyChange.None;
			shebangStringsPropertyLocation = default;
			lineCommentSymbols = null;
			lineCommentSymbolsPropertyLocation = default;
			blockCommentSymbols = null;
			blockCommentSymbolsPropertyLocation = default;
			memberOperator = null;
			memberOperatorPropertyLocation = default;
			prototypeEnders = null;
			lineExtender = null;
			lineExtenderPropertyLocation = default;
			enumValues = null;
			enumValuesPropertyLocation = default;
			caseSensitive = null;
			caseSensitivePropertyLocation = default;
			}


		/* Function: Duplicate
		 * Creates an independent copy of the language and all its properties.
		 */
		public TextFileLanguage Duplicate ()
			{
			TextFileLanguage copy = new TextFileLanguage(name, namePropertyLocation, alterLanguage);

			copy.simpleIdentifier = simpleIdentifier;
			copy.simpleIdentifierPropertyLocation = simpleIdentifierPropertyLocation;

			if (aliases != null)
				{
				copy.aliases = new List<string>(aliases.Count);
				copy.aliases.AddRange(aliases);
				}

			copy.aliasesPropertyChange = aliasesPropertyChange;
			copy.aliasesPropertyLocation = aliasesPropertyLocation;

			if (fileExtensions != null)
				{
				copy.fileExtensions = new List<string>(fileExtensions.Count);
				copy.fileExtensions.AddRange(fileExtensions);
				}

			copy.fileExtensionsPropertyChange = fileExtensionsPropertyChange;
			copy.fileExtensionsPropertyLocation = fileExtensionsPropertyLocation;

			if (shebangStrings != null)
				{
				copy.shebangStrings = new List<string>(shebangStrings.Count);
				copy.shebangStrings.AddRange(shebangStrings);
				}

			copy.shebangStringsPropertyChange = shebangStringsPropertyChange;
			copy.shebangStringsPropertyLocation = shebangStringsPropertyLocation;

			if (lineCommentSymbols != null)
				{
				copy.lineCommentSymbols = new List<string>(lineCommentSymbols.Count);
				copy.lineCommentSymbols.AddRange(lineCommentSymbols);
				}

			copy.lineCommentSymbolsPropertyLocation = lineCommentSymbolsPropertyLocation;

			if (blockCommentSymbols != null)
				{
				copy.blockCommentSymbols = new List<BlockCommentSymbols>(blockCommentSymbols.Count);
				copy.blockCommentSymbols.AddRange(blockCommentSymbols);  // works because it's a struct
				}

			copy.blockCommentSymbolsPropertyLocation = blockCommentSymbolsPropertyLocation;
			copy.memberOperator = memberOperator;
			copy.memberOperatorPropertyLocation = memberOperatorPropertyLocation;

			if (prototypeEnders != null)
				{
				copy.prototypeEnders = new List<TextFilePrototypeEnders>();
				foreach (var prototypeEnder in prototypeEnders)
					{  copy.prototypeEnders.Add( prototypeEnder.Duplicate() );  }
				}

			copy.lineExtender = lineExtender;
			copy.lineExtenderPropertyLocation = lineExtenderPropertyLocation;
			copy.enumValues = enumValues;
			copy.enumValuesPropertyLocation = enumValuesPropertyLocation;
			copy.caseSensitive = caseSensitive;
			copy.caseSensitivePropertyLocation = caseSensitivePropertyLocation;

			return copy;
			}


		/* Function: FixNameCapitalization
		 * Allows you to change <Name>, but only if the new value matches according to <Config.KeySettingsForLanguageName>.
		 */
		public void FixNameCapitalization (string newName)
			{
			#if DEBUG
			if (name.NormalizeKey(Config.KeySettingsForLanguageName) !=
				newName.NormalizeKey(Config.KeySettingsForLanguageName))
				{  throw new Exception("Can only use FixNameCapitalization() if the resulting key is the same.");  }
			#endif

			name = newName;
			}


		/* Function: SetSimpleIdentifier
		 * Sets the language name using only the letters A to Z.
		 */
		public void SetSimpleIdentifier (string simpleIdentifier, PropertyLocation propertyLocation)
			{
			this.simpleIdentifier = simpleIdentifier;
			this.simpleIdentifierPropertyLocation = propertyLocation;
			}

		/* Function: SetAliases
		 * Sets the list of the aliases for this language.  If <AlterLanguage> is true then you must set either <PropertyChange.Add>
		 * or <PropertyChange.Replace>.
		 */
		public void SetAliases (IList<string> aliases, PropertyLocation propertyLocation, 
										 PropertyChange propertyChange = PropertyChange.None)
			{
			#if DEBUG
			if (alterLanguage == true && propertyChange == PropertyChange.None)
				{  throw new Exception("You must set PropertyChange when setting aliases on an Alter Language entry.");  }
			if (alterLanguage == false && propertyChange != PropertyChange.None)
				{  throw new Exception("You can't set PropertyChange when setting aliases on a non-Alter Language entry.");  }
			#endif

			if (aliases == null)	
				{  this.aliases = null;  }
			else
				{
				this.aliases = new List<string>(aliases.Count);
				this.aliases.AddRange(aliases);
				}

			this.aliasesPropertyChange = propertyChange;
			this.aliasesPropertyLocation = propertyLocation;
			}
		
		/* Function: SetFileExtensions
		 * Sets the list of the file extensions for this language.  If <AlterLanguage> is true then you must set either <PropertyChange.Add>
		 * or <PropertyChange.Replace>.
		 */
		public void SetFileExtensions (IList<string> fileExtensions, PropertyLocation propertyLocation,
													PropertyChange propertyChange = PropertyChange.None)
			{
			#if DEBUG
			if (alterLanguage == true && propertyChange == PropertyChange.None)
				{  throw new Exception("You must set PropertyChange when setting extensions on an Alter Language entry.");  }
			if (alterLanguage == false && propertyChange != PropertyChange.None)
				{  throw new Exception("You can't set PropertyChange when setting extensions on a non-Alter Language entry.");  }
			#endif

			if (fileExtensions == null)	
				{  this.fileExtensions = null;  }
			else
				{
				this.fileExtensions = new List<string>(fileExtensions.Count);
				this.fileExtensions.AddRange(fileExtensions);
				}

			this.fileExtensionsPropertyChange = propertyChange;
			this.fileExtensionsPropertyLocation = propertyLocation;
			}
		
		/* Function: SetShebangStrings
		 * Sets the list of the shebang strings for this language.  If <AlterLanguage> is true then you must set either <PropertyChange.Add>
		 * or <PropertyChange.Replace>.
		 */
		public void SetShebangStrings (IList<string> shebangStrings, PropertyLocation propertyLocation,
													  PropertyChange propertyChange = PropertyChange.None)
			{
			#if DEBUG
			if (alterLanguage == true && propertyChange == PropertyChange.None)
				{  throw new Exception("You must set PropertyChange when setting shebang strings on an Alter Language entry.");  }
			if (alterLanguage == false && propertyChange != PropertyChange.None)
				{  throw new Exception("You can't set PropertyChange when setting shebang strings on a non-Alter Language entry.");  }
			#endif

			if (shebangStrings == null)	
				{  this.shebangStrings = null;  }
			else
				{
				this.shebangStrings = new List<string>(shebangStrings.Count);
				this.shebangStrings.AddRange(shebangStrings);
				}

			this.shebangStringsPropertyChange = propertyChange;
			this.shebangStringsPropertyLocation = propertyLocation;
			}
		
		/* Function: SetLineCommentSymbols
		 * Sets the list of strings that start line comments.
		 */
		public void SetLineCommentSymbols (IList<string> lineCommentSymbols, PropertyLocation propertyLocation)
			{
			if (lineCommentSymbols == null || lineCommentSymbols.Count == 0)
				{  this.lineCommentSymbols = null;  }
			else
				{
				this.lineCommentSymbols = new List<string>(lineCommentSymbols.Count);
				this.lineCommentSymbols.AddRange(lineCommentSymbols);
				}

			this.lineCommentSymbolsPropertyLocation = propertyLocation;
			}

		/* Function: SetBlockCommentSymbols
		 * Sets the list of strings that start and end block comments.
		 */
		public void SetBlockCommentSymbols (IList<BlockCommentSymbols> blockCommentSymbols, PropertyLocation propertyLocation)
			{
			if (blockCommentSymbols == null || blockCommentSymbols.Count == 0)
				{  this.blockCommentSymbols = null;  }
			else
				{
				this.blockCommentSymbols = new List<BlockCommentSymbols>(blockCommentSymbols.Count);
				this.blockCommentSymbols.AddRange(blockCommentSymbols);  // this works because it's a struct
				}

			this.blockCommentSymbolsPropertyLocation = propertyLocation;
			}
		
		/* Function: SetMemberOperator
		 * Sets the string representing the default member operator symbol.
		 */
		public void SetMemberOperator (string memberOperator, PropertyLocation propertyLocation)
			{
			this.memberOperator = memberOperator;
			this.memberOperatorPropertyLocation = propertyLocation;
			}

		/* Function: AddPrototypeEnders
		 * Adds <TextFilePrototypeEnders> which maps a comment type string to its ender strings.  Line breaks are represented
		 * with "\n".  If a set of enders strings already exists for the comment type string it will be replaced.
		 */
		public void AddPrototypeEnders (TextFilePrototypeEnders prototypeEnders)
			{
			if (this.prototypeEnders == null)
				{  this.prototypeEnders = new List<TextFilePrototypeEnders>();  }

			int index = FindPrototypeEndersIndex(prototypeEnders.CommentType);

			if (index == -1)
				{  this.prototypeEnders.Add(prototypeEnders);  }
			else
				{  
				// We could just overwrite the entry at the index but we want to preserve the order they appear in.
				this.prototypeEnders.RemoveAt(index);
				this.prototypeEnders.Add(prototypeEnders);
				}
			}

		/* Function: FindPrototypeEnders
		 * Returns the <TextFilePrototypeEnders> associated with the passed comment type name, or null if there aren't any.
		 */
		public TextFilePrototypeEnders FindPrototypeEnders (string commentType)
			{
			int index = FindPrototypeEndersIndex(commentType);

			if (index == -1)
				{  return null;  }
			else
				{  return prototypeEnders[index];  }
			}

		/* Function: FindPrototypeEndersIndex
		 * Returns the index into <prototypeEnders> associated with the passed comment type name, or -1 if there isn't one.
		 */
		protected int FindPrototypeEndersIndex (string commentType)
			{
			if (prototypeEnders == null)
				{  return -1;  }

			string key = commentType.NormalizeKey(CommentTypes.Config.KeySettingsForCommentTypes);

			for (int i = 0; i < prototypeEnders.Count; i++)
				{
				if (prototypeEnders[i].CommentType.NormalizeKey(CommentTypes.Config.KeySettingsForCommentTypes) == key)
					{  return i;  }
				}

			return -1;
			}

		/* Function: SetLineExtender
		 * Sets the string representing the line extender symbol if line breaks are significant to the language.
		 */
		public void SetLineExtender (string lineExtender, PropertyLocation propertyLocation)
			{
			this.lineExtender = lineExtender;
			this.lineExtenderPropertyLocation = propertyLocation;
			}

		/* Function: SetEnumValues
		 * Sets how the language handles enum values.
		 */
		public void SetEnumValues (Language.EnumValues? enumValues, PropertyLocation propertyLocation)
			{
			this.enumValues = enumValues;
			this.enumValuesPropertyLocation = propertyLocation;
			}

		/* Function: SetCaseSensitive
		 * Sets whether the language's identifiers are case sensitive.
		 */
		public void SetCaseSensitive (bool? caseSensitive, PropertyLocation propertyLocation)
			{
			this.caseSensitive = caseSensitive;
			this.caseSensitivePropertyLocation = propertyLocation;
			}



		// Group: Properties
		// __________________________________________________________________________
		
		
		/* Property: PropertyLocation
		 * The <PropertyLocation> where the language is defined.
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
		 * The language name.
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
		
		/* Property: AlterLanguage
		 * Whether this is an Alter Language entry.
		 */
		public bool AlterLanguage
			{
			get
				{  return alterLanguage;  }
			}

		/* Property: HasSimpleIdentifier
		 * Whether <SimpleIdentifier> was defined.
		 */
		public bool HasSimpleIdentifier
			{
			get
				{  return (simpleIdentifier != null);  }
			}

		/* Property: SimpleIdentifier
		 * The language name using only the letters A to Z, or null if not set.
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
		
		/* Property: HasAliases
		 * Whether any <Aliases> are defined.
		 */
		public bool HasAliases
			{
			get
				{  return (aliases != null);  }
			}
		
		/* Property: Aliases
		 * A list of the aliases for this language, or null if none.
		 */
		public IList<string> Aliases
			{
			get
				{  return aliases;  }
			}
		
		/* Property: AliasesPropertyChange
		 * Whether the <Aliases> should be added to or replaced if <AlterLanguage> is true.
		 */
		public PropertyChange AliasesPropertyChange
			{
			get
				{  return aliasesPropertyChange;  }
			}

		/* Property: AliasesPropertyLocation
		 * The <PropertyLocation> where <Aliases> is defined.
		 */
		public PropertyLocation AliasesPropertyLocation
			{
			get
				{  return aliasesPropertyLocation;  }
			}

		/* Property: HasFileExtensions
		 * Whether any <FileExtensions> are defined.
		 */
		public bool HasFileExtensions
			{
			get
				{  return (fileExtensions != null);  }
			}
		
		/* Property: FileExtensions
		 * A list of the file extensions for this language, or null if none.
		 */
		public IList<string> FileExtensions
			{
			get
				{  return fileExtensions;  }
			}
		
		/* Property: ExtensionsPropertyChange
		 * Whether the <FileExtensions> should be added to or replaced if <AlterLanguage> is true.
		 */
		public PropertyChange FileExtensionsPropertyChange
			{
			get
				{  return fileExtensionsPropertyChange;  }
			}

		/* Property: FileExtensionsPropertyLocation
		 * The <PropertyLocation> where <FileExtensions> is defined.
		 */
		public PropertyLocation FileExtensionsPropertyLocation
			{
			get
				{  return fileExtensionsPropertyLocation;  }
			}

		/* Property: HasShebangStrings
		 * Whether any <ShebangStrings> are defined.
		 */
		public bool HasShebangStrings
			{
			get
				{  return (shebangStrings != null);  }
			}
		
		/* Property: ShebangStrings
		 * A list of the shebang strings for this language, or null if none.
		 */
		public IList<string> ShebangStrings
			{
			get
				{  return shebangStrings;  }
			}
		
		/* Property: ShebangStringsPropertyChange
		 * Whether the <ShebangStrings> should be added to or replaced if <AlterLanguage> is true.
		 */
		public PropertyChange ShebangStringsPropertyChange
			{
			get
				{  return shebangStringsPropertyChange;  }
			}

		/* Property: ShebangStringsPropertyLocation
		 * The <PropertyLocation> where <ShebangStrings> is defined.
		 */
		public PropertyLocation ShebangStringsPropertyLocation
			{
			get
				{  return shebangStringsPropertyLocation;  }
			}

		/* Property: HasLineCommentSymbols
		 * Whether any <LineCommentSymbols> were defined.
		 */
		public bool HasLineCommentSymbols
			{
			get
				{  return (lineCommentSymbols != null);  }
			}

		/* Property: LineCommentSymbols
		 * A list of strings that start line comments, or null if none.
		 */
		public IList<string> LineCommentSymbols
			{
			get
				{  return lineCommentSymbols;  }
			}

		/* Property: LineCommentSymbolsPropertyLocation
		 * The <PropertyLocation> where <LineCommentSymbols> is defined.
		 */
		public PropertyLocation LineCommentSymbolsPropertyLocation
			{
			get
				{  return lineCommentSymbolsPropertyLocation;  }
			}

		/* Property: HasBlockCommentSymbols
		 * Whether any <BlockCommentSymbols> are defined.
		 */
		public bool HasBlockCommentSymbols
			{
			get
				{  return (blockCommentSymbols != null);  }
			}
		
		/* Property: BlockCommentSymbols
		 * A list of <Languages.BlockCommentSymbols> that start and end block comments, or null if none.
		 */
		public IList<BlockCommentSymbols> BlockCommentSymbols
			{
			get
				{  return blockCommentSymbols;  }
			}
		
		/* Property: BlockCommentSymbolsPropertyLocation
		 * The <PropertyLocation> where <BlockCommentSymbols> is defined.
		 */
		public PropertyLocation BlockCommentSymbolsPropertyLocation
			{
			get
				{  return blockCommentSymbolsPropertyLocation;  }
			}

		/* Property: HasMemberOperator
		 * Whether <MemberOperator> is defined.
		 */
		public bool HasMemberOperator
			{
			get
				{  return (memberOperator != null);  }
			}
		
		/* Property: MemberOperator
		 * A string representing the default member operator symbol, or null if it's not set.
		 */
		public string MemberOperator
			{
			get
				{  return memberOperator;  }
			}
		
		/* Property: MemberOperatorPropertyLocation
		 * The <PropertyLocation> where <MemberOperator> is defined.
		 */
		public PropertyLocation MemberOperatorPropertyLocation
			{
			get
				{  return memberOperatorPropertyLocation;  }
			}

		/* Property: HasPrototypeEnders
		 * Whether any <PrototypeEnders> are defined.
		 */
		public bool HasPrototypeEnders
			{
			get
				{  return (prototypeEnders != null);  }
			}
		
		/* Property: PrototypeEnders
		 * A list of <TextFilePrototypeEnders> mapping comment type strings to their ender strings.  Line breaks are represented
		 * with "\n".  Will be null if there are none set.
		 */
		public IList<TextFilePrototypeEnders> PrototypeEnders
			{
			get
				{  return prototypeEnders;  }
			}
		
		/* Property: HasLineExtender
		 * Whether <LineExtender> is defined.
		 */
		public bool HasLineExtender
			{
			get
				{  return (lineExtender != null);  }
			}
		
		/* Property: LineExtender
		 * A string representing the line extender symbol if line breaks are significant to the language.  Will be null if not set.
		 */
		public string LineExtender
			{
			get
				{  return lineExtender;  }
			}
		
		/* Property: LineExtenderPropertyLocation
		 * The <PropertyLocation> where <LineExtender> is defined.
		 */
		public PropertyLocation LineExtenderPropertyLocation
			{
			get
				{  return lineExtenderPropertyLocation;  }
			}

		/* Property: HasEnumValues
		 * Whether <EnumValues> is defined.
		 */
		public bool HasEnumValues
			{
			get
				{  return (enumValues != null);  }
			}

		/* Property: EnumValues
		 * How the language handles enum values, or null if not set.
		 */
		public Language.EnumValues? EnumValues
			{
			get
				{  return enumValues;  }
			}

		/* Property: EnumValuesPropertyLocation
		 * The <PropertyLocation> where <EnumValues> is defined.
		 */
		public PropertyLocation EnumValuesPropertyLocation
			{
			get
				{  return enumValuesPropertyLocation;  }
			}

		/* Property: HasCaseSensitive
		 * Whether <CaseSensitive> is defined.
		 */
		public bool HasCaseSensitive
			{
			get
				{  return (caseSensitive != null);  }
			}
		
		/* Property: CaseSensitive
		 * Whether the language's identifiers are case sensitive, or null if not set.
		 */
		public bool? CaseSensitive
			{
			get
				{  return caseSensitive;  }
			}
		
		/* Property: CaseSensitivePropertyLocation
		 * The <PropertyLocation> where <CaseSensitive> is defined.
		 */
		public PropertyLocation CaseSensitivePropertyLocation
			{
			get
				{  return caseSensitivePropertyLocation;  }
			}
		


		// Group: Variables
		// __________________________________________________________________________
		
		/* var: name
		 * The language name.
		 */
		protected string name;

		/* var: namePropertyLocation
		 * The <PropertyLocation> where <name> is defined.
		 */
		protected PropertyLocation namePropertyLocation;
		
		/* var: alterLanguage
		 * Whether this is an Alter Language entry.
		 */
		protected bool alterLanguage;
		
		/* var: simpleIdentifier
		 * The language name using only the letters A to Z, or null if not set.
		 */
		protected string simpleIdentifier;

		/* var: simpleIdentifierPropertyLocation
		 * The <PropertyLocation> where <simpleIdentifier> is defined.
		 */
		protected PropertyLocation simpleIdentifierPropertyLocation;
		
		/* var: aliases
		 * A list of the aliases for this language, or null if none.
		 */
		protected List<string> aliases;
		
		/* var: aliasesPropertyChange
		 * Whether the <aliases> should be added to or replaced if <alterLanguage> is true.
		 */
		protected PropertyChange aliasesPropertyChange;

		/* var: aliasesPropertyLocation
		 * The <PropertyLocation> where <aliases> is defined.
		 */
		protected PropertyLocation aliasesPropertyLocation;

		/* var: fileExtensions
		 * A list of the file extensions for this language, or null if none.
		 */
		protected List<string> fileExtensions;
		
		/* var: fileExtensionsPropertyChange
		 * Whether the <fileExtensions> should be added to or replaced if <alterLanguage> is true.
		 */
		protected PropertyChange fileExtensionsPropertyChange;

		/* var: fileExtensionsPropertyLocation
		 * The <PropertyLocation> where <fileExtensions> is defined.
		 */
		protected PropertyLocation fileExtensionsPropertyLocation;

		/* var: shebangStrings
		 * A list of the shebang strings for this language, or null if none.
		 */
		protected List<string> shebangStrings;
		
		/* var: shebangStringsPropertyChange
		 * Whether the <shebangStrings> should be added to or replaced if <alterLanguage> is true.
		 */
		protected PropertyChange shebangStringsPropertyChange;

		/* var: shebangStringsPropertyLocation
		 * The <PropertyLocation> where <shebangStrings> is defined.
		 */
		protected PropertyLocation shebangStringsPropertyLocation;

		/* var: lineCommentSymbols
		 * A list of strings that start line comments, or null if none.
		 */
		protected List<string> lineCommentSymbols;

		/* var: lineCommentSymbolsPropertyLocation
		 * The <PropertyLocation> where <lineCommentSymbols> is defined.
		 */
		protected PropertyLocation lineCommentSymbolsPropertyLocation;

		/* var: blockCommentSymbols
		 * A list of <Languages.BlockCommentSymbols> that start and end block comments, or null if none.
		 */
		protected List<BlockCommentSymbols> blockCommentSymbols;
		
		/* var: blockCommentSymbolsPropertyLocation
		 * The <PropertyLocation> where <blockCommentSymbols> is defined.
		 */
		protected PropertyLocation blockCommentSymbolsPropertyLocation;

		/* var: memberOperator
		 * A string representing the default member operator symbol, or null if it's not set.
		 */
		protected string memberOperator;
		
		/* var: memberOperatorPropertyLocation
		 * The <PropertyLocation> where <memberOperator> is defined.
		 */
		protected PropertyLocation memberOperatorPropertyLocation;

		/* var: prototypeEnders
		 * A list of <TextFilePrototypeEnders> mapping comment type strings to their ender strings.  Line breaks are represented
		 * with "\n".  Will be null if there are none set.
		 */
		protected List<TextFilePrototypeEnders> prototypeEnders;
		
		/* var: lineExtender
		 * A string representing the line extender symbol if line breaks are significant to the language.  Will be null if not set.
		 */
		protected string lineExtender;
		
		/* var: lineExtenderPropertyLocation
		 * The <PropertyLocation> where <lineExtender> is defined.
		 */
		protected PropertyLocation lineExtenderPropertyLocation;

		/* var: enumValues
		 * How the language handles enum values, or null if not set.
		 */
		protected Language.EnumValues? enumValues;

		/* var: enumValuesPropertyLocation
		 * The <PropertyLocation> where <enumValues> is defined.
		 */
		protected PropertyLocation enumValuesPropertyLocation;

		/* var: caseSensitive
		 * Whether the language's identifiers are case sensitive, or null if not set.
		 */
		protected bool? caseSensitive;
		
		/* var: caseSensitivePropertyLocation
		 * The <PropertyLocation> where <caseSensitive> is defined.
		 */
		protected PropertyLocation caseSensitivePropertyLocation;

		}
	}