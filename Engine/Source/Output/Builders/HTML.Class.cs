/* 
 * Class: CodeClear.NaturalDocs.Engine.Output.Builders.HTML
 * ____________________________________________________________________________
 * 
 */

// This file is part of Natural Docs, which is Copyright © 2003-2020 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using System.Text;
using CodeClear.NaturalDocs.Engine.Collections;
using CodeClear.NaturalDocs.Engine.Languages;
using CodeClear.NaturalDocs.Engine.Links;
using CodeClear.NaturalDocs.Engine.Output.Components;
using CodeClear.NaturalDocs.Engine.Symbols;
using CodeClear.NaturalDocs.Engine.Topics;
using CodeClear.NaturalDocs.Engine.CommentTypes;


namespace CodeClear.NaturalDocs.Engine.Output.Builders
	{
	public partial class HTML
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

			Output.HTML.Context context;
			bool hasTopics = false;

			accessor.GetReadOnlyLock();

			try
				{  
				ClassString classString = accessor.GetClassByID(classID);  
				context = new Output.HTML.Context(this, classID, classString);
				Components.HTMLTopicPages.Class page = new Components.HTMLTopicPages.Class(context);

				hasTopics = page.Build(accessor, cancelDelegate);
				}
			finally
				{  accessor.ReleaseLock();  }

			if (cancelDelegate())
				{  return;  }


			if (hasTopics)
				{
				lock (accessLock)
					{
					if (buildState.ClassFilesWithContent.Add(classID) == true)
						{  buildState.NeedToBuildMenu = true;  }
					}
				}
			else
				{
				DeleteOutputFileIfExists(context.OutputFile);
				DeleteOutputFileIfExists(context.ToolTipsFile);
				DeleteOutputFileIfExists(context.SummaryFile);
				DeleteOutputFileIfExists(context.SummaryToolTipsFile);

				lock (accessLock)
					{
					if (buildState.ClassFilesWithContent.Remove(classID) == true)
						{  buildState.NeedToBuildMenu = true;  }
					}
				}
			}

		}
	}

