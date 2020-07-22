/* 
 * Class: CodeClear.NaturalDocs.Engine.Output.HTML.TopicPage
 * ____________________________________________________________________________
 * 
 * A topic page location, such as for a source file or class.
 * 
 */

// This file is part of Natural Docs, which is Copyright © 2003-2020 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;


namespace CodeClear.NaturalDocs.Engine.Output.HTML
	{
	public struct TopicPage
		{

		// Group: Functions
		// __________________________________________________________________________


		/* Function: TopicPage
		 * Creates a new source file based topic page.
		 */
		public TopicPage (int fileID)
			{
			this.fileID = fileID;
			this.classID = 0;
			this.classString = default;
			}

		/* Function: TopicPage
		 * Creates a new class hierarchy based topic page.
		 */
		public TopicPage (int classID, Symbols.ClassString classString)
			{
			#if DEBUG
			if (classString != null && classID == 0)
				{  throw new Exception("Can't create a TopicPage from a class string when its ID isn't known.");  }
			if (classID != 0 && classString == null)
				{  throw new Exception("Can't create a TopicPage from a class ID when its string isn't known.");  }
			#endif

			this.fileID = 0;
			this.classID = classID;
			this.classString = classString;
			}



		// Group: Properties
		// __________________________________________________________________________


		/* Property: IsNull
		 * Whether there is any topic page set.
		 */
		public bool IsNull
			{
			get
				{  return (fileID == 0 && classID == 0);  }
			}

		/* Property: IsSourceFile
		 * Whether the topic page is for a source file.
		 */
		public bool IsSourceFile
			{
			get
				{  return (fileID != 0);  }
			}

		/* Property: IsClass
		 * Whether the topic page is for a class.  If you need to know whether the topic page is for any page that is part of the
		 * class hierarchy, use <InHierarchy> instead.
		 */
		public bool IsClass
			{
			get
				{  return (InHierarchy && classString.Hierarchy == Hierarchy.Class);  }
			}

		/* Property: IsDatabase
		 * Whether the topic page is for a database.
		 */
		public bool IsDatabase
			{
			get
				{  return (InHierarchy && classString.Hierarchy == Hierarchy.Database);  }
			}

		/* Property: InHierarchy
		 * Whether the topic page appears in any class hierarchy, such as for classes or databases.
		 */
		public bool InHierarchy
			{
			get
				{  return (classID != 0);  }
			}

		/* Property: FileID
		 * If the topic page is a source file, returns the file ID associated with it.  Returns zero otherwise.
		 */
		public int FileID
			{
			get
				{  return fileID;  }
			}

		/* Property: ClassID
		 * If the topic page is in the class hierarchy, returns the class ID associated with it.  Returns zero otherwise.
		 */
		public int ClassID
			{
			get
				{  return classID;  }
			}

		/* Property: ClassString
		 * If the topic page is in the class hierarchy, returns the <Symbols.ClassString> associated with it.  Returns null otherwise.
		 */
		public Symbols.ClassString ClassString
			{
			get
				{  return classString;  }
			}		



		// Group: Variables
		// __________________________________________________________________________


		/* var: fileID
		 * The file ID of the topic page's source file, or zero if it's not source file based.  This cannot be set at the same time as
		 * <classID> and <classString>.
		 */
		private readonly int fileID;

		/* var: classID
		 * The class ID of the topic page's class, or zero if it's not class hierarchy based.  Both classID and <classString> must
		 * be set together, and <fileID> cannot be set.
		 */
		private readonly int classID;

		/* var: classString
		 * The <Symbols.ClassString> of the topic page's class, or null if it's not class hierarchy based.  Both classString and
		 * <classID> must be set together, and <fileID> cannot be set.
		 */
		private readonly Symbols.ClassString classString;

		}
	}

