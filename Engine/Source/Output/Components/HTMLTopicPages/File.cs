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
		public File (Builders.HTML htmlBuilder, int fileID) : base (htmlBuilder)
			{
			this.fileID = fileID;
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
				{  return accessor.GetTopicsInFile(fileID, cancelDelegate);  }
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
				{  return accessor.GetLinksInFile(fileID, cancelDelegate);  }
			finally
				{
				if (releaseLock)
					{  accessor.ReleaseLock();  }
				}
			}


		/* Function: GetLinkTarget
		 */
		public override HTMLTopicPage GetLinkTarget (Topic targetTopic)
			{
			return new HTMLTopicPages.File (htmlBuilder, targetTopic.FileID);
			}



		// Group: Properties
		// __________________________________________________________________________


		/* Property: PageTitle
		 */
		override public string PageTitle
			{
			get
				{  return EngineInstance.Files.FromID(fileID).FileName.NameWithoutPath;  }
			}

		/* Property: IncludeClassInTopicHashPaths
		 */
		override public bool IncludeClassInTopicHashPaths
			{
			get
				{  return true;  }
			}



		// Group: Path Properties
		// __________________________________________________________________________


		/* Property: OutputFile
		 * The path of the topic page's output file, or null if none.  It may be null if the <FileSource> that created
		 * it no longer exists.
		 */
		override public Path OutputFile
		   {  
			get
				{  
				Files.File file = EngineInstance.Files.FromID(fileID);
				Files.FileSource fileSource = EngineInstance.Files.FileSourceOf(file);

				if (fileSource == null)
					{  return null;  }

				Path relativePath = fileSource.MakeRelative(file.FileName);

				return htmlBuilder.Source_OutputFolder(fileSource.Number, relativePath.ParentFolder) + '/' + OutputFileNameOnly;
				}
			}

		/* Property: OutputFileHashPath
		 * The hash path of the topic page, or null if none.  It may be null if the <FileSource> that created it no longer exists.
		 */
		override public string OutputFileHashPath
			{
			get
				{  
				Files.File file = EngineInstance.Files.FromID(fileID);
				Files.FileSource fileSource = EngineInstance.Files.FileSourceOf(file);

				if (fileSource == null)
					{  return null;  }

				Path relativePath = fileSource.MakeRelative(file.FileName);

				// OutputFolderHashPath already includes the trailing symbol so we don't need + '/' +
				return htmlBuilder.Source_OutputFolderHashPath(fileSource.Number, relativePath.ParentFolder) + OutputFileNameOnlyHashPath;
				}
			}

		/* Property: OutputFileNameOnly
		 * The output file name of topic page without the path.
		 */
		public Path OutputFileNameOnly
			{
			get
				{
				Files.File file = EngineInstance.Files.FromID(fileID);
				string nameString = file.FileName.NameWithoutPath.ToString();
				return Output.HTML.Paths.Utilities.Sanitize(nameString, true) + ".html";
				}
			}


		/* Property: OutputFileNameOnlyHashPath
		 * The file name portion of the topic page's hash path.
		 */
		public string OutputFileNameOnlyHashPath
			{
			get
				{
				Files.File file = EngineInstance.Files.FromID(fileID);
				string nameString = file.FileName.NameWithoutPath.ToString();
				return Output.HTML.Paths.Utilities.Sanitize(nameString);
				}
			}

		/* Property: ToolTipsFile
		 * The path of the topic page's tool tips file, or null if none.  It may be null if the <FileSource> that created it no longer exists.
		 */
		override public Path ToolTipsFile
		   {  
			get
				{  
				Files.File file = EngineInstance.Files.FromID(fileID);
				Files.FileSource fileSource = EngineInstance.Files.FileSourceOf(file);

				if (fileSource == null)
					{  return null;  }

				Path relativePath = fileSource.MakeRelative(file.FileName);

				return htmlBuilder.Source_OutputFolder(fileSource.Number, relativePath.ParentFolder) + '/' + ToolTipsFileNameOnly;
				}
			}

		/* Property: ToolTipsFileNameOnly
		 * The file name of the topic page's tool tips file without the path.
		 */
		public Path ToolTipsFileNameOnly
			{
			get
				{
				Files.File file = EngineInstance.Files.FromID(fileID);
				string nameString = file.FileName.NameWithoutPath.ToString();
				return Output.HTML.Paths.Utilities.Sanitize(nameString, true) + "-ToolTips.js";
				}
			}

		/* Property: SummaryFile
		 * The path of the topic page's summary file, or null if none.  It may be null if the <FileSource> that created it no longer 
		 * exists.
		 */
		override public Path SummaryFile
		   {  
			get
				{  
				Files.File file = EngineInstance.Files.FromID(fileID);
				Files.FileSource fileSource = EngineInstance.Files.FileSourceOf(file);

				if (fileSource == null)
					{  return null;  }

				Path relativePath = fileSource.MakeRelative(file.FileName);

				return htmlBuilder.Source_OutputFolder(fileSource.Number, relativePath.ParentFolder) + '/' + SummaryFileNameOnly;
				}
			}

		/* Property: SummaryFileNameOnly
		 * The file name of the topic page's summary file without the path.
		 */
		public Path SummaryFileNameOnly
			{
			get
				{
				Files.File file = EngineInstance.Files.FromID(fileID);
				string nameString = file.FileName.NameWithoutPath.ToString();
				return Output.HTML.Paths.Utilities.Sanitize(nameString, true) + "-Summary.js";
				}
			}

		/* Property: SummaryToolTipsFile
		 * The path of the topic page's summary tool tips file, or null if none.  It may be null if the <FileSource> that created it
		 * no longer exists.
		 */
		override public Path SummaryToolTipsFile
		   {  
			get
				{  
				Files.File file = EngineInstance.Files.FromID(fileID);
				Files.FileSource fileSource = EngineInstance.Files.FileSourceOf(file);

				if (fileSource == null)
					{  return null;  }

				Path relativePath = fileSource.MakeRelative(file.FileName);

				return htmlBuilder.Source_OutputFolder(fileSource.Number, relativePath.ParentFolder) + '/' + SummaryToolTipsFileNameOnly;
				}
			}

		/* Property: SummaryToolTipsFileNameOnly
		 * The file name of the topic page's summary tool tips file without the path.
		 */
		public Path SummaryToolTipsFileNameOnly
			{
			get
				{
				Files.File file = EngineInstance.Files.FromID(fileID);
				string nameString = file.FileName.NameWithoutPath.ToString();
				return Output.HTML.Paths.Utilities.Sanitize(nameString, true) + "-SummaryToolTips.js";
				}
			}



		// Group: Variables
		// __________________________________________________________________________


		/* var: fileID
		 * The ID of the file that this object is building.
		 */
		protected int fileID;

		}
	}

