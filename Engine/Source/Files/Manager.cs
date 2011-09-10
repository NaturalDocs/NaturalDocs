/* 
 * Class: GregValure.NaturalDocs.Engine.Files.Manager
 * ____________________________________________________________________________
 * 
 * A module which handles all the files Natural Docs scans.  In addition to source files, this includes image files that can be
 * referenced with "(see image.jpg)" and extras tied to CSS styles.
 * 
 * 
 * Topic: Usage
 * 
 *		- Add file sources with <AddFileSource()>.  You can use <FileSources.Folder> or derive classes from <FileSource>.
 *		  <FileSource.Number> can be set or left at the default which will cause it to autogenerate.
 *		  
 *		- If desired, add filters with <AddFilter()>.
 * 
 *		- If desired, add event handlers to <FileChangesEvent> or sleep on <WhenThereAreFileChanges>.
 *		  
 *		- Call <Engine.Instance.Start()> which will start this module.
 *		
 *		- At this point the class is usable, but the file information is as of the last run.
 *		
 *		- Use update functions like <WorkOnAddingAllFiles()> to make changes.  After that completes, detect deleted files by 
 *		  calling <DeleteFilesNotInFileSources()>.
 *		  
 *		- Use processing functions like <WorkOnProcessingChanges()> to process the changes.
 *		  
 * 
 * Multithreading: Thread Safety Notes
 * 
 *		This class is thread safe.  All locking is handled internally unless you call functions like <LockForBatchFileUpdates()> 
 *		and <LockForFileEnumeration()> to explicitly leave the class in a locked state between function calls.
 *		
 * 
 * File: Files.nd
 * 
 *		A binary file which stores the state of the source files as of Natural Docs' last run.
 *		
 *		> [[Binary Header]]
 *		
 *		The file starts with the standard binary file header as managed by <BinaryFile>.
 *		
 *		> [Int32: ID]
 *		> [String: Path]
 *		> [Byte: Type]
 *		> [Int64: Last Modification in Ticks or 0]
 *		> ...
 *		> [Int32: 0]
 *		
 *		For each file it stores the ID number, the absolute path, <FileType>, and the last modification time in ticks.  If the file 
 *		wasn't fully processed when Natural Docs shut down, either due to a change or a deletion, the tick count will be zero to 
 *		indicate that it should be processed again.
 *		
 *		This continues until there is an ID number of zero.
 *			
 *		Revisions:
 *		
 *			2.0:
 *				
 *				- The file was introduced.
 *			
 */

// This file is part of Natural Docs, which is Copyright © 2003-2011 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using System.Threading;
using GregValure.NaturalDocs.Engine.Collections;


namespace GregValure.NaturalDocs.Engine.Files
	{
	public class Manager : IDisposable
		{
		
		// Group: Types
		// __________________________________________________________________________
		#region Types
		
		
		/* enum: ReleaseClaimedFileReason
		 * The reason for calling <ReleaseClaimedFile()>.
		 * 
		 * SuccessfullyProcessed - The file was successfully processed.  Take it off the changed/deleted list.
		 * CancelledProcessing - The file's processing was cancelled.  Release the claim but leave it on the changed/deleted
		 *								  list for later.
		 *	FileDoesntExist - The file couldn't be opened because it doesn't exist.  This obviously only applies to changed files,
		 *							as this is expected with deleted files.
		 *	CantAccessFile - The file exists but couldn't be open, such as if the program doesn't have permission to access
		 *							the file.
		 */
		public enum ReleaseClaimedFileReason : byte
			{
			SuccessfullyProcessed, CancelledProcessing, FileDoesntExist, CantAccessFile
			}
		
		#endregion 
			
		
		// Group: Initialization and Configuration Functions
		// __________________________________________________________________________
		#region Initialization and Configuration Functions
		
		/* Function: Manager
		 */
		public Manager ()
			{
			fileSources = new List<FileSource>();
			filters = new List<Filter>();
			claimedFolderPrefixes = new StringSet(true, false);			

			files = new IDObjects.Manager<File>( Config.Manager.IgnoreCaseInPaths, false );
			unprocessedChangedFileIDs = new IDObjects.NumberSet();
			unprocessedDeletedFileIDs = new IDObjects.NumberSet();
			claimedFileIDs = new IDObjects.NumberSet();
			
			writeLock = new object();
			batchingFileUpdates = 0;
			batchHasChanges = false;
			styleChangeWatchers = new List<IStyleChangeWatcher>();
			
			WhenThereAreFileChanges = new System.Threading.ManualResetEvent(false);
			}
			
			
		/* Function: AddFileSource
		 * Adds a file source to the list.
		 */
		public void AddFileSource (FileSource source)
			{
			fileSources.Add(source);
			}
			
		/* Function: AddFilter
		 * Adds a filter that folders will be checked against.
		 */
		public void AddFilter (Filter filter)
			{
			filters.Add(filter);
			}

		/* Function: AddStyleChangeWatcher
		 * Adds an object that wants to be notified whenever a style file changes.
		 */
		public void AddStyleChangeWatcher (IStyleChangeWatcher watcher)
			{
			styleChangeWatchers.Add(watcher);
			}
					
			
		/* Function: Start
		 * 
		 * Validates the configuration and starts the module if successful.  This can only be called once.  If it fails, scrap the
		 * entire <Engine.Instance> and start again.
		 * 
		 * Dependencies:
		 * 
		 *		- <Config.Manager> and <Languages.Manager> must be started before using the rest of the class.  Also 
		 *		  anything that could possibly set <Config.Manager.RebuildEverything>.
		 */
		public bool Start (Errors.ErrorList errors)
			{
			int startingErrorCount = errors.Count;
			
			if (fileSources.Count == 0)
				{
				errors.Add(
					Locale.Get("NaturalDocs.Engine", "Error.NoFileSourcesDefined")
					);
				}
			else
				{
				foreach (FileSource fileSource in fileSources)
					{  fileSource.Validate(errors);  }
				}
				
			foreach (FileSource fileSource in fileSources)
				{
				if (fileSource is FileSources.Folder)
					{
					FileSources.Folder folderFileSource = (FileSources.Folder)fileSource;
					
					if (folderFileSource.Type == InputType.Source &&
						SourceFolderIsIgnored(folderFileSource.Path))
						{
						errors.Add(
							Locale.Get("NaturalDocs.Engine", "Error.SourceFolderIsIgnored(sourceFolder)", folderFileSource.Path)
							);
						}
					}
				}
				
			if (Engine.Instance.Config.ReparseEverything == false)
				{
				if (LoadBinaryFile( Engine.Instance.Config.WorkingDataFolder + "/Files.nd", out files) == false)
					{  Engine.Instance.Config.ReparseEverything = true;  }
				}
		        			
			return (errors.Count == startingErrorCount);
			}
			
		#endregion
			
			
		// Group: Information Functions
		// __________________________________________________________________________
		#region Information Functions

			
		/* Function: FromID
		 * Returns the <File> associated with the passed file ID, or null if there isn't one.
		 */
		public File FromID (int fileID)
			{
			lock (writeLock)
				{
				return files[fileID];
				}
			}
			
		/* Function: FromPath
		 * Returns the <File> associated with the passed file <Path>, or null if there isn't one.  The <Path> must be
		 * absolute.
		 */
		public File FromPath (Path filePath)
			{
			if (filePath.IsRelative)
				{  throw new InvalidOperationException();  }

			lock (writeLock)
				{
				return files[filePath];
				}
			}
			
			
		/* Function: FileSourceOf
		 * Returns the <FileSource> which contains the passed <File>, or null if none.
		 */
		public FileSource FileSourceOf (File file)
			{
			foreach (FileSource fileSource in fileSources)
				{
				if (fileSource.Contains(file.FileName))
					{  return fileSource;  }
				}
				
			return null;
			}


		/* Function: SourceFolderIsIgnored
		 * Returns whether the passed source folder should be ignored based on any <Filters> defined.  This function is available 
		 * before <Start()> is called.
		 */
		public bool SourceFolderIsIgnored (Path path)
			{
			foreach (Filter filter in filters)
				{
				if (filter.IgnoreSourceFolder(path))
					{  return true;  }
				}
				
			return false;
			}


		/* Function: LockForFileEnumeration
		 * 
		 * Locks the class so your can retrieve the file manager for enumeration.  You must call <EndFileEnumeration()> afterwards.  
		 * 
		 * This function automatically calls <LockForBatchFileUpdates()> so that <FileChangesEvent> and <WhenThereAreFileChanges> 
		 * don't trigger while you have the lock.  Since they nest, it's okay if you were already batching files.
		 */
		public IDObjects.Manager<File> LockForFileEnumeration ()
			{
			Monitor.Enter(writeLock);
			LockForBatchFileUpdates();
			return files;
			}
			
			
		/* Function: EndFileEnumeration
		 * Releases the lock acquired by <LockForFileEnumeration()>.  The object it had returned can no longer be accessed 
		 * in a thread safe manner.  It also calls <EndBatchFileUpdates()> to trigger any events that happened while you held
		 * this lock, provided you weren't already batching them yourself.
		 */
		public void EndFileEnumeration ()
			{
			EndBatchFileUpdates();
			Monitor.Exit(writeLock);
			}

		#endregion


		// Group: Group File Management Functions
		// __________________________________________________________________________
		#region Group File Management Functions
		
		/* Function: WorkOnAddingAllFiles
		 * 
		 * Works on the task of going through all the files in all the <FileSources> and calling <AddOrUpdateFile()> on each one.
		 * This is a parallelizable task, so multiple threads can call this function and they will divide up the work until it's done.
		 * 
		 * The function returns when there is no more work for this thread to do.  If this is the only thread working on it then the
		 * task is complete, but if there are multiple threads, the task is only complete after they all return.  An individual thread
		 * may return prior to that point.
		 */
		public void WorkOnAddingAllFiles (CancelDelegate cancelDelegate)
			{
			string claimedFolderPrefix = null;
			
			Monitor.Enter(writeLock);
			bool locked = true;
			
			try
				{
				for (;;)
					{
					FileSource claimedFileSource = null;
					
					if (cancelDelegate())
						{  return;  }
						
						
					// If we have a claimed folder prefix, try to find another file source with the same one.
					
					if (claimedFolderPrefix != null)
						{
						foreach (FileSource fileSource in fileSources)
							{
							if (fileSource is FileSources.Folder && 
								fileSource.Claimed == false &&
								fileSource.AllFilesAdded == false && 
								String.Compare(claimedFolderPrefix, (fileSource as FileSources.Folder).Path.Prefix, true) == 0)
								{
								claimedFileSource = fileSource;
								fileSource.Claimed = true;
								break;
								}
							}
							
						if (claimedFileSource == null)
							{
							claimedFolderPrefixes.Remove(claimedFolderPrefix);
							claimedFolderPrefix = null;
							}
						}
							
						
					// If that didn't work, either because we didn't have a claimed folder prefix or there were no more available,
					// claim the first untaken file source there is.  If it's a folder file source, claim its prefix as well, but ignore it if the
					// prefix was already claimed because it wouldn't benefit from parallelization.
					
					if (claimedFileSource == null)
						{
						foreach (FileSource fileSource in fileSources)
							{
							if (fileSource is FileSources.Folder)
								{
								if (fileSource.AllFilesAdded == false && 
									fileSource.Claimed == false &&
									claimedFolderPrefixes.Contains( (fileSource as FileSources.Folder).Path.Prefix ) == false)
									{
									claimedFileSource = fileSource;
									fileSource.Claimed = true;
									
									claimedFolderPrefix = (fileSource as FileSources.Folder).Path.Prefix;
									claimedFolderPrefixes.Add(claimedFolderPrefix);
									
									break;
									}
								}
								
							else if (fileSource.AllFilesAdded == false && fileSource.Claimed == false)
								{
								claimedFileSource = fileSource;
								fileSource.Claimed = true;
								
								break;
								}
							}
						}
						
						
					// If that didn't work either it means there are no available file sources left so we can quit.
					
					if (claimedFileSource == null)
						{  return;  }
						
						
					// Now release the lock while we work on it.
					
					Monitor.Exit(writeLock);
					locked = false;

					claimedFileSource.AddAllFiles(cancelDelegate);
					// If it failed because of the cancelDelegate AllFilesAdded will be false.
					
					Monitor.Enter(writeLock);
					locked = true;
					
					claimedFileSource.Claimed = false;
						
					}  // for
				} // try
				
			finally
				{
				if (locked)
					{  Monitor.Exit(writeLock);  }
				}
			}
			
			
		/* Function: GetAddAllFilesStatus
		 * Fills the passed object with the status of <WorkOnAddingAllFiles()>.  The object will be a snapshot of the values, not
		 * a live monitor, so the values will not change out from under you.
		 */
		public void GetAddAllFilesStatus (ref AddAllFilesStatus statusTarget)
			{
			statusTarget.Reset();
			
			for (int i = 0; i < fileSources.Count; i++)
				{
				fileSources[i].CombineAddAllFilesStatus(ref statusTarget);
				}
			}
			
			
		/* Function: DeleteFilesNotInFileSources
		 * 
		 * Calls <DeleteFile()> on any file that doesn't have <File.InFileSource> set.  <AddOrUpdateFile()> automatically sets this
		 * flag, so after <WorkOnAddingAllFiles()> successfully completes this will delete any files that existed on the last run but
		 * no longer exist.
		 * 
		 * While this function takes a <CancelDelegate>, it is not a WorkOn function because more than one thread cannot work
		 * on this task simultaneously.
		 */
		public void DeleteFilesNotInFileSources (CancelDelegate cancelDelegate)
			{
			LockForBatchFileUpdates();
			
			try
				{
				foreach (File file in files)
					{
					if (file.InFileSource == false)
						{  DeleteFile(file.FileName);  }
						
					if (cancelDelegate())
						{  return;  }
					}
				}
			finally
				{  EndBatchFileUpdates();  }
			}
			
			
		#endregion
		
		
			
		// Group: Individual File Management Functions
		// __________________________________________________________________________
		#region Individual File Management Functions
		
			
		/* Function: AddOrUpdateFile
		 * 
		 * Adds a file or updates its last modification time.  If the file was not previously known to the class, it will be treated as 
		 * new, whereas if it was known but has a different modification time it will be treated as changed.  Returns whether this
		 * call changed anything.  It is okay to call this multiple times on the same file.
		 * 
		 * If you did not call <LockForBatchFileUpdates()> beforehand this function will automatically get and release a lock and
		 * trigger <FileChangesEvent> and <WhenThereAreFileChanges>.  If <LockForBatchFileUpdates()> was called then they
		 * won't be triggered until <EndBatchFileUpdates()>.
		 * 
		 * This is assumed to be called for files that are in a file source so it automatically sets <File.InFileSource>.
		 */
		public bool AddOrUpdateFile (Path name, FileType type, DateTime lastModified, bool forceReparse = false)
			{
			bool changed = false;
			bool suppressEvent = false;
			
			Monitor.Enter(writeLock);
			
			try
				{
				File file = files[name];
				
				if (file == null)
					{
					file = new File(name, type, lastModified);
					file.Status = FileFlags.NewOrChanged;
					files.Add(file);
					
					unprocessedChangedFileIDs.Add(file.ID);
					changed = true;
					}
				else if (file.Type != type)
					{
					throw new Exception("Added an existing file but the types didn't match.");
					}
				else if (file.Claimed == true)
					{
					if (file.LastModified != lastModified || file.StatusSinceClaimed == FileFlags.DeletedSinceClaimed || forceReparse)
						{
						file.LastModified = lastModified;
						file.StatusSinceClaimed = FileFlags.NewOrChangedSinceClaimed;
		
						changed = true;

						// There's no point in firing an event here since it's claimed.  It will fire when the file is released.
						suppressEvent = true;
						}
					}
				else if (file.LastModified != lastModified || file.Status == FileFlags.Deleted || forceReparse)
					{
					file.LastModified = lastModified;
					file.Status = FileFlags.NewOrChanged;
					
					unprocessedDeletedFileIDs.Remove(file.ID);
					unprocessedChangedFileIDs.Add(file.ID);
					changed = true;
					}

				file.InFileSource = true;
				
				if (batchingFileUpdates > 0 && changed && !suppressEvent)
					{  
					suppressEvent = true;  
					batchHasChanges = true;
					}
					
				// This is okay to call while holding the lock because we need it to keep the state consistent and other threads can
				// just wait until it's released.
				if (changed && !suppressEvent)
					{  WhenThereAreFileChanges.Set();  }
				}
			finally
				{  Monitor.Exit(writeLock);  }
				
			// This is NOT okay to call while holding the lock because any event handlers will be called on this thread instead of
			// their own, and thus would be unwittingly holding the lock as well.
			if (changed && !suppressEvent)
				{  TriggerFileChangesEvent();  }
				
			return changed;
			}
			
			
		/* Function: DeleteFile
		 * 
		 * Notifies the class that the file has been deleted.  Returns whether this call changed anything.  It is okay to call this
		 * multiple times on the same file.
		 * 
		 * If you did not call <LockForBatchFileUpdates()> beforehand this function will automatically get and release a lock and
		 * trigger <FileChangesEvent> and <WhenThereAreFileChanges>.  If <LockForBatchFileUpdates()> was called then they 
		 * won't be triggered until <EndBatchFileUpdates()>.
		 */
		public bool DeleteFile (Path name)
			{
			bool changed = false;
			bool suppressEvent = false;
			
			Monitor.Enter(writeLock);
			
			try
				{
				File file = files[name];
				
				if (file == null)
					{
					// Nada
					}
				else if (file.Claimed == true)
					{
					if (file.StatusSinceClaimed != FileFlags.DeletedSinceClaimed)
						{
						file.StatusSinceClaimed = FileFlags.DeletedSinceClaimed;
						changed = true;
						}
					}
				else if (file.Status != FileFlags.Deleted)
					{
					file.Status = FileFlags.Deleted;
					
					unprocessedChangedFileIDs.Remove(file.ID);
					unprocessedDeletedFileIDs.Add(file.ID);
					changed = true;
					}
						
				if (batchingFileUpdates > 0 && changed)
					{
					suppressEvent = true;
					batchHasChanges = true;
					}
					
				// This is okay to call while holding the lock because we need it to keep the state consistent and other threads can
				// just wait until it's released.
				if (changed && !suppressEvent)
					{  WhenThereAreFileChanges.Set();  }
				}
			finally
				{  Monitor.Exit(writeLock);  }
				
			// This is NOT okay to call while holding the lock because any event handlers will be called on this thread instead of
			// their own, and thus would be unwittingly holding the lock as well.
			if (changed && !suppressEvent)
				{  TriggerFileChangesEvent();  }
				
			return changed;
			}
			
			
		/* Function: LockForBatchFileUpdates
		 * Notifies the class that you're going to send multiple file update calls as a batch.  This prevents the class from 
		 * relinquishing the lock between calls and supresses <FileChangesEvent> and <WhenThereAreFileChanges> until the 
		 * batch ends.  Batches nest so this can be called multiple times and the changes will only apply when they are all released.
		 */
		public void LockForBatchFileUpdates ()
			{
			Monitor.Enter(writeLock);

			if (batchingFileUpdates == 0)
				{  batchHasChanges = false;  }

			batchingFileUpdates++;
			}


		/* Function: EndBatchFileUpdates
		 * Notifies the class that you're done with your batch of file updates.  Relinquishes the overall lock and triggers
		 * <FileChangesEvent> and <WhenThereAreFileChanges> if there were any changes since the batch started and this isn't
		 * a nested batch. 
		 */
		 public void EndBatchFileUpdates()
			{
			batchingFileUpdates--;
			
			// Need this as a separate variable because we want to trigger the event after releasing the lock, but we can't
			// test these variables then.
			bool triggerEvent = (batchingFileUpdates == 0 && batchHasChanges == true);
			
			if (triggerEvent)
				{
				// This is okay to call while holding the lock because we need it to keep the state consistent and other threads can
				// just wait until it's released.
				WhenThereAreFileChanges.Set();
				
				batchHasChanges = false;
				}
			
			Monitor.Exit(writeLock);
			
			// This is NOT okay to call while holding the lock because any event handlers will be called on this thread instead of
			// their own, and thus would be unwittingly holding the lock as well.
			if (triggerEvent)
				{  TriggerFileChangesEvent();  }
			}
			
		#endregion			
			
			
		// Group: Group File Processing Functions
		// __________________________________________________________________________
		#region Group File Processing Functions
		
		
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
			using (Engine.CodeDB.Accessor codeDBAccessor = Engine.Instance.CodeDB.GetAccessor())
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
						ReleaseClaimedFileReason result = ProcessDeletedFile(file, codeDBAccessor, cancelDelegate);
						ReleaseClaimedFile(file, result);
						}
					
					for (;;)
						{
						File file = ClaimChangedFile();
					
						if (file == null)
							{  break;  }

						changedFiles = true;
						ReleaseClaimedFileReason result = ProcessChangedFile(file, codeDBAccessor, cancelDelegate);
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
			
			lock (writeLock)
				{
				statusTarget.TotalFiles = files.Count;
				statusTarget.FilesBeingProcessed = claimedFileIDs.Count;
				statusTarget.ChangedFilesRemaining = unprocessedChangedFileIDs.Count;
				statusTarget.DeletedFilesRemaining = unprocessedDeletedFileIDs.Count;
				}
			}
			
		
		#endregion
		

		// Group: Individual File Processing Functions
		// __________________________________________________________________________
		#region Individual File Processing Functions
		
			
		/* Function: ClaimChangedFile
		 * Claims a changed file to work on, if there are any.  If not it will return null.  Claimed files must be released with
		 * <ReleaseClaimedFile()>.
		 */
		public File ClaimChangedFile ()
			{
			lock (writeLock)
				{
				if (unprocessedChangedFileIDs.IsEmpty)
					{  return null;  }
					
				int fileID = unprocessedChangedFileIDs.Highest;

				File file = files[fileID];
				file.Claimed = true;
				file.StatusSinceClaimed = FileFlags.UnchangedSinceClaimed;
				
				unprocessedChangedFileIDs.Remove(fileID);
				claimedFileIDs.Add(fileID);
				
				if (unprocessedChangedFileIDs.IsEmpty && unprocessedDeletedFileIDs.IsEmpty)
					{  WhenThereAreFileChanges.Reset();  }
				
				return file;
				}
			}
			
			
		/* Function: ClaimDeletedFile
		 * Claims a deleted file to work on, if there are any.  Will return null if not.  Claimed files must be released with
		 * <ReleaseClaimedFile()>.
		 */
		public File ClaimDeletedFile ()
			{
			lock (writeLock)
				{
				if (unprocessedDeletedFileIDs.IsEmpty)
					{  return null;  }
					
				int fileID = unprocessedDeletedFileIDs.Highest;

				File file = files[fileID];
				file.Claimed = true;
				file.StatusSinceClaimed = FileFlags.UnchangedSinceClaimed;
				
				unprocessedDeletedFileIDs.Remove(fileID);
				claimedFileIDs.Add(fileID);
				
				if (unprocessedDeletedFileIDs.IsEmpty && unprocessedChangedFileIDs.IsEmpty)
					{  WhenThereAreFileChanges.Reset();  }
				
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

			// Source files
			
			if (file.Type == FileType.Source)
				{
				Engine.Languages.Language language = Engine.Instance.Languages.FromFileName(file.FileName);
				string content = null;
				
				try
					{  content = System.IO.File.ReadAllText(file.FileName);  }

				catch (System.IO.FileNotFoundException)
					{  return ReleaseClaimedFileReason.FileDoesntExist;  }
				catch (System.IO.DirectoryNotFoundException)
					{  return ReleaseClaimedFileReason.FileDoesntExist;  }
				catch
					{  return ReleaseClaimedFileReason.CantAccessFile;  }
					
				if (cancelDelegate())
					{  return ReleaseClaimedFileReason.CancelledProcessing;  }
				
				IList<Topic> topics = null;

				if (content != null)
					{  
					// It would be nice to share a single parser via WorkOnProcessingChanges() since 90% of the source files are going to
					// be from the same language.  They're reusable, it's just that every thread needs their own since they're not thread safe.
					// I don't see how to do it in a way that isn't really ugly so I'm keeping code cleanliness ahead of what might be a 
					// premature optimization anyway.
					language.GetParser().Parse(content, file.ID, cancelDelegate, out topics);  
					}
					
				if (cancelDelegate())
					{  return ReleaseClaimedFileReason.CancelledProcessing;  }
					
				codeDBAccessor.GetReadPossibleWriteLock();
					
				try
					{
					if (topics != null && topics.Count > 0)  
						{  
						codeDBAccessor.UpdateTopicsInFile(file.ID, topics, cancelDelegate);  
						}
					else
						{  
						codeDBAccessor.DeleteTopicsInFile(file.ID, cancelDelegate);  
						}
					}
				finally
					{  codeDBAccessor.ReleaseLock();  }
					
				// Need this final check in case CodeDB quit with a cancellation.
				if (cancelDelegate())
					{  return ReleaseClaimedFileReason.CancelledProcessing;  }
					
				return ReleaseClaimedFileReason.SuccessfullyProcessed;
				}


			// Style files

			else if (file.Type == FileType.Style)
				{
				ReleaseClaimedFileReason result = ReleaseClaimedFileReason.SuccessfullyProcessed;

				foreach (IStyleChangeWatcher watcher in styleChangeWatchers)
					{
					ReleaseClaimedFileReason watcherResult = watcher.OnAddOrChangeFile(file.Name);

					// There's really no perfect answer on what to return if the watchers return multiple values, other than if any
					// of them don't return success this function should not return success either.  So this ends up returning the
					// last non-success reason any of them returned.  We do want every one of the watchers to be called regardless
					// though.
					if (watcherResult != ReleaseClaimedFileReason.SuccessfullyProcessed)
						{  result = watcherResult;  }

					if (cancelDelegate())
						{  return ReleaseClaimedFileReason.CancelledProcessing;  }
					}

				return result;
				}


			// Image files
									
			else
				{
				// XXX
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
					{  codeDBAccessor.DeleteTopicsInFile(file.ID, cancelDelegate);  }
				finally
					{  codeDBAccessor.ReleaseLock();  }
				
				// Need this check in case CodeDB quit early because of the cancel delegate.
				if (cancelDelegate())
					{  return ReleaseClaimedFileReason.CancelledProcessing;  }
				else
					{  return ReleaseClaimedFileReason.SuccessfullyProcessed;  }
				}
				
				
			// Style files

			else if (file.Type == FileType.Style)
				{
				ReleaseClaimedFileReason result = ReleaseClaimedFileReason.SuccessfullyProcessed;

				foreach (IStyleChangeWatcher watcher in styleChangeWatchers)
					{
					ReleaseClaimedFileReason watcherResult = watcher.OnDeleteFile(file.Name);

					// There's really no perfect answer on what to return if the watchers return multiple values, other than if any
					// of them don't return success this function should not return success either.  So this ends up returning the
					// last non-success reason any of them returned.  We do want every one of the watchers to be called regardless
					// though.
					if (watcherResult != ReleaseClaimedFileReason.SuccessfullyProcessed)
						{  result = watcherResult;  }

					if (cancelDelegate())
						{  return ReleaseClaimedFileReason.CancelledProcessing;  }
					}

				return result;
				}


			// Image files
			
			else
				{
				// XXX
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
			bool triggerFileChanges = false;

			lock (writeLock)
				{
				file.Claimed = false;
				claimedFileIDs.Remove(file.ID);
				
				// This logic is a little tricky, so we'll chart out all the possibilities for the two claim reasons (changed/deleted)
				// the four release reasons (success/cancel/can't access/doesn't exist) and the three statuses since the claim
				// (unchanged/changed/deleted) to see that this covers all the bases correctly.
				

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
					triggerFileChanges = true;
					}


				// If a change was cancelled, we put it back on the list and fire an event so it gets handled again.

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
					triggerFileChanges = true;
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
					triggerFileChanges = true;
					}
					
				// This is okay to call while holding the lock because we need it to keep the state consistent and other threads can
				// just wait until it's released.
				if (triggerFileChanges)
					{  WhenThereAreFileChanges.Set();  }
				}
				
			// This is NOT okay to call while holding the lock because any event handlers will be called on this thread instead of
			// their own, and thus would be unwittingly holding the lock as well.
			if (triggerFileChanges)
				{  TriggerFileChangesEvent();  }
			}
			
		#endregion



		// Group: Misc Functions
		// __________________________________________________________________________

			
		/* Function: Cleanup
		 * Cleans up the module's internal data when everything is up to date.  This will remove any successfully processed 
		 * deleted files, after which they can no longer be found with <FromID()> and their IDs may be reassigned.  You can
		 * pass a <CancelDelegate> to interrupt the process if necessary.
		 */
		public void Cleanup (CancelDelegate cancelDelegate)
			{
			lock (writeLock)
				{
				IDObjects.NumberSet toDelete = new IDObjects.NumberSet();
				
				foreach (File file in files)
					{
					if (cancelDelegate())
						{  return;  }
						
					if (file.Status == FileFlags.Deleted)
						{  toDelete.Add(file.ID);  }
					}
					
				foreach (int id in toDelete)
					{
					if (cancelDelegate())
						{  return;  }
						
					files.Remove(id);
					}
				}
			}



		// Group: Static Functions
		// __________________________________________________________________________
		#region Static Functions
		
		
		/* Function: LoadBinaryFile
		 * Loads <Files.nd> and returns whether it was successful.  If it wasn't it will still return valid objects, they will just
		 * be empty.
		 */
		public static bool LoadBinaryFile (Path filename, out IDObjects.Manager<File> files)
			{
			files = new IDObjects.Manager<File>( Config.Manager.IgnoreCaseInPaths, false );

			BinaryFile binaryFile = new BinaryFile();
			bool result = true;
			
			try
				{
				if (binaryFile.OpenForReading(filename, "2.0") == false)
					{
					result = false;
					}
				else
					{
					// [Int32: ID]
					// [String: Path]
					// [Byte: Type]
					// [Int64: Last Modification in Ticks or 0]
					// ...
					// [Int32: 0]
					
					int id;
					Path path;
					FileType type;
					DateTime lastModification;
					File file;
					
					for (;;)
						{
						id = binaryFile.ReadInt32();
						
						if (id == 0)
							{  break;  }
							
						path = binaryFile.ReadString();
						type = (FileType)binaryFile.ReadByte();
						lastModification = new DateTime(binaryFile.ReadInt64());
						
						file = new File(path, type, lastModification);
						file.ID = id;
						file.InBinaryFile = true;
						
						files.Add(file);
						}
					}
				}
			catch
				{
				result = false;
				}
			finally
				{  
				binaryFile.Close();  
				}
				
			if (result == false)
				{  files.Clear();  }
				
			return result;
			}
			
			
		/* Function: SaveBinaryFile
		 * Saves the current state into <Files.nd>.  Throws an exception if unsuccessful.  All <Files> in the structure should have
		 * their last modification time set to tick count zero before calling this function.
		 */
		public static void SaveBinaryFile (Path filename, IDObjects.Manager<File> files)
			{
			BinaryFile binaryFile = new BinaryFile();
			binaryFile.OpenForWriting(filename);
			
			try
				{
				foreach (File file in files)
					{
					// [Int32: ID]
					// [String: Path]
					// [Byte: Type]
					// [Int64: Last Modification in Ticks or 0]
					
					binaryFile.WriteInt32(file.ID);
					binaryFile.WriteString(file.FileName);
					binaryFile.WriteByte((byte)file.Type);
					binaryFile.WriteInt64(file.LastModified.Ticks);
					}

				// [Int32: 0]
				binaryFile.WriteInt32(0);
				}
			finally
				{  binaryFile.Close();  }
			}
			
		#endregion
			
			
		// Group: Properties
		// __________________________________________________________________________
		
		
		/* Property: FileSources
		 * Retrieves a read-only list of the file sources this instance has.
		 */
		public IList<FileSource> FileSources
			{
			get
				{  return fileSources.AsReadOnly();  }
			}
			
			
			
		// Group: Events
		// __________________________________________________________________________
		// 
		// Note that these events may be thrown by worker threads.  It is recommended that the event handlers hand them
		// off to the main thread and return quickly rather than handling them themselves.
		
		
		/* Event: FileChangesEvent
		 * Triggered when a file has been added, changed, or deleted.  Multiple changes may be batched together into a single
		 * event.
		 */
		public event SimpleDelegate FileChangesEvent;
				
		
		
		
		// Group: Protected Functions
		// __________________________________________________________________________
		
		
		/* Function: TriggerFileChangesEvent
		 * Triggers the <FileChangesEvent> event.
		 */
		protected void TriggerFileChangesEvent ()
			{
			SimpleDelegate temp = FileChangesEvent;
			
			if (temp != null)
				{  temp();  }
			}
			
					
			
			
		// Group: Thread Synchronization Objects
		// __________________________________________________________________________
		
		
		/* var: WhenThereAreFileChanges
		 * A thread synchronization object that remains signaled when there are changes that can be claimed with
		 * <ClaimChangedFile()> or <ClaimDeletedFile()>.  This allows threads to sleep until they become available.  
		 * You should only use this object for sleeping.  The set and reset functions will be managed by this class.
		 */
		public System.Threading.ManualResetEvent WhenThereAreFileChanges;
		
		
		
		
		// Group: IDisposable Functions
		// __________________________________________________________________________
		
		
		/* Function: Dispose
		 */
		public void Dispose ()
			{
			foreach (FileSource fileSource in fileSources)
				{
				if (fileSource is IDisposable)
					{  ((IDisposable)fileSource).Dispose();  }
				}
					
			// Set the last modification time to zero for anything still being worked on
				
			DateTime zero = new DateTime(0);

			foreach (int id in unprocessedChangedFileIDs)
				{  files[id].LastModified = zero;  }
			foreach (int id in unprocessedDeletedFileIDs)
				{  files[id].LastModified = zero;  }
			foreach (int id in claimedFileIDs)
				{  files[id].LastModified = zero;  }
					
			SaveBinaryFile( Engine.Instance.Config.WorkingDataFolder + "/Files.nd", files );
			}
			
			

		// Group: File Source Variables
		// __________________________________________________________________________
		
		
		/* var: fileSources
		 * A list of all the file sources.
		 */
		protected List<FileSource> fileSources;
		
		/* var: filters
		 * A list of all the filters.
		 */
		protected List<Filter> filters;
		
		/* var: claimedFolderPrefixes
		 * A set of all the <Path.Prefixes> that are currently being searched by threads.  This is used to prevent multiple 
		 * threads from searching <Folder>-based <FileSources> with the same prefix at the same time, since they are most 
		 * likely on the same physical disk and would not benefit from the parallelism.
		 */
		protected StringSet claimedFolderPrefixes;
		
		
		
		// Group: File Variables
		// __________________________________________________________________________
		

		/* var: files
		 * All the files that are managed by this object.  You must have <writeLock> to use this variable.
		 */
		protected IDObjects.Manager<File> files;
		
		/* var: unprocessedChangedFileIDs
		 * A <IDObjects.NumberSet> of the file IDs which have changed since the last run.  Will not include IDs that are in
		 * <claimedFileIDs>.  You must have <writeLock> to use this variable.
		 */
		protected IDObjects.NumberSet unprocessedChangedFileIDs;
		
		/* var: unprocessedDeletedFileIDs
		 * A <IDObjects.NumberSet> of the file IDs which have been deleted since the last run.  Will not include IDs that are
		 * in <claimedFileIDs>.  You must have <writeLock> to use this variable.
		 */
		protected IDObjects.NumberSet unprocessedDeletedFileIDs;
		
		/* var: claimedFileIDs
		 * A <IDObjects.NumberSet> of the file IDs which are currently claimed.  Any ID in here will not be in
		 * <unprocessedChangedFileIDs> or <unprocessedDeletedFileIDs>.
		 */
		protected IDObjects.NumberSet claimedFileIDs;
				
				
				
		// Group: Other Variables
		// __________________________________________________________________________
		
		
		/* var: writeLock
		 * 
		 * An object used for a monitor lock that prevents more than one thread from changing any of the files 
		 * structures at a time.
		 */
		protected object writeLock;
		
		/* var: batchingFileUpdates
		 * Whether file updates are currently being batched, and at what nesting level if so.  If the value is zero, it is not being 
		 * batched.  If it is one or greater, that's how many nested batches there are.  You must have <writeLock> to use this 
		 * variable, even to read it lest you create a race condition.
		 */
		protected int batchingFileUpdates;
		
		/* var: batchHasChanges
		 * If <batchingFileUpdates> is true, this is set to whether any changes have occurred since the batch was started.
		 */
		protected bool batchHasChanges;

		/* var: styleChangeWatchers
		 * A list of <IStyleChangeWatcher> objects that want to be notified whenever style files change.
		 */
		protected List<IStyleChangeWatcher> styleChangeWatchers;



		// Group: Static Variables
		// __________________________________________________________________________
		
		
		/* var: ImageExtensions
		 * A <StringSet> of the supported image extensions.
		 */
		static public StringSet ImageExtensions = new StringSet (true, false, new string[] { "gif", "jpg", "jpeg", "png", "bmp" });

		}
	}