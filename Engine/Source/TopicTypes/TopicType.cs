/* 
 * Class: GregValure.NaturalDocs.Engine.TopicTypes.TopicType
 * ____________________________________________________________________________
 * 
 * A class encapsulating information about a topic type.  This differs from <ConfigFileTopicType> in that its meant to 
 * represent the final combined settings of a topic type rather than its entry in a config file.  For example, all fields are 
 * initialized to default values rather than null or Default, and it doesn't store the type's keywords.
 * 
 * 
 * Topic: Internal Design Notes
 * 
 *		- Why set <displayName> and <pluralDisplayName> to null?  Why not set them to default values in the constructor like
 *		   the rest of the variables?
 *		
 *		Because their default values change based on what was set.  <displayName's> default value is <name> no matter
 *		what so that can be done, but <pluralDisplayName's> default value is whatever <displayName> is.  If it's set in the
 *		constructor it gets set to <name> as well, but if <displayName> then gets changed it wouldn't affect 
 *		<pluralDisplayName>.
 * 
 */

// This file is part of Natural Docs, which is Copyright © 2003-2012 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;


namespace GregValure.NaturalDocs.Engine.TopicTypes
	{
	public class TopicType : IDObjects.Base
		{
		
		// Group: Types
		// __________________________________________________________________________
		
		/* Enum: ScopeValue
		 * Can be Normal, Start, End, or AlwaysGlobal.
		 */
		public enum ScopeValue : byte
			{  Normal, Start, End, AlwaysGlobal  };
		
		/* Enum: IndexValue
		 * Can be Yes, No, or IndexWith.
		 */
		public enum IndexValue : byte
			{  Yes, No, IndexWith  };
		
		/* Enum: TopicTypeFlags
		 * 
		 * InSystemFile - Set if the topic type was defined in the system config file <Topics.txt>.
		 * InProjectFile - Set if the topic type was defined in the project config file <Topics.txt>.  Not set for Alter TopicType.
		 * 
		 * InConfigFiles - A combination of <InSystemFile> and <InProjectFile> used for testing if either are set.
		 * 
		 * InBinaryFile - Set if the topic type appears in <Topics.nd>.
		 */
		protected enum TopicTypeFlags : byte
			{
			InSystemFile = 0x01,
			InProjectFile = 0x02,
			
			InConfigFiles = InSystemFile | InProjectFile,
			
			InBinaryFile = 0x04
			}
		
		
		
		// Group: Functions and Properties
		// __________________________________________________________________________
		
		
		/* Constructor: TopicType
		 */
		public TopicType (string newName) : base()
			{
			name = newName;
			
			// To indicate that they're not manually set.  Their properties will always return a value though.
			displayName = null;
			pluralDisplayName = null;
			simpleIdentifier = null;
			
			index = IndexValue.Yes;
			indexWith = 0;
			scope = ScopeValue.Normal;
			classHierarchy = false;
			variableType = false;
			breakLists = false;
			flags = 0;
			}
			
			
		/* Property: Name
		 * The name of the topic type, not to be confused with <DisplayName>.
		 */
		override public string Name
			{
			get
				{  return name;  }
			}
			
		/* Property: DisplayName
		 * The topic type's display name.
		 */
		public string DisplayName
			{
			get
				{  
				if (displayName == null)
					{  return name;  }
				else
					{  return displayName;  }
				}
			set
				{  displayName = value;  }
			}
			
		/* Property: PluralDisplayName
		 * The topic type's plural display name.
		 */
		public string PluralDisplayName
			{
			get
				{  
				if (pluralDisplayName == null)
					{  return DisplayName;  }
				else
					{  return pluralDisplayName;  }
				}
			set
				{  pluralDisplayName = value;  }
			}
			
		/* Property: SimpleIdentifier
		 * The topic type's name using only the letters A to Z.
		 */
		public string SimpleIdentifier
			{
			get
				{
				// Generate and store simpleIdentifier.  Since Name can't change, we don't have to worry about keeping
				// this null so that it can be regenerated again.
				if (simpleIdentifier == null)
					{  simpleIdentifier = name.OnlyAToZ();  }
					
				// A fallback if that didn't work.
				if (simpleIdentifier == null)
					{  simpleIdentifier = "TopicTypeID" + ID;  }
					
				return simpleIdentifier;
				}
			set
				{  simpleIdentifier = value;  }
			}
			
		/* Property: Index
		 * Whether the topic type is indexed.  If set to IndexWith, you must also set the <IndexWith> property.
		 */
		public IndexValue Index
			{
			get
				{  return index;  }
			set
				{  
				index = value;  
				
				if (index != IndexValue.IndexWith)
					{  indexWith = 0;  }
				}
			}
			
		/* Property: IndexWith
		 * The ID of the topic type this one is indexed with, provided <Index> is set to IndexWith.  Will be zero otherwise.
		 */
		public int IndexWith
			{
			get
				{
				if (index == IndexValue.IndexWith)
					{  return indexWith;  }
				else
					{  return 0;  }
				}
			set
				{  indexWith = value;  }
			}
			
		/* Property: Scope
		 * The scope of the topic type.
		 */
		public ScopeValue Scope
			{
			get
				{  return scope;  }
			set
				{  scope = value;  }
			}
			
		/* Property: ClassHierarchy
		 * Whether the topic type can be included in the class hierarchy.
		 */
		public bool ClassHierarchy
			{
			get
				{  return classHierarchy;  }
			set
				{  classHierarchy = value;  }
			}
			
		/* Property: VariableType
		 * Whether the topic type can be used as a variable type.
		 */
		public bool VariableType
			{
			get
				{  return variableType;  }
			set
				{  variableType = value;  }
			}
			
		/* Property: BreakLists
		 * Whether list topics should be broken into individual topics in the output.
		 */
		public bool BreakLists
			{
			get
				{  return breakLists;  }
			set
				{  breakLists = value;  }
			}
			
			
		
		// Group: Flags
		// These properties do not affect the equality operators.
		// __________________________________________________________________________
		
		
		/* Property: InSystemFile
		 * Whether this topic type was defined in the system <Topics.txt> file.
		 */
		public bool InSystemFile
			{
			get
				{  return ( (flags & TopicTypeFlags.InSystemFile) != 0);  }
			set
				{  
				if (value == true)
					{  flags |= TopicTypeFlags.InSystemFile;  }
				else
					{  flags &= ~TopicTypeFlags.InSystemFile;  }
				}
			}
			
		/* Property: InProjectFile
		 * Whether this topic type was defined in the project <Topics.txt> file.
		 */
		public bool InProjectFile
			{
			get
				{  return ( (flags & TopicTypeFlags.InProjectFile) != 0);  }
			set
				{  
				if (value == true)
					{  flags |= TopicTypeFlags.InProjectFile;  }
				else
					{  flags &= ~TopicTypeFlags.InProjectFile;  }
				}
			}
			
		/* Property: InConfigFiles
		 * Whether this topic type was defined in either of the <Topics.txt> files.
		 */
		public bool InConfigFiles
			{
			get
				{  return ( (flags & TopicTypeFlags.InConfigFiles) != 0);  }
			}
			
		/* Property: InBinaryFile
		 * Whether this topic type appears in <Topics.nd>.
		 */
		public bool InBinaryFile
			{
			get
				{  return ( (flags & TopicTypeFlags.InBinaryFile) != 0);  }
			set
				{
				if (value == true)
					{  flags |= TopicTypeFlags.InBinaryFile;  }
				else
					{  flags &= ~TopicTypeFlags.InBinaryFile;  }
				}
			}
			
			
			
			
		// Group: Operators
		// __________________________________________________________________________
		
		
		/* Function: operator ==
		 * Returns whether all the properties of the two topic types are equal, including Name and ID, but excluding flags.
		 */
		public static bool operator == (TopicType type1, TopicType type2)
			{
			if ((object)type1 == null && (object)type2 == null)
				{  return true;  }
			else if ((object)type1 == null || (object)type2 == null)
				{  return false;  }
			else
				{
				// Deliberately does not include Flags
				return (type1.ID == type2.ID &&
							  type1.Index == type2.Index &&
							  type1.Scope == type2.Scope &&
							  type1.ClassHierarchy == type2.ClassHierarchy &&
							  type1.VariableType == type2.VariableType &&
							  type1.BreakLists == type2.BreakLists &&
							  (type1.Index != IndexValue.IndexWith || type1.IndexWith == type2.IndexWith) &&

							  type1.Name == type2.Name &&
							  type1.DisplayName == type2.DisplayName &&
							  type1.PluralDisplayName == type2.PluralDisplayName &&
							  type1.SimpleIdentifier == type2.SimpleIdentifier);
				}
			}
			
		
		/* Function: operator !=
		 * Returns if any of the properties of the two topic types are inequal, including Name and ID, but excluding flags.
		 */
		public static bool operator != (TopicType type1, TopicType type2)
			{
			return !(type1 == type2);
			}
			
			
			
		// Group: Interface Functions
		// __________________________________________________________________________
		
		
		public override bool Equals (object o)
			{
			if (o is TopicType)
				{  return (this == (TopicType)o);  }
			else
				{  return false;  }
			}


		public override int GetHashCode ()
			{
			return Name.GetHashCode();
			}
			
			
		
		// Group: Variables
		// __________________________________________________________________________
		
		/* var: name
		 * The topic type name.
		 */
		protected string name;
		
		/* var: displayName
		 * The topic type's display name.  May be null to indicate that it was not set.  <DisplayName> will always return a value
		 * though.
		 */
		protected string displayName;
		
		/* var: pluralDisplayName
		 * The topic type's plural display name.  May be null to indicate that it was not set.  <PluralDisplayName> will always
		 * return a value though.
		 */
		protected string pluralDisplayName;
		
		/* var: simpleIdentifier
		 * The topic type's name using only the letters A to Z.  No spaces, numbers, symbols, or Unicode.
		 * May be null to indicate that it was not set.  <SimpleIdentifier> will always return a value though.
		 */
		protected string simpleIdentifier;
		
		/* var: index
		 * Whether the topic type is indexed.
		 */
		protected IndexValue index;
		
		/* var: indexWith
		 * The ID of the topic type to index this one with, or zero if none.
		 */
		protected int indexWith;
		
		/* var: scope
		 * The scope of the topic type.
		 */
		protected ScopeValue scope;
		
		/* var: classHierarchy
		 * Whether the topic type can be included in the class hierarchy.
		 */
		protected bool classHierarchy;
		
		/* var: variableType
		 * Whether the topic type can be used as a variable type.
		 */
		protected bool variableType;
		
		/* var: breakLists
		 * Whether lists topics should be broken into individual ones.
		 */
		protected bool breakLists;
		
		/* var: flags
		 * A combination of <FlagValues> describing the type.
		 */
		protected TopicTypeFlags flags;
		}
	}