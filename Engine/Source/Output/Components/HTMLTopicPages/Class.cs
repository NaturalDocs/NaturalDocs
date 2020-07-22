/* 
 * Class: CodeClear.NaturalDocs.Engine.Output.Components.HTMLTopicPages.Class
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

// This file is part of Natural Docs, which is Copyright © 2003-2020 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using CodeClear.NaturalDocs.Engine.Languages;
using CodeClear.NaturalDocs.Engine.Links;
using CodeClear.NaturalDocs.Engine.Symbols;
using CodeClear.NaturalDocs.Engine.Topics;
using CodeClear.NaturalDocs.Engine.CommentTypes;


namespace CodeClear.NaturalDocs.Engine.Output.Components.HTMLTopicPages
	{
	public class Class : HTMLTopicPage
		{

		// Group: Functions
		// __________________________________________________________________________


		/* Constructor: Class
		 * Creates a new class topic page.
		 */
		public Class (Output.HTML.Context context) : base (context)
			{
			}



		// Group: Database Functions
		// __________________________________________________________________________


		/* Function: GetTopics
		 * 
		 * Retrieves the <Topics> in the class.
		 * 
		 * If the <CodeDB.Accessor> doesn't have a lock this function will acquire and release a read-only lock.
		 * If it already has a lock it will use it and not release it.
		 */
		public override List<Topic> GetTopics (CodeDB.Accessor accessor, CancelDelegate cancelDelegate)
			{
			#if DEBUG
			if (context.TopicPage.ClassID <= 0)
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
				topics = accessor.GetTopicsInClass(context.TopicPage.ClassID, cancelDelegate);  
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
					CommentType commentType = EngineInstance.CommentTypes.FromID(topics[i].CommentTypeID);

					if (commentType.Flags.ClassHierarchy || commentType.Flags.DatabaseHierarchy)
						{  remove = true;  }
					}

				if (remove)
					{  topics.RemoveAt(i);  }
				else
					{  i++;  }
				}


			// Merge the topics from multiple files into one coherent list.

			ClassView.MergeTopics(topics, context.Builder);


			return topics;
			}


		/* Function: GetLinks
		 * 
		 * Retrieves the <Links> appearing in the class.
		 * 
		 * If the <CodeDB.Accessor> doesn't have a lock this function will acquire and release a read-only lock.
		 * If it already has a lock it will use it and not release it.
		 */
		public override List<Link> GetLinks (CodeDB.Accessor accessor, CancelDelegate cancelDelegate)
			{
			bool releaseLock = false;
			if (accessor.LockHeld == CodeDB.Accessor.LockType.None)
				{
				accessor.GetReadOnlyLock();
				releaseLock = true;
				}

			List<Link> links = null;

			try
				{  
				links = accessor.GetLinksInClass(context.TopicPage.ClassID, cancelDelegate);  
				}
			finally
				{
				if (releaseLock)
					{  accessor.ReleaseLock();  }
				}

			return links;
			}

		}
	}

