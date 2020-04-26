/* 
 * Class: CodeClear.NaturalDocs.Engine.Files.AdderStatus
 * ____________________________________________________________________________
 * 
 * Statistics on the progress of <Adder.WorkOnAddingAllFiles()>.
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


namespace CodeClear.NaturalDocs.Engine.Files
	{
	public class AdderStatus
		{
		
		// Group: Functions
		// __________________________________________________________________________
		
		
		/* Function: AdderStatus
		 */
		public AdderStatus ()
			{
			accessLock = new object();
			Reset();
			}
			
		/* Function: Reset
		 * Reset all values back to their initial state.
		 */
		public void Reset ()
			{
			lock (accessLock)
				{
				sourceFilesFound = 0;
				sourceFoldersFound = 0;

				imageFilesFound = 0;
				imageFoldersFound = 0;

				styleFilesFound = 0;
				styleFoldersFound = 0;
				}
			}

		/* Function: Add
		 * Adds the passed status to this one.
		 */
		public void Add (AdderStatus other)
			{
			// We'll take a temporary copy of the other status so we never hold two locks at the same time.

			int otherSourceFilesFound, otherSourceFoldersFound;
			int otherImageFilesFound, otherImageFoldersFound;
			int otherStyleFilesFound, otherStyleFoldersFound;

			lock (other.accessLock)
				{
				otherSourceFilesFound = other.sourceFilesFound;
				otherSourceFoldersFound = other.sourceFoldersFound;

				otherImageFilesFound = other.imageFilesFound;
				otherImageFoldersFound = other.imageFoldersFound;

				otherStyleFilesFound = other.styleFilesFound;
				otherStyleFoldersFound = other.styleFoldersFound;
				}

			lock (accessLock)
				{
				sourceFilesFound += otherSourceFilesFound;
				sourceFoldersFound += otherSourceFoldersFound;

				imageFilesFound += otherImageFilesFound;
				imageFoldersFound += otherImageFoldersFound;

				styleFilesFound += otherStyleFilesFound;
				styleFoldersFound += otherStyleFoldersFound;
				}
			}
			
			
		/* Function: Copy
		 * Copies the passed status to this one.
		 */
		public void Copy (AdderStatus other)
			{
			// We'll take a temporary copy of the other status so we never hold two locks at the same time.

			int otherSourceFilesFound, otherSourceFoldersFound;
			int otherImageFilesFound, otherImageFoldersFound;
			int otherStyleFilesFound, otherStyleFoldersFound;

			lock (other.accessLock)
				{
				otherSourceFilesFound = other.sourceFilesFound;
				otherSourceFoldersFound = other.sourceFoldersFound;

				otherImageFilesFound = other.imageFilesFound;
				otherImageFoldersFound = other.imageFoldersFound;

				otherStyleFilesFound = other.styleFilesFound;
				otherStyleFoldersFound = other.styleFoldersFound;
				}

			lock (accessLock)
				{
				sourceFilesFound = otherSourceFilesFound;
				sourceFoldersFound = otherSourceFoldersFound;

				imageFilesFound = otherImageFilesFound;
				imageFoldersFound = otherImageFoldersFound;

				styleFilesFound = otherStyleFilesFound;
				styleFoldersFound = otherStyleFoldersFound;
				}
			}

		/* Function: AddFiles
		 * Adds files to the count.
		 */
		public void AddFiles (FileType type, int count = 1)
			{
			lock (accessLock)
				{
				switch (type)
					{
					case FileType.Source:
						sourceFilesFound += count;
						break;
					case FileType.Image:
						imageFilesFound += count;
						break;
					case FileType.Style:
						styleFilesFound += count;
						break;
					default:
						throw new NotImplementedException();
					}
				}
			}

		/* Function: AddFolders
		 * Adds folders to the count.
		 */
		public void AddFolders (InputType type, int count = 1)
			{
			lock (accessLock)
				{
				switch (type)
					{
					case InputType.Source:
						sourceFoldersFound += count;
						break;
					case InputType.Image:
						imageFoldersFound += count;
						break;
					case InputType.Style:
						styleFoldersFound += count;
						break;
					default:
						throw new NotImplementedException();
					}
				}
			}

			
			
		// Group: Properties
		// __________________________________________________________________________


		public int SourceFilesFound
			{
			get
				{
				lock (accessLock)
					{  return sourceFilesFound;  }
				}
			}

		public int SourceFoldersFound
			{
			get
				{
				lock (accessLock)
					{  return sourceFoldersFound;  }
				}
			}

		public int ImageFilesFound
			{
			get
				{
				lock (accessLock)
					{  return imageFilesFound;  }
				}
			}

		public int ImageFoldersFound
			{
			get
				{
				lock (accessLock)
					{  return imageFoldersFound;  }
				}
			}

		public int StyleFilesFound
			{
			get
				{
				lock (accessLock)
					{  return styleFilesFound;  }
				}
			}

		public int StyleFoldersFound
			{
			get
				{
				lock (accessLock)
					{  return styleFoldersFound;  }
				}
			}



		// Group: Variables
		// __________________________________________________________________________


		protected object accessLock;		
		
		protected int sourceFilesFound;
		protected int sourceFoldersFound;

		protected int imageFilesFound;
		protected int imageFoldersFound;

		protected int styleFilesFound;
		protected int styleFoldersFound;

		}
	}