/* 
 * Class: CodeClear.NaturalDocs.Engine.Files.Processor
 * ____________________________________________________________________________
 * 
 * A module which handles processing changes to the files Natural Docs scans.  In addition to source files, this includes 
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

// This file is part of Natural Docs, which is Copyright © 2003-2017 Code Clear LLC.
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
	public class Processor : Module, IChangeWatcher
		{
		
		// Group: Types
		// __________________________________________________________________________
		
		
		/* enum: ReleaseClaimedFileReason
		 * 
		 * The reason for calling <ReleaseClaimedFile()>.
		 * 
		 * SuccessfullyProcessed - The file was successfully processed.  Take it off the changed/deleted list.
		 * CancelledProcessing - The file's processing was cancelled.  Release the claim but leave it on the changed/deleted
		 *								  list for later.
		 *	FileDoesntExist - The file couldn't be opened because it doesn't exist.  This obviously only applies to changed files,
		 *							as this is expected with deleted files.
		 *	CantAccessFile - The file exists but couldn't be opened, such as if the program doesn't have permission to access
		 *							the file.
		 */
		public enum ReleaseClaimedFileReason : byte
			{
			SuccessfullyProcessed, CancelledProcessing, FileDoesntExist, CantAccessFile
			}
			

		
		// Group: Initialization and Configuration Functions
		// __________________________________________________________________________
		

		/* Function: Processor
		 */
		public Processor (Engine.Instance engineInstance) : base (engineInstance)
			{
			unprocessedChangedFileIDs = new IDObjects.NumberSet();
			unprocessedDeletedFileIDs = new IDObjects.NumberSet();
			claimedFileIDs = new IDObjects.NumberSet();
			
			accessLock = new object();
			}
			
		override protected void Dispose (bool strictRulesApply)
			{
			if (!strictRulesApply)
				{
				// Set the last modification time to zero for anything still being worked on
				DateTime zero = new DateTime(0);

				foreach (int id in unprocessedChangedFileIDs)
					{  Manager.FromID(id).LastModified = zero;  }
				foreach (int id in unprocessedDeletedFileIDs)
					{  Manager.FromID(id).LastModified = zero;  }
				foreach (int id in claimedFileIDs)
					{  Manager.FromID(id).LastModified = zero;  }
				}
			}

		public bool Start (Errors.ErrorList errors)
			{
			Manager.AddChangeWatcher(this);
			return true;
			}
			
			

		// Group: Change Watcher Functions
		// __________________________________________________________________________
		
			
		public void OnAddFile (File file)
			{
			lock (accessLock)
				{
				if (file.Claimed == false)
					{
					unprocessedDeletedFileIDs.Remove(file.ID);
					unprocessedChangedFileIDs.Add(file.ID);
					}
				}
			}
			
			
		public void OnFileChanged (File file)
			{
			lock (accessLock)
				{
				if (file.Claimed == false)
					{
					unprocessedDeletedFileIDs.Remove(file.ID);
					unprocessedChangedFileIDs.Add(file.ID);
					}
				}
			}
			
			
		public void OnDeleteFile (File file)
			{
			lock (accessLock)
				{
				if (file.Claimed == false)
					{
					unprocessedChangedFileIDs.Remove(file.ID);
					unprocessedDeletedFileIDs.Add(file.ID);
					}
				}
			}
			

			
		// Group: Group File Processing Functions
		// __________________________________________________________________________
		
		
		/* Function: WorkOnProcessingChanges
		 * 
		 * Works on the task of going through all the file changes and deletions and calling <ProcessChangedFile()> and 
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
				bool deletedFiles, changedFiles;

				do
					{
					deletedFiles = false;
					changedFiles = false;
			
					for (;;)
						{
						File file = ClaimDeletedFile();

						if (file == null)
							{  break;  }
						
						deletedFiles = true;
						var result = ProcessDeletedFile(file, codeDBAccessor, cancelDelegate);
						ReleaseClaimedFile(file, result);
						}
					
					for (;;)
						{
						File file = ClaimChangedFile();
					
						if (file == null)
							{  break;  }

						changedFiles = true;
						var result = ProcessChangedFile(file, codeDBAccessor, cancelDelegate);
						ReleaseClaimedFile(file, result);
						}

					}
				// It's possible more deleted files appeared while processing changes so we have to make it through an iteration where 
				// both claim functions come up empty.
				while (deletedFiles == true || changedFiles == true);

				}
			}
		 
		
		/* Function: GetProcessChangesStatus
		 * Fills the passed object with the status of <WorkOnProcessingChanges()>.  This will be a snapshot of its
		 * progress rather than a live object, so the values won't change out from under you.
		 */
		public void GetProcessChangesStatus (ref ProcessChangesStatus statusTarget)
			{
			statusTarget.Reset();
			
			lock (accessLock)
				{
				statusTarget.FilesBeingProcessed = claimedFileIDs.Count;
				statusTarget.ChangedFilesRemaining = unprocessedChangedFileIDs.Count;
				statusTarget.DeletedFilesRemaining = unprocessedDeletedFileIDs.Count;
				}
			}
			
		

		// Group: Individual File Processing Functions
		// __________________________________________________________________________
		
			
		/* Function: ClaimChangedFile
		 * Claims a changed file to work on, if there are any.  If not it will return null.  Claimed files must be released with
		 * <ReleaseClaimedFile()>.
		 */
		public File ClaimChangedFile ()
			{
			lock (accessLock)
				{
				if (unprocessedChangedFileIDs.IsEmpty)
					{  return null;  }
					
				int fileID = unprocessedChangedFileIDs.Highest;

				File file = Manager.FromID(fileID);
				file.Claimed = true;
				file.StatusSinceClaimed = FileFlags.UnchangedSinceClaimed;
				
				unprocessedChangedFileIDs.Remove(fileID);
				claimedFileIDs.Add(fileID);
				
				return file;
				}
			}
			
			
		/* Function: ClaimDeletedFile
		 * Claims a deleted file to work on, if there are any.  Will return null if not.  Claimed files must be released with
		 * <ReleaseClaimedFile()>.
		 */
		public File ClaimDeletedFile ()
			{
			lock (accessLock)
				{
				if (unprocessedDeletedFileIDs.IsEmpty)
					{  return null;  }
					
				int fileID = unprocessedDeletedFileIDs.Highest;

				File file = Manager.FromID(fileID);
				file.Claimed = true;
				file.StatusSinceClaimed = FileFlags.UnchangedSinceClaimed;
				
				unprocessedDeletedFileIDs.Remove(fileID);
				claimedFileIDs.Add(fileID);
				
				return file;
				}
			}
			
			
		/* Function: ProcessChangedFile
		 * Takes a changed <File>, parses it, and updates <CodeDB.Manager> with its contents.  It returns the result code that 
		 * should be passed to <ReleaseClaimedFile()> if the file was retrieved with <ClaimChangedFile()>.  The <CodeDB.Accessor> 
		 * should NOT already hold a lock.
		 */
		public ReleaseClaimedFileReason ProcessChangedFile (File file, Engine.CodeDB.Accessor codeDBAccessor, 
																												CancelDelegate cancelDelegate)
			{

			// Process source files
			
			if (file.Type == FileType.Source)
				{
				try
					{
					var language = EngineInstance.Languages.FromFileName(file.FileName);
					IList<Topic> topics = null;
					LinkSet links = null;
					ImageLinkSet imageLinks = null;

					var parseResult = language.Parse(file.FileName, file.ID, cancelDelegate, out topics, out links);

					if (parseResult == Language.ParseResult.FileDoesntExist)
						{  
						return ReleaseClaimedFileReason.FileDoesntExist;  
						}
					else if (parseResult == Language.ParseResult.CantAccessFile)
						{  
						return ReleaseClaimedFileReason.CantAccessFile;  
						}
					else if (parseResult == Language.ParseResult.Cancelled)
						{
						return ReleaseClaimedFileReason.CancelledProcessing;
						}


					// Parse the topic bodies for Natural Docs links and image links.  Parse the prototypes for type links.

					if (topics != null && topics.Count > 0)
						{
						imageLinks = new ImageLinkSet();

						foreach (Topic topic in topics)
							{
							if (topic.Body != null)
								{
								ExtractBodyLinks(topic, links, imageLinks);

								if (cancelDelegate())
									{  return ReleaseClaimedFileReason.CancelledProcessing;  }
								}
						
							// We want to extract type links even for prototypes in the class hierarchy because the HTML output falls back to
							// regular prototypes if there's no class prototype.  Also, if there's parameter lists in the description the HTML
							// generator will require type links to exist regardless of what type of prototype it creates.  For example, this 
							// SystemVerilog interface:
							//
							//    // Interface: myInterface
							//    //
							//    // Parameters:
							//    //    PARAMNAME - description
							//
							//    interface myInterface #(parameter PARAMNAME = 8) (input reset, clk);
							//
							// The HTML generation for the Parameters section will expect a type link to exist for PARAMNAME.
							//
							if (topic.Prototype != null
							)
								{
								ExtractTypeLinks(topic, links);

								if (cancelDelegate())
									{  return ReleaseClaimedFileReason.CancelledProcessing;  }
								}
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
						}
					finally
						{  codeDBAccessor.ReleaseLock();  }
					
					// Need this final check in case CodeDB quit with a cancellation.
					if (cancelDelegate())
						{  return ReleaseClaimedFileReason.CancelledProcessing;  }
					
					return ReleaseClaimedFileReason.SuccessfullyProcessed;

					}
				catch (Exception e)
					{
					try
						{  e.AddNaturalDocsTask("Parsing File: " + file.FileName);  }
					catch
						{  }

					throw;
					}
				}


			// Process style and image files

			else
				{
				// These are only processed by output builders.  They're not in CodeDB so we don't need to do anything here.
				return ReleaseClaimedFileReason.SuccessfullyProcessed;
				}
			}
			
			
		/* Function: ProcessDeletedFile
		 * Takes a deleted <File> retrieved using <ClaimDeletedFile()> and updates <CodeDB.Manager>.  Returns the result code that
		 * should be passed to <ReleaseClaimedFile()> if it was retrieved by <ClaimDeletedFile()>.  The <CodeDB.Accessor> should NOT
		 * already hold a lock.
		 */
		public ReleaseClaimedFileReason ProcessDeletedFile (File file, CodeDB.Accessor codeDBAccessor, CancelDelegate cancelDelegate)
			{

			// Source files
			
			if (file.Type == FileType.Source)
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
					{  return ReleaseClaimedFileReason.CancelledProcessing;  }
				else
					{  return ReleaseClaimedFileReason.SuccessfullyProcessed;  }
				}
				
				
			// Style and image files

			else
				{
				// These are only processed by output builders.  They're not in CodeDB so we don't need to do anything here.
				return ReleaseClaimedFileReason.SuccessfullyProcessed;
				}
			}
		 
			
		/* Function: ReleaseClaimedFile
		 * 
		 * Releases a previously claimed file.  You must provide a reason via <ReleaseClaimedFileReason>.
		 * 
		 * If you release a file you claimed with <ClaimDeletedFile()> and set the reason to
		 * <ReleaseClaimedFileReason.SuccessfullyProcessed>, the file object and ID will still exist until the next <Cleanup()> so it's 
		 * safe to rely on them.
		 */
		public void ReleaseClaimedFile (File file, ReleaseClaimedFileReason releaseReason)
			{
			lock (accessLock)
				{
				file.Claimed = false;
				claimedFileIDs.Remove(file.ID);
				
				// This logic is a little tricky, so we'll chart out all the possibilities for the two claim reasons (changed/deleted)
				// the four release reasons (success/cancel/can't access/doesn't exist) and the three statuses since the claim
				// (unchanged/changed/deleted) to see that this covers all the bases correctly.

				// We don't have to worry about notifying change watchers here.  They were notified by Files.Manager at the 
				// time of the event while the file was claimed.  It doesn't delay those notifications until the file is released.
				

				// First throw an exception when Doesn't Exist and Can't Access are used when processing a deleted file.  They don't
				// make sense.

				// ☐ File changed - Successfully processed - Unchanged since claim
				// ☐ File changed - Successfully processed - Changed since claim
				// ☐ File changed - Successfully processed - Deleted since claim
				// ☐ File changed - Cancelled - Unchanged since claim
				// ☐ File changed - Cancelled - Changed since claim
				// ☐ File changed - Cancelled - Deleted since claim
				// ☐ File changed - Can't access - Unchanged since claim
				// ☐ File changed - Can't access - Changed since claim
				// ☐ File changed - Can't access - Deleted since claim
				// ☐ File changed - Doesn't exist - Unchanged since claim
				// ☐ File changed - Doesn't exist - Changed since claim
				// ☐ File changed - Doesn't exist - Deleted since claim
				// ☐ File deleted - Successfully processed - Unchanged since claim
				// ☐ File deleted - Successfully processed - Changed since claim
				// ☐ File deleted - Successfully processed - Deleted since claim
				// ☐ File deleted - Cancelled - Unchanged since claim
				// ☐ File deleted - Cancelled - Changed since claim
				// ☐ File deleted - Cancelled - Deleted since claim
				// ➤ File deleted - Can't access - Unchanged since claim
				// ➤ File deleted - Can't access - Changed since claim
				// ➤ File deleted - Can't access - Deleted since claim
				// ➤ File deleted - Doesn't exist - Unchanged since claim
				// ➤ File deleted - Doesn't exist - Changed since claim
				// ➤ File deleted - Doesn't exist - Deleted since claim

				if (file.Status == FileFlags.Deleted &&
					( releaseReason == ReleaseClaimedFileReason.CantAccessFile || 
					  releaseReason == ReleaseClaimedFileReason.FileDoesntExist ) )
					{
					throw new System.ArgumentException();
					}
				

				// Next handle successfully processing a change.
				
				// ➤ File changed - Successfully processed - Unchanged since claim
				// ☐ File changed - Successfully processed - Changed since claim
				// ☐ File changed - Successfully processed - Deleted since claim
				// ☐ File changed - Cancelled - Unchanged since claim
				// ☐ File changed - Cancelled - Changed since claim
				// ☐ File changed - Cancelled - Deleted since claim
				// ☐ File changed - Can't access - Unchanged since claim
				// ☐ File changed - Can't access - Changed since claim
				// ☐ File changed - Can't access - Deleted since claim
				// ☐ File changed - Doesn't exist - Unchanged since claim
				// ☐ File changed - Doesn't exist - Changed since claim
				// ☐ File changed - Doesn't exist - Deleted since claim
				// ☐ File deleted - Successfully processed - Unchanged since claim
				// ☐ File deleted - Successfully processed - Changed since claim
				// ☐ File deleted - Successfully processed - Deleted since claim
				// ☐ File deleted - Cancelled - Unchanged since claim
				// ☐ File deleted - Cancelled - Changed since claim
				// ☐ File deleted - Cancelled - Deleted since claim
				// ☒ File deleted - Can't access - Unchanged since claim
				// ☒ File deleted - Can't access - Changed since claim
				// ☒ File deleted - Can't access - Deleted since claim
				// ☒ File deleted - Doesn't exist - Unchanged since claim
				// ☒ File deleted - Doesn't exist - Changed since claim
				// ☒ File deleted - Doesn't exist - Deleted since claim

				else if (file.Status == FileFlags.NewOrChanged && 
					releaseReason == ReleaseClaimedFileReason.SuccessfullyProcessed &&
					file.StatusSinceClaimed == FileFlags.UnchangedSinceClaimed)
					{
					file.Status = FileFlags.Unchanged;
					}


				// Next handle successfully processing a deletion
				
				// ☒ File changed - Successfully processed - Unchanged since claim
				// ☐ File changed - Successfully processed - Changed since claim
				// ☐ File changed - Successfully processed - Deleted since claim
				// ☐ File changed - Cancelled - Unchanged since claim
				// ☐ File changed - Cancelled - Changed since claim
				// ☐ File changed - Cancelled - Deleted since claim
				// ☐ File changed - Can't access - Unchanged since claim
				// ☐ File changed - Can't access - Changed since claim
				// ☐ File changed - Can't access - Deleted since claim
				// ☐ File changed - Doesn't exist - Unchanged since claim
				// ☐ File changed - Doesn't exist - Changed since claim
				// ☐ File changed - Doesn't exist - Deleted since claim
				// ➤ File deleted - Successfully processed - Unchanged since claim
				// ☐ File deleted - Successfully processed - Changed since claim
				// ➤ File deleted - Successfully processed - Deleted since claim
				// ☐ File deleted - Cancelled - Unchanged since claim
				// ☐ File deleted - Cancelled - Changed since claim
				// ☐ File deleted - Cancelled - Deleted since claim
				// ☒ File deleted - Can't access - Unchanged since claim
				// ☒ File deleted - Can't access - Changed since claim
				// ☒ File deleted - Can't access - Deleted since claim
				// ☒ File deleted - Doesn't exist - Unchanged since claim
				// ☒ File deleted - Doesn't exist - Changed since claim
				// ☒ File deleted - Doesn't exist - Deleted since claim

				else if (file.Status == FileFlags.Deleted &&
						  releaseReason == ReleaseClaimedFileReason.SuccessfullyProcessed &&
						  ( file.StatusSinceClaimed == FileFlags.UnchangedSinceClaimed ||
						    file.StatusSinceClaimed == FileFlags.DeletedSinceClaimed ) )
					{
					// We leave the object in the files list and leave it's status as Deleted.  It will be removed later by Cleanup().
					// We cannot remove it now because builders may still rely on the ID.
					}
					

				// If it's been changed since the file was claimed, put it back on the changed list regardless of anything else.  If
				// a deleted file was changed, it means its been recreated.  If a successfully processed change was followed by
				// another, it needs to be processed again.  If a file couldn't be opened or didn't exist but was changed afterwards,
				// give it another try.
				
				// ☒ File changed - Successfully processed - Unchanged since claim
				// ➤ File changed - Successfully processed - Changed since claim
				// ☐ File changed - Successfully processed - Deleted since claim
				// ☐ File changed - Cancelled - Unchanged since claim
				// ➤ File changed - Cancelled - Changed since claim
				// ☐ File changed - Cancelled - Deleted since claim
				// ☐ File changed - Can't access - Unchanged since claim
				// ➤ File changed - Can't access - Changed since claim
				// ☐ File changed - Can't access - Deleted since claim
				// ☐ File changed - Doesn't exist - Unchanged since claim
				// ➤ File changed - Doesn't exist - Changed since claim
				// ☐ File changed - Doesn't exist - Deleted since claim
				// ☒ File deleted - Successfully processed - Unchanged since claim
				// ➤ File deleted - Successfully processed - Changed since claim
				// ☒ File deleted - Successfully processed - Deleted since claim
				// ☐ File deleted - Cancelled - Unchanged since claim
				// ➤ File deleted - Cancelled - Changed since claim
				// ☐ File deleted - Cancelled - Deleted since claim
				// ☒ File deleted - Can't access - Unchanged since claim
				// ☒ File deleted - Can't access - Changed since claim
				// ☒ File deleted - Can't access - Deleted since claim
				// ☒ File deleted - Doesn't exist - Unchanged since claim
				// ☒ File deleted - Doesn't exist - Changed since claim
				// ☒ File deleted - Doesn't exist - Deleted since claim

				else if (file.StatusSinceClaimed == FileFlags.NewOrChangedSinceClaimed)
					{
					file.Status = FileFlags.NewOrChanged;
					unprocessedChangedFileIDs.Add(file.ID);
					}


				// If a change was cancelled, we put it back on the list so it gets handled again.

				// ☒ File changed - Successfully processed - Unchanged since claim
				// ☒ File changed - Successfully processed - Changed since claim
				// ☐ File changed - Successfully processed - Deleted since claim
				// ➤ File changed - Cancelled - Unchanged since claim
				// ☒ File changed - Cancelled - Changed since claim
				// ☐ File changed - Cancelled - Deleted since claim
				// ☐ File changed - Can't access - Unchanged since claim
				// ☒ File changed - Can't access - Changed since claim
				// ☐ File changed - Can't access - Deleted since claim
				// ☐ File changed - Doesn't exist - Unchanged since claim
				// ☒ File changed - Doesn't exist - Changed since claim
				// ☐ File changed - Doesn't exist - Deleted since claim
				// ☒ File deleted - Successfully processed - Unchanged since claim
				// ☒ File deleted - Successfully processed - Changed since claim
				// ☒ File deleted - Successfully processed - Deleted since claim
				// ☐ File deleted - Cancelled - Unchanged since claim
				// ☒ File deleted - Cancelled - Changed since claim
				// ☐ File deleted - Cancelled - Deleted since claim
				// ☒ File deleted - Can't access - Unchanged since claim
				// ☒ File deleted - Can't access - Changed since claim
				// ☒ File deleted - Can't access - Deleted since claim
				// ☒ File deleted - Doesn't exist - Unchanged since claim
				// ☒ File deleted - Doesn't exist - Changed since claim
				// ☒ File deleted - Doesn't exist - Deleted since claim

				else if (file.Status == FileFlags.NewOrChanged &&
						  releaseReason == ReleaseClaimedFileReason.CancelledProcessing &&
						  file.StatusSinceClaimed == FileFlags.UnchangedSinceClaimed)
					{
					unprocessedChangedFileIDs.Add(file.ID);
					}
					
					
				// Everything else, add it to the deleted list.  If it was opened for a change and has been deleted since, it's
				// deleted regardless of the processing result or release reason.  If it was opened for a change and the file
				// doesn't exist or can't be opened, treat it as deleted.  If it was opened for deletion but was cancelled put it
				// back on the list.
				
				// ☒ File changed - Successfully processed - Unchanged since claim
				// ☒ File changed - Successfully processed - Changed since claim
				// ➤ File changed - Successfully processed - Deleted since claim
				// ☒ File changed - Cancelled - Unchanged since claim
				// ☒ File changed - Cancelled - Changed since claim
				// ➤ File changed - Cancelled - Deleted since claim
				// ➤ File changed - Can't access - Unchanged since claim
				// ☒ File changed - Can't access - Changed since claim
				// ➤ File changed - Can't access - Deleted since claim
				// ➤ File changed - Doesn't exist - Unchanged since claim
				// ☒ File changed - Doesn't exist - Changed since claim
				// ➤ File changed - Doesn't exist - Deleted since claim
				// ☒ File deleted - Successfully processed - Unchanged since claim
				// ☒ File deleted - Successfully processed - Changed since claim
				// ☒ File deleted - Successfully processed - Deleted since claim
				// ➤ File deleted - Cancelled - Unchanged since claim
				// ☒ File deleted - Cancelled - Changed since claim
				// ➤ File deleted - Cancelled - Deleted since claim
				// ☒ File deleted - Can't access - Unchanged since claim
				// ☒ File deleted - Can't access - Changed since claim
				// ☒ File deleted - Can't access - Deleted since claim
				// ☒ File deleted - Doesn't exist - Unchanged since claim
				// ☒ File deleted - Doesn't exist - Changed since claim
				// ☒ File deleted - Doesn't exist - Deleted since claim

				else
					{
					file.Status = FileFlags.Deleted;
					unprocessedDeletedFileIDs.Add(file.ID);
					}
				}
			}



		// Group: Misc Functions
		// __________________________________________________________________________


		/* Function: ExtractBodyLinks
		 * Goes through the body of the passed <Topic> and adds any Natural Docs and image links it finds in the <NDMarkup>
		 * to <LinkSet> and <ImageLinkSet>.
		 */
		protected void ExtractBodyLinks (Topic topic, LinkSet linkSet, ImageLinkSet imageLinkSet)
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

			
		/* Function: ExtractTypeLinks
		 * Goes through the prototype of the passed <Topic> and adds any type links it finds to <LinkSet>.
		 */
		protected void ExtractTypeLinks (Topic topic, LinkSet linkSet)
			{
			if (topic.Prototype == null)
				{  return;  }

			Language language = EngineInstance.Languages.FromID(topic.LanguageID);

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

					if (language.IsBuiltInType(symbolStart, symbolEnd) == false)
						{
						Link link = new Link();

						// ignore LinkID
						link.Type = LinkType.Type;
						link.Symbol = SymbolString.FromPlainText_NoParameters( symbolStart.Tokenizer.TextBetween(symbolStart, symbolEnd) );
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

		

		// Group: File ID Variables
		// __________________________________________________________________________
		

		/* var: unprocessedChangedFileIDs
		 * 
		 * A <IDObjects.NumberSet> of the file IDs which have changed since the last run.  Will not include IDs that are in
		 * <claimedFileIDs>.
		 * 
		 * Thread Safety:
		 * 
		 *		You must hold <accessLock> in order to use this variable.
		 */
		protected IDObjects.NumberSet unprocessedChangedFileIDs;
		

		/* var: unprocessedDeletedFileIDs
		 * 
		 * A <IDObjects.NumberSet> of the file IDs which have been deleted since the last run.  Will not include IDs that are
		 * in <claimedFileIDs>.
		 * 
		 * Thread Safety:
		 * 
		 *		You must hold <accessLock> in order to use this variable.
		 */
		protected IDObjects.NumberSet unprocessedDeletedFileIDs;
		

		/* var: claimedFileIDs
		 * 
		 * A <IDObjects.NumberSet> of the file IDs which are currently claimed.  Any ID in here will not be in
		 * <unprocessedChangedFileIDs> or <unprocessedDeletedFileIDs>.
		 * 
		 * Thread Safety:
		 * 
		 *		You must hold <accessLock> in order to use this variable.
		 */
		protected IDObjects.NumberSet claimedFileIDs;
				
				
				
		// Group: Other Variables
		// __________________________________________________________________________
		
		
		/* var: accessLock
		 * An object used for a monitor that prevents more than one thread from accessing any of the variables
		 * at a time.
		 */
		protected object accessLock;

		}
	}