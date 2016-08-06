/* 
 * Class: CodeClear.NaturalDocs.Engine.TopicTypes.Tag
 * ____________________________________________________________________________
 * 
 * A class encompassing a topic type tag.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2016 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;


namespace CodeClear.NaturalDocs.Engine.TopicTypes
	{
	public class Tag : IDObjects.Base
		{
		
		// Group: Types
		// __________________________________________________________________________
		
		/* Enum: TagFlags
		 * 
		 * InSystemFile - Set if the tag was defined in the system config file <Topics.txt>.
		 * InProjectFile - Set if the tag was defined in the project config file <Topics.txt>.
		 * 
		 * InConfigFiles - A combination of <InSystemFile> and <InProjectFile> used for testing if either are set.
		 * 
		 * InBinaryFile - Set if the topic type appears in <Topics.nd>.
		 */
		protected enum TagFlags : byte
			{
			InSystemFile = 0x01,
			InProjectFile = 0x02,
			
			InConfigFiles = InSystemFile | InProjectFile,
			
			InBinaryFile = 0x04
			}
			
			
		// Group: Functions
		// __________________________________________________________________________

		public Tag (string newName) : base()
			{
			name = newName;
			flags = 0;
			}
		

		/* Function: FixNameCapitalization
		 * Replaces <Name> with a version with alternate capitalization but is otherwise equal.  This expects the tag
		 * to be retrievable by <TopicTypes.Manager.TagFromName()> so it can verify that the new name won't cause
		 * any problems.
		 */
		public void FixNameCapitalization (string newName)
			{
			if (string.Compare(name, newName, true) != 0)
				{  throw new Exceptions.NameChangeDifferedInMoreThanCapitalization(name, newName, "Tag");  }
				
			name = newName;
			}
			
			
		
		// Group: Properties
		// __________________________________________________________________________
		
		override public string Name
			{
			get
				{  return name;  }
			}
			
			
		// Group: Flags
		// These do not affect equality.
		// __________________________________________________________________________
			
		/* Property: InSystemFile
		 * Whether this tag was defined in the system <Topics.txt> file.
		 */
		public bool InSystemFile
			{
			get
				{  return ( (flags & TagFlags.InSystemFile) != 0);  }
			set
				{  
				if (value == true)
					{  flags |= TagFlags.InSystemFile;  }
				else
					{  flags &= ~TagFlags.InSystemFile;  }
				}
			}
			
		/* Property: InProjectFile
		 * Whether this tag was defined in the project <Topics.txt> file.
		 */
		public bool InProjectFile
			{
			get
				{  return ( (flags & TagFlags.InProjectFile) != 0);  }
			set
				{  
				if (value == true)
					{  flags |= TagFlags.InProjectFile;  }
				else
					{  flags &= ~TagFlags.InProjectFile;  }
				}
			}
			
		/* Property: InConfigFiles
		 * Whether this tag was defined in either of the <Topics.txt> files.
		 */
		public bool InConfigFiles
			{
			get
				{  return ( (flags & TagFlags.InConfigFiles) != 0);  }
			}
			
		/* Property: InBinaryFile
		 * Whether this tag appears in <Topics.nd>.
		 */
		public bool InBinaryFile
			{
			get
				{  return ( (flags & TagFlags.InBinaryFile) != 0);  }
			set
				{
				if (value == true)
					{  flags |= TagFlags.InBinaryFile;  }
				else
					{  flags &= ~TagFlags.InBinaryFile;  }
				}
			}

			
		// Group: Operators
		// __________________________________________________________________________
		
		
		/* Function: operator ==
		 * Returns whether all the properties of the two tags are equal, including Name and ID, but excluding flags.
		 */
		public static bool operator == (Tag tag1, Tag tag2)
			{
			if ((object)tag1 == null && (object)tag2 == null)
				{  return true;  }
			else if ((object)tag1 == null || (object)tag2 == null)
				{  return false;  }
			else
				{
				// Deliberately does not include Flags
				return (tag1.ID == tag2.ID &&
						   tag1.Name == tag2.Name);
				}
			}
			
		
		/* Function: operator !=
		 * Returns if any of the properties of the two tags are inequal, including Name and ID, but excluding flags.
		 */
		public static bool operator != (Tag tag1, Tag tag2)
			{
			return !(tag1 == tag2);
			}
			
			
			
		// Group: Interface Functions
		// __________________________________________________________________________
		
		
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
		
		protected TagFlags flags;
		}
	}