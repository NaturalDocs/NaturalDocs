/*
 * Class: CodeClear.NaturalDocs.Engine.Output.HTML.PageLocation
 * ____________________________________________________________________________
 *
 * The location of a topic page, such as for a source file or class.
 *
 */

// This file is part of Natural Docs, which is Copyright © 2003-2025 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;


namespace CodeClear.NaturalDocs.Engine.Output.HTML
	{
	public struct PageLocation
		{

		// Group: Functions
		// __________________________________________________________________________


		/* Function: PageLocation
		 * Creates a new page location for a source file.
		 */
		public PageLocation (int fileID)
			{
			this.fileID = fileID;
			this.classID = 0;
			this.classString = default;
			}

		/* Function: PageLocation
		 * Creates a new page location for a class page.
		 */
		public PageLocation (int classID, Symbols.ClassString classString)
			{
			#if DEBUG
			if (classString != null && classID == 0)
				{  throw new Exception("Can't create a PageLocation from a class string when its ID isn't known.");  }
			if (classID != 0 && classString == null)
				{  throw new Exception("Can't create a PageLocation from a class ID when its string isn't known.");  }
			#endif

			this.fileID = 0;
			this.classID = classID;
			this.classString = classString;
			}



		// Group: Properties
		// __________________________________________________________________________


		/* Property: IsNull
		 * Whether there is any page set.
		 */
		public bool IsNull
			{
			get
				{  return (fileID == 0 && classID == 0);  }
			}

		/* Property: IsSourceFile
		 * Whether the page is for a source file.
		 */
		public bool IsSourceFile
			{
			get
				{  return (fileID != 0);  }
			}

		/* Property: IsClass
		 * Whether the page is for a class.
		 */
		public bool IsClass
			{
			get
				{  return (classID != 0);  }
			}

		/* Property: FileID
		 * If the page is a source file, returns the file ID associated with it.  Returns zero otherwise.
		 */
		public int FileID
			{
			get
				{  return fileID;  }
			}

		/* Property: ClassID
		 * If the page is in the class hierarchy, returns the class ID associated with it.  Returns zero otherwise.
		 */
		public int ClassID
			{
			get
				{  return classID;  }
			}

		/* Property: ClassString
		 * If the page is in the class hierarchy, returns the <Symbols.ClassString> associated with it.  Returns null otherwise.
		 */
		public Symbols.ClassString ClassString
			{
			get
				{  return classString;  }
			}



		// Group: Variables
		// __________________________________________________________________________


		/* var: fileID
		 * The file ID of the page's source file, or zero if it's not source file based.  This cannot be set at the same time as
		 * <classID> and <classString>.
		 */
		private readonly int fileID;

		/* var: classID
		 * The class ID of the page's class, or zero if it's not class hierarchy based.  Both classID and <classString> must
		 * be set together, and <fileID> cannot be set.
		 */
		private readonly int classID;

		/* var: classString
		 * The <Symbols.ClassString> of the page's class, or null if it's not class hierarchy based.  Both classString and
		 * <classID> must be set together, and <fileID> cannot be set.
		 */
		private readonly Symbols.ClassString classString;

		}
	}
