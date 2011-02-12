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
 *		Local Folder Entries:
 *		
 *			> Type: LocalFolder
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
 *		File Entries:
 *		
 *			> Type: File
 *			> Name: HTML string
 *			> Path: Relative path to output file
 *			
 *			File entries don't store the full path to its output file as that would be a lot of duplicated memory.  Rather, the
 *			path is relative to its containing folder which it must be combined with.
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
		 * LocalFolder - The entry is a folder that has its members stored inline as an array.
		 * DynamicFolder - The entry is a folder that has its members in a different file and stores only its ID.
		 * File - The entry is a file.
		 */
		protected enum FileHierarchyEntryType : byte  
			{
			// If these values ever change, you also have to change the substitutions in the JavaScript styles.
			RootFolder = 0, 
			LocalFolder = 1,
			DynamicFolder = 2,
			File = 3
			};

		/* Enum: FileHierarchyEntryMember
		 * The indexes into the array of various file hierarchy entry members.
		 */
		protected enum FileHierarchyEntryMember : byte
			{  
			// If these values ever change, you also have to change the substitutions in the JavaScript styles and the
			// JSON generation functions.
			Type = 0,  // All
			ID = 1,  // Root
			Name = 1,  // Local Folder, Dynamic Folder, File
			Path = 2, // All
			Members = 3  // Root Folder, Local Folder, Dynamic Folder
			};



		// Group: Functions
		// __________________________________________________________________________


		/* Function: BuildFileHierarchy
		 * Generates <FileHierarchy.json> from <sourceFilesWithContent>.
		 */
		protected void BuildFileHierarchy (CancelDelegate cancelDelegate)
			{
			FileHierarchy fileHierarchy = new FileHierarchy();

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



			//xxx printing
			fileHierarchy.ForEach(FileHierarchy.ForEachMethod.Linear, xxxPrintEntry);
			}

		void xxxPrintEntry (FileHierarchyEntries.Entry entry)
			{
			for (var parent = entry.Parent; parent != null; parent = parent.Parent)
				{  System.Console.Write("   ");  }

			if (entry is FileHierarchyEntries.FileSource)
				{
				System.Console.WriteLine("### " + ((entry as FileHierarchyEntries.FileSource).WrappedFileSource.Name ?? "Default File Source"));
				}
			else if (entry is FileHierarchyEntries.Folder)
				{
				System.Console.WriteLine("[+] " + (entry as FileHierarchyEntries.Folder).PathFragment);
				}
			else if (entry is FileHierarchyEntries.File)
				{
				System.Console.WriteLine(" -  " + (entry as FileHierarchyEntries.File).FileName);
				}
			}
		}

	}

