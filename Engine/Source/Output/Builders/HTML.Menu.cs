/* 
 * Class: GregValure.NaturalDocs.Engine.Output.Builders.HTML
 * ____________________________________________________________________________
 * 
 * 
 * File: FileHierarchy.json
 * 
 *		A data file representing all or part of the source file hierarchy.  The hierarchy is represented by entries, each one 
 *		being an array with the first value being its type.  The values are defined in <FileHierarchyEntryType>, and the 
 *		indexes of other members are defined in <FileHierarchyEntryMember>.
 *		
 *		Every file starts off with a root folder entry, which in turn contains other file and folder entries.  For small projects
 *		the entire hierarchy may be contained in a single JSON file, but in larger projects it may use dynamic folder entries
 *		to split off sections of it into separate files.  This keeps the file sizes down, allowing only the sections of the 
 *		hierarchy in use to be loaded.
 *		
 *		Root Folder Entries:
 *		
 *			> Type: RootFolder
 *			> ID: Number.  The number for the main JSON file is always 1.
 *			> Path: Full path to output folder
 *			> Members: An array of further entries
 *			
 *		Inline Folder Entries:
 *		
 *			> Type: InlineFolder
 *			> Name: HTML string or array of HTML strings
 *			> Path: Full path to output folder
 *			> Members: An array of further entries
 *			
 *			If the folder entry was condensed (making a single entry for "FolderA/FolderB" if folder A contains nothing except
 *			folder B) Name will be an array of each separate folder name.  Otherwise it will just be the name itself.
 *		
 *		Dynamic Folder Entries:
 *		
 *			> Type: DynamicFolder
 *			> Name: HTML string or array of HTML strings
 *			> Path: Full path to output folder
 *			> Members: ID number of the file containing its members.
 *			
 *			Unlike local and root folders, the Members variable just holds an ID number for the file that contains them.  They
 *			will be stored in FileHierarchy[number].json.
 *		
 *		Explicit File Entries:
 *		
 *			> Type: File
 *			> Name: HTML string
 *			> Path: Output file name without path
 *			
 *			A file entry with an explicit path.  The path is just a file name.  The full path isn't stored because that would be
 *			a lot of duplicated memory.  Rather, the path is relative to the containing folder.
 *			
 *		Implicit File Entries:
 *		
 *			> Type: AutoFile
 *			> Name: HTML string
 *			
 *			The same as an explicit file entry except the path is implied.  It can be obtained by taking the name string, 
 *			substituting dashes for periods, and then appending ".html".  This saves a significant amount of space in the 
 *			generated JSON because this applies to most files.  Explicit file entries are for when this translation wouldn't work, 
 *			such as if the generated HTML string contained entity characters.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2011 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using System.Text;
using GregValure.NaturalDocs.Engine.Output.Styles;


namespace GregValure.NaturalDocs.Engine.Output.Builders
	{
	public partial class HTML
		{

		// Group: Types
		// __________________________________________________________________________


		/* Enum: FileHierarchyEntryType
		 * 
		 * The type value of a file hierarchy entry.
		 *
		 * RootFolder - The entry is the root folder.
		 * InlineFolder - The entry is a folder that has its members stored inline as an array.
		 * DynamicFolder - The entry is a folder that has its members in a different file and stores only its ID.
		 * ExplicitFile - The entry is a file with an explicit path.
		 * ImplicitFile - The entry is a file with an implicit path.
		 */
		public enum FileHierarchyEntryType : byte  
			{
			// If these values ever change, you also have to change the substitutions in the JavaScript styles.
			RootFolder = 0, 
			InlineFolder = 1,
			DynamicFolder = 2,
			ExplicitFile = 3,
			ImplicitFile = 4
			};

		/* Enum: FileHierarchyEntryMember
		 * The indexes into the array of various file hierarchy entry members.
		 */
		public enum FileHierarchyEntryMember : byte
			{  
			// If these values ever change, you also have to change the substitutions in the JavaScript styles and the
			// JSON generation functions.
			Type = 0,  // All
			ID = 1,  // Root Folder
			Name = 1,  // Local Folder, Dynamic Folder, Explicit File, Implicit File
			Path = 2, // Root Folder, Local Folder, Dynamic Folder, Explicit File
			Members = 3  // Root Folder, Local Folder, Dynamic Folder
			};



		// Group: Functions
		// __________________________________________________________________________


		/* Function: BuildFileHierarchy
		 * Generates <FileHierarchy.json> from <sourceFilesWithContent>.
		 */
		protected void BuildFileHierarchy (CancelDelegate cancelDelegate)
			{
			HTMLFileHierarchy fileHierarchy = new HTMLFileHierarchy();

			foreach (Files.FileSource fileSource in Instance.Files.FileSources)
				{
				if (fileSource.Type == Files.InputType.Source)
					{  fileHierarchy.AddFileSource(fileSource);  }
				}

			foreach (int fileID in sourceFilesWithContent)
				{
				if (cancelDelegate())
					{  return;  }

				Files.File file = Instance.Files.FromID(fileID);
				fileHierarchy.AddFile(file);
				}

			fileHierarchy.Condense();
			
			if (cancelDelegate())
				{  return;  }

			fileHierarchy.Sort();

			if (cancelDelegate())
				{  return;  }

			fileHierarchy.PrepareJSON(this);

			if (cancelDelegate())
				{  return;  }

			fileHierarchy.SegmentJSON(JSONMenuSegmentLength);

			if (cancelDelegate())
				{  return;  }

			Stack<FileHierarchyEntries.HTMLRootFolder> rootFolders = new Stack<FileHierarchyEntries.HTMLRootFolder>();
			rootFolders.Push( (FileHierarchyEntries.HTMLRootFolder)fileHierarchy.RootFolder );

			while (rootFolders.Count > 0)
				{
				StringBuilder json = new StringBuilder();

				FileHierarchyEntries.HTMLRootFolder rootFolder = rootFolders.Pop();
				rootFolder.AppendJSON(json, rootFolders);

				System.IO.StreamWriter outputFile = CreateTextFileAndPath(config.Folder + "/menu/files" + 
																															(rootFolder.ID == 1 ? "" : rootFolder.ID.ToString()) + ".js");

				try
					{
					outputFile.Write("NDMenu.FileMenuSectionLoaded(");
					outputFile.Write(rootFolder.ID);
					outputFile.Write(',');
					outputFile.Write(json.ToString());
					outputFile.Write(");");
					}
				finally
					{
					outputFile.Dispose();
					}
				}
			}


		// Group: Constants
		// __________________________________________________________________________


		/* const: JSONMenuSegmentLength
		 * The amount of data to try to fit in each JSON file before splitting it off into another one.  This will be
		 * artificially low in debug builds to better test the loading mechanism.
		 */
		#if DEBUG
			protected const int JSONMenuSegmentLength = 1024*3;
		#else
			protected const int JSONMenuSegmentLength = 1024*32;
		#endif

		}

	}

