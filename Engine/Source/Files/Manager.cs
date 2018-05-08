/* 
 * Class: CodeClear.NaturalDocs.Engine.Files.Manager
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
 *		- Add any change watchers with <AddChangeWatcher()>.  This can be done before the module is started.
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
 *		Externally, this class is thread safe.
 *		
 *		Internally, all variable accesses must use a monitor on <accessLock>.
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
 *		> (if image)
 *		>    [UInt32: Width in Pixels or 0 if unknown]
 *		>    [UInt32: Height in Pixels or 0 if unknown]
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
 *			2.0.2:
 *			
 *				- Added dimensions for image files.  They will always be zero because image file support was only partially
 *				  implemented and it would have been too much effort to back it out for 2.0.2.
 *		
 *			2.0:
 *				
 *				- The file was introduced.
 *			
 */

// This file is part of Natural Docs, which is Copyright © 2003-2017 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using System.Threading;
using CodeClear.NaturalDocs.Engine.Collections;


namespace CodeClear.NaturalDocs.Engine.Files
	{
	public class Manager : Module
		{
		
		// Group: Initialization and Configuration Functions
		// __________________________________________________________________________
		

		/* Function: Manager
		 */
		public Manager (Engine.Instance engineInstance) : base (engineInstance)
			{
			fileSources = new List<FileSource>();
			filters = new List<Filter>();
			claimedFolderPrefixes = new StringSet (KeySettings.IgnoreCase);			

			files = new IDObjects.Manager<File>(Config.Manager.KeySettingsForPaths, false);
			
			accessLock = new object();
			changeWatchers = new List<IChangeWatcher>();
			}
			
						
		/* Function: Dispose
		 */
		override protected void Dispose (bool strictRulesApply)
			{
			if (!strictRulesApply)
				{
				foreach (FileSource fileSource in fileSources)
					{
					if (fileSource is IDisposable)
						{  ((IDisposable)fileSource).Dispose();  }
					}
					
				try
					{
					SaveBinaryFile( EngineInstance.Config.WorkingDataFolder + "/Files.nd", files );
					}
				catch
					{  }
				}
			}


		/* Function: AddFileSource
		 * Adds a file source to the list.  This can only be called before the <Engine.Instance> is started, not while it is running.
		 */
		public void AddFileSource (FileSource source)
			{
			lock (accessLock)
				{  fileSources.Add(source);  }
			}
			
		/* Function: AddFilter
		 * Adds a filter that folders will be checked against.
		 */
		public void AddFilter (Filter filter)
			{
			lock (accessLock)
				{  filters.Add(filter);  }
			}

		/* Function: AddChangeWatcher
		 * Adds an object that wants to be notified whenever files change.
		 */
		public void AddChangeWatcher (IChangeWatcher watcher)
			{
			lock (accessLock)
				{  changeWatchers.Add(watcher);  }
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
				
			if (EngineInstance.Config.ReparseEverything == false)
				{
				if (LoadBinaryFile( EngineInstance.Config.WorkingDataFolder + "/Files.nd", out files) == false)
					{  EngineInstance.Config.ReparseEverything = true;  }
				}
		        			
			return (errors.Count == startingErrorCount);
			}
			
			

		// Group: Information Functions
		// __________________________________________________________________________

			
		/* Function: FromID
		 * Returns the <File> associated with the passed file ID, or null if there isn't one.
		 */
		public File FromID (int fileID)
			{
			lock (accessLock)
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

			lock (accessLock)
				{
				return files[filePath];
				}
			}
			
			
		/* Function: FileSourceOf
		 * Returns the <FileSource> which contains the passed <File>, or null if none.
		 */
		public FileSource FileSourceOf (File file)
			{
			lock (accessLock)
				{
				foreach (FileSource fileSource in fileSources)
					{
					if (fileSource.Contains(file.FileName))
						{  return fileSource;  }
					}
				
				return null;
				}
			}


		/* Function: SourceFolderIsIgnored
		 * Returns whether the passed source folder should be ignored based on any <Filters> defined.  This function is available 
		 * before <Start()> is called.
		 */
		public bool SourceFolderIsIgnored (Path path)
			{
			lock (accessLock)
				{
				foreach (Filter filter in filters)
					{
					if (filter.IgnoreSourceFolder(path))
						{  return true;  }
					}
				
				return false;
				}
			}



		// Group: Group File Management Functions
		// __________________________________________________________________________


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
			
			Monitor.Enter(accessLock);
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
					
					Monitor.Exit(accessLock);
					locked = false;

					claimedFileSource.AddAllFiles(cancelDelegate);
					// If it failed because of the cancelDelegate AllFilesAdded will be false.
					
					Monitor.Enter(accessLock);
					locked = true;
					
					claimedFileSource.Claimed = false;
						
					}  // for
				} // try
				
			finally
				{
				if (locked)
					{  Monitor.Exit(accessLock);  }
				}
			}
			
			
		/* Function: GetAddAllFilesStatus
		 * Fills the passed object with the status of <WorkOnAddingAllFiles()>.  The object will be a snapshot of the values, not
		 * a live monitor, so the values will not change out from under you.
		 */
		public void GetAddAllFilesStatus (ref AddAllFilesStatus statusTarget)
			{
			statusTarget.Reset();
			
			lock (accessLock)
				{
				for (int i = 0; i < fileSources.Count; i++)
					{
					fileSources[i].CombineAddAllFilesStatus(ref statusTarget);
					}
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
			Monitor.Enter(accessLock);
			bool haveLock = true;
			
			try
				{
				// We want to release the lock before calling DeleteFile so it won't be held when it notifies change watchers.
				// Since that can potentially allow the list of files to change we take a private copy of the used IDs and work 
				// from that instead of iterating through the files object.

				IDObjects.NumberSet fileIDs = files.GetUsedIDs();

				while (!fileIDs.IsEmpty)
					{
					int fileID = fileIDs.Pop();
					File file = files[fileID];

					if (file != null && file.InFileSource == false)
						{  
						Monitor.Exit(accessLock);
						haveLock = false;

						DeleteFile(file.FileName);

						Monitor.Enter(accessLock);
						haveLock = true;
						}
						
					if (cancelDelegate())
						{  return;  }
					}
				}
			finally
				{  
				if (haveLock)
					{  Monitor.Exit(accessLock);  }
				}
			}			
		
		
			
		// Group: Individual File Management Functions
		// __________________________________________________________________________
		
			
		/* Function: AddOrUpdateFile
		 * 
		 * Adds a file or updates its last modification time.  If the file was not previously known to the class, it will be treated as 
		 * new, whereas if it was known but has a different modification time it will be treated as changed.  Returns whether this
		 * call changed anything.  It is okay to call this multiple times on the same file.
		 * 
		 * This is assumed to be called for files that are in a file source so it automatically sets <File.InFileSource>.
		 */
		public bool AddOrUpdateFile (Path name, FileType type, DateTime lastModified, bool forceReparse = false)
			{
			bool changed = false;

			Monitor.Enter(accessLock);
			
			try
				{
				File file = files[name];
				
				// The file didn't exist in our records so it's new
				if (file == null)
					{
					if (type == FileType.Image)
						{  file = new ImageFile(name, lastModified);  }
					else
						{  file = new File(name, type, lastModified);  }

					file.Status = FileFlags.NewOrChanged;
					files.Add(file);
					
					foreach (var changeWatcher in changeWatchers)
						{  changeWatcher.OnAddFile(file);  }

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
						bool wasDeletedSinceClaimed = (file.StatusSinceClaimed == FileFlags.DeletedSinceClaimed);

						file.LastModified = lastModified;
						file.StatusSinceClaimed = FileFlags.NewOrChangedSinceClaimed;

						foreach (var changeWatcher in changeWatchers)
							{  
							if (wasDeletedSinceClaimed)
								{  changeWatcher.OnAddFile(file);  }
							else
								{  changeWatcher.OnFileChanged(file);  }
							}
		
						changed = true;
						}
					}

				else if (file.LastModified != lastModified || file.Status == FileFlags.Deleted || forceReparse)
					{
					bool wasDeleted = (file.Status == FileFlags.Deleted);

					file.LastModified = lastModified;
					file.Status = FileFlags.NewOrChanged;
					
					foreach (var changeWatcher in changeWatchers)
						{  
						if (wasDeleted)
							{  changeWatcher.OnAddFile(file);  }
						else
							{  changeWatcher.OnFileChanged(file);  }
						}

					changed = true;
					}

				file.InFileSource = true;
				}
			finally
				{  Monitor.Exit(accessLock);  }

			return changed;
			}
			
			
		/* Function: DeleteFile
		 * 
		 * Notifies the class that the file has been deleted.  Returns whether this call changed anything.  It is okay to call this
		 * multiple times on the same file.
		 */
		public bool DeleteFile (Path name)
			{
			bool changed = false;
			
			Monitor.Enter(accessLock);
			
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

						foreach (var changeWatcher in changeWatchers)
							{  changeWatcher.OnDeleteFile(file);  }

						changed = true;
						}
					}

				else if (file.Status != FileFlags.Deleted)
					{
					file.Status = FileFlags.Deleted;
					
					foreach (var changeWatcher in changeWatchers)
						{  changeWatcher.OnDeleteFile(file);  }

					changed = true;
					}
				}
			finally
				{  Monitor.Exit(accessLock);  }
				
			return changed;
			}
			

			
		// Group: Misc Functions
		// __________________________________________________________________________


		/* Function: Cleanup
		 * Cleans up the module's internal data when everything is up to date.  This will remove any successfully processed 
		 * deleted files, after which they can no longer be found with <FromID()> and their IDs may be reassigned.  You can
		 * pass a <CancelDelegate> to interrupt the process if necessary.
		 */
		public void Cleanup (CancelDelegate cancelDelegate)
			{
			lock (accessLock)
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
		
		
		/* Function: LoadBinaryFile
		 * Loads <Files.nd> and returns whether it was successful.  If it wasn't it will still return valid objects, they will just
		 * be empty.
		 */
		public static bool LoadBinaryFile (Path filename, out IDObjects.Manager<File> files)
			{
			files = new IDObjects.Manager<File>(Config.Manager.KeySettingsForPaths, false );

			BinaryFile binaryFile = new BinaryFile();
			bool result = true;
			
			try
				{
				// We'll continue to handle 2.0 files in 2.0.2 since it's easy enough
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
					// (if image)
					//    [UInt32: Width in Pixels or 0 if unknown]
					//    [UInt32: Height in Pixels or 0 if unknown]
 					// ...
					// [Int32: 0]
					
					int id;
					Path path;
					FileType type;
					DateTime lastModification;
					File file;
					uint width, height;
					
					for (;;)
						{
						id = binaryFile.ReadInt32();
						
						if (id == 0)
							{  break;  }
							
						path = binaryFile.ReadString();
						type = (FileType)binaryFile.ReadByte();
						lastModification = new DateTime(binaryFile.ReadInt64());
						
						if (type == FileType.Image)
							{
							if (binaryFile.Version < "2.0.2")
								{
								width = 0;
								height = 0;

								// Reset last modification time so they'll be reparsed
								lastModification = new DateTime(0);
								}
							else
								{
								width = binaryFile.ReadUInt32();
								height = binaryFile.ReadUInt32();
								}

							if (width == 0 || height == 0)
								{  file = new ImageFile(path, lastModification);  }
							else
								{  file = new ImageFile(path, lastModification, width, height);  }
							}
						else
							{
							file = new File(path, type, lastModification);
							}

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
					// (if image)
					//    [UInt32: Width in Pixels or 0 if unknown]
					//    [UInt32: Height in Pixels or 0 if unknown]
					
					binaryFile.WriteInt32(file.ID);
					binaryFile.WriteString(file.FileName);
					binaryFile.WriteByte((byte)file.Type);
					binaryFile.WriteInt64(file.LastModified.Ticks);

					if (file.Type == FileType.Image)
						{
						ImageFile imageFile = (ImageFile)file;

						if (imageFile.DimensionsKnown)
							{
							binaryFile.WriteUInt32(imageFile.Width);
							binaryFile.WriteUInt32(imageFile.Height);
							}
						else
							{
							binaryFile.WriteUInt32(0);
							binaryFile.WriteUInt32(0);
							}
						}
					}

				// [Int32: 0]
				binaryFile.WriteInt32(0);
				}
			finally
				{  binaryFile.Close();  }
			}
			
			

		// Group: Properties
		// __________________________________________________________________________
		
		
		/* Property: FileSources
		 * 
		 * Retrieves a read-only list of the file sources this instance has.
		 * 
		 * Thread Safety:
		 * 
		 *		During engine initialization this function is not thread safe, but engine initialization should be a single-threaded operation
		 *		anyway.
		 *		
		 *		Once the engine is started this is treated as a read-only variable, which would make it inherently thread safe.
		 */
		public IList<FileSource> FileSources
			{
			get
				{  return fileSources.AsReadOnly();  }
			}
			
			
			
		// Group: File Source Variables
		// __________________________________________________________________________
		
		
		/* var: fileSources
		 * 
		 * A list of all the file sources.
		 * 
		 * Thread Safety:
		 * 
		 *		You must hold <accessLock> in order to use this variable.
		 */
		protected List<FileSource> fileSources;
		

		/* var: filters
		 * 
		 * A list of all the filters.
		 * 
		 * Thread Safety:
		 * 
		 *		You must hold <accessLock> in order to use this variable.
		 */
		protected List<Filter> filters;
		

		/* var: claimedFolderPrefixes
		 * 
		 * A set of all the <Path.Prefixes> that are currently being searched by threads.  This is used to prevent multiple 
		 * threads from searching <Folder>-based <FileSources> with the same prefix at the same time, since they are most 
		 * likely on the same physical disk and would not benefit from the parallelism.
		 * 
		 * Thread Safety:
		 * 
		 *		You must hold <accessLock> in order to use this variable.
		 */
		protected StringSet claimedFolderPrefixes;
		
		
		
		// Group: File Variables
		// __________________________________________________________________________
		

		/* var: files
		 * 
		 * All the files that are managed by this object.
		 * 
		 * Thread Safety:
		 * 
		 *		You must hold <accessLock> in order to use this variable.
		 */
		protected IDObjects.Manager<File> files;
		
				
				
		// Group: Other Variables
		// __________________________________________________________________________
		
		
		/* var: accessLock
		 * An object used for a monitor that prevents more than one thread from accessing any of the variables
		 * at a time.
		 */
		protected object accessLock;

		
		/* var: changeWatchers
		 * 
		 * A list of <IChangeWatchers> that want to be notified whenever files change.
		 * 
		 * Thread Safety:
		 * 
		 *		You must hold <accessLock> in order to use this variable.
		 */
		protected List<IChangeWatcher> changeWatchers;



		// Group: Static Variables
		// __________________________________________________________________________
		
		
		/* var: ImageExtensions
		 * A <StringSet> of the supported image extensions.
		 */
		static public StringSet ImageExtensions = new StringSet (KeySettings.IgnoreCase, new string[] { "gif", "jpg", "jpeg", "png", "bmp", "svg" });

		}
	}