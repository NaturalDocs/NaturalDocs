/* 
 * Class: CodeClear.NaturalDocs.Engine.Output.HTML.BuildState_nd
 * ____________________________________________________________________________
 * 
 * A class to handle loading and saving <BuildState.nd>.
 * 
 * 
 * Threading: Not Thread Safe
 * 
 *		The parser object may be reused, but multiple threads cannot use it at the same time.
 *		
 * 
 * File: BuildState.nd
 * 
 *		A file used to store the build state of this output target the last time it was built.
 *	
 *		> [Byte: Need to Build Frame Page (0 or 1)]
 *		> [Byte: Need to Build Main Style Files (0 or 1)]
 *		> [Byte: Need to Build Menu (0 or 1)]
 *		> [Byte: Need to Build Main Search Files (0 or 1)]
 *		
 *		Flags for some of the structural items that need to be built.
 * 
 *		> [NumberSet: Source File IDs to Rebuild]
 *		> [NumberSet: Class IDs to Rebuild]
 *		> [NumberSet: Style File IDs to Rebuild]
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
 *		Version History:
 *		
 *			- 2.0.2
 *				- Added Style File IDs to Rebuild.
 *			
 */

// This file is part of Natural Docs, which is Copyright © 2003-2020 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using CodeClear.NaturalDocs.Engine.Collections;
using CodeClear.NaturalDocs.Engine.IDObjects;
using CodeClear.NaturalDocs.Engine.Styles;


namespace CodeClear.NaturalDocs.Engine.Output.HTML
	{
	public class BuildState_nd
		{
		
		// Group: Functions
		// __________________________________________________________________________
		
		
		/* Constructor: BuildState_nd
		 */
		public BuildState_nd ()
			{
			}


		/* Function: Load
		 * Loads the information in <BuildState.nd> and returns whether it was successful.  If not the function will return an empty
		 * BuildState object.
		 */
		public bool Load (Path filename, out BuildState buildState, out UnprocessedChanges unprocessedChanges)
			{
			// Since we're creating new objects only we have access to them right now and thus we don't need to use Lock() and 
			// Unlock().
			buildState = new BuildState();
			unprocessedChanges = new UnprocessedChanges();

			BinaryFile binaryFile = new BinaryFile();
			bool result = true;

			try
				{
				if (binaryFile.OpenForReading(filename, "2.0.2") == false)
					{  result = false;  }
				else
					{
					// [Byte: Need to Build Frame Page (0 or 1)]
					// [Byte: Need to Build Main Style Files (0 or 1)]
					// [Byte: Need to Build Menu (0 or 1)]
					// [Byte: Need to Build Main Search Files (0 or 1)]

					unprocessedChanges.framePage = (binaryFile.ReadByte() == 1);
					unprocessedChanges.mainStyleFiles = (binaryFile.ReadByte() == 1);
					unprocessedChanges.menu = (binaryFile.ReadByte() == 1);
					unprocessedChanges.mainSearchFiles = (binaryFile.ReadByte() == 1);

					// [NumberSet: Source File IDs to Rebuild]
					// [NumberSet: Class IDs to Rebuild]
					// [NumberSet: Style File IDs to Rebuild]
					// [NumberSet: Source File IDs with Content]
					// [NumberSet: Class IDs with Content]

					unprocessedChanges.sourceFiles.ReadFrom(binaryFile);
					unprocessedChanges.classes.ReadFrom(binaryFile);
					unprocessedChanges.styleFiles.ReadFrom(binaryFile);
					buildState.sourceFilesWithContent.ReadFrom(binaryFile);
					buildState.classesWithContent.ReadFrom(binaryFile);

					// [StringSet: Search Prefixes to Rebuild]
					// [StringSet: Folders to Check for Deletion]

					unprocessedChanges.searchPrefixes.ReadFrom(binaryFile);
					unprocessedChanges.possiblyEmptyFolders.ReadFrom(binaryFile);

					// [String: Menu Data File Type] [NumberSet: Menu Data File Numbers]
					// [String: Menu Data File Type] [NumberSet: Menu Data File Numbers]
					// ...
					// [String: null]

					string menuDataFileType = binaryFile.ReadString();

					while (menuDataFileType != null)
						{
						Hierarchy hierarchy;

						if (menuDataFileType == "files")
							{  hierarchy = Hierarchy.File;  }
						else if (menuDataFileType == "classes")
							{  hierarchy = Hierarchy.Class;  }
						else if (menuDataFileType == "database")
							{  hierarchy = Hierarchy.Database;  }
						else
							{  throw new NotImplementedException();  }

						NumberSet menuDataFileNumbers = binaryFile.ReadNumberSet();
						buildState.usedMenuDataFiles.Add(hierarchy, menuDataFileNumbers);

						menuDataFileType = binaryFile.ReadString();
						}
					}
				}
			catch
				{  result = false;  }
			finally
				{  binaryFile.Dispose();  }

			if (result == false)
				{  
				buildState = new BuildState();
				unprocessedChanges = new UnprocessedChanges();
				}

			return result;
			}


		/* Function: Save
		 * Saves the passed information in <BuildState.nd>.
		 */
		public void Save (Path filename, BuildState buildState, UnprocessedChanges unprocessedChanges)
			{
			// We're assuming no other code is accessing these variables at the point at which we'd be saving them so we don't
			// need to call Lock() and Unlock().

			using (BinaryFile binaryFile = new BinaryFile())
				{
				binaryFile.OpenForWriting(filename);

				// [Byte: Need to Build Frame Page (0 or 1)]
				// [Byte: Need to Build Main Style Files (0 or 1)]
				// [Byte: Need to Build Menu (0 or 1)]
				// [Byte: Need to Build Main Search Files (0 or 1)]

				binaryFile.WriteByte( (byte)(unprocessedChanges.framePage ? 1 : 0) );
				binaryFile.WriteByte( (byte)(unprocessedChanges.mainStyleFiles ? 1 : 0) );
				binaryFile.WriteByte( (byte)(unprocessedChanges.menu ? 1 : 0) );
				binaryFile.WriteByte( (byte)(unprocessedChanges.mainSearchFiles ? 1 : 0) );

				// [NumberSet: Source File IDs to Rebuild]
				// [NumberSet: Class IDs to Rebuild]
				// [NumberSet: Style File IDs to Rebuild]
				// [NumberSet: Source File IDs with Content]
				// [NumberSet: Class IDs with Content]

				binaryFile.WriteNumberSet(unprocessedChanges.sourceFiles);
				binaryFile.WriteNumberSet(unprocessedChanges.classes);
				binaryFile.WriteNumberSet(unprocessedChanges.styleFiles);
				binaryFile.WriteNumberSet(buildState.sourceFilesWithContent);
				binaryFile.WriteNumberSet(buildState.classesWithContent);

				// [StringSet: Search Prefixes to Rebuild]
				// [StringSet: Folders to Check for Deletion]

				binaryFile.WriteStringSet(unprocessedChanges.searchPrefixes);
				binaryFile.WriteStringSet(unprocessedChanges.possiblyEmptyFolders);

				// [String: Menu Data File Type] [NumberSet: Menu Data File Numbers]
				// [String: Menu Data File Type] [NumberSet: Menu Data File Numbers]
				// ...
				// [String: null]

				foreach (var menuDataFilePair in buildState.usedMenuDataFiles)
					{
					switch (menuDataFilePair.Key)
						{
						case Hierarchy.File:
							binaryFile.WriteString("files");
							break;
						case Hierarchy.Class:
							binaryFile.WriteString("classes");
							break;
						case Hierarchy.Database:
							binaryFile.WriteString("database");
							break;
						default:
							throw new NotImplementedException();
						}

					binaryFile.WriteNumberSet(menuDataFilePair.Value);
					}

				binaryFile.WriteString(null);
				}
			}

		}
	}