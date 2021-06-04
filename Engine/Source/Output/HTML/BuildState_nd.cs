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
 *		> [Byte: Need to Build Home Page (0 or 1)]
 *		> [Byte: Need to Build Main Style Files (0 or 1)]
 *		> [Byte: Need to Build Menu (0 or 1)]
 *		> [Byte: Need to Build Main Search Files (0 or 1)]
 *		
 *		Flags for some of the structural items that need to be built.
 * 
 *		> [NumberSet: Source File IDs to Rebuild]
 *		> [NumberSet: Class IDs to Rebuild]
 *		> [NumberSet: Image File IDs to Rebuild]
 *		> [NumberSet: Style File IDs to Rebuild]
 *		
 *		The files that needed to be rebuilt but weren't yet.  If the last build was run to completion these should 
 *		be empty sets, though if the build was interrupted this will have the ones left to do.
 *		
 *		> [NumberSet: Source File IDs with Content]
 *		> [NumberSet: Class IDs with Content]
 *		
 *		A set of all the source and class files known to have content after all filters were applied.
 *		
 *		> [NumberSet: Used Image File IDs]
 *		
 *		A set of all the image file IDs which were used in the output.
 *		
 *		> [NumberSet: Unchanged Image File Use Check IDs]
 *		
 *		A set of all the image file IDs that haven't changed but whether they're used in the output may have.
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
 *		> [String: File Menu Data File Identifier] [NumberSet: File Menu Data File Numbers]
 * 
 *		> [Int32: Hierarchy ID] [String: Menu Data File Identifier] [NumberSet: Menu Data File Numbers]
 *		> [Int32: Hierarchy ID] [String: Menu Data File Identifier] [NumberSet: Menu Data File Numbers]
 *		> ...
 *		> [Int32: 0]
 *		
 *		File identifiers and <NumberSets> of the data files that were created when the menus were built, so if the file
 *		menu created files.js, files2.js, and files3.js, it will store "files" and {1-3}.  This allows us to clean up old data 
 *		files if we're using fewer than before.  Hierarchy menus are stored as Int32-String-NumberSet groups that repeat 
 *		until there's a hierarchy ID of zero.
 *		
 *		> [String: Path to Home Page or null]
 *		
 *		The custom home page file, or null if using the default.
 *		
 *		> [String: Generated Timestamp or null]
 *		
 *		The timestamp used the last time the output was built.  This is the generated result, so "Updated January 1, 2020", 
 *		and not the code like "Updated month d, yyyy".
 *		
 *		> [Byte: Home Page Uses Timestamp (0 or 1)]
 *		
 *		Whether the home page uses the generated timestamp or not.
 *		
 *		
 *		Version History:
 *		
 *			- 2.2
 *				- Added Need to Build Home Page.
 *				- Added Path to Home Page, Generated Timestamp, and Home Page Uses Timestamp.
 *				- Changed how menu data file information is stored.
 *		
 *			- 2.1
 *				- Added Used Image File IDs, Image Files to Rebuild, and Unchanged Image File Use Check IDs.
 *			
 *			- 2.0.2
 *				- Added Style File IDs to Rebuild.
 *			
 */

// This file is part of Natural Docs, which is Copyright © 2003-2021 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using CodeClear.NaturalDocs.Engine.Collections;
using CodeClear.NaturalDocs.Engine.IDObjects;


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
				if (binaryFile.OpenForReading(filename, "2.2") == false)
					{  result = false;  }
				else
					{
					// [Byte: Need to Build Frame Page (0 or 1)]
					// [Byte: Need to Build Home Page (0 or 1)]
					// [Byte: Need to Build Main Style Files (0 or 1)]
					// [Byte: Need to Build Menu (0 or 1)]
					// [Byte: Need to Build Main Search Files (0 or 1)]

					unprocessedChanges.framePage = (binaryFile.ReadByte() == 1);
					unprocessedChanges.homePage = (binaryFile.ReadByte() == 1);
					unprocessedChanges.mainStyleFiles = (binaryFile.ReadByte() == 1);
					unprocessedChanges.menu = (binaryFile.ReadByte() == 1);
					unprocessedChanges.mainSearchFiles = (binaryFile.ReadByte() == 1);

					// [NumberSet: Source File IDs to Rebuild]
					// [NumberSet: Class IDs to Rebuild]
					// [NumberSet: Image File IDs to Rebuild]
					// [NumberSet: Style File IDs to Rebuild]
					// [NumberSet: Source File IDs with Content]
					// [NumberSet: Class IDs with Content]
					// [NumberSet: Used Image File IDs]
					// [NumberSet: Unchanged Image File Use Check IDs]

					unprocessedChanges.sourceFiles.ReadFrom(binaryFile);
					unprocessedChanges.classes.ReadFrom(binaryFile);
					unprocessedChanges.imageFiles.ReadFrom(binaryFile);
					unprocessedChanges.styleFiles.ReadFrom(binaryFile);
					buildState.sourceFilesWithContent.ReadFrom(binaryFile);
					buildState.classesWithContent.ReadFrom(binaryFile);
					buildState.usedImageFiles.ReadFrom(binaryFile);
					unprocessedChanges.unchangedImageFileUseChecks.ReadFrom(binaryFile);

					// [StringSet: Search Prefixes to Rebuild]
					// [StringSet: Folders to Check for Deletion]

					unprocessedChanges.searchPrefixes.ReadFrom(binaryFile);
					unprocessedChanges.possiblyEmptyFolders.ReadFrom(binaryFile);

					// [String: File Menu Data File Identifier] [NumberSet: File Menu Data File Numbers]
					
					string fileMenuDataFileIdentifier = binaryFile.ReadString();
					NumberSet fileMenuDataFileNumbers = binaryFile.ReadNumberSet();

					if (!fileMenuDataFileNumbers.IsEmpty)
						{  
						buildState.fileMenuInfo = new BuildState.MenuInfo(fileMenuDataFileIdentifier, fileMenuDataFileNumbers);
						}

					// [Int32: Hierarchy ID] [String: Menu Data File Identifier] [NumberSet: Menu Data File Numbers]
					// [Int32: Hierarchy ID] [String: Menu Data File Identifier] [NumberSet: Menu Data File Numbers]
					// ...
					// [Int32: 0]

					int hierarchyID = binaryFile.ReadInt32();

					while (hierarchyID != 0)
						{
						if (buildState.hierarchyMenuInfo == null)
							{  buildState.hierarchyMenuInfo = new List<BuildState.MenuInfo>();  }

						buildState.hierarchyMenuInfo.Add(
							new BuildState.MenuInfo(binaryFile.ReadString(), binaryFile.ReadNumberSet(), hierarchyID)
							);

						hierarchyID = binaryFile.ReadInt32();
						}

					// [String: Path to Home Page or null]
					// [String: Generated Timestamp or null]
					// [Byte: Home Page Uses Timestamp (0 or 1)]

					buildState.HomePage = binaryFile.ReadString();
					buildState.GeneratedTimestamp = binaryFile.ReadString();
					buildState.HomePageUsesTimestamp = (binaryFile.ReadByte() != 0);
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
				// [Byte: Need to Build Home Page (0 or 1)]
				// [Byte: Need to Build Main Style Files (0 or 1)]
				// [Byte: Need to Build Menu (0 or 1)]
				// [Byte: Need to Build Main Search Files (0 or 1)]

				binaryFile.WriteByte( (byte)(unprocessedChanges.framePage ? 1 : 0) );
				binaryFile.WriteByte( (byte)(unprocessedChanges.homePage ? 1 : 0) );
				binaryFile.WriteByte( (byte)(unprocessedChanges.mainStyleFiles ? 1 : 0) );
				binaryFile.WriteByte( (byte)(unprocessedChanges.menu ? 1 : 0) );
				binaryFile.WriteByte( (byte)(unprocessedChanges.mainSearchFiles ? 1 : 0) );

				// [NumberSet: Source File IDs to Rebuild]
				// [NumberSet: Class IDs to Rebuild]
				// [NumberSet: Image File IDs to Rebuild]
				// [NumberSet: Style File IDs to Rebuild]
				// [NumberSet: Source File IDs with Content]
				// [NumberSet: Class IDs with Content]
				// [NumberSet: Used Image File IDs]
				// [NumberSet: Unchanged Image File Use Check IDs]

				binaryFile.WriteNumberSet(unprocessedChanges.sourceFiles);
				binaryFile.WriteNumberSet(unprocessedChanges.classes);
				binaryFile.WriteNumberSet(unprocessedChanges.imageFiles);
				binaryFile.WriteNumberSet(unprocessedChanges.styleFiles);
				binaryFile.WriteNumberSet(buildState.sourceFilesWithContent);
				binaryFile.WriteNumberSet(buildState.classesWithContent);
				binaryFile.WriteNumberSet(buildState.usedImageFiles);
				binaryFile.WriteNumberSet(unprocessedChanges.unchangedImageFileUseChecks);

				// [StringSet: Search Prefixes to Rebuild]
				// [StringSet: Folders to Check for Deletion]

				binaryFile.WriteStringSet(unprocessedChanges.searchPrefixes);
				binaryFile.WriteStringSet(unprocessedChanges.possiblyEmptyFolders);

				// [String: File Menu Data File Identifier] [NumberSet: File Menu Data File Numbers]

				if (buildState.fileMenuInfo != null)
					{
					binaryFile.WriteString(buildState.fileMenuInfo.DataFileIdentifier);
					binaryFile.WriteNumberSet(buildState.fileMenuInfo.UsedDataFileNumbers);
					}
				else
					{
					binaryFile.WriteString(null);
					binaryFile.WriteNumberSet( new NumberSet() );
					}

				// [Int32: Hierarchy ID] [String: Menu Data File Identifier] [NumberSet: Menu Data File Numbers]
				// [Int32: Hierarchy ID] [String: Menu Data File Identifier] [NumberSet: Menu Data File Numbers]
				// ...
				// [Int32: 0]

				if (buildState.hierarchyMenuInfo != null)
					{
					foreach (var hierarchyMenu in buildState.hierarchyMenuInfo)
						{
						binaryFile.WriteInt32(hierarchyMenu.HierarchyID);
						binaryFile.WriteString(hierarchyMenu.DataFileIdentifier);
						binaryFile.WriteNumberSet(hierarchyMenu.UsedDataFileNumbers);
						}
					}

				binaryFile.WriteInt32(0);

				// [String: Path to Home Page or null]
				// [String: Generated Timestamp or null]
				// [Byte: Home Page Uses Timestamp (0 or 1)]

				binaryFile.WriteString(buildState.HomePage);
				binaryFile.WriteString(buildState.GeneratedTimestamp);
				binaryFile.WriteByte( (byte)(buildState.HomePageUsesTimestamp ? 1 : 0) );
				}
			}

		}
	}