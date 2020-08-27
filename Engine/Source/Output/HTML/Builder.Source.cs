/* 
 * Class: CodeClear.NaturalDocs.Engine.Output.HTML.Builder
 * ____________________________________________________________________________
 * 
 */

// This file is part of Natural Docs, which is Copyright © 2003-2020 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using CodeClear.NaturalDocs.Engine.Links;
using CodeClear.NaturalDocs.Engine.Topics;


namespace CodeClear.NaturalDocs.Engine.Output.HTML
	{
	public partial class Builder
		{

		// Group: Functions
		// __________________________________________________________________________
		
		/* Function: BuildSourceFile
		 * Builds an output file based on a source file.  The accessor should NOT hold a lock on the database.  This will also
		 * build the metadata files.
		 */
		protected void BuildSourceFile (int fileID, CodeDB.Accessor accessor, CancelDelegate cancelDelegate)
			{
			#if DEBUG
			if (accessor.LockHeld != CodeDB.Accessor.LockType.None)
				{  throw new Exception ("Shouldn't call BuildSourceFile() when the accessor already holds a database lock.");  }
			#endif

			var context = new Context(this, fileID);
			var pageContent = new Components.PageContent(context);

			bool hasTopics = pageContent.BuildDataFiles(context, accessor, cancelDelegate);

			if (cancelDelegate())
				{  return;  }

			if (hasTopics)
				{
				if (buildState.AddSourceFileWithContent(fileID) == true)
					{  unprocessedChanges.AddMenu();  }
				}
			else
				{
				DeleteOutputFileIfExists(context.OutputFile);
				DeleteOutputFileIfExists(context.ToolTipsFile);
				DeleteOutputFileIfExists(context.SummaryFile);
				DeleteOutputFileIfExists(context.SummaryToolTipsFile);

				if (buildState.RemoveSourceFileWithContent(fileID) == true)
					{  unprocessedChanges.AddMenu();  }
				}
			}


		/* Function: DeleteOutputFileIfExists
		 * If the passed file exists, deletes it and adds its parent folder to <foldersToCheckForDeletion>.  It's okay for the
		 * output path to be null.
		 */
		public void DeleteOutputFileIfExists (Path outputFile)
			{
			if (outputFile != null && System.IO.File.Exists(outputFile))
				{  
				System.IO.File.Delete(outputFile);
				unprocessedChanges.AddPossiblyEmptyFolder(outputFile.ParentFolder);
				}
			}

		}
	}

