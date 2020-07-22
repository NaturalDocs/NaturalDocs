/* 
 * Class: CodeClear.NaturalDocs.Engine.Output.Components.HTMLTopicPages.File
 * ____________________________________________________________________________
 * 
 * Creates a <HTMLTopicPage> for a source file.
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
using CodeClear.NaturalDocs.Engine.Links;
using CodeClear.NaturalDocs.Engine.Topics;


namespace CodeClear.NaturalDocs.Engine.Output.Components.HTMLTopicPages
	{
	public class File : HTMLTopicPage
		{

		// Group: Functions
		// __________________________________________________________________________


		/* Constructor: File
		 */
		public File (Output.HTML.Context context) : base (context)
			{
			}


		/* Function: GetTopics
		 * 
		 * Retrieves the <Topics> in the file.
		 * 
		 * If the <CodeDB.Accessor> doesn't have a lock this function will acquire and release a read-only lock.
		 * If it already has a lock it will use it and not release it.
		 */
		public override List<Topic> GetTopics (CodeDB.Accessor accessor, CancelDelegate cancelDelegate)
			{
			bool releaseLock = false;
			if (accessor.LockHeld == CodeDB.Accessor.LockType.None)
				{  
				accessor.GetReadOnlyLock();
				releaseLock = true;
				}

			try
				{  return accessor.GetTopicsInFile(context.TopicPage.FileID, cancelDelegate);  }
			finally
				{
				if (releaseLock)
					{  accessor.ReleaseLock();  }
				}
			}


		/* Function: GetLinks
		 * 
		 * Retrieves the <Links> appearing in the file.
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

			try
				{  return accessor.GetLinksInFile(context.TopicPage.FileID, cancelDelegate);  }
			finally
				{
				if (releaseLock)
					{  accessor.ReleaseLock();  }
				}
			}

		}
	}

