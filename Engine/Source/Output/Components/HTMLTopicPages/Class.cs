/* 
 * Class: GregValure.NaturalDocs.Engine.Output.Components.HTMLTopicPages.Class
 * ____________________________________________________________________________
 * 
 * Creates a <HTMLTopicPage> for a class.
 * 
 * 
 * Threading: Not Thread Safe
 * 
 *		This class is only designed to be used by one thread at a time.
 * 
 */

// This file is part of Natural Docs, which is Copyright © 2003-2012 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using GregValure.NaturalDocs.Engine.Languages;
using GregValure.NaturalDocs.Engine.Links;
using GregValure.NaturalDocs.Engine.Symbols;
using GregValure.NaturalDocs.Engine.Topics;
using GregValure.NaturalDocs.Engine.TopicTypes;


namespace GregValure.NaturalDocs.Engine.Output.Components.HTMLTopicPages
	{
	public class Class : HTMLTopicPage
		{

		// Group: Functions
		// __________________________________________________________________________


		/* Constructor: Class
		 * 
		 * Creates a new class topic page.  Which features are available depend on which parameters you pass.
		 * 
		 * - If you pass a <ClassString> AND a class ID, all features are available immediately.
		 * - If you only pass a <ClassString>, then only the <path properties> are available.
		 * - If you only pass a class ID, then only the <database functions> are available.
		 *		- The <path properties> will be available after one of the <database functions> is called as they will look up the
		 *			<ClassString>.
		 *		- It is safe to call <HTMLTopicPage.Build()> when you only passed a class ID.
		 */
		public Class (Builders.HTML htmlBuilder, int classID = 0, ClassString classString = default(ClassString)) : base (htmlBuilder)
			{
			// DEPENDENCY: This class assumes HTMLTopicPage.Build() will call a database function before using any path properties.

			this.classID = classID;
			this.classString = classString;
			}



		// Group: Database Functions
		// __________________________________________________________________________


		/* Function: GetTopics
		 * 
		 * Retrieves the <Topics> in the class.  If there are no topics it will return an empty list.
		 * 
		 * If the <CodeDB.Accessor> doesn't have a lock this function will acquire and release a read-only lock.
		 * If it already has a lock it will use it and not release it.
		 */
		public override List<Topic> GetTopics (CodeDB.Accessor accessor, CancelDelegate cancelDelegate)
			{
			#if DEBUG
			if (classID <= 0)
				{  throw new Exception("You cannot use HTMLTopicPages.Class.GetTopics when classID is not set.");  }
			#endif


			// Retrieve the topics from the database.

			bool releaseLock = false;
			if (accessor.LockHeld == CodeDB.Accessor.LockType.None)
				{
				accessor.GetReadOnlyLock();
				releaseLock = true;
				}

			List<Topic> topics = null;
			
			try
				{  
				if (classString == null)
					{  classString = accessor.GetClassByID(classID);  }

				topics = accessor.GetTopicsInClass(classID, cancelDelegate);  
				}
			finally
				{
				if (releaseLock)
					{  accessor.ReleaseLock();  }
				}


			// Filter out any list topics that are members of the hierarchy.  If someone documents classes as part of a list,
			// we only want pages for the individual members, not the list topic.

			for (int i = 0; i < topics.Count; i++)
				{
				bool remove = false;

				if (topics[i].IsList)
					{
					TopicType topicType = Engine.Instance.TopicTypes.FromID(topics[i].TopicTypeID);

					if (topicType.Flags.ClassHierarchy || topicType.Flags.DatabaseHierarchy)
						{  remove = true;  }
					}

				if (remove)
					{  topics.RemoveAt(i);  }
				else
					{  i++;  }
				}


			// Merge the topics from multiple files into one coherent list.

			ClassView.MergeTopics(topics);


			return topics;
			}


		/* Function: GetLinks
		 * 
		 * Retrieves the <Links> appearing in the class.  If there are no links it will return an empty list.
		 * 
		 * If the <CodeDB.Accessor> doesn't have a lock this function will acquire and release a read-only lock.
		 * If it already has a lock it will use it and not release it.
		 */
		public override List<Link> GetLinks (CodeDB.Accessor accessor, CancelDelegate cancelDelegate)
			{
			#if DEBUG
			if (classID <= 0)
				{  throw new Exception("You cannot use HTMLTopicPages.Class.GetLinks when classID is not set.");  }
			#endif

			bool releaseLock = false;
			if (accessor.LockHeld == CodeDB.Accessor.LockType.None)
				{
				accessor.GetReadOnlyLock();
				releaseLock = true;
				}

			List<Link> links = null;

			try
				{  
				if (classString == null)
					{  classString = accessor.GetClassByID(classID);  }

				links = accessor.GetLinksInClass(classID, cancelDelegate);  
				}
			finally
				{
				if (releaseLock)
					{  accessor.ReleaseLock();  }
				}

			return links;
			}


		/* Function: GetLinkTarget
		 */
		public override HTMLTopicPage GetLinkTarget (Topic targetTopic)
			{
			// We want to stay in the class view if we can, but since there's no generated page for globals we have to
			// switch to the file view for them.

			if (targetTopic.ClassID != 0)
				{  return new HTMLTopicPages.Class (htmlBuilder, targetTopic.ClassID, targetTopic.ClassString);  }
			else
				{  return new HTMLTopicPages.File (htmlBuilder, targetTopic.FileID);  }
			}



		// Group: Properties
		// __________________________________________________________________________


		/* Property: PageTitle
		 */
		override public string PageTitle
			{
			get
				{  
				#if DEBUG
				if (classString == null)
					{  throw new Exception("You cannot use HTMLTopicPages.Class.PageTitle when classString is not set.");  }
				#endif

				return classString.Symbol.LastSegment;  
				}
			}

		/* Property: IncludeClassInTopicHashPaths
		 */
		override public bool IncludeClassInTopicHashPaths
			{
			get
				{  return false;  }
			}



		// Group: Path Properties
		// __________________________________________________________________________


		/* Property: OutputFile
		 * The path of the topic page's output file.
		 */
		override public Path OutputFile
		   {  
			get
				{  
				#if DEBUG
				if (classString == null)
					{  throw new Exception("You cannot use the path properties in HTMLTopicPages.Class when classString is not set.");  }
				#endif

				if (classString.Hierarchy == ClassString.HierarchyType.Class)
					{
					var language = Engine.Instance.Languages.FromID(classString.LanguageID);
					return htmlBuilder.Class_OutputFolder(language, classString.Symbol.WithoutLastSegment) + '/' + OutputFileNameOnly;
					}
				else // Database
					{  
					return htmlBuilder.Database_OutputFolder(classString.Symbol.WithoutLastSegment) + '/' + OutputFileNameOnly;
					}
				}
			}

		/* Property: OutputFileHashPath
		 * The hash path of the topic page.
		 */
		override public string OutputFileHashPath
			{
			get
				{  
				#if DEBUG
				if (classString == null)
					{  throw new Exception("You cannot use the path properties in HTMLTopicPages.Class when classString is not set.");  }
				#endif

				if (classString.Hierarchy == ClassString.HierarchyType.Class)
					{
					var language = Engine.Instance.Languages.FromID(classString.LanguageID);

					// OutputFolderHashPath already includes the trailing separator so we can just concatenate them.
					return htmlBuilder.Class_OutputFolderHashPath(language, classString.Symbol.WithoutLastSegment) + 
								 OutputFileNameOnlyHashPath;
					}
				else // Database
					{
					return htmlBuilder.Database_OutputFolderHashPath(classString.Symbol.WithoutLastSegment) + 
								 OutputFileNameOnlyHashPath;
					}
				}
			}

		/* Property: OutputFileNameOnly
		 * The output file name of topic page without the path.
		 */
		public Path OutputFileNameOnly
			{
			get
				{
				#if DEBUG
				if (classString == null)
					{  throw new Exception("You cannot use the path properties in HTMLTopicPages.Class when classString is not set.");  }
				#endif

				string nameString = classString.Symbol.LastSegment;
				return Builders.HTML.SanitizePath(nameString, true) + ".html";
				}
			}


		/* Property: OutputFileNameOnlyHashPath
		 * The file name portion of the topic page's hash path.
		 */
		public string OutputFileNameOnlyHashPath
			{
			get
				{
				#if DEBUG
				if (classString == null)
					{  throw new Exception("You cannot use the path properties in HTMLTopicPages.Class when classString is not set.");  }
				#endif

				string nameString = classString.Symbol.LastSegment;
				return Builders.HTML.SanitizePath(nameString);
				}
			}

		/* Property: ToolTipsFile
		 * The path of the topic page's tool tips file.
		 */
		override public Path ToolTipsFile
		   {  
			get
				{  
				#if DEBUG
				if (classString == null)
					{  throw new Exception("You cannot use the path properties in HTMLTopicPages.Class when classString is not set.");  }
				#endif

				if (classString.Hierarchy == ClassString.HierarchyType.Class)
					{
					var language = Engine.Instance.Languages.FromID(classString.LanguageID);
					return htmlBuilder.Class_OutputFolder(language, classString.Symbol.WithoutLastSegment) + '/' + ToolTipsFileNameOnly;
					}
				else // Database
					{
					return htmlBuilder.Database_OutputFolder(classString.Symbol.WithoutLastSegment) + '/' + ToolTipsFileNameOnly;
					}
				}
			}

		/* Property: ToolTipsFileNameOnly
		 * The file name of the topic page's tool tips file without the path.
		 */
		public Path ToolTipsFileNameOnly
			{
			get
				{
				#if DEBUG
				if (classString == null)
					{  throw new Exception("You cannot use the path properties in HTMLTopicPages.Class when classString is not set.");  }
				#endif

				string nameString = classString.Symbol.LastSegment;
				return Builders.HTML.SanitizePath(nameString, true) + "-ToolTips.js";
				}
			}

		/* Property: SummaryFile
		 * The path of the topic page's summary file.
		 */
		override public Path SummaryFile
		   {  
			get
				{  
				#if DEBUG
				if (classString == null)
					{  throw new Exception("You cannot use the path properties in HTMLTopicPages.Class when classString is not set.");  }
				#endif

				if (classString.Hierarchy == ClassString.HierarchyType.Class)
					{
					var language = Engine.Instance.Languages.FromID(classString.LanguageID);
					return htmlBuilder.Class_OutputFolder(language, classString.Symbol.WithoutLastSegment) + '/' + SummaryFileNameOnly;
					}
				else
					{
					return htmlBuilder.Database_OutputFolder(classString.Symbol.WithoutLastSegment) + '/' + SummaryFileNameOnly;
					}
				}
			}

		/* Property: SummaryFileNameOnly
		 * The file name of the topic page's summary file without the path.
		 */
		public Path SummaryFileNameOnly
			{
			get
				{
				#if DEBUG
				if (classString == null)
					{  throw new Exception("You cannot use the path properties in HTMLTopicPages.Class when classString is not set.");  }
				#endif

				string nameString = classString.Symbol.LastSegment;
				return Builders.HTML.SanitizePath(nameString, true) + "-Summary.js";
				}
			}

		/* Property: SummaryToolTipsFile
		 * The path of the topic page's summary tool tips file.
		 */
		override public Path SummaryToolTipsFile
		   {  
			get
				{  
				#if DEBUG
				if (classString == null)
					{  throw new Exception("You cannot use the path properties in HTMLTopicPages.Class when classString is not set.");  }
				#endif

				if (classString.Hierarchy == ClassString.HierarchyType.Class)
					{
					var language = Engine.Instance.Languages.FromID(classString.LanguageID);
					return htmlBuilder.Class_OutputFolder(language, classString.Symbol.WithoutLastSegment) + '/' + 
								 SummaryToolTipsFileNameOnly;
					}
				else // Database
					{
					return htmlBuilder.Database_OutputFolder(classString.Symbol.WithoutLastSegment) + '/' + 
								 SummaryToolTipsFileNameOnly;
					}
				}
			}

		/* Property: SummaryToolTipsFileNameOnly
		 * The file name of the topic page's summary tool tips file without the path.
		 */
		public Path SummaryToolTipsFileNameOnly
			{
			get
				{
				#if DEBUG
				if (classString == null)
					{  throw new Exception("You cannot use the path properties in HTMLTopicPages.Class when classString is not set.");  }
				#endif

				string nameString = classString.Symbol.LastSegment;
				return Builders.HTML.SanitizePath(nameString, true) + "-SummaryToolTips.js";
				}
			}



		// Group: Variables
		// __________________________________________________________________________


		/* var: classID
		 * The ID of the class that this object is building.
		 */
		protected int classID;

		/* var: classString
		 * The <Symbols.ClassString> associated with <classID>.
		 */
		protected Symbols.ClassString classString;

		}
	}

