/* 
 * Class: CodeClear.NaturalDocs.Engine.CommentTypes.CommentType
 * ____________________________________________________________________________
 * 
 * A class encapsulating information about a comment type.  This differs from <ConfigFileCommentType> in that its meant to 
 * represent the final combined settings of a comment type rather than its entry in a config file.  For example, all fields are 
 * initialized to default values rather than null or Default, and it doesn't store the type's keywords.
 * 
 */

// This file is part of Natural Docs, which is Copyright © 2003-2016 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;


namespace CodeClear.NaturalDocs.Engine.CommentTypes
	{
	public class CommentType : IDObjects.Base
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
		
		
		
		// Group: Functions
		// __________________________________________________________________________
		
		
		/* Constructor: CommentType
		 */
		public CommentType (string newName) : base()
			{
			name = newName;
			
			// Null to indicate that they're not manually set.  Their properties will always return a value though.
			displayName = null;
			pluralDisplayName = null;
			simpleIdentifier = null;
			
			// Why not set them to default values like name and the rest of the variables?  Because their default values change 
			// based on what else was set.  displayName's default value is name no matter what so that could be done, but 
			// pluralDisplayName's default value is whatever displayName is.  If it's set here it would get set to name as well, but 
			// if displayName then gets changed it wouldn't carry over to pluralDisplayName.

			index = IndexValue.Yes;
			indexWith = 0;
			scope = ScopeValue.Normal;
			breakLists = false;
			Flags = new CommentTypeFlags();
			}
			


		// Group: Properties
		// __________________________________________________________________________

			
		/* Property: Name
		 * The name of the comment type, not to be confused with <DisplayName>.
		 */
		override public string Name
			{
			get
				{  return name;  }
			}
			
		/* Property: DisplayName
		 * The comment type's display name.
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
		 * The comment type's plural display name.
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
		 * The comment type's name using only the letters A to Z.
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
					{  simpleIdentifier = "CommentTypeID" + ID;  }
					
				return simpleIdentifier;
				}
			set
				{  simpleIdentifier = value;  }
			}
			
		/* Property: Index
		 * Whether the comment type is indexed.  If set to IndexWith, you must also set the <IndexWith> property.
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
		 * The ID of the comment type this one is indexed with, provided <Index> is set to IndexWith.  Will be zero otherwise.
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
		 * The scope of the comment type.
		 */
		public ScopeValue Scope
			{
			get
				{  return scope;  }
			set
				{  scope = value;  }
			}
			
		/* Property: BreakLists
		 * Whether list comments should be broken into individual topics in the output.
		 */
		public bool BreakLists
			{
			get
				{  return breakLists;  }
			set
				{  breakLists = value;  }
			}

		/* Variable: Flags
		 * <CommentTypeFlags> as a public variable, so you can use it like a property.
		 */
		public CommentTypeFlags Flags;

			
						
		// Group: Operators
		// __________________________________________________________________________
		
		
		/* Function: operator ==
		 * Returns whether all the properties of the two comment types are equal, including Name and ID, but excluding 
		 * <CommentTypeFlags.Location Properties>.
		 */
		public static bool operator == (CommentType type1, CommentType type2)
			{
			if ((object)type1 == null && (object)type2 == null)
				{  return true;  }
			else if ((object)type1 == null || (object)type2 == null)
				{  return false;  }
			else
				{
				return (type1.ID == type2.ID &&
							  type1.Index == type2.Index &&
							  type1.Scope == type2.Scope &&
							  type1.BreakLists == type2.BreakLists &&
							  type1.Flags.AllConfigurationProperties == type2.Flags.AllConfigurationProperties &&
							  (type1.Index != IndexValue.IndexWith || type1.IndexWith == type2.IndexWith) &&

							  type1.Name == type2.Name &&
							  type1.DisplayName == type2.DisplayName &&
							  type1.PluralDisplayName == type2.PluralDisplayName &&
							  type1.SimpleIdentifier == type2.SimpleIdentifier);
				}
			}
			
		
		/* Function: operator !=
		 * Returns if any of the properties of the two comment types are inequal, including Name and ID, but excluding
		 * <CommentTypeFlags.Location Properties>.
		 */
		public static bool operator != (CommentType type1, CommentType type2)
			{
			return !(type1 == type2);
			}
			
			
			
		// Group: Interface Functions
		// __________________________________________________________________________
		
		
		public override bool Equals (object o)
			{
			if (o is CommentType)
				{  return (this == (CommentType)o);  }
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
		 * The comment type name.
		 */
		protected string name;
		
		/* var: displayName
		 * The comment type's display name.  May be null to indicate that it was not set.  <DisplayName> will always return a value
		 * though.
		 */
		protected string displayName;
		
		/* var: pluralDisplayName
		 * The comment type's plural display name.  May be null to indicate that it was not set.  <PluralDisplayName> will always
		 * return a value though.
		 */
		protected string pluralDisplayName;
		
		/* var: simpleIdentifier
		 * The comment type's name using only the letters A to Z.  No spaces, numbers, symbols, or Unicode.
		 * May be null to indicate that it was not set.  <SimpleIdentifier> will always return a value though.
		 */
		protected string simpleIdentifier;
		
		/* var: index
		 * Whether the comment type is indexed.
		 */
		protected IndexValue index;
		
		/* var: indexWith
		 * The ID of the comment type to index this one with, or zero if none.
		 */
		protected int indexWith;
		
		/* var: scope
		 * The scope of the comment type.
		 */
		protected ScopeValue scope;
		
		/* var: breakLists
		 * Whether lists comments should be broken into individual topics.
		 */
		protected bool breakLists;
		
		// Flags
		// Also remember Flags is a public variable.

		}
	}