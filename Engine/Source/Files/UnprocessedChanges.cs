/*
 * Class: CodeClear.NaturalDocs.Engine.Files.UnprocessedChanges
 * ____________________________________________________________________________
 *
 * An object which stores all the unprocessed file changes that have been detected and allows them to be retrieved
 * for processing.  In addition to source files, this includes image files that can be referenced with "(see image.jpg)"
 * and extras tied to CSS styles.
 *
 *
 * Multithreading: Thread Safety Notes
 *
 *		Externally, this class is thread safe.
 *
 *		Internally, all variable accesses must use a monitor on <accessLock>.
 *
 */

// This file is part of Natural Docs, which is Copyright © 2003-2023 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;


namespace CodeClear.NaturalDocs.Engine.Files
	{
	public class UnprocessedChanges
		{

		// Group: Functions
		// __________________________________________________________________________


		/* Function: UnprocessedChanges
		 */
		public UnprocessedChanges ()
			{
			newOrChangedFileIDs = new IDObjects.NumberSet();
			deletedFileIDs = new IDObjects.NumberSet();

			accessLock = new object();
			}


		public void AddNewFile (File file)
			{
			lock (accessLock)
				{
				deletedFileIDs.Remove(file.ID);
				newOrChangedFileIDs.Add(file.ID);
				}
			}


		public void AddNewFiles (IDObjects.NumberSet fileIDs)
			{
			lock (accessLock)
				{
				deletedFileIDs.Remove(fileIDs);
				newOrChangedFileIDs.Add(fileIDs);
				}
			}


		public void AddChangedFile (File file)
			{
			lock (accessLock)
				{
				deletedFileIDs.Remove(file.ID);
				newOrChangedFileIDs.Add(file.ID);
				}
			}


		public void AddChangedFiles (IDObjects.NumberSet fileIDs)
			{
			lock (accessLock)
				{
				deletedFileIDs.Remove(fileIDs);
				newOrChangedFileIDs.Add(fileIDs);
				}
			}


		public void AddDeletedFile (File file)
			{
			lock (accessLock)
				{
				newOrChangedFileIDs.Remove(file.ID);
				deletedFileIDs.Add(file.ID);
				}
			}



		/* Function: GetStatus
		 * Returns a snapshot of the changes yet to be processed.
		 */
		public void GetStatus (ref ChangeProcessorStatus status)
			{
			lock (accessLock)
				{
				status.NewOrChangedFilesRemaining = newOrChangedFileIDs.Count;
				status.DeletedFilesRemaining = deletedFileIDs.Count;
				}
			}


		/* Function: GetStatus
		 * Returns a detailed snapshot of the changes yet to be processed.
		 */
		public void GetStatus (ref ChangeProcessorDetailedStatus status)
			{
			lock (accessLock)
				{
				status.NewOrChangedFileIDsRemaining = newOrChangedFileIDs.Duplicate();
				status.DeletedFileIDsRemaining = deletedFileIDs.Duplicate();
				}
			}



		// Group: Pick Functions
		// __________________________________________________________________________


		/* Function: PickNewOrChangedFileID
		 * Picks an added or changed file ID to work on, if there are any.  It will be removed from the list of unprocessed
		 * changes.  If there aren't any it will return zero.
		 */
		public int PickNewOrChangedFileID ()
			{
			lock (accessLock)
				{
				return newOrChangedFileIDs.Pop();
				}
			}


		/* Function: PickDeletedFileID
		 * Picks a deleted file to work on, if there are any.  It will be removed from the list of unprocessed changes.  If there
		 * aren't any it will return zero.
		 */
		public int PickDeletedFileID ()
			{
			lock (accessLock)
				{
				return deletedFileIDs.Pop();
				}
			}



		// Group: Properties
		// __________________________________________________________________________


		public bool IsEmpty
			{
			get
				{
				lock (accessLock)
					{
					return (newOrChangedFileIDs.IsEmpty && deletedFileIDs.IsEmpty);
					}
				}
			}

		public IEnumerable<int> AllNewOrChangedFileIDs
			{
			get
				{  return (IEnumerable<int>)newOrChangedFileIDs;  }
			}


		public IEnumerable<int> AllDeletedFileIDs
			{
			get
				{  return (IEnumerable<int>)deletedFileIDs;  }
			}



		// Group: Variables
		// __________________________________________________________________________


		/* var: newOrChangedFileIDs
		 *
		 * A <IDObjects.NumberSet> of the file IDs which have been added or changed and have yet to be
		 * processed.
		 *
		 * Thread Safety:
		 *
		 *		You must hold <accessLock> in order to use this variable.
		 */
		protected IDObjects.NumberSet newOrChangedFileIDs;


		/* var: deletedFileIDs
		 *
		 * A <IDObjects.NumberSet> of the file IDs which have been deleted and have yet to be processed.
		 *
		 * Thread Safety:
		 *
		 *		You must hold <accessLock> in order to use this variable.
		 */
		protected IDObjects.NumberSet deletedFileIDs;


		/* var: accessLock
		 * An object used for a monitor that prevents more than one thread from accessing any of the variables
		 * at a time.
		 */
		protected object accessLock;

		}
	}
