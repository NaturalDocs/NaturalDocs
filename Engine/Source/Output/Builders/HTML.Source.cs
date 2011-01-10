/* 
 * Class: GregValure.NaturalDocs.Engine.Output.Builders.HTML
 * ____________________________________________________________________________
 * 
 * 
 * File: Output.nd
 * 
 *		A file used to store information about the last time this output target was built.
 *		
 *		> [String: Style Path]
 *		> [String: Style Path]
 *		> ...
 *		> [String: null]
 *		
 *		Stores the list of styles that apply to this target, in the order in which they must be loaded, as a null-terminated
 *		list of style paths.  The paths are either to <HTMLStyle.CSSFile> or <HTMLStyle.ConfigFile>.  These are stored
 *		instead of the names so that if a name is interpreted differently from one run to the next it will be detected.  It's
 *		also the computed list of styles after all inheritance has been applied.
 * 
 */

// This file is part of Natural Docs, which is Copyright © 2003-2008 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using System.Text;


namespace GregValure.NaturalDocs.Engine.Output.Builders
	{
	public partial class HTML
		{

		// Group: Functions
		// __________________________________________________________________________
		
		
		/* Function: BuildSourceFile
		 * Builds an output file based on a source file.  The accessor should NOT hold a lock on the database.
		 */
		protected void BuildSourceFile (int fileID, CodeDB.Accessor accessor, CancelDelegate cancelDelegate)
			{
			accessor.GetReadOnlyLock();
			bool haveLock = true;
			
			try
				{
				IList<Topic> topics = accessor.GetTopicsInFile(fileID, cancelDelegate);
				
				if (cancelDelegate())
					{  return;  }
					
				if (topics.Count == 0)
					{
					accessor.ReleaseLock();
					haveLock = false;
					
					DeleteSourceFile(fileID);  
					return;
					}
				else
					{
					StringBuilder content = new StringBuilder();
					content.Append("<ul>");
						
					foreach (Topic topic in topics)
						{
						content.Append("<li>" + topic.Title + "</li>");
						}
							
					content.Append("</ul>");

					Path outputPath = ToSourceOutputPath(fileID);
					BuildFile(outputPath, "test", content.ToString(), PageType.Content);
					}
				}
				
			finally
				{ 
				if (haveLock)
					{  accessor.ReleaseLock();  }
				}
			}
			
			
		/* Function: DeleteSourceFile
		 * Deletes an output file based on a source file.
		 */
		protected void DeleteSourceFile (int fileID)
			{
			Path outputFile = ToSourceOutputPath(fileID);

			if (outputFile != null &&  System.IO.File.Exists(outputFile))
				{  
				System.IO.File.Delete(outputFile);
				foldersToCheckForDeletion.Add(outputFile.ParentFolder);
				}
			}



		// Group: Path Functions
		// __________________________________________________________________________


		/* Function: ToSourceOutputPath
		 * Returns the output path of the passed source file ID, or null if none.  It may be null if the <FileSource> that created
		 * it no longer exists.
		 */
		public Path ToSourceOutputPath (int fileID)
			{
			Files.File file = Engine.Instance.Files.FromID(fileID);
			Files.FileSource fileSource = Engine.Instance.Files.FileSourceOf(file);

			if (fileSource == null)
				{  return null;  }

			Path relativePath = fileSource.MakeRelative(file.FileName);
			
			string fileName = relativePath.NameWithoutPath;
			
			// We can't have dots in the file name because Apache will try to execute Script.pl.html even though .pl is not
			// the last extension.  Dots in folder names are okay though.
			fileName = fileName.Replace('.', '-');
			
			// Use /../ to ninja in a file name replacement.  It will be fixed in Path's normalization.
			return config.Folder + "/files" + (fileSource.Number != 1 ? fileSource.Number.ToString() : "") + 
					  '/' + relativePath + "/../" + fileName + ".html";
			}


		/* Function: OutputFolder
		 * Returns the output folder for the passed type and number.  This only works with source and image folders.  To
		 * do styles, use <StyleOutputFolder()>.
		 */
		protected Path OutputFolder (Files.InputType type, int number)
			{
			StringBuilder folder = new StringBuilder(config.Folder);
			folder.Append('/');

			if (type == Files.InputType.Source)
				{  folder.Append("files");  }
			else if (type == Files.InputType.Image)
				{  folder.Append("images");  }
			else
				{  throw new InvalidOperationException();  }

			if (number != 1)
				{  folder.Append(number);  }
				
			return folder.ToString();
			}


		/* Function: OutputFolder
		 * Returns the output folder for the passed <FileSource>.  This only works with source and image FileSources.  To
		 * do styles, use <StyleOutputFolder()>.
		 */
		protected Path OutputFolder (Files.FileSource fileSource)
			{
			return OutputFolder(fileSource.Type, fileSource.Number);
			}



		// Group: CodeDB.IChangeWatcher Functions
		// __________________________________________________________________________
		
		
		override public void OnAddTopic (Topic topic, CodeDB.EventAccessor eventAccessor)
			{
			lock (writeLock)
				{
				sourceFilesToRebuild.Add(topic.FileID);
				}
			}

		override public void OnUpdateTopic (Topic oldTopic, int newCommentLineNumber, int newCodeLineNumber, string newBody, 
															CodeDB.EventAccessor eventAccessor)
			{
			lock (writeLock)
				{
				sourceFilesToRebuild.Add(oldTopic.FileID);
				}
			}

		override public void OnDeleteTopic (Topic topic, CodeDB.EventAccessor eventAccessor)
			{
			lock (writeLock)
				{
				sourceFilesToRebuild.Add(topic.FileID);
				}
			}

		}
	}

