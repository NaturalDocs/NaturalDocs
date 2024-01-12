/*
 * Class: CodeClear.NaturalDocs.Engine.Output.HTML.UnprocessedChanges
 * ____________________________________________________________________________
 *
 * An object which stores all the unprocessed changes that need to be applied for a HTML build target.
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
using CodeClear.NaturalDocs.Engine.Collections;
using CodeClear.NaturalDocs.Engine.IDObjects;


namespace CodeClear.NaturalDocs.Engine.Output.HTML
	{
	public class UnprocessedChanges
		{

		// Group: Functions
		// __________________________________________________________________________


		/* Constructor: UnprocessedChanges
		 */
		public UnprocessedChanges ()
			{
			sourceFiles = new NumberSet();
			classes = new NumberSet();

			imageFiles = new NumberSet();
			unchangedImageFileUseChecks = new NumberSet();

			styleFiles = new NumberSet();
			mainStyleFiles = false;

			searchPrefixes = new StringSet();
			mainSearchFiles = false;

			framePage = false;
			homePage = false;
			menu = false;

			possiblyEmptyFolders = new StringSet(Config.Manager.KeySettingsForPaths);

			accessLock = new object();
			}


		/* Function: GetStatus
		 * Returns a numeric value representing the total changes yet to be processed.  It is the sum of everything in
		 * this class weighted by the <TargetBuilder.Cost Constants> which estimate how hard they are to perform.  The value
		 * of the total is meaningless other than to track progress as it works its way towards zero.
		 */
		public void GetStatus (out long workRemaining)
			{
			long count = 0;

			lock (accessLock)
				{
				count += sourceFiles.Count * TargetBuilder.SourceFileCost;
				count += classes.Count * TargetBuilder.ClassCost;
				count += imageFiles.Count * TargetBuilder.ImageFileCost;
				count += unchangedImageFileUseChecks.Count * TargetBuilder.UnchangedImageFileUseCheckCost;
				count += styleFiles.Count * TargetBuilder.StyleFileCost;

				if (mainStyleFiles)
					{  count += TargetBuilder.MainStyleFilesCost;  }

				count += searchPrefixes.Count * TargetBuilder.SearchPrefixCost;

				if (mainSearchFiles)
					{  count += TargetBuilder.MainSearchFilesCost;  }

				if (framePage)
					{  count += TargetBuilder.FramePageCost;  }

				if (homePage)
					{  count += TargetBuilder.HomePageCost;  }

				if (menu)
					{  count += TargetBuilder.MenuCost;  }

				count += possiblyEmptyFolders.Count * TargetBuilder.PossiblyEmptyFolderCost;
				}

			workRemaining = count;
			}


		/* Function: Lock
		 *
		 * Locks the object to apply multiple changes efficiently or to directly access the variables.  This prevents other threads
		 * from accessing the object until you call <Unlock()>.
		 *
		 * Use of the other functions will lock and unlock this object automatically, so it is not necessary for most use cases.
		 */
		public void Lock ()
			{
			System.Threading.Monitor.Enter(accessLock);
			}


		/* Function: Unlock
		 * Releases the thread lock applied by <Lock()>.
		 */
		public void Unlock ()
			{
			System.Threading.Monitor.Exit(accessLock);
			}



		// Group: Add Functions
		// __________________________________________________________________________


		/* Function: AddSourceFile
		 * Adds a source file ID to the list that need their output files rebuilt.
		 */
		public void AddSourceFile (int fileID)
			{
			lock (accessLock)
				{  sourceFiles.Add(fileID);  }
			}

		/* Function: AddSourceFiles
		 * Adds a set of source file IDs to the list that need their output files rebuilt.
		 */
		public void AddSourceFiles (NumberSet fileIDs)
			{
			lock (accessLock)
				{  sourceFiles.Add(fileIDs);  }
			}

		/* Function: AddClass
		 * Adds a class ID to the list that need their output files rebuilt.
		 */
		public void AddClass (int classID)
			{
			lock (accessLock)
				{  classes.Add(classID);  }
			}

		/* Function: AddClasses
		 * Adds a set of class IDs to the list that need their output files rebuilt.
		 */
		public void AddClasses (NumberSet classIDs)
			{
			lock (accessLock)
				{  classes.Add(classIDs);  }
			}

		/* Function: AddImageFile
		 * Adds an image file ID to the list that need their output files rebuilt.
		 */
		public void AddImageFile (int imageFileID)
			{
			lock (accessLock)
				{
				imageFiles.Add(imageFileID);
				unchangedImageFileUseChecks.Remove(imageFileID);
				}
			}

		/* Function: AddImageFiles
		 * Adds a set of image file IDs to the list that need their output files rebuilt.
		 */
		public void AddImageFiles (NumberSet imageFileIDs)
			{
			lock (accessLock)
				{
				imageFiles.Add(imageFileIDs);
				unchangedImageFileUseChecks.Remove(imageFileIDs);
				}
			}

		/* Function: AddImageFileUseCheck
		 * Adds an image file ID to the list that need to be checked to see if they're still used in the output.
		 */
		public void AddImageFileUseCheck (int imageFileID)
			{
			lock (accessLock)
				{
				if (!imageFiles.Contains(imageFileID))
					{  unchangedImageFileUseChecks.Add(imageFileID);  }
				}
			}

		/* Function: AddStyleFile
		 * Adds a style file ID to the list that need their output files rebuilt.
		 */
		public void AddStyleFile (int fileID)
			{
			lock (accessLock)
				{  styleFiles.Add(fileID);  }
			}

		/* Function: AddMainStyleFiles
		 * Adds the main style files, main.css and main.js, to the list of things that need to be rebuilt.
		 */
		public void AddMainStyleFiles ()
			{
			lock (accessLock)
				{  mainStyleFiles = true;  }
			}

		/* Function: AddSearchPrefix
		 * Adds the search prefix to the list that need to be rebuilt.
		 */
		public void AddSearchPrefix (string prefix)
			{
			lock (accessLock)
				{  searchPrefixes.Add(prefix);  }
			}

		/* Function: AddMainSearchFiles
		 * Adds the main search file, index.js, to the list of things that need to be rebuilt.
		 */
		public void AddMainSearchFiles ()
			{
			lock (accessLock)
				{  mainSearchFiles = true;  }
			}

		/* Function: AddFramePage
		 * Adds the frame page, index.html, to the list of things that need to be rebuilt.
		 */
		public void AddFramePage ()
			{
			lock (accessLock)
				{  framePage = true;  }
			}

		/* Function: AddHomePage
		 * Adds the home page, home.html, to the list of things that need to be rebuilt.
		 */
		public void AddHomePage ()
			{
			lock (accessLock)
				{  homePage = true;  }
			}

		/* Function: AddMenu
		 * Adds the menu to the list of things that need to be rebuilt.
		 */
		public void AddMenu ()
			{
			lock (accessLock)
				{  menu = true;  }
			}

		/* Function: AddPossiblyEmptyFolder
		 * Adds a folder that has had a file deleted from it to the list of folders we need to check to see if they're empty.
		 */
		public void AddPossiblyEmptyFolder (Path folder)
			{
			lock (accessLock)
				{  possiblyEmptyFolders.Add(folder);  }
			}



		// Group: Pick Functions
		// __________________________________________________________________________


		/* Function: PickSourceFile
		 * Picks a source file ID that needs its output file rebuilt to work on, if there are any.  It will be removed from the
		 * list of unprocessed changes.  If there aren't any it will return zero.
		 */
		 public int PickSourceFile ()
			{
			lock (accessLock)
				{
				return sourceFiles.Pop();
				}
			}

		/* Function: PickClass
		 * Picks a class ID that needs its output file rebuilt to work on, if there are any.  It will be removed from the list of
		 * unprocessed changes.  If there aren't any it will return zero.
		 */
		 public int PickClass ()
			{
			lock (accessLock)
				{
				return classes.Pop();
				}
			}

		/* Function: PickImageFile
		 * Picks an image file ID that needs its output file rebuilt to work on, if there are any.  This will include new, changed,
		 * and deleted files so you should check the file's status to see if it's deleted before processing it.  You also need to
		 * check that it's actually used in the output.  It will be removed from the list of unprocessed changes.  If there aren't
		 * any it will return zero.
		 */
		 public int PickImageFile ()
			{
			lock (accessLock)
				{
				return imageFiles.Pop();
				}
			}

		/* Function: PickUnchangedImageFileUseCheck
		 * Picks an image file ID where whether it's used in the output may have changed.  This will only return IDs where the
		 * file itself hasn't changed and thus the ID wouldn't also be returned by <PickImageFile()>.  It will be removed from
		 * the list of unprocessed changes.  If there aren't any it will return zero.
		 */
		 public int PickUnchangedImageFileUseCheck ()
			{
			lock (accessLock)
				{
				return unchangedImageFileUseChecks.Pop();
				}
			}

		/* Function: PickStyleFile
		 * Picks a style file ID that needs its output file rebuilt to work on, if there are any.  It will be removed from the list
		 * of unprocessed changes.  If there aren't any it will return zero.
		 */
		 public int PickStyleFile ()
			{
			lock (accessLock)
				{
				return styleFiles.Pop();
				}
			}

		/* Function: PickMainStyleFiles
		 * Picks the main style files to work on, returning whether it's necessary.  If it returns true it will be removed from the
		 * list of unprocessed changes.
		 */
		 public bool PickMainStyleFiles ()
			{
			lock (accessLock)
				{
				if (mainStyleFiles)
					{
					mainStyleFiles = false;
					return true;
					}
				else
					{  return false;  }
				}
			}

		/* Function: PickSearchPrefix
		 * Picks a search prefix that needs its output file rebuilt to work on, if there are any.  It will be removed from the list
		 * of unprocessed  changes.  If there aren't any it will return null.
		 */
		 public string PickSearchPrefix ()
			{
			lock (accessLock)
				{
				return searchPrefixes.RemoveOne();
				}
			}

		/* Function: PickMainSearchFiles
		 * Picks the main search files to work on, returning whether it's necessary.  If it returns true it will be removed from the
		 * list of unprocessed changes.
		 */
		 public bool PickMainSearchFiles ()
			{
			lock (accessLock)
				{
				if (mainSearchFiles)
					{
					mainSearchFiles = false;
					return true;
					}
				else
					{  return false;  }
				}
			}

		/* Function: PickFramePage
		 * Picks the frame page to work on, returning whether it's necessary.  If it returns true it will be removed from the list of
		 * unprocessed changes.
		 */
		 public bool PickFramePage ()
			{
			lock (accessLock)
				{
				if (framePage)
					{
					framePage = false;
					return true;
					}
				else
					{  return false;  }
				}
			}

		/* Function: PickHomePage
		 * Picks the home page to work on, returning whether it's necessary.  If it returns true it will be removed from the list of
		 * unprocessed changes.
		 */
		 public bool PickHomePage ()
			{
			lock (accessLock)
				{
				if (homePage)
					{
					homePage = false;
					return true;
					}
				else
					{  return false;  }
				}
			}

		/* Function: PickMenu
		 * Picks the menu to work on, returning whether it's necessary.  If it returns true it will be removed from the list of
		 * unprocessed changes.
		 */
		 public bool PickMenu ()
			{
			lock (accessLock)
				{
				if (menu)
					{
					menu = false;
					return true;
					}
				else
					{  return false;  }
				}
			}

		/* Function: PickPossiblyEmptyFolders
		 *
		 * Picks the possibly empty folders to work on, if there are any.  You have to process all of them at once, so it is returned
		 * as a list and they will all be removed from the unprocessed changes.  If there aren't any it will return null.
		 *
		 * The reason is because task of deleting empty folders is not parallelizable.  Theoretically it should be, but in practice when
		 * two or more threads try to delete the same folder at the same time they both fail.  This could happen if both the folder
		 * and it's parent folder are on the list, so one thread gets it from the list while the other thread gets it by walking up the
		 * child's tree.
		 */
		 public List<Path> PickPossiblyEmptyFolders ()
			{
			lock (accessLock)
				{
				if (possiblyEmptyFolders.IsEmpty)
					{  return null;  }

				List<Path> list = new List<Path>();

				var enumerator = possiblyEmptyFolders.GetEnumerator();

				while (enumerator.MoveNext())
				   {  list.Add(enumerator.Current);  }

				possiblyEmptyFolders.Clear();

				return list;
				}
			}



		// Group: Variables
		// __________________________________________________________________________
		//
		// These variables are protected internal because some code may need to access them directly.  You should use the
		// access functions instead of doing this whenever possible.  All direct access to the variables must be surrounded by
		// calls to <Lock()> and <Unlock()>.
		//


		/* var: sourceFiles
		 *
		 * A set of the source file IDs that need their output file rebuilt.
		 *
		 * This variable is protected internal because some code may need to access it directly.  You should use the access
		 * functions instead of doing this whenever possible.  All direct access to the variable must be surrounded by calls to
		 * <Lock()> and <Unlock()>.
		 */
		protected internal NumberSet sourceFiles;


		/* var: classes
		 *
		 * A set of the class IDs that need their output file rebuilt.
		 *
		 * This variable is protected internal because some code may need to access it directly.  You should use the access
		 * functions instead of doing this whenever possible.  All direct access to the variable must be surrounded by calls to
		 * <Lock()> and <Unlock()>.
		 */
		protected internal NumberSet classes;


		/* var: imageFiles
		 *
		 * A set of IDs for the image files that need their output updated.  This includes new, changed, and deleted files so
		 * you should check the file's status to see if it's deleted before processing it.  You also need to check that it's actually
		 * used in the output.
		 *
		 * This variable is protected internal because some code may need to access it directly.  You should use the access
		 * functions instead of doing this whenever possible.  All direct access to the variable must be surrounded by calls to
		 * <Lock()> and <Unlock()>.
		 */
		protected internal NumberSet imageFiles;


		/* var: unchangedImageFileUseChecks
		 *
		 * A set of IDs for image files where the file itself didn't change, but whether it's used or not might have.  No IDs will
		 * appear both in here and in <imageFiles>.
		 *
		 * This variable is protected internal because some code may need to access it directly.  You should use the access
		 * functions instead of doing this whenever possible.  All direct access to the variable must be surrounded by calls to
		 * <Lock()> and <Unlock()>.
		 */
		protected internal NumberSet unchangedImageFileUseChecks;


		/* var: styleFiles
		 *
		 * A set of IDs for the style files that need their output updated.  This includes new, changed, and deleted files so
		 * you should check the file's status to see if it's deleted before processing it.
		 *
		 * This variable is protected internal because some code may need to access it directly.  You should use the access
		 * functions instead of doing this whenever possible.  All direct access to the variable must be surrounded by calls to
		 * <Lock()> and <Unlock()>.
		 */
		protected internal NumberSet styleFiles;


		/* var: mainStyleFiles
		 *
		 * Whether the main style files, main.css and main.js, need to be rebuilt.
		 *
		 * This variable is protected internal because some code may need to access it directly.  You should use the access
		 * functions instead of doing this whenever possible.  All direct access to the variable must be surrounded by calls to
		 * <Lock()> and <Unlock()>.
		 */
		protected internal bool mainStyleFiles;


		/* var: searchPrefixes
		 *
		 * A set of all the search index prefixes which need to be rebuilt.  This set combines changed and deleted IDs, so when
		 * using <SearchIndex.Manager.GetKeywordEntries()> make sure to test each result for null.
		 *
		 * This variable is protected internal because some code may need to access it directly.  You should use the access
		 * functions instead of doing this whenever possible.  All direct access to the variable must be surrounded by calls to
		 * <Lock()> and <Unlock()>.
		 */
		protected internal StringSet searchPrefixes;


		/* var: mainSearchFiles
		 *
		 * Whether the main search file, index.js, needs to be rebuilt.
		 *
		 * This variable is protected internal because some code may need to access it directly.  You should use the access
		 * functions instead of doing this whenever possible.  All direct access to the variable must be surrounded by calls to
		 * <Lock()> and <Unlock()>.
		 */
		protected internal bool mainSearchFiles;


		/* var: framePage
		 *
		 * Whether the frame page, index.html, needs to be rebuilt.
		 *
		 * This variable is protected internal because some code may need to access it directly.  You should use the access
		 * functions instead of doing this whenever possible.  All direct access to the variable must be surrounded by calls to
		 * <Lock()> and <Unlock()>.
		 */
		protected internal bool framePage;


		/* var: homePage
		 *
		 * Whether the home page, home.html, needs to be rebuilt.
		 *
		 * This variable is protected internal because some code may need to access it directly.  You should use the access
		 * functions instead of doing this whenever possible.  All direct access to the variable must be surrounded by calls to
		 * <Lock()> and <Unlock()>.
		 */
		protected internal bool homePage;


		/* var: menu
		 *
		 * Whether the menu data files need to be rebuilt.
		 *
		 * This variable is protected internal because some code may need to access it directly.  You should use the access
		 * functions instead of doing this whenever possible.  All direct access to the variable must be surrounded by calls to
		 * <Lock()> and <Unlock()>.
		 */
		protected internal bool menu;


		/* var: possiblyEmptyFolders
		 *
		 * A set of folders that have had files removed, and thus should be deleted if empty.
		 *
		 * This variable is protected internal because some code may need to access it directly.  You should use the access
		 * functions instead of doing this whenever possible.  All direct access to the variable must be surrounded by calls to
		 * <Lock()> and <Unlock()>.
		 */
		protected internal StringSet possiblyEmptyFolders;


		/* var: accessLock
		 * An object used for a monitor that prevents more than one thread from accessing any of the variables at a time.
		 */
		protected object accessLock;

		}
	}
