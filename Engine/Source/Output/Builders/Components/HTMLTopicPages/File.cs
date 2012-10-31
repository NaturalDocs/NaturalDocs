/* 
 * Class: GregValure.NaturalDocs.Engine.Output.Builders.Components.HTMLTopicPages.File
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

// This file is part of Natural Docs, which is Copyright © 2003-2012 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using GregValure.NaturalDocs.Engine.Links;
using GregValure.NaturalDocs.Engine.Topics;


namespace GregValure.NaturalDocs.Engine.Output.Builders.Components.HTMLTopicPages
	{
	public class File : HTMLTopicPage
		{

		// Group: Functions
		// __________________________________________________________________________


		/* Constructor: File
		 */
		public File (Builders.HTML htmlBuilder, CodeDB.Accessor accessor, int fileID) : base (htmlBuilder, accessor)
			{
			this.fileID = fileID;
			}


		/* Function: GetTopics
		 * Retrieves the <Topics> in the file.  If there are no topics it will return an empty list.
		 */
		public override List<Topic> GetTopics (CancelDelegate cancelDelegate)
			{
			bool releaseLock = false;
			if (accessor.LockHeld == CodeDB.Accessor.LockType.None)
				{  
				accessor.GetReadOnlyLock();
				releaseLock = true;
				}

			try
				{  return accessor.GetTopicsInFile(fileID, cancelDelegate);  }
			finally
				{
				if (releaseLock)
					{  accessor.ReleaseLock();  }
				}
			}


		/* Function: GetLinks
		 * Retrieves the <Links> appearing in the file.  If there are no links it will return an empty list.
		 */
		public override List<Link> GetLinks (CancelDelegate cancelDelegate)
			{
			bool releaseLock = false;
			if (accessor.LockHeld == CodeDB.Accessor.LockType.None)
				{  
				accessor.GetReadOnlyLock();
				releaseLock = true;
				}

			try
				{  return accessor.GetLinksInFile(fileID, cancelDelegate);  }
			finally
				{
				if (releaseLock)
					{  accessor.ReleaseLock();  }
				}
			}



		// Group: Properties
		// __________________________________________________________________________


		/* Property: PageTitle
		 */
		override public string PageTitle
			{
			get
				{  return Instance.Files.FromID(fileID).FileName.NameWithoutPath;  }
			}

		/* Property: OutputFile
		 */
		override public Path OutputFile
		   {  
			get
				{  return htmlBuilder.Source_OutputFile(fileID);  }
			}

		/* Property: OutputFileHashPath
		 */
		override public string OutputFileHashPath
			{
			get
				{  return htmlBuilder.Source_OutputFileHashPath(fileID);  }
			}

		/* Property: ToolTipsFile
		 */
		override public Path ToolTipsFile
		   {  
			get
				{  return htmlBuilder.Source_ToolTipsFile(fileID);  }
			}

		/* Property: SummaryFile
		 */
		override public Path SummaryFile
		   {  
			get
				{  return htmlBuilder.Source_SummaryFile(fileID);  }
			}

		/* Property: SummaryToolTipsFile
		 */
		override public Path SummaryToolTipsFile
		   {  
			get
				{  return htmlBuilder.Source_SummaryToolTipsFile(fileID);  }
			}



		// Group: Variables
		// __________________________________________________________________________


		/* var: fileID
		 * The ID of the file that this object is building.
		 */
		protected int fileID;

		}
	}

