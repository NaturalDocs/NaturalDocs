/*
 * Class: CodeClear.NaturalDocs.Engine.CommentTypes.Tag
 * ____________________________________________________________________________
 *
 * A class encompassing a comment type tag.
 *
 *
 * Multithreading: Thread Safe, Read-Only
 *
 *		As this object is read-only after it is created, it is inherently thread safe.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2025 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;


namespace CodeClear.NaturalDocs.Engine.CommentTypes
	{
	public class Tag : IDObjects.IDObject
		{

		// Group: Functions
		// __________________________________________________________________________

		public Tag (string newName) : base()
			{
			name = newName;
			}



		// Group: Properties
		// __________________________________________________________________________

		override public string Name
			{
			get
				{  return name;  }
			}



		// Group: Operators
		// __________________________________________________________________________


		/* Function: operator ==
		 * Returns whether all the properties of the two tags are equal.
		 */
		public static bool operator == (Tag tag1, Tag tag2)
			{
			if ((object)tag1 == null && (object)tag2 == null)
				{  return true;  }
			else if ((object)tag1 == null || (object)tag2 == null)
				{  return false;  }
			else
				{
				return (tag1.ID == tag2.ID &&
						   tag1.Name == tag2.Name);
				}
			}

		/* Function: operator !=
		 * Returns if any of the properties of the two tags are different.
		 */
		public static bool operator != (Tag tag1, Tag tag2)
			{
			return !(tag1 == tag2);
			}

		public override bool Equals (object o)
			{
			if (o is Tag)
				{  return (this == (Tag)o);  }
			else
				{  return false;  }
			}

		public override int GetHashCode ()
			{
			return Name.GetHashCode();
			}



		// Group: Variables
		// __________________________________________________________________________

		protected string name;

		}
	}
