/* 
 * Class: CodeClear.NaturalDocs.Engine.Files.Adder
 * ____________________________________________________________________________
 * 
 * A module which handles adding all files in <FileSources> to <Files.Manager>.
 * 
 * 
 * Topic: Usage
 * 
 *		- Call <Start()> after <Files.Manager> is started, as it depends on its list of <FileSources>.
 *		
 *		- Multiple threads can call <WorkOnAddingAllFiles()>.
 *		
 * 
 * Multithreading: Thread Safety Notes
 * 
 *		Externally, this class is thread safe.
 *		
 *		Internally, all variable accesses must use a monitor on <accessLock>.
 *		
 */

// This file is part of Natural Docs, which is Copyright © 2003-2020 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using CodeClear.NaturalDocs.Engine.Collections;


namespace CodeClear.NaturalDocs.Engine.Files
	{
	public class Adder : Module
		{
		
		// Group: Initialization and Configuration Functions
		// __________________________________________________________________________
		

		/* Function: Adder
		 */
		public Adder (Engine.Instance engineInstance) : base (engineInstance)
			{
			fileSources = null;
			fileSourcesClaimed = null;
			folderPrefixesClaimed = null;
			accessLock = new object();
			}
			
						
		/* Function: Dispose
		 */
		override protected void Dispose (bool strictRulesApply)
			{
			}


		/* Function: Start
		 */
		public bool Start (Errors.ErrorList errors)
			{
			fileSources = Manager.FileSources;
			fileSourcesClaimed = new bool[fileSources.Count];
			folderPrefixesClaimed = new StringSet (KeySettings.IgnoreCase);

			return true;
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
			for (;;)
				{
				if (cancelDelegate())
					{  return;  }

				var fileSource = PickFileSource();

				if (fileSource == null)
					{  return;  }

				fileSource.AddAllFiles(cancelDelegate);

				ReleaseFileSource(fileSource, !cancelDelegate());
				}
			}
			
			
		/* Function: GetStatus
		 * Fills the passed object with the status of <WorkOnAddingAllFiles()>.
		 */
		public void GetStatus (ref AdderStatus statusTarget)
			{
			statusTarget.Reset();
			
			foreach (var fileSource in fileSources)
				{  fileSource.AddStatusTo(ref statusTarget);  }
			}
			
			

		// Group: Individual File Management Functions
		// __________________________________________________________________________


		/* Function: PickFileSource
		 * 
		 * Returns a <FileSource> that is available to be scanned for files, or null if there aren't any.  You must pass it to
		 * <ReleaseFileSource()> when done.
		 * 
		 * If this function returns null it doesn't mean that there are no unscanned <FileSources> remaining, as it may be requiring 
		 * some to be scanned sequentially to prevent multiple sources from the same disk from being scanned at the same time.
		 * So while this function may return null while a particular <FileSource> is being scanned, it may return another one after
		 * it's released.
		 */
		protected FileSource PickFileSource ()
			{
			lock (accessLock)
				{
				for (int i = 0; i < fileSources.Count; i++)
					{
					if (fileSourcesClaimed[i] == false)
						{
						var fileSource = fileSources[i];

						if (fileSource is FileSources.Folder)
							{
							FileSources.Folder folderFileSource = (FileSources.Folder)fileSource;
							string folderPrefix = folderFileSource.Path.Prefix;

							if (folderPrefixesClaimed.Contains(folderPrefix) == false)
								{
								fileSourcesClaimed[i] = true;
								folderPrefixesClaimed.Add(folderPrefix);
								return fileSource;
								}
							}
						else // not a folder
							{
							fileSourcesClaimed[i] = true;
							return fileSource;
							}
						}
					}
				}

			return null;
			}


		/* Function: ReleaseFileSource
		 * Releases a <FileSource> claimed via <PickFileSource()> after all processing is complete on it.
		 */
		protected void ReleaseFileSource (FileSource fileSource, bool scanningCompleted)
			{
			lock (accessLock)
				{
				if (!scanningCompleted)
					{
					for (int i = 0; i < fileSources.Count; i++)
						{
						if ((object)fileSources[i] == (object)fileSource)
							{
							fileSourcesClaimed[i] = false;
							break;
							}
						}
					}

				if (fileSource is FileSources.Folder)
					{
					folderPrefixesClaimed.Remove(
						(fileSource as FileSources.Folder).Path.Prefix
						);
					}
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
		
		/* var: fileSources
		 * 
		 * The list of <FileSources> being scanned.
		 * 
		 * Thread Safety:
		 * 
		 *		This variable is read-only once set so can be read at any time.
		 */
		protected IList<FileSource> fileSources;

		/* var: fileSourcesClaimed
		 * 
		 * An array of bools corresponding to the entries in <fileSources>, with each one representng whether the corresponding 
		 * index into <fileSources> has been scanned already, or the scan is in process.
		 * 
		 * Thread Safety:
		 * 
		 *		You must hold <accessLock> in order to use this variable.
		 */
		protected bool[] fileSourcesClaimed;

		/* var: folderPrefixesClaimed
		 * 
		 * A set of all the <Path.Prefixes> that are currently being searched by threads.  This is used to prevent multiple 
		 * threads from searching <FileSource.Folder>-based sources with the same prefix at the same time, since they are most 
		 * likely on the same physical disk and would not benefit from the parallelism.
		 * 
		 * Thread Safety:
		 * 
		 *		You must hold <accessLock> in order to use this variable.
		 */
		protected StringSet folderPrefixesClaimed;
		
		/* var: accessLock
		 * An object used for a monitor that prevents more than one thread from accessing any of the variables
		 * at a time.
		 */
		protected object accessLock;

		}
	}