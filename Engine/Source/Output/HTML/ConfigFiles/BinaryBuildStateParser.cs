﻿/*
 * Class: CodeClear.NaturalDocs.Engine.Output.HTML.ConfigFiles.BinaryBuildStateParser
 * ____________________________________________________________________________
 *
 * A class to handle loading and saving <BuildState.nd>.
 *
 *
 * Threading: Not Thread Safe
 *
 *		The parser object may be reused, but multiple threads cannot use it at the same time.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2023 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using CodeClear.NaturalDocs.Engine.Collections;
using CodeClear.NaturalDocs.Engine.IDObjects;


namespace CodeClear.NaturalDocs.Engine.Output.HTML.ConfigFiles
	{
	public class BinaryBuildStateParser
		{

		// Group: Functions
		// __________________________________________________________________________


		/* Constructor: BinaryBuildStateParser
		 */
		public BinaryBuildStateParser ()
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
				if (binaryFile.OpenForReading(filename) == false)
					{
					result = false;
					}
				else if (!binaryFile.Version.IsAtLeastRelease("2.3") &&  // Rebuild all output for 2.3
						  !binaryFile.Version.IsSamePreRelease(Engine.Instance.Version))
					{
					binaryFile.Close();
					result = false;
					}
				else
					{

					// Build Flags

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


					// Build Sets

					// [NumberSet: Source File IDs to Rebuild]
					// [NumberSet: Class IDs to Rebuild]
					// [NumberSet: Image File IDs to Rebuild]
					// [NumberSet: Style File IDs to Rebuild]

					unprocessedChanges.sourceFiles.ReadFrom(binaryFile);
					unprocessedChanges.classes.ReadFrom(binaryFile);
					unprocessedChanges.imageFiles.ReadFrom(binaryFile);
					unprocessedChanges.styleFiles.ReadFrom(binaryFile);

					// [StringSet: Search Prefixes to Rebuild]

					unprocessedChanges.searchPrefixes.ReadFrom(binaryFile);


					// Cleanup Sets

					// [NumberSet: Unchanged Image File Use Check IDs]
					// [StringSet: Folders to Check for Deletion]

					unprocessedChanges.unchangedImageFileUseChecks.ReadFrom(binaryFile);
					unprocessedChanges.possiblyEmptyFolders.ReadFrom(binaryFile);


					// Content Info

					// [NumberSet: Source File IDs with Content]
					// [NumberSet: Class IDs with Content]
					// [NumberSet: Used Image File IDs]

					buildState.sourceFilesWithContent.ReadFrom(binaryFile);
					buildState.classesWithContent.ReadFrom(binaryFile);
					buildState.usedImageFiles.ReadFrom(binaryFile);


					// Menu Data File Info

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


					// Project Info

					// [String: Calculated Home Page Path (absolute) or null]
					// [Int64: Home Page Last Modification Time in ticks or 0]
					// [String: Generated Timestamp or null]
					// [Byte: Home Page Uses Timestamp (0 or 1)]

					buildState.CalculatedHomePage = (AbsolutePath)binaryFile.ReadString();
					buildState.HomePageLastModified = new DateTime(binaryFile.ReadInt64());
					buildState.GeneratedTimestamp = binaryFile.ReadString();
					buildState.HomePageUsesTimestamp = (binaryFile.ReadByte() != 0);
					}
				}
			catch
				{
				result = false;
				}
			finally
				{
				if (binaryFile.IsOpen)
					{  binaryFile.Close();  }
				}

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

				// Build Flags

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


				// Build Sets

				// [NumberSet: Source File IDs to Rebuild]
				// [NumberSet: Class IDs to Rebuild]
				// [NumberSet: Image File IDs to Rebuild]
				// [NumberSet: Style File IDs to Rebuild]

				binaryFile.WriteNumberSet(unprocessedChanges.sourceFiles);
				binaryFile.WriteNumberSet(unprocessedChanges.classes);
				binaryFile.WriteNumberSet(unprocessedChanges.imageFiles);
				binaryFile.WriteNumberSet(unprocessedChanges.styleFiles);

				// [StringSet: Search Prefixes to Rebuild]

				binaryFile.WriteStringSet(unprocessedChanges.searchPrefixes);


				// Cleanup Sets

				// [NumberSet: Unchanged Image File Use Check IDs]
				// [StringSet: Folders to Check for Deletion]

				binaryFile.WriteNumberSet(unprocessedChanges.unchangedImageFileUseChecks);
				binaryFile.WriteStringSet(unprocessedChanges.possiblyEmptyFolders);


				// Content Info

				// [NumberSet: Source File IDs with Content]
				// [NumberSet: Class IDs with Content]
				// [NumberSet: Used Image File IDs]

				binaryFile.WriteNumberSet(buildState.sourceFilesWithContent);
				binaryFile.WriteNumberSet(buildState.classesWithContent);
				binaryFile.WriteNumberSet(buildState.usedImageFiles);


				// Menu Data File Info

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


				// Project Info

				// [String: Calculated Home Page Path (absolute) or null]
				// [Int64: Home Page Last Modification Time in ticks or 0]
				// [String: Generated Timestamp or null]
				// [Byte: Home Page Uses Timestamp (0 or 1)]

				binaryFile.WriteString(buildState.CalculatedHomePage);
				binaryFile.WriteInt64(buildState.HomePageLastModified.Ticks);
				binaryFile.WriteString(buildState.GeneratedTimestamp);
				binaryFile.WriteByte( (byte)(buildState.HomePageUsesTimestamp ? 1 : 0) );
				}
			}

		}
	}
