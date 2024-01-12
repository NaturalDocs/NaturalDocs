/*
 * Class: CodeClear.NaturalDocs.Engine.Files.ChangeProcessor
 * ____________________________________________________________________________
 *
 * A process which handles processing changes to the files Natural Docs scans.  In addition to source files, this includes
 * image files that can be referenced with "(see image.jpg)" and extras tied to CSS styles.
 *
 *
 * Multithreading: Thread Safety Notes
 *
 *		Externally, this class is thread safe.
 *
 *		Internally, all variable accesses must use a monitor on <accessLock>.
 *
 */

// This file is part of Natural Docs, which is Copyright © 2003-2024 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using CodeClear.NaturalDocs.Engine.Languages;
using CodeClear.NaturalDocs.Engine.Links;
using CodeClear.NaturalDocs.Engine.Symbols;
using CodeClear.NaturalDocs.Engine.Tokenization;
using CodeClear.NaturalDocs.Engine.Topics;


namespace CodeClear.NaturalDocs.Engine.Files
	{
	public class ChangeProcessor : Process
		{

		// Group: Types
		// __________________________________________________________________________


		/* enum: ProcessFileResult
		 *
		 * Success - The file was successfully processed.
		 * Cancelled - The file's processing was cancelled.
		 *	FileDoesntExist - The file couldn't be opened because it doesn't exist.  This obviously only applies to changed files,
		 *							 as this is expected with deleted files.
		 *	CantAccessFile - The file exists but couldn't be opened, such as if the program doesn't have permission to access
		 *							the file.
		 */
		public enum ProcessFileResult : byte
			{
			Success, Cancelled, FileDoesntExist, CantAccessFile
			}



		// Group: Initialization and Configuration Functions
		// __________________________________________________________________________


		/* Function: ChangeProcessor
		 */
		public ChangeProcessor (Engine.Instance engineInstance) : base (engineInstance)
			{
			filesBeingProcessed = new FilesBeingProcessed();
			inaccessibleFiles = new IDObjects.NumberSet();
			accessLock = new object();
			}

		override protected void Dispose (bool strictRulesApply)
			{
			if (!strictRulesApply)
				{
				if (!filesBeingProcessed.IsEmpty)
					{  throw new Exception("Files.ChangeProcessor shut down while files were still being processed.");  }
				}
			}



		// Group: Group File Processing Functions
		// __________________________________________________________________________


		/* Function: WorkOnProcessingChanges
		 *
		 * Works on the task of going through all the file changes and deletions and calling <ProcessNewOrChangedFile()> and
		 * <ProcessDeletedFile()> on each one.  This is a parallelizable task, so multiple threads can call this function and they
		 * will divide up the work until it's done.
		 *
		 * The function returns when there is no more work for this thread to do.  If this is the only thread working on it then the
		 * task is complete, but if there are multiple threads, the task is only complete after they all return.  An individual thread
		 * may return prior to that point.
		 */
		public void WorkOnProcessingChanges (CancelDelegate cancelDelegate)
			{
			using (Engine.CodeDB.Accessor codeDBAccessor = EngineInstance.CodeDB.GetAccessor())
				{
				bool deletedFiles, newOrChangedFiles;

				do
					{
					deletedFiles = false;
					newOrChangedFiles = false;

					while (!cancelDelegate())
						{
						File file = PickDeletedFile();

						if (file == null)
							{  break;  }

						deletedFiles = true;
						var result = ProcessDeletedFile(file, codeDBAccessor, cancelDelegate);
						FinalizeDeletedFile(file, result);
						}

					while (!cancelDelegate())
						{
						File file = PickNewOrChangedFile();

						if (file == null)
							{  break;  }

						newOrChangedFiles = true;
						var result = ProcessNewOrChangedFile(file, codeDBAccessor, cancelDelegate);
						FinalizeNewOrChangedFile(file, result);
						}
					}
				// It's possible more deleted files appeared while processing changes so we have to make it through an
				// iteration where both pick functions come up empty.
				while (deletedFiles == true || newOrChangedFiles == true);

				}
			}


		/* Function: GetStatus
		 * Fills the passed object with the status of <WorkOnProcessingChanges()>.  This will be a snapshot of its
		 * progress rather than a live object, so the values won't change out from under you.
		 */
		public void GetStatus (ref ChangeProcessorStatus status)
			{
			lock (accessLock)
				{
				filesBeingProcessed.GetStatus(ref status);
				Manager.UnprocessedChanges.GetStatus(ref status);
				}
			}


		/* Function: GetStatus
		 * Fills the passed object with the status of <WorkOnProcessingChanges()>.  This will be a snapshot of its
		 * progress rather than a live object, so the values won't change out from under you.
		 */
		public void GetStatus (ref ChangeProcessorDetailedStatus status)
			{
			lock (accessLock)
				{
				filesBeingProcessed.GetStatus(ref status);
				Manager.UnprocessedChanges.GetStatus(ref status);
				}
			}



		// Group: Individual File Processing Functions
		// __________________________________________________________________________


		/* Function: PickNewOrChangedFile
		 * Picks a new or changed file to work on, if there are any.  If not it will return null.  Picked files will be added to
		 * <filesBeingProcessed> and must be released with <FinalizeNewOrChangedFile()>.
		 */
		protected File PickNewOrChangedFile ()
			{
			lock (accessLock)
				{
				for (;;)
					{
					int fileID = Manager.UnprocessedChanges.PickNewOrChangedFileID();

					if (fileID == 0)
						{  return null;  }

					// If it's not being processed by another thread, return it.
					if (!filesBeingProcessed.Contains(fileID))
						{
						File file = Manager.FromID(fileID);
						filesBeingProcessed.Add(file);
						return file;
						}

					// If it is being processed by another thread, discard it and pick again.  It's okay to discard it from the list
					// of changes because when processing ends FinalizeNewOrChangedFile() will compare it to the snapshot in
					// filesBeingProcessed and re-add it to the change list if it's different.
					}
				}
			}


		/* Function: PickDeletedFile
		 * Picks a deleted file to work on, if there are any.  If not it will return null.  Picked files will be added to <filesBeingProcessed>
		 * and must be released with <FinalizeDeletedFile()>.
		 */
		protected File PickDeletedFile ()
			{
			lock (accessLock)
				{
				for (;;)
					{
					int fileID = Manager.UnprocessedChanges.PickDeletedFileID();

					if (fileID == 0)
						{  return null;  }

					// If it's not being processed by another thread, return it.
					if (!filesBeingProcessed.Contains(fileID))
						{
						File file = Manager.FromID(fileID);
						filesBeingProcessed.Add(file);
						return file;
						}

					// If it is being processed by another thread, discard it and pick again.  It's okay to discard it from the list
					// of changes because when processing ends FinalizeDeletedFile() will compare it to the snapshot in
					// filesBeingProcessed and re-add it to the change list if it's different.
					}
				}
			}


		/* Function: ProcessNewOrChangedFile
		 * Takes a new or changed <File> and processes it according to its type.  The <CodeDB.Accessor> should NOT already
		 * hold a lock.
		 */
		protected ProcessFileResult ProcessNewOrChangedFile (File file, Engine.CodeDB.Accessor codeDBAccessor,
																					  CancelDelegate cancelDelegate)
			{
			if (file.Type == FileType.Source)
				{  return ProcessNewOrChangedSourceFile(file, codeDBAccessor, cancelDelegate);  }

			else if (file.Type == FileType.Image)
				{  return ProcessNewOrChangedImageFile((ImageFile)file, codeDBAccessor, cancelDelegate);  }

			// Style files are only processed by output targets.  They're not in CodeDB so we don't need to do anything here.
			else
				{  return ProcessFileResult.Success;  }
			}


		/* Function: ProcessNewOrChangedSourceFile
		 * Takes a new or changed <File>, parses it, and updates <CodeDB.Manager> with its contents.  The <CodeDB.Accessor>
		 * should NOT already hold a lock.
		 */
		protected ProcessFileResult ProcessNewOrChangedSourceFile (File file, Engine.CodeDB.Accessor codeDBAccessor,
																								CancelDelegate cancelDelegate)
			{
			try
				{
				var language = EngineInstance.Languages.FromFileName(file.FileName);
				IList<Topic> topics = null;
				LinkSet links = null;
				ImageLinkSet imageLinks = null;


				// Parse the file

				var parseResult = language.Parser.Parse(file.FileName, file.ID, cancelDelegate, out topics, out links);

				if (parseResult == Parser.ParseResult.Cancelled)
					{  return ProcessFileResult.Cancelled;  }
				else if (parseResult == Parser.ParseResult.CantAccessFile)
					{  return ProcessFileResult.CantAccessFile;  }
				else if (parseResult == Parser.ParseResult.FileDoesntExist)
					{  return ProcessFileResult.FileDoesntExist;  }
				else if (parseResult != Parser.ParseResult.Success)
					{  throw new NotImplementedException();  }


				// Extract links from the bodies and prototypes

				if (topics != null && topics.Count > 0)
					{
					imageLinks = new ImageLinkSet();

					foreach (Topic topic in topics)
						{
						GetLinks(topic, ref links, ref imageLinks);

						if (cancelDelegate())
							{  return ProcessFileResult.Cancelled;  }
						}
					}


				// Update the database

				codeDBAccessor.GetReadPossibleWriteLock();

				try
					{
					if (topics != null && topics.Count > 0)
						{  codeDBAccessor.UpdateTopicsInFile(file.ID, topics, cancelDelegate);  }
					else
						{  codeDBAccessor.DeleteTopicsInFile(file.ID, cancelDelegate);  }

					if (links != null && links.Count > 0)
						{  codeDBAccessor.UpdateLinksInFile(file.ID, links, cancelDelegate);  }
					else
						{  codeDBAccessor.DeleteLinksInFile(file.ID, cancelDelegate);  }

					if (imageLinks != null && imageLinks.Count > 0)
						{  codeDBAccessor.UpdateImageLinksInFile(file.ID, imageLinks, cancelDelegate);  }
					else
						{  codeDBAccessor.DeleteImageLinksInFile(file.ID, cancelDelegate);  }

					// Need this check in case CodeDB quit early because of the cancel delegate.
					if (cancelDelegate())
						{  return ProcessFileResult.Cancelled;  }
					}
				finally
					{  codeDBAccessor.ReleaseLock();  }

				return ProcessFileResult.Success;
				}

			catch (Exception e)
				{
				try
					{  e.AddNaturalDocsTask("Parsing file " + file.FileName);  }
				catch
					{  }

				throw;
				}
			}


		/* Function: ProcessNewOrChangedImageFile
		 * Takes a new or changed <ImageFile> and determines its dimensions if it can.
		 */
		protected ProcessFileResult ProcessNewOrChangedImageFile (ImageFile file, Engine.CodeDB.Accessor codeDBAccessor,
																								 CancelDelegate cancelDelegate)
			{
			try
				{
				uint width, height;

				ImageFileProcessor.Result result = ImageFileProcessor.GetDimensions(file.FileName, out width, out height);

				if (result == ImageFileProcessor.Result.FileDoesntExist)
					{  return ProcessFileResult.FileDoesntExist;  }
				else if (result == ImageFileProcessor.Result.CantAccessFile)
					{  return ProcessFileResult.CantAccessFile;  }
				else if (result == ImageFileProcessor.Result.IncorrectFormat)
					{
					// If we can't determine the dimensions because of a file format error or some other reason, then the dimensions
					// aren't knowable to us.  Knowing that is still a successful evaluation of the file because there's no point in trying
					// to do it again.
					file.DimensionsKnown = false;
					return ProcessFileResult.Success;
					}
				else if (result == ImageFileProcessor.Result.Success)
					{
					file.SetDimensions(width, height);
					return ProcessFileResult.Success;
					}
				else
					{  throw new NotImplementedException();  }
				}

			catch (Exception e)
				{
				try
					{  e.AddNaturalDocsTask("Determining dimensions of " + file.FileName);  }
				catch
					{  }

				throw;
				}
			}


		/* Function: ProcessDeletedFile
		 * Takes a deleted <File> and updates <CodeDB.Manager>.  The <CodeDB.Accessor> should NOT already hold a lock.
		 */
		protected ProcessFileResult ProcessDeletedFile (File file, CodeDB.Accessor codeDBAccessor, CancelDelegate cancelDelegate)
			{
			if (file.Type == FileType.Source)
				{  return ProcessDeletedSourceFile(file, codeDBAccessor, cancelDelegate);  }

			// Style and image files are only processed by output targets.  They're not in CodeDB so we don't need to do anything here.
			else
				{  return ProcessFileResult.Success;  }
			}


		/* Function: ProcessDeletedSourceFile
		 * Takes a deleted <File> and updates <CodeDB.Manager>.  The <CodeDB.Accessor> should NOT already hold a lock.
		 */
		protected ProcessFileResult ProcessDeletedSourceFile (File file, CodeDB.Accessor codeDBAccessor,
																					 CancelDelegate cancelDelegate)
			{
			codeDBAccessor.GetReadPossibleWriteLock();

			try
				{
				codeDBAccessor.DeleteTopicsInFile(file.ID, cancelDelegate);
				codeDBAccessor.DeleteLinksInFile(file.ID, cancelDelegate);
				codeDBAccessor.DeleteImageLinksInFile(file.ID, cancelDelegate);
				}
			finally
				{  codeDBAccessor.ReleaseLock();  }

			// Need this check in case CodeDB quit early because of the cancel delegate.
			if (cancelDelegate())
				{  return ProcessFileResult.Cancelled;  }

			return ProcessFileResult.Success;
			}


		/* Function: FinalizeNewOrChangedFile
		 *
		 * Finalizes processing of a changed file based on the <ProcessFileResult> and whether the original file has changed or
		 * been deleted since processing began.
		 */
		protected void FinalizeNewOrChangedFile (File file, ProcessFileResult processResult)
			{
			bool deletedSinceProcessed, changedSinceProcessed;

			lock (accessLock)
				{
				var originalProperties = filesBeingProcessed.GetPropertiesWhenAdded(file);

				deletedSinceProcessed = file.Deleted;
				changedSinceProcessed = (!deletedSinceProcessed && file.LastModified != originalProperties.LastModified);

				filesBeingProcessed.Remove(file);
				}

			if (processResult == ProcessFileResult.Success)
				{
				if (changedSinceProcessed)
					{  Manager.UnprocessedChanges.AddChangedFile(file);  }
				else if (deletedSinceProcessed)
					{  Manager.UnprocessedChanges.AddDeletedFile(file);  }
				// else
					// success
				}

			else if (processResult == ProcessFileResult.Cancelled)
				{
				if (deletedSinceProcessed)
					{  Manager.UnprocessedChanges.AddDeletedFile(file);  }
				else
					{  Manager.UnprocessedChanges.AddChangedFile(file);  }
				}

			else if (processResult == ProcessFileResult.FileDoesntExist)
				{
				if (changedSinceProcessed)
					{  Manager.UnprocessedChanges.AddChangedFile(file);  }
				else
					{  Manager.UnprocessedChanges.AddDeletedFile(file);  }
				}

			else if (processResult == ProcessFileResult.CantAccessFile)
				{
				inaccessibleFiles.Add(file.ID);
				Manager.UnprocessedChanges.AddDeletedFile(file);
				}

			else
				{  throw new ArgumentException();  }
			}


		/* Function: FinalizeDeletedFile
		 *
		 * Finalizes processing of a deleted file based on the <ProcessFileResult> and whether the original file has changed or
		 * been deleted since processing began.
		 */
		protected void FinalizeDeletedFile (File file, ProcessFileResult processResult)
			{
			bool recreatedSinceProcessed = (!file.Deleted && !inaccessibleFiles.Contains(file.ID));

			lock (accessLock)
				{
				filesBeingProcessed.Remove(file);
				}

			if (processResult == ProcessFileResult.Success)
				{
				if (recreatedSinceProcessed)
					{  Manager.UnprocessedChanges.AddNewFile(file);  }
				// else
					// success
				}

			else if (processResult == ProcessFileResult.Cancelled)
				{
				if (recreatedSinceProcessed)
					{  Manager.UnprocessedChanges.AddNewFile(file);  }
				else
					{  Manager.UnprocessedChanges.AddDeletedFile(file);  }
				}

			else
				{  throw new ArgumentException();  }
			}



		// Group: Topic Functions
		// __________________________________________________________________________


		/* Function: GetLinks
		 * Goes through the body and prototype of the passed <Topic> and adds any Natural Docs and image links it finds in the
		 * <NDMarkup> to <LinkSet> and <ImageLinkSet>.
		 */
		protected void GetLinks (Topic topic, ref LinkSet linkSet, ref ImageLinkSet imageLinkSet)
			{
			GetBodyLinks(topic, ref linkSet, ref imageLinkSet);
			GetPrototypeLinks(topic, ref linkSet);
			}


		/* Function: GetBodyLinks
		 * Goes through the body of the passed <Topic> and adds any Natural Docs and image links it finds in the <NDMarkup>
		 * to <LinkSet> and <ImageLinkSet>.
		 */
		protected void GetBodyLinks (Topic topic, ref LinkSet linkSet, ref ImageLinkSet imageLinkSet)
			{
			if (topic.Body == null)
				{  return;  }

			NDMarkup.Iterator iterator = new NDMarkup.Iterator(topic.Body);

			// Doing two passes of GoToFirstTag is probably faster than iterating through each element

			if (iterator.GoToFirstTag("<link type=\"naturaldocs\""))
				{
				do
					{
					Link link = new Link();

					// ignore LinkID
					link.Type = LinkType.NaturalDocs;
					link.Text = iterator.Property("originaltext");
					link.Context = topic.BodyContext;
					// ignore contextID
					link.FileID = topic.FileID;
					link.ClassString = topic.ClassString;
					// ignore classID
					link.LanguageID = topic.LanguageID;
					// ignore EndingSymbol
					// ignore TargetTopicID
					// ignore TargetScore

					linkSet.Add(link);
					}
				while (iterator.GoToNextTag("<link type=\"naturaldocs\""));
				}

			iterator = new NDMarkup.Iterator(topic.Body);

			if (iterator.GoToFirstTag("<image"))
				{
				do
					{
					ImageLink imageLink = new ImageLink();

					// ignore ImageLinkID
					imageLink.OriginalText = iterator.Property("originaltext");
					imageLink.Path = new Path( iterator.Property("target") );
					// ignore FileName, generated from Path
					imageLink.FileID = topic.FileID;
					imageLink.ClassString = topic.ClassString;
					// ignore classID
					// ignore TargetFileID
					// ignore TargetScore

					imageLinkSet.Add(imageLink);
					}
				while (iterator.GoToNextTag("<image"));
				}
			}


		/* Function: GetPrototypeLinks
		 * Goes through the prototype of the passed <Topic> and adds any type links it finds to <LinkSet>.
		 */
		protected void GetPrototypeLinks (Topic topic, ref LinkSet linkSet)
			{
			if (topic.Prototype == null)
				{  return;  }

			Language language = EngineInstance.Languages.FromID(topic.LanguageID);

			// We do this even for topics in the class hierarchy because the HTML output falls back to regular prototypes
			// if there's no class prototype.  Also, if there's parameter lists in the description the HTML generator will require
			// type links to exist regardless of what type of prototype it creates.  For example, this SystemVerilog interface:
			//
			//    // Interface: myInterface
			//    //
			//    // Parameters:
			//    //    PARAMNAME - description
			//
			//    interface myInterface #(parameter PARAMNAME = 8) (input reset, clk);
			//
			// The HTML generation for the Parameters section will expect a type link to exist for PARAMNAME.

			TokenIterator symbolStart = topic.ParsedPrototype.Tokenizer.FirstToken;
			TokenIterator symbolEnd;

			while (symbolStart.IsInBounds)
				{
				if (symbolStart.PrototypeParsingType == PrototypeParsingType.Type ||
					 symbolStart.PrototypeParsingType == PrototypeParsingType.TypeQualifier)
					{
					symbolEnd = symbolStart;

					do
						{  symbolEnd.Next();  }
					while (symbolEnd.PrototypeParsingType == PrototypeParsingType.Type ||
							 symbolEnd.PrototypeParsingType == PrototypeParsingType.TypeQualifier);

					if (language.Parser.IsBuiltInType(symbolStart, symbolEnd) == false)
						{
						Link link = new Link();

						// ignore LinkID
						link.Type = LinkType.Type;
						link.Symbol = SymbolString.FromPlainText_NoParameters( symbolStart.TextBetween(symbolEnd) );
						link.Context = topic.PrototypeContext;
						// ignore contextID
						link.FileID = topic.FileID;
						link.ClassString = topic.ClassString;
						// ignore classID
						link.LanguageID = topic.LanguageID;
						// ignore EndingSymbol
						// ignore TargetTopicID
						// ignore TargetScore

						linkSet.Add(link);
						}

					symbolStart = symbolEnd;
					}

				else
					{  symbolStart.Next();  }
				}
			}



		// Group: Properties
		// __________________________________________________________________________


		public Files.Manager Manager
			{
			get
				{  return EngineInstance.Files;  }
			}



		// Group: Variables
		// __________________________________________________________________________


		/* var: filesBeingProcessed
		 *
		 * The files currently being worked on.  It also stores snapshots of each file's properties as of the time
		 * processing began, so when finishing processing they may be compared to the current state to detect
		 * if they changed.
		 *
		 * Thread Safety:
		 *
		 *		You must hold <accessLock> in order to use this variable.
		 */
		protected FilesBeingProcessed filesBeingProcessed;


		/* var: inaccessibleFiles
		 * A set of all the file IDs we attempted to process but which returned <Parser.ParseResult.CantAccessFile>.
		 */
		protected IDObjects.NumberSet inaccessibleFiles;


		/* var: accessLock
		 * An object used for a monitor that prevents more than one thread from accessing any of the variables
		 * at a time.
		 */
		protected object accessLock;

		}
	}
