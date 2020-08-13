/* 
 * Class: CodeClear.NaturalDocs.Engine.Output.Builders.HTMLBuildState
 * ____________________________________________________________________________
 * 
 * A class encompassing all the build state information for a HTML output target.
 *		
 */

// This file is part of Natural Docs, which is Copyright © 2003-2020 Code Clear LLC.
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
		
		
		/* Construction: HTMLBuildState
		 * Creates a new HTMLBuildState object.  If createEmptyObjects is changed to false, fields such as <SourceFilesToRebuild>
		 * and <UsedMenuDataFiles> will be set to null instead of empty objects.  This is useful to avoid unnecessary memory 
		 * allocations if they're just going to get replaced, such as by <BuildState_nd.Load()>.
		 */
		public HTMLBuildState (bool createEmptyObjects = true)
			{
			if (createEmptyObjects)
				{
				sourceFilesToRebuild = new IDObjects.NumberSet();
				sourceFilesWithContent = new IDObjects.NumberSet();
				classFilesToRebuild = new IDObjects.NumberSet();
				classFilesWithContent = new IDObjects.NumberSet();
				styleFilesToRebuild = new IDObjects.NumberSet();

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
				styleFilesToRebuild = null;

				searchPrefixesToRebuild = null;
				foldersToCheckForDeletion = null;
				usedMenuDataFiles = null;
				}

			needToBuildFramePage = false;
			needToBuildMainStyleFiles = false;
			needToBuildMenu = false;
			needToBuildSearchPrefixIndex = false;
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
			set
				{  sourceFilesToRebuild = value;  }
			}

		/* Property: SourceFilesWithContent
		 * A set of the source file IDs that contain content this output target can use.  This is different from all the files with
		 * content in <CodeDB.Manager> because it is after all filters have been applied.
		 */
		public IDObjects.NumberSet SourceFilesWithContent
			{
			get
				{  return sourceFilesWithContent;  }
			set
				{  sourceFilesWithContent = value;  }
			}

		/* Property: ClassFilesToRebuild
		 * A set of the class IDs that need their output file rebuilt.
		 */
		public IDObjects.NumberSet ClassFilesToRebuild
			{
			get
				{  return classFilesToRebuild;  }
			set
				{  classFilesToRebuild = value;  }
			}

		/* Property: ClassFilesWithContent
		 * A set of the class IDs that contain content this output target can use.  This is different from all the classes with 
		 * content in <CodeDB.Manager> because it is after all filters have been applied.
		 */
		public IDObjects.NumberSet ClassFilesWithContent
			{
			get
				{  return classFilesWithContent;  }
			set
				{  classFilesWithContent = value;  }
			}

		/* Property: StyleFilesToRebuild
		 * A set of IDs for the style files that need their output updated.  This includes new, changed, and deleted files so
		 * you should check the file's status to see if it's deleted before processing it.
		 */
		public IDObjects.NumberSet StyleFilesToRebuild
			{
			get
				{  return styleFilesToRebuild;  }
			set
				{  styleFilesToRebuild = value;  }
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
			set
				{  searchPrefixesToRebuild = value;  }
			}
		
		/* Property: FoldersToCheckForDeletion
		 * A set of folders that have had files removed, and thus should be deleted if empty.
		 */
		public StringSet FoldersToCheckForDeletion
			{
			get
				{  return foldersToCheckForDeletion;  }
			set
				{  foldersToCheckForDeletion = value;  }
			}



		// Group: Variables
		// __________________________________________________________________________
				
		protected IDObjects.NumberSet sourceFilesToRebuild;
		protected IDObjects.NumberSet sourceFilesWithContent;
		protected IDObjects.NumberSet classFilesToRebuild;
		protected IDObjects.NumberSet classFilesWithContent;
		protected IDObjects.NumberSet styleFilesToRebuild;

		protected StringSet searchPrefixesToRebuild;
		protected StringSet foldersToCheckForDeletion;
		protected StringTable<IDObjects.NumberSet> usedMenuDataFiles;

		protected bool needToBuildFramePage;
		protected bool needToBuildMainStyleFiles;
		protected bool needToBuildMenu;
		protected bool needToBuildSearchPrefixIndex;

		}
	}

