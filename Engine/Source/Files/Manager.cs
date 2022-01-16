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
 *		- Call <Engine.Instance.Start()> which will start this module. At this point the class is usable, but the file information
 *		  is as of the last run.
 *
 *		- Use <CreateAdderProcess()> to scan for added and changed files and to register them with the class.
 *
 *		- Call <DeleteFilesNotReAdded()> to mark everything not found by <Files.Adder> as deleted.
 *
 *		- Use <CreateChangeProcessor()> to process the changes.
 *
 *
 * Multithreading: Thread Safety Notes
 *
 *		Externally, this class is thread safe.
 *
 *		Internally, all variable accesses must use a monitor on <accessLock>.
 *
 */

// This file is part of Natural Docs, which is Copyright © 2003-2022 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
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

			files = new IDObjects.Manager<File>(Config.Manager.KeySettingsForPaths, false);
			filesAddedSinceStart = new IDObjects.NumberSet();
			unprocessedChanges = new UnprocessedChanges();

			accessLock = new object();
			changeWatchers = new List<IChangeWatcher>();
			}


		/* Function: Dispose
		 */
		override protected void Dispose (bool strictRulesApply)
			{
			// We don't want to save Files.nd if the module wasn't started because it would blank out all the existing data.
			if (!strictRulesApply && started)
				{
				// Set the last modification time to zero for anything still being worked on
				DateTime zero = new DateTime(0);

				foreach (int id in unprocessedChanges.AllNewOrChangedFileIDs)
					{  files[id].LastModified = zero;  }
				foreach (int id in unprocessedChanges.AllDeletedFileIDs)
					{  files[id].LastModified = zero;  }

				foreach (var fileSource in fileSources)
					{
					if (fileSource is IDisposable)
						{  ((IDisposable)fileSource).Dispose();  }
					}

				ConfigFiles.BinaryFileParser binaryFileParser = new ConfigFiles.BinaryFileParser();

				try
					{
					binaryFileParser.Save( EngineInstance.Config.WorkingDataFolder + "/Files.nd", files );
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
			StartupIssues newStartupIssues = StartupIssues.None;


			// Validate FileSources

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


			// Make sure no source folders are completely ignored because of filters

			foreach (var fileSource in fileSources)
				{
				if (fileSource is FileSources.SourceFolder)
					{
					FileSources.SourceFolder folderFileSource = (FileSources.SourceFolder)fileSource;

					if (SourceFolderIsIgnored(folderFileSource.Path))
						{
						errors.Add(
							Locale.Get("NaturalDocs.Engine", "Error.SourceFolderIsIgnored(sourceFolder)", folderFileSource.Path)
							);
						}
					}
				}


			// Load Files.nd

			if (EngineInstance.HasIssues( StartupIssues.NeedToStartFresh ))
				{
				newStartupIssues |= StartupIssues.FileIDsInvalidated |
												StartupIssues.NeedToReparseAllFiles;
				}
			else
				{
				ConfigFiles.BinaryFileParser binaryFileParser = new ConfigFiles.BinaryFileParser();

				if (!binaryFileParser.Load( EngineInstance.Config.WorkingDataFolder + "/Files.nd", out files ))
					{
					newStartupIssues |= StartupIssues.FileIDsInvalidated |
												   StartupIssues.NeedToReparseAllFiles;
					}
				else // Files.nd loaded successfully
					{
					if (EngineInstance.HasIssues( StartupIssues.NeedToReparseAllFiles ))
						{  unprocessedChanges.AddChangedFiles(files.usedIDs);  }
					}
				}


			if (newStartupIssues != StartupIssues.None)
				{  EngineInstance.AddStartupIssues(newStartupIssues);  }

			bool success = (errors.Count == startingErrorCount);

			started = success;
			return success;
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
			return FileSourceOf(file.FileName);
			}


		/* Function: FileSourceOf
		 * Returns the <FileSource> which contains the passed <Path>, or null if none.
		 */
		public FileSource FileSourceOf (Path file)
			{
			lock (accessLock)
				{
				foreach (var fileSource in fileSources)
					{
					if (fileSource.Contains(file))
						{  return fileSource;  }
					}

				return null;
				}
			}


		/* Function: CharacterEncodingID
		 * Returns the character encoding ID of the passed file.  Zero means it's not a text file or use Unicode auto-detection,
		 * which will handle all forms of UTF-8, UTF-16, and UTF-32.
		 */
		public int CharacterEncodingID (File file)
			{
			// If there's a File object it's already calculated.  This version is just for consistency.
			return file.CharacterEncodingID;
			}


		/* Function: CharacterEncodingID
		 *
		 * Returns the character encoding ID of the passed file.  Zero means it's not a text file or use Unicode auto-detection,
		 * which will handle all forms of UTF-8, UTF-16, and UTF-32.
		 *
		 * This version of the function will work regardless of whether the file was ever added with <AddOrUpdateFile()>.
		 * However, it still requires the file's path to be part of a <FileSource> to apply encoding rules.  Otherwise it will
		 * always return zero.
		 */
		public int CharacterEncodingID (Path file)
			{
			var fileSource = FileSourceOf(file);

			if (fileSource != null)
				{  return fileSource.CharacterEncodingID(file);  }
			else
				{  return 0;  }
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



		// Group: Individual File Management Functions
		// __________________________________________________________________________


		/* Function: AddOrUpdateFile
		 *
		 * Adds a file or updates its last modification time and character encoding ID.  If the file was not previously known it will be
		 * treated as new, whereas if it was known but has different properties it will be treated as changed.  Returns whether this
		 * call changed anything.  It is okay to call this multiple times on the same file.
		 */
		public bool AddOrUpdateFile (AbsolutePath name, FileType type, DateTime lastModified, bool forceReparse = false,
												  int characterEncodingID = 0)
			{
			lock (accessLock)
				{
				File file = files[name];

				// If the file didn't exist in our records it's new
				if (file == null)
					{
					if (type == FileType.Image)
						{  file = new ImageFile(name, lastModified);  }
					else
						{  file = new File(name, type, lastModified, characterEncodingID);  }

					files.Add(file);
					filesAddedSinceStart.Add(file.ID);
					unprocessedChanges.AddNewFile(file);

					foreach (var changeWatcher in changeWatchers)
						{  changeWatcher.OnAddFile(file);  }

					return true;
					}

				// Make sure the file isn't being re-added as a different type
				else if (file.Type != type)
					{
					throw new Exception("Added an existing file but the types didn't match.");
					}

				// If the file was previously marked as deleted it was recreated
				else if (file.Deleted)
					{
					file.Deleted = false;
					file.LastModified = lastModified;
					file.CharacterEncodingID = characterEncodingID;

					filesAddedSinceStart.Add(file.ID);
					unprocessedChanges.AddNewFile(file);

					foreach (var changeWatcher in changeWatchers)
						{  changeWatcher.OnAddFile(file);  }

					return true;
					}

				// If the file changed or we're forcing everything to be reparsed anyway
				else if (file.LastModified != lastModified ||
						   file.CharacterEncodingID != characterEncodingID ||
						   forceReparse)
					{
					file.LastModified = lastModified;
					file.CharacterEncodingID = characterEncodingID;

					filesAddedSinceStart.Add(file.ID);
					unprocessedChanges.AddChangedFile(file);

					foreach (var changeWatcher in changeWatchers)
						{  changeWatcher.OnFileChanged(file);  }

					return true;
					}

				// Otherwise the file is the same as the last time we saw it.
				else
					{
					// This is still important because it's needed to know which files do and don't exist since the last time
					// Natural Docs was run
					filesAddedSinceStart.Add(file.ID);

					return false;
					}
				}
			}


		/* Function: DeleteFile
		 *
		 * Notifies the class that the file has been deleted.  Returns whether this call changed anything.  It is okay to call this
		 * multiple times on the same file.
		 */
		public bool DeleteFile (Path name)
			{
			lock (accessLock)
				{
				File file = files[name];

				// If the file didn't exist in our records or was already marked as deleted
				if (file == null || file.Deleted)
					{
					return false;
					}

				// The file does exist
				else
					{
					file.Deleted = true;

					// Probably not needed but let's be thorough
					filesAddedSinceStart.Remove(file.ID);

					unprocessedChanges.AddDeletedFile(file);

					foreach (var changeWatcher in changeWatchers)
						{  changeWatcher.OnDeleteFile(file);  }

					return true;
					}
				}
			}



		// Group: Misc Functions
		// __________________________________________________________________________


		/* Function: DeleteFilesNotReAdded
		 *
		 * Calls <DeleteFile()> on any file that hasn't been passed to <AddOrUpdateFile()> since <Start()> was called.  <Start()>
		 * loads the file information as of Natural Docs' last run, and <Files.Adder> adds everything it finds, so when all the
		 * <Adder> threads have completed then this represents files that no longer exist and should be treated as deleted.
		 *
		 * While this function takes a <CancelDelegate>, it is not a WorkOn function because more than one thread cannot work
		 * on this task simultaneously.
		 */
		public void DeleteFilesNotReAdded (CancelDelegate cancelDelegate)
			{
			lock (accessLock)
				{
				IDObjects.NumberSet fileIDsToDelete = files.GetUsedIDs();
				fileIDsToDelete.Remove(filesAddedSinceStart);

				foreach (int fileIDToDelete in fileIDsToDelete)
					{
					File file = files[fileIDToDelete];
					DeleteFile(file.FileName);

					if (cancelDelegate())
						{  return;  }
					}
				}
			}


		/* Function: Cleanup
		 * Cleans up the module's internal data when everything is up to date.  This will remove any successfully processed
		 * deleted files, after which they can no longer be found with <FromID()> and their IDs may be reassigned.  You can
		 * pass a <CancelDelegate> to interrupt the process if necessary.
		 */
		public void Cleanup (CancelDelegate cancelDelegate)
			{
			lock (accessLock)
				{
				#if DEBUG
				if (!unprocessedChanges.IsEmpty)
					{  throw new Exception("Called Cleanup() when there were still unprocessed changes.");  }
				#endif

				IDObjects.NumberSet toDelete = new IDObjects.NumberSet();

				foreach (File file in files)
					{
					if (cancelDelegate())
						{  return;  }

					if (file.Deleted)
						{  toDelete.Add(file.ID);  }
					}

				foreach (int id in toDelete)
					{
					if (cancelDelegate())
						{  return;  }

					filesAddedSinceStart.Remove(id);
					files.Remove(id);
					}
				}
			}



		// Group: Processes
		// __________________________________________________________________________


		/* Function: CreateAdderProcess
		 * Creates and returns an <Adder> process for adding all files in the <FileSources> to this manager.
		 */
		public Files.Adder CreateAdderProcess ()
			{
			return new Files.Adder(EngineInstance);
			}

		/* Function: CreateChangeProcessor
		 * Creates and returns a <ChangeProcessor> for processing all file changes recorded by this manager.
		 */
		public Files.ChangeProcessor CreateChangeProcessor ()
			{
			return new Files.ChangeProcessor(EngineInstance);
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


		/* Property: Filters
		 *
		 * Retrieves a read-only list of the filters this instance has.
		 *
		 * Thread Safety:
		 *
		 *		During engine initialization this function is not thread safe, but engine initialization should be a single-threaded operation
		 *		anyway.
		 *
		 *		Once the engine is started this is treated as a read-only variable, which would make it inherently thread safe.
		 */
		public IList<Filter> Filters
			{
			get
				{  return filters.AsReadOnly();  }
			}


		/* Property: UnprocessedChanges
		 *
		 * Returns a <Files.UnprocessedChanges> object which stores all of the unprocessed file changes that have been detected.
		 */
		public Files.UnprocessedChanges UnprocessedChanges
			{
			get
				{  return unprocessedChanges;  }
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


		/* var: filesAddedSinceStart
		 *
		 * The IDs of all the files that have been passed to <AddOrUpdateFile()> since <Start()> was called.  This means these
		 * are all the files found in the filesystem by <Adder>, whereas <files> will also include files that existed on the last
		 * run.  Anything in <files> that isn't in here has thus been deleted since the last run.
		 *
		 * Thread Safety:
		 *
		 *		You must hold <accessLock> in order to use this variable.
		 */
		protected IDObjects.NumberSet filesAddedSinceStart;


		/* var: unprocessedChanges
		 *
		 * All the unprocessed file changes that have been detected.
		 *
		 * Thread Safety:
		 *
		 *		You must hold <accessLock> in order to use this variable.
		 */
		protected UnprocessedChanges unprocessedChanges;



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
