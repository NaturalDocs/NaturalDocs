/*
 * Class: CodeClear.NaturalDocs.Engine.CommentTypes.CommentType
 * ____________________________________________________________________________
 *
 * A class encapsulating information about a comment type.
 *
 *
 * Multithreading: Not Thread Safe, Supports Multiple Readers
 *
 *		This object doesn't have any locking built in, and so it is up to the class managing it to provide thread safety if needed.
 *		However, it does support multiple concurrent readers.  This means it can be used in read-only mode with no locking or
 *		in read/write mode with a ReaderWriterLock.
 *
 */

// This file is part of Natural Docs, which is Copyright © 2003-2026 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;


namespace CodeClear.NaturalDocs.Engine.CommentTypes
	{
	public class CommentType : IDObjects.IDObject
		{

		// Group: Types
		// __________________________________________________________________________


		/* Enum: ScopeValue
		 * Can be Normal, Start, End, or AlwaysGlobal.
		 */
		public enum ScopeValue : byte
			{  Normal, Start, End, AlwaysGlobal  };


		/* Enum: FlagValue
		 *
		 *		Code - Set if the comment type describes a code element.
		 *		File - Set if the comment type describes a file.
		 *		Documentation - Set if the comment type is for standalone documentation.
		 *
		 *		VariableType - Set if the comment type can be used as a type for a variable.
		 *
		 *		Enum - Set if the comment type describes an enum.
		 */
		[Flags]
		public enum FlagValue : byte
			{
			Code = 0x01,
			File = 0x02,
			Documentation = 0x04,

			VariableType = 0x08,

			Enum = 0x40
			}



		// Group: Functions
		// __________________________________________________________________________


		/* Constructor: CommentType
		 */
		public CommentType (string name) : base()
			{
			this.name = name;

			displayName = null;
			pluralDisplayName = null;
			simpleIdentifier = null;
			scope = ScopeValue.Normal;
			hierarchyID = 0;
			flags = default;
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
				{  return displayName;  }
			set
				{  displayName = value;  }
			}

		/* Property: PluralDisplayName
		 * The comment type's plural display name.
		 */
		public string PluralDisplayName
			{
			get
				{  return pluralDisplayName;  }
			set
				{  pluralDisplayName = value;  }
			}

		/* Property: SimpleIdentifier
		 * The comment type's name using only the letters A to Z.
		 */
		public string SimpleIdentifier
			{
			get
				{  return simpleIdentifier;  }
			set
				{  simpleIdentifier = value;  }
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

		/* Property: InHierarchy
		 * Whether this comment type belongs to any hierarchies.  You can get which from <HierarchyID>.
		 */
		public bool InHierarchy
			{
			get
				{  return (hierarchyID != 0);  }
			}

		/* Property: HierarchyID
		 * The ID of the hierarchy this comment type belongs to, or zero if none.
		 */
		public int HierarchyID
			{
			get
				{  return hierarchyID;  }
			set
				{  hierarchyID = value;  }
			}

		/* Property: Flags
		 * The combination of all <FlagValues> applying to the comment type.
		 */
		public FlagValue Flags
			{
			get
				{  return flags;  }
			set
				{  flags = value;  }
			}



		// Group: Flags
		// __________________________________________________________________________


		/* Property: IsCode
		 * Whether the comment type describes a code element.
		 */
		public bool IsCode
			{
			get
				{  return ( (flags & FlagValue.Code) != 0);  }
			set
				{
				if (value == true)
					{  flags |= FlagValue.Code;  }
				else
					{  flags &= ~FlagValue.Code;  }
				}
			}

		/* Property: IsFile
		 * Whether the comment type describes a file.
		 */
		public bool IsFile
			{
			get
				{  return ( (flags & FlagValue.File) != 0);  }
			set
				{
				if (value == true)
					{  flags |= FlagValue.File;  }
				else
					{  flags &= ~FlagValue.File;  }
				}
			}

		/* Property: IsDocumentation
		 * Whether the comment type is used for standalone documentation.
		 */
		public bool IsDocumentation
			{
			get
				{  return ( (flags & FlagValue.Documentation) != 0);  }
			set
				{
				if (value == true)
					{  flags |= FlagValue.Documentation;  }
				else
					{  flags &= ~FlagValue.Documentation;  }
				}
			}

		/* Property: IsVariableType
		 * Whether the comment type describes a code element that can be used as the type of a variable.
		 */
		public bool IsVariableType
			{
			get
				{  return ( (flags & FlagValue.VariableType) != 0);  }
			set
				{
				if (value == true)
					{  flags |= FlagValue.VariableType;  }
				else
					{  flags &= ~FlagValue.VariableType;  }
				}
			}

		/* Property: IsEnum
		 * Whether the comment type describes an enum.
		 */
		public bool IsEnum
			{
			get
				{  return ( (flags & FlagValue.Enum) != 0);  }
			set
				{
				if (value == true)
					{  flags |= FlagValue.Enum;  }
				else
					{  flags &= ~FlagValue.Enum;  }
				}
			}



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
						   type1.Name == type2.Name &&
						   type1.DisplayName == type2.DisplayName &&
						   type1.PluralDisplayName == type2.PluralDisplayName &&
						   type1.SimpleIdentifier == type2.SimpleIdentifier &&
						   type1.Scope == type2.Scope &&
						   type1.HierarchyID == type2.HierarchyID &&
						   type1.Flags == type2.Flags);
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
		 * The comment type's display name.
		 */
		protected string displayName;

		/* var: pluralDisplayName
		 * The comment type's plural display name.
		 */
		protected string pluralDisplayName;

		/* var: simpleIdentifier
		 * The comment type's name using only the letters A to Z.  No spaces, numbers, symbols, or Unicode.
		 */
		protected string simpleIdentifier;

		/* var: scope
		 * The scope of the comment type.
		 */
		protected ScopeValue scope;

		/* var: hierarchyID
		 * The ID of the hierarchy this comment type belongs to, or zero if none.
		 */
		protected int hierarchyID;

		/* var: flags
		 * The combination of all <FlagValues> applying to the comment type.
		 */
		protected FlagValue flags;

		}
	}
