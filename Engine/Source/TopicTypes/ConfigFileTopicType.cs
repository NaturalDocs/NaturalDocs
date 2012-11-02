/* 
 * Class: GregValure.NaturalDocs.Engine.TopicTypes.ConfigFileTopicType
 * ____________________________________________________________________________
 * 
 * A class encapsulating information about a file as parsed from a <ConfigFile>.  This differs from <TopicType> in that its
 * meant to represent how its entry appears in the config file rather than the final combined settings.  For example, any field
 * can be null if it's not defined.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2012 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;


namespace GregValure.NaturalDocs.Engine.TopicTypes
	{
	public class ConfigFileTopicType
		{

		// Group: Functions and Properties
		// __________________________________________________________________________
		
		
		/* Constructor: ConfigFileTopicType
		 */
		public ConfigFileTopicType (string newName, bool newAlterType, int newLineNumber)
			{
			name = newName;
			alterType = newAlterType;
			lineNumber = newLineNumber;
			
			displayName = null;
			displayNameFromLocale = null;
			pluralDisplayName = null;
			pluralDisplayNameFromLocale = null;
			simpleIdentifier = null;
			index = null;
			indexWith = null;
			scope = null;
			classHierarchy = null;
			variableType = null;
			breakLists = null;
			keywords = new List<string>();
			}
			
			
		/* Property: Name
		 * The name of the topic type, not to be confused with <DisplayName>.
		 */
		public string Name
			{
			get
				{  return name;  }
			}
			
		/* Property: AlterType
		 * Whether this topic is altering an existing one or not.
		 */
		public bool AlterType
			{
			get
				{  return alterType;  }
			}
			
		/* Property: LineNumber
		 * The line number where this topic type appears in the file.
		 */
		public int LineNumber
			{
			get
				{  return lineNumber;  }
			}
			
		/* Property: DisplayName
		 * The topic type's display name, or null if it's not defined.  If this is set to something other than null, 
		 * <DisplayNameFromLocale> becomes null.
		 */
		public string DisplayName
			{
			get
				{  return displayName;  }
			set
				{  
				displayName = value;
				
				if (displayName != null)
					{  displayNameFromLocale = null;  }				
				}
			}
			
		/* Property: DisplayNameFromLocale
		 * The topic type's display name identifier to be retrieved from <Engine.Locale>, or null if it's not defined.  If this is set
		 * to something other than null, <DisplayName> becomes null.
		 */
		public string DisplayNameFromLocale
			{
			get
				{  return displayNameFromLocale;  }
			set
				{  
				displayNameFromLocale = value;
				
				if (displayNameFromLocale != null)
					{  displayName = null;  }
				}
			}
			
		/* Property: PluralDisplayName
		 * The topic type's plural display name, or null if it's not defined.  If this is set to something other than null, 
		 * <PluralDisplayNameFromLocale> becomes null.
		 */
		public string PluralDisplayName
			{
			get
				{  return pluralDisplayName;  }
			set
				{  
				pluralDisplayName = value;
				
				if (pluralDisplayName != null)
					{  pluralDisplayNameFromLocale = null;  }
				}
			}
			
		/* Property: PluralDisplayNameFromLocale
		 * The topic type's plural display name identifier to be retrieved from <Engine.Locale>, or null if it's not defined.  If this 
		 * is set to something other than null, <PluralDisplayName> becomes null.
		 */
		public string PluralDisplayNameFromLocale
			{
			get
				{  return pluralDisplayNameFromLocale;  }
			set
				{  
				pluralDisplayNameFromLocale = value;
				
				if (pluralDisplayNameFromLocale != null)
					{  pluralDisplayName = null;  }
				}
			}
			
			
		/* Property: SimpleIdentifier
		 * The topic type's name using only the letters A to Z, or null if it's not defined.
		 */
		public string SimpleIdentifier
			{
			get
				{  return simpleIdentifier;  }
			set
				{  simpleIdentifier = value;  }
			}
			
			
		/* Property: Index
		 * Whether the topic type is indexed, or null if it is not defined.  If set to IndexWith, you must also set the <IndexWith>
		 * property.
		 */
		public TopicType.IndexValue? Index
			{
			get
				{  return index;  }
			set
				{  
				index = value;  
				
				if (index != TopicType.IndexValue.IndexWith)
					{  indexWith = null;  }
				}
			}
			
		/* Property: IndexWith
		 * A string representing the name of the topic type this one is indexed with, provided <Index> is set to IndexWith.  Will
		 * be null otherwise.
		 */
		public string IndexWith
			{
			get
				{
				if (index == TopicType.IndexValue.IndexWith)
					{  return indexWith;  }
				else
					{  return null;  }
				}
			set
				{  indexWith = value;  }
			}
			
		/* Property: Scope
		 * The scope of the topic type, or null if it is not defined.
		 */
		public TopicType.ScopeValue? Scope
			{
			get
				{  return scope;  }
			set
				{  scope = value;  }
			}
			
		/* Property: ClassHierarchy
		 * Whether the topic type can be included in the class hierarchy, or null if it is not defined.
		 */
		public bool? ClassHierarchy
			{
			get
				{  return classHierarchy;  }
			set
				{  classHierarchy = value;  }
			}
			
		/* Property: VariableType
		 * Whether the topic type can be used as a variable type, or null if it is not defined.
		 */
		public bool? VariableType
			{
			get
				{  return variableType;  }
			set
				{  variableType = value;  }
			}
			
		/* Property: BreakLists
		 * Whether list topics should be broken into individual topics in the output, or null if it is not defined.
		 */
		public bool? BreakLists
			{
			get
				{  return breakLists;  }
			set
				{  breakLists = value;  }
			}
			
		/* Property: Keywords
		 * An array of keywords this topic type defines.  It will never be null.  The array's values are arranged in pairs, with
		 * odd ones being the singular form and even ones being the plural.  If there is no plural form for a keyword, it's even
		 * entry will be null.
		 */
		public List<string> Keywords
			{
			get
				{  return keywords;  }
			set
				{  keywords = value;  }
			}
			
			
		/* Function: FixNameCapitalization
		 * Replaces <Name> with a version with alternate capitalization but is otherwise equal.
		 */
		public void FixNameCapitalization (string newName)
			{
			if (string.Compare(name, newName, true) != 0)
				{  throw new Exceptions.NameChangeDifferedInMoreThanCapitalization(name, newName, "ConfigFileTopicType");  }
				
			name = newName;
			}
			
			
		
		// Group: Variables
		// __________________________________________________________________________
		
		/* var: name
		 * The topic type name.
		 */
		protected string name;
		
		/* var: alterType
		 * Whether this entry is for altering a topic type instead of defining a new one.
		 */
		protected bool alterType;
		
		/* var: lineNumber
		 * The line number where this topic type appears in the file.
		 */
		protected int lineNumber;
		
		/* var: displayName
		 * The topic type's display name, or null if it's not defined.
		 */
		protected string displayName;
		
		/* var: displayNameFromLocale
		 * The locale identifier of the topic type's display name, or null if it's not defined.
		 */
		protected string displayNameFromLocale;
		
		/* var: pluralDisplayName
		 * The topic type's plural display name, or null if it's not defined.
		 */
		protected string pluralDisplayName;
		
		/* var: pluralDisplayNameFromLocale
		 * The locale identifier of the topic type's plural display name, or null if it's not defined.
		 */
		protected string pluralDisplayNameFromLocale;
		
		/* var: simpleIdentifier
		 * The topic type's name using only the letters A to Z, or null if it's not defined.
		 */
		protected string simpleIdentifier;
		
		/* var: index
		 * Whether the topic type is indexed, or null if it is not defined.
		 */
		protected TopicType.IndexValue? index;
		
		/* var: indexWith
		 * The name of the topic type to index this one with, or null if none.
		 */
		protected string indexWith;
		
		/* var: scope
		 * The scope of the topic type, or null if it is not defined.
		 */
		protected TopicType.ScopeValue? scope;
		
		/* var: classHierarchy
		 * Whether the topic type can be included in the class hierarchy, or null if it is not defined.
		 */
		protected bool? classHierarchy;
		
		/* var: variableType
		 * Whether the topic type can be used as a variable type, or null if it is not defined.
		 */
		protected bool? variableType;
		
		/* var: breakLists
		 * Whether lists topics should be broken into individual ones, or null if it is not defined.
		 */
		protected bool? breakLists;
		
		/* array: keywords
		 * An array of keyword pairs.  Odd indexes are singular forms, even are plural.  An even entry will be null if a plural
		 * form is not defined.  The variable itself will never be null.
		 */
		protected List<string> keywords;
		}
	}