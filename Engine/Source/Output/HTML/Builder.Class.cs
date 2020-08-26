/* 
 * Class: CodeClear.NaturalDocs.Engine.Output.HTML.Builder
 * ____________________________________________________________________________
 * 
 */

// This file is part of Natural Docs, which is Copyright © 2003-2020 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using CodeClear.NaturalDocs.Engine.Symbols;


namespace CodeClear.NaturalDocs.Engine.Output.HTML
	{
	public partial class Builder
		{

		// Group: Functions
		// __________________________________________________________________________


		/* Function: BuildClassFile
		 * Builds an output file based on a class.  The accessor should NOT hold a lock on the database.  This will also
		 * build the metadata files.
		 */
		protected void BuildClassFile (int classID, CodeDB.Accessor accessor, CancelDelegate cancelDelegate)
			{
			#if DEBUG
			if (accessor.LockHeld != CodeDB.Accessor.LockType.None)
				{  throw new Exception ("Shouldn't call BuildClassFile() when the accessor already holds a database lock.");  }
			#endif

			Context context;
			bool hasTopics = false;

			accessor.GetReadOnlyLock();

			try
				{  
				ClassString classString = accessor.GetClassByID(classID);

				context = new Context(this, classID, classString);
				var pageContent = new Components.PageContent(context);

				hasTopics = pageContent.BuildDataFiles(context, accessor, cancelDelegate, releaseExistingLocks: true);
				}
			finally
				{  
				if (accessor.LockHeld != CodeDB.Accessor.LockType.None)
					{  accessor.ReleaseLock();  }
				}

			if (cancelDelegate())
				{  return;  }


			if (hasTopics)
				{
				if (buildState.AddClassWithContent(classID) == true)
					{  unprocessedChanges.AddMenu();  }
				}
			else
				{
				DeleteOutputFileIfExists(context.OutputFile);
				DeleteOutputFileIfExists(context.ToolTipsFile);
				DeleteOutputFileIfExists(context.SummaryFile);
				DeleteOutputFileIfExists(context.SummaryToolTipsFile);

				if (buildState.RemoveClassWithContent(classID) == true)
					{  unprocessedChanges.AddMenu();  }
				}
			}

		}
	}

