/* 
 * Class: GregValure.NaturalDocs.Engine.Languages.ConfigFileLanguage
 * ____________________________________________________________________________
 * 
 * A class encapsulating information about a language as it appears in <Languages.txt>.  This differs from <Language> in 
 * that its meant to represent its entry in a config file rather than the final combined settings of a language.
 * 
 */

// This file is part of Natural Docs, which is Copyright © 2003-2012 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;


namespace GregValure.NaturalDocs.Engine.Languages
	{
	public class ConfigFileLanguage
		{
		
		// Group: Functions and Properties
		// __________________________________________________________________________
		
		
		/* Constructor: ConfigFileLanguage
		 */
		public ConfigFileLanguage (string newName, bool newAlterLanguage, int newLineNumber)
			{
			name = newName;
			alterLanguage = newAlterLanguage;
			lineNumber = newLineNumber;

			simpleIdentifier = null;
			aliases = null;
			addAliases = false;
			extensions = null;
			addExtensions = false;
			shebangStrings = null;
			addShebangStrings = false;
			lineCommentStrings = null;
			blockCommentStringPairs = null;
			memberOperator = null;
			prototypeEndersList = null;
			lineExtender = null;
			enumValue = null;
			caseSensitive = null;
			}
			
			
		/* Property: Name
		 * The name of the language.
		 */
		public string Name
			{
			get
				{  return name;  }
			}
			
		/* Property: SimpleIdentifier
		 * The name of the language using only the letters A to Z, or null if it's not set.
		 */
		public string SimpleIdentifier
			{
			get
				{  return simpleIdentifier;  }
			set
				{  simpleIdentifier = value;  }
			}
			
		/* Property: Aliases
		 * An array of the aliases for this language.  Will be null if none are defined.
		 */
		public string[] Aliases
			{
			get
				{  return aliases;  }
			set
				{
				if (value != null && value.Length != 0)
					{  aliases = value;  }
				else
					{  aliases = null;  }
				}
			}
			
		/* Property: AddAliases
		 * Whether <Aliases> is adding to the list instead of replacing it.
		 */
		public bool AddAliases
			{
			get
				{  return addAliases;  }
			set
				{  addAliases = value;  }
			}

		/* Property: AlterLanguage
		 * Whether this is an Alter Language entry or not.
		 */
		public bool AlterLanguage
			{
			get
				{  return alterLanguage;  }
			}
			
		/* Property: LineNumber
		 * The number at which the language appears in the file.
		 */
		public int LineNumber
			{
			get
				{  return lineNumber;  }
			}
			
		/* Property: Extensions
		 * An array of the file extensions representing this language.  Will be null if none are defined.
		 */
		public string[] Extensions
			{
			get
				{  return extensions;  }
			set
				{
				if (value != null && value.Length != 0)
					{  extensions = value;  }
				else
					{  extensions = null;  }
				}
			}
			
		/* Property: AddExtensions
		 * Whether <Extensions> is adding to the file extension list or replacing it.
		 */
		public bool AddExtensions
			{
			get
				{  return addExtensions;  }
			set
				{  addExtensions = value;  }
			}
			
		/* Property: ShebangStrings
		 * An array of the shebang strings representing this language.  Will be null if none are defined.
		 */
		public string[] ShebangStrings
			{
			get
				{  return shebangStrings;  }
			set
				{
				if (value != null && value.Length != 0)
					{  shebangStrings = value;  }
				else
					{  shebangStrings = null;  }
				}
			}
			
		/* Property: AddShebangStrings
		 * Whether <ShebangStrings> is adding to the list or replacing it.
		 */
		public bool AddShebangStrings
			{
			get
				{  return addShebangStrings;  }
			set
				{  addShebangStrings = value;  }
			}
			
		/* Property: LineCommentStrings
		 * An array of strings representing line comment symbols.  Will be null if none are defined.
		 */
		public string[] LineCommentStrings
			{
			get
				{  return lineCommentStrings;  }
			set
				{
				if (value != null && value.Length != 0)
					{  lineCommentStrings = value;  }
				else
					{  lineCommentStrings = null;  }
				}
			}
			
		/* Property: BlockCommentStringPairs
		 * An array of string pairs representing start and stop block comment symbols.  Will be null if none are defined.
		 */
		public string[] BlockCommentStringPairs
			{
			get
				{  return blockCommentStringPairs;  }
			set
				{
				if (value != null && value.Length != 0)
					{  
					if (value.Length % 2 == 1)
						{  throw new Engine.Exceptions.ArrayDidntHaveEvenLength("BlockCommentStringPairs");  }

					blockCommentStringPairs = value;  
					}
				else
					{  blockCommentStringPairs = null;  }
				}
			}
			
		/* Property: MemberOperator
		 * A string representing the default member operator symbol.  Will be null if not defined.
		 */
		public string MemberOperator
			{
			get
				{  return memberOperator;  }
			set
				{  memberOperator = value;  }
			}
		
		/* Property: LineExtender
		 * A string representing the line extender symbol if line breaks are significant to the language.  Will be null if not defined.
		 */
		 public string LineExtender
			{
			get
				{  return lineExtender;  }
			set
				{  lineExtender = value;  }
			}
			
		/* Function: GetPrototypeEnderStrings
		 * Returns the prototype ender strings for the passed topic type, or null if there are none.  If line breaks are supported they
		 * will appear in this list as "\n".
		 */
		public string[] GetPrototypeEnderStrings (string topicTypeName)
			{
			if (prototypeEndersList == null)
				{  return null;  }
			else
				{  
				string lcTopicTypeName = topicTypeName.ToLower();
				
				for (int i = 0; i < prototypeEndersList.Count; i++)
					{
					if (lcTopicTypeName == prototypeEndersList[i].TopicTypeName.ToLower())
						{  return prototypeEndersList[i].EnderStrings;  }
					}
					
				return null;
				}
			}
			
		/* Function: SetPrototypeEnders
		 * Sets the prototype ender strings for the passed topic type.  If line breaks are supported they should be included in the
		 * array as "\n".
		 */
		public void SetPrototypeEnderStrings (string topicTypeName, string[] prototypeEnderStrings)
			{
			if (prototypeEnderStrings != null && prototypeEnderStrings.Length == 0)
				{  prototypeEnderStrings = null;  }

			// Attempting to remove an entry should be rare, so we can deal with the potential performance hit of creating and
			// deleting a list every time we try to remove from an empty list.  The code simplification is worth it.
			if (prototypeEndersList == null)
				{  prototypeEndersList = new List<ConfigFilePrototypeEnders>();  }
					
			string lcTopicTypeName = topicTypeName.ToLower();
			bool found = false;
				
			for (int i = 0; i < prototypeEndersList.Count; i++)
				{
				if (lcTopicTypeName == prototypeEndersList[i].TopicTypeName.ToLower())
					{
					if (prototypeEnderStrings == null)
						{  prototypeEndersList.RemoveAt(i);  }
					else
						{  prototypeEndersList[i].EnderStrings = prototypeEnderStrings;  }
					
					found = true;
					break;
					}
				}
					
			if (found == false && prototypeEnderStrings != null)
				{  
				ConfigFilePrototypeEnders prototypeEnders = new ConfigFilePrototypeEnders();
				prototypeEnders.TopicTypeName = topicTypeName;
				prototypeEnders.EnderStrings = prototypeEnderStrings;
				prototypeEndersList.Add(prototypeEnders);  
				}
			else if (prototypeEndersList.Count == 0)
				{  prototypeEndersList = null;  }
			}
			
		/* Function: GetTopicTypeNamesWithPrototypeEnders
		 * Returns an array of all the topic types that have prototype enders defined, or null if none.
		 */
		public string[] GetTopicTypeNamesWithPrototypeEnders()
			{
			if (prototypeEndersList == null)
				{  return null;  }
			else
				{
				string[] result = new string[ prototypeEndersList.Count ];
				
				for (int i = 0; i < prototypeEndersList.Count; i++)
					{  result[i] = prototypeEndersList[i].TopicTypeName;  }
				
				return result;  
				}
			}
			
			
		/* Property: EnumValue
		 * How enum values are referenced.  Will be null if not defined.
		 */
		public Language.EnumValues? EnumValue
			{
			get
				{  return enumValue;  }
			set
				{  enumValue = value;  }
			}


		/* Property: CaseSensitive
		 * Whether the language's identifiers are case sensitive.  Will be null if not set.
		 */
		public bool? CaseSensitive
			{
			get
				{  return caseSensitive;  }
			set
				{  caseSensitive = value;  }
			}
		

		/* Function: FixNameCapitalization
		 * Replaces <Name> with a version with alternate capitalization but is otherwise equal.
		 */
		public void FixNameCapitalization (string newName)
			{
			if (string.Compare(name, newName, true) != 0)
				{  throw new Engine.Exceptions.NameChangeDifferedInMoreThanCapitalization(name, newName, "ConfigFileLanguage");  }
				
			name = newName;
			}
			
		
		/* Function: FixPrototypeEnderTopicTypeCapitalization
		 * Replaces the list of topic types with prototype enders with versions that have alternate capitalization but are otherwise
		 * equal.  You pass to it an array of names in the same order as returned by <GetTopicTypeNamesWithPrototypeEnders()>.
		 */
		public void FixPrototypeEnderTopicTypeCapitalization (string[] newTopicTypeNames)
			{
			if (newTopicTypeNames.Length != prototypeEndersList.Count)
				{  throw new InvalidOperationException();  }
				
			for (int i = 0; i < prototypeEndersList.Count; i++)
				{
				if (string.Compare(newTopicTypeNames[i], prototypeEndersList[i].TopicTypeName, true) != 0)
					{  
					throw new Engine.Exceptions.NameChangeDifferedInMoreThanCapitalization(prototypeEndersList[i].TopicTypeName,
																														newTopicTypeNames[i],
																														"ConfigFileLanguage topic type");  
					}
					
				prototypeEndersList[i].TopicTypeName = newTopicTypeNames[i];
				}
			}
			
			

		// Group: Variables
		// __________________________________________________________________________
		
		/* var: name
		 * The language name.
		 */
		protected string name;
		
		/* bool: alterLanguage
		 * Whether this is an Alter Language entry.
		 */
		protected bool alterLanguage;
		
		/* int: lineNumber
		 * The line number at which the language appears in the file.
		 */
		protected int lineNumber;

		/* var: simpleIdentifier
		 * The language name using only the letters A to Z, or null if not set.
		 */
		protected string simpleIdentifier;
		
		/* array: aliases
		 * An array of the aliases for this language.  Will be null if not set.
		 */
		protected string[] aliases;
		
		/* bool: addAliases
		 * If this is an Alter Language entry, the aliases should be added to the original list instead of replacing them.
		 */
		protected bool addAliases;
		
		/* array: extensions
		 * An array of the file extensions for this language.  Will be null if not set.
		 */
		protected string[] extensions;
		
		/* bool: addExtensions
		 * If this is an Alter Language entry, the extensions should be added to the original list instead of replacing them.
		 */
		protected bool addExtensions;
		
		/* array: shebangStrings
		 * An array of the shebang strings for this language.  Will be null if not set.
		 */
		protected string[] shebangStrings;
		
		/* bool: addShebangStrings
		 * If this is an Alter Language entry, the shebang strings should be added to the original list instead of replacing them.
		 */
		protected bool addShebangStrings;

		/* array: lineCommentStrings
		 * An array of strings that start line comments.  Will be null if not set.
		 */
		protected string[] lineCommentStrings;
		
		/* array: blockCommentStringPairs
		 * An array of string pairs that start and end block comments.  Will be null if not set.
		 */
		protected string[] blockCommentStringPairs;
		
		/* string: memberOperator
		 * A string representing the default member operator symbol.  Will be null if not set.
		 */
		protected string memberOperator;
		
		/* object: prototypeEndersList
		 * A list of <ConfigFilePrototypeEnders> mapping topic type strings to arrays of ender strings.  Line breaks are 
		 * represented with "\n".  Will be null if not set.
		 */
		protected List<ConfigFilePrototypeEnders> prototypeEndersList;
		
		/* string: lineExtender
		 * A string representing the line extender symbol if line breaks are significant to the language.  Will be null if not set.
		 */
		protected string lineExtender;
		
		/* var: enumValue
		 * How the language handles enum values.  Will be null if not set.
		 */
		protected Language.EnumValues? enumValue;

		/* var: caseSensitive
		 * Whether the language's identifiers are case sensitive.  Will be null if not set.
		 */
		protected bool? caseSensitive;
		



		/* Class: GregValure.NaturalDocs.Engine.Languages.ConfigFileLanguage.ConfigFilePrototypeEnders
		* __________________________________________________________________________
		*/
		protected class ConfigFilePrototypeEnders
			{
			public ConfigFilePrototypeEnders()
				{
				TopicTypeName = null;
				EnderStrings = null;
				}
			
			public string TopicTypeName;

			/* var: EnderStrings
			 * An array of ender strings which may be symbols and/or "\n".
			 */
			public string[] EnderStrings;
			}

		}
	
	}