/* 
 * Class: CodeClear.NaturalDocs.Engine.Output.HTML.DataFiles.BuildStateFileParser
 * ____________________________________________________________________________
 * 
 * A class to handle loading and saving <BuildState.nd>.
 * 
 * 
 * Threading: Not Thread Safe
 * 
 *		The parser object may be reused, but multiple threads cannot use it at the same time.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2021 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using CodeClear.NaturalDocs.Engine.Collections;
using CodeClear.NaturalDocs.Engine.IDObjects;


namespace CodeClear.NaturalDocs.Engine.Output.HTML.DataFiles
	{
	public class BuildStateFileParser
		{
		
		// Group: Functions
		// __________________________________________________________________________
		
		
		/* Constructor: BuildStateFileParser
		 */
		public BuildStateFileParser ()
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