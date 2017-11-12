/* 
 * Class: CodeClear.NaturalDocs.Engine.Output.Builders.HTMLBuildState
 * ____________________________________________________________________________
 * 
 * A class encompassing all the build state information for a HTML output target.
 * 
 * 
 * File: BuildState.nd
 * 
 *		A file used to store the build state of this output target the last time it was built.
 *	
 *		> [Byte: Need to Build Frame Page (0 or 1)]
 *		> [Byte: Need to Build Main Style Files (0 or 1)]
 *		> [Byte: Need to Build Menu (0 or 1)]
 *		> [Byte: Need to Build Search Prefix Index (0 or 1)]
 *		
 *		Flags for some of the structural items that need to be built.
 * 
 *		> [NumberSet: Source File IDs to Rebuild]
 *		> [NumberSet: Class IDs to Rebuild]
 *		
 *		The source and class files that needed to be rebuilt but weren't yet.  If the last build was run to completion these should 
 *		be empty sets, though if the build was interrupted this will have the ones left to do.
 *		
 *		> [NumberSet: Source File IDs with Content]
 *		> [NumberSet: Class IDs with Content]
 *		
 *		A set of all the source and class files known to have content after all filters were applied.
 *		
 *		> [StringSet: Search Prefixes to Rebuild]
 *		
 *		A set of all the search index prefixes which were changed or deleted and thus need to be rebuilt.
 * 
 *		> [StringSet: Folders to Check for Deletion]
 *		
 *		A set of all folders which have had files removed and thus should be removed if empty.  If the last build was run
 *		to completion this should be an empty set.
 * 
 *		> [String: Menu Data File Type] [NumberSet: Menu Data File Numbers]
 *		> [String: Menu Data File Type] [NumberSet: Menu Data File Numbers]
 *		> ...
 *		> [String: null]
 *		
 *		A list of the data files that were created to build the menu, stored as string-NumberSet pairs that repeats until there
 *		is a null string.  This allows us to clean up old data files if we're using fewer than before.
 *		
 *		The type will be strings like "files" and "classes", so if the menu created files.js, files2.js, and files3.js, this will be
 *		stored as "files" and {1-3}.
 *		
 */

// This file is part of Natural Docs, which is Copyright © 2003-2017 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using CodeClear.NaturalDocs.Engine.Collections;
using CodeClear.NaturalDocs.Engine.IDObjects;


namespace CodeClear.NaturalDocs.Engine.Output.Builders
	{
	public partial class HTMLBuildState
		{

		// Group: Functions
		// __________________________________________________________________________
		
		
		/* Function: HTMLBuildState
		 * 
		 * Constructor.
		 * 
		 * If createEmptyObjects is set things like <sourceFilesToRebuild> will have objects associated with them.  If it's falso,
		 * they will be set to null.  Only set it to false if you're going to be creating the objects yourself since they shouldn't be
		 * set to null.
		 */
		public HTMLBuildState (bool createEmptyObjects = true)
			{
			if (createEmptyObjects)
				{
				sourceFilesToRebuild = new IDObjects.NumberSet();
				sourceFilesWithContent = new IDObjects.NumberSet();
				classFilesToRebuild = new IDObjects.NumberSet();
				classFilesWithContent = new IDObjects.NumberSet();
				searchPrefixesToRebuild = new StringSet();
				foldersToCheckForDeletion = new StringSet(Config.Manager.KeySettingsForPaths);
				usedMenuDataFiles = new StringTable<NumberSet>();
				}
			else
				{
				sourceFilesToRebuild = null;
				sourceFilesWithContent = null;
				classFilesToRebuild = null;
				classFilesWithContent = null;
				searchPrefixesToRebuild = null;
				foldersToCheckForDeletion = null;
				usedMenuDataFiles = null;
				}

			needToBuildFramePage = false;
			needToBuildMainStyleFiles = false;
			needToBuildMenu = false;
			needToBuildSearchPrefixIndex = false;
			}



		// Group: File Functions
		// __________________________________________________________________________


		/* Function: LoadBinaryFile
		 * Loads the information in <BuildState.nd> and returns whether it was successful.  If not the function will return an empty
		 * BuildState object.
		 */
		public static bool LoadBinaryFile (Path filename, out HTMLBuildState buildState)
			{
			buildState = new HTMLBuildState(createEmptyObjects: false);

			BinaryFile binaryFile = new BinaryFile();
			bool result = true;

			try
				{
				if (binaryFile.OpenForReading(filename, "2.0") == false)
					{  result = false;  }
				else
					{
					// [Byte: Need to Build Frame Page (0 or 1)]
					// [Byte: Need to Build Main Style Files (0 or 1)]
					// [Byte: Need to Build Menu (0 or 1)]
					// [Byte: Need to Build Search Prefix Index (0 or 1)]

					buildState.needToBuildFramePage = (binaryFile.ReadByte() == 1);
					buildState.needToBuildMainStyleFiles = (binaryFile.ReadByte() == 1);
					buildState.needToBuildMenu = (binaryFile.ReadByte() == 1);
					buildState.needToBuildSearchPrefixIndex = (binaryFile.ReadByte() == 1);

					// [NumberSet: Source File IDs to Rebuild]
					// [NumberSet: Class IDs to Rebuild]
					// [NumberSet: Source File IDs with Content]
					// [NumberSet: Class IDs with Content]
					// [StringSet: Search Prefixes to Rebuild]
					// [StringSet: Folders to Check for Deletion]

					buildState.sourceFilesToRebuild = binaryFile.ReadNumberSet();
					buildState.classFilesToRebuild = binaryFile.ReadNumberSet();
					buildState.sourceFilesWithContent = binaryFile.ReadNumberSet();
					buildState.classFilesWithContent = binaryFile.ReadNumberSet();
					buildState.searchPrefixesToRebuild = binaryFile.ReadStringSet();
					buildState.foldersToCheckForDeletion = binaryFile.ReadStringSet(Config.Manager.KeySettingsForPaths);

					// [String: Menu Data File Type] [NumberSet: Menu Data File Numbers]
					// [String: Menu Data File Type] [NumberSet: Menu Data File Numbers]
					// ...
					// [String: null]

					buildState.usedMenuDataFiles = new StringTable<IDObjects.NumberSet>();
					string menuDataFileType = binaryFile.ReadString();

					while (menuDataFileType != null)
						{
						IDObjects.NumberSet menuDataFileNumbers = binaryFile.ReadNumberSet();
						buildState.usedMenuDataFiles.Add(menuDataFileType, menuDataFileNumbers);

						menuDataFileType = binaryFile.ReadString();
						}
					}
				}
			catch
				{  result = false;  }
			finally
				{  binaryFile.Dispose();  }

			if (result == false)
				{  buildState = new HTMLBuildState(createEmptyObjects: true);  }

			return result;
			}


		/* Function: SaveBinaryFile
		 * Saves the passed information in <BuildState.nd>.
		 */
		public static void SaveBinaryFile (Path filename, HTMLBuildState buildState)
			{
			using (BinaryFile binaryFile = new BinaryFile())
				{
				binaryFile.OpenForWriting(filename);

				// [Byte: Need to Build Frame Page (0 or 1)]
				// [Byte: Need to Build Main Style Files (0 or 1)]
				// [Byte: Need to Build Menu (0 or 1)]
				// [Byte: Need to Build Search Prefix Index (0 or 1)]

				binaryFile.WriteByte( (byte)(buildState.needToBuildFramePage ? 1 : 0) );
				binaryFile.WriteByte( (byte)(buildState.needToBuildMainStyleFiles ? 1 : 0) );
				binaryFile.WriteByte( (byte)(buildState.needToBuildMenu ? 1 : 0) );
				binaryFile.WriteByte( (byte)(buildState.needToBuildSearchPrefixIndex ? 1 : 0) );

				// [NumberSet: Source File IDs to Rebuild]
				// [NumberSet: Class IDs to Rebuild]
				// [NumberSet: Source File IDs with Content]
				// [NumberSet: Class IDs with Content]
				// [StringSet: Search Prefixes to Rebuild]
				// [StringSet: Folders to Check for Deletion]

				binaryFile.WriteNumberSet(buildState.sourceFilesToRebuild);
				binaryFile.WriteNumberSet(buildState.classFilesToRebuild);
				binaryFile.WriteNumberSet(buildState.sourceFilesWithContent);
				binaryFile.WriteNumberSet(buildState.classFilesWithContent);
				binaryFile.WriteStringSet(buildState.searchPrefixesToRebuild);
				binaryFile.WriteStringSet(buildState.foldersToCheckForDeletion);

				// [String: Menu Data File Type] [NumberSet: Menu Data File Numbers]
				// [String: Menu Data File Type] [NumberSet: Menu Data File Numbers]
				// ...
				// [String: null]

				foreach (var menuDataFilePair in buildState.usedMenuDataFiles)
					{
					binaryFile.WriteString(menuDataFilePair.Key);
					binaryFile.WriteNumberSet(menuDataFilePair.Value);
					}

				binaryFile.WriteString(null);
				}
			}



		// Group: Properties
		// __________________________________________________________________________


		/* Property: SourceFilesToRebuild
		 * A set of the source file IDs that need their output file rebuilt.
		 */
		public IDObjects.NumberSet SourceFilesToRebuild
			{
			get
				{  return sourceFilesToRebuild;  }
			}

		/* Property: SourceFilesWithContent
		 * A set of the source file IDs that contain content this output target can use.  This is different from all the files with
		 * content in <CodeDB.Manager> because it is after all filters have been applied.
		 */
		public IDObjects.NumberSet SourceFilesWithContent
			{
			get
				{  return sourceFilesWithContent;  }
			}

		/* Property: ClassFilesToRebuild
		 * A set of the class IDs that need their output file rebuilt.
		 */
		public IDObjects.NumberSet ClassFilesToRebuild
			{
			get
				{  return classFilesToRebuild;  }
			}

		/* Property: ClassFilesWithContent
		 * A set of the class IDs that contain content this output target can use.  This is different from all the classes with 
		 * content in <CodeDB.Manager> because it is after all filters have been applied.
		 */
		public IDObjects.NumberSet ClassFilesWithContent
			{
			get
				{  return classFilesWithContent;  }
			}

		/* Property: NeedToBuildFramePage
		 * Whether the frame page, index.html, needs to be rebuilt.
		 */
		public bool NeedToBuildFramePage
			{
			get
				{  return needToBuildFramePage;  }
			set
				{  needToBuildFramePage = value;  }
			}

		/* Property: NeedToBuildMainStyleFiles
		 * Whether the main style files, main.css and main.js, need to be rebuilt.
		 */
		public bool NeedToBuildMainStyleFiles
			{
			get
				{  return needToBuildMainStyleFiles;  }
			set
				{  needToBuildMainStyleFiles = value;  }
			}

		/* Property: NeedToBuildMenu
		 * Whether the menu data files need to be rebuilt.
		 */
		public bool NeedToBuildMenu
			{
			get
				{  return needToBuildMenu;  }
			set
				{  needToBuildMenu = value;  }
			}

		/* Property: UsedMenuDataFiles
		 * The menu data files created the last time the menu was built.  It maps strings like "files" and "classes" to NumberSets,
		 * so files.js, files2.js, and files3.js would map to "files" and {1-3}.
		 */
		public StringTable<IDObjects.NumberSet> UsedMenuDataFiles
			{
			get
				{  return usedMenuDataFiles;  }
			set
				{  usedMenuDataFiles = value;  }
			}

		/* Property: NeedToBuildSearchPrefixIndex
		 * Whether the search prefix index needs to be rebuilt.
		 */
		public bool NeedToBuildSearchPrefixIndex
			{
			get
				{  return needToBuildSearchPrefixIndex;  }
			set
				{  needToBuildSearchPrefixIndex = value;  }
			}

		/* Property: SearchPrefixesToRebuild
		 * A set of all the search index prefixes which need to be rebuilt.  This set combines changed and deleted IDs, so when 
		 * using <SearchIndex.Manager.GetKeywordEntries()> make sure to test each result for null.
		 */
		public StringSet SearchPrefixesToRebuild
			{
			get
				{  return searchPrefixesToRebuild;  }
			}
		
		/* Property: FoldersToCheckForDeletion
		 * A set of folders that have had files removed, and thus should be deleted if empty.
		 */
		public StringSet FoldersToCheckForDeletion
			{
			get
				{  return foldersToCheckForDeletion;  }
			}



		// Group: Variables
		// __________________________________________________________________________
				
		protected IDObjects.NumberSet sourceFilesToRebuild;
		protected IDObjects.NumberSet sourceFilesWithContent;
		protected IDObjects.NumberSet classFilesToRebuild;
		protected IDObjects.NumberSet classFilesWithContent;

		protected StringSet searchPrefixesToRebuild;
		protected StringSet foldersToCheckForDeletion;
		protected StringTable<IDObjects.NumberSet> usedMenuDataFiles;

		protected bool needToBuildFramePage;
		protected bool needToBuildMainStyleFiles;
		protected bool needToBuildMenu;
		protected bool needToBuildSearchPrefixIndex;

		}
	}

