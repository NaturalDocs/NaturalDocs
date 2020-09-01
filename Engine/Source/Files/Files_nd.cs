/* 
 * Class: CodeClear.NaturalDocs.Engine.Files.Files_nd
 * ____________________________________________________________________________
 * 
 * A class to handle loading and saving <Files.nd>.
 * 
 * 
 * Threading: Not Thread Safe
 * 
 *		The parser object may be reused, but multiple threads cannot use it at the same time.
 *		
 * 
 * File: Files.nd
 * 
 *		A binary file which stores the state of the source files as of Natural Docs' last run.
 *		
 *		> [[Binary Header]]
 *		
 *		The file starts with the standard binary file header as managed by <BinaryFile>.
 *		
 *		> [Int32: ID]
 *		> [String: Path]
 *		> [Byte: Type]
 *		> [Int64: Last Modification in Ticks or 0]
 *		> (if image)
 *		>    [UInt32: Width in Pixels or 0 if unknown]
 *		>    [UInt32: Height in Pixels or 0 if unknown]
 *		> ...
 *		> [Int32: 0]
 *		
 *		For each file it stores the ID number, the absolute path, <FileType>, and the last modification time in ticks.  If the file 
 *		wasn't fully processed when Natural Docs shut down, either due to a change or a deletion, the tick count will be zero to 
 *		indicate that it should be processed again.
 *		
 *		This continues until there is an ID number of zero.
 *			
 *		Revisions:
 *		
 *			- 2.0.2
 *				- Added dimensions for image files.  They will always be zero because image file support was only partially
 *				  implemented and it would have been too much effort to back it out for 2.0.2.
 *		
 *			- 2.0
 *				- The file was introduced.
 *			
 */

// This file is part of Natural Docs, which is Copyright © 2003-2020 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;


namespace CodeClear.NaturalDocs.Engine.Files
	{
	public class Files_nd
		{
		
		// Group: Functions
		// __________________________________________________________________________
		
		
		/* Constructor: Files_nd
		 */
		public Files_nd ()
			{
			}


		/* Function: Load
		 * Loads <Files.nd> and returns whether it was successful.  If it wasn't it will still return valid objects, they will just
		 * be empty.
		 */
		public bool Load (Path filename, out IDObjects.Manager<File> files)
			{
			files = new IDObjects.Manager<File>(Config.Manager.KeySettingsForPaths, false );

			BinaryFile binaryFile = new BinaryFile();
			bool result = true;
			
			try
				{
				// We'll continue to handle 2.0 files in 2.0.2 since it's easy enough
				if (binaryFile.OpenForReading(filename, "2.0") == false)
					{
					result = false;
					}
				else
					{
					// [Int32: ID]
					// [String: Path]
					// [Byte: Type]
					// [Int64: Last Modification in Ticks or 0]
					// (if image)
					//    [UInt32: Width in Pixels or 0 if unknown]
					//    [UInt32: Height in Pixels or 0 if unknown]
 					// ...
					// [Int32: 0]
					
					int id;
					Path path;
					FileType type;
					DateTime lastModification;
					File file;
					uint width, height;
					
					for (;;)
						{
						id = binaryFile.ReadInt32();
						
						if (id == 0)
							{  break;  }
							
						path = binaryFile.ReadString();
						type = (FileType)binaryFile.ReadByte();
						lastModification = new DateTime(binaryFile.ReadInt64());
						
						if (type == FileType.Image)
							{
							if (binaryFile.Version < "2.0.2")
								{
								width = 0;
								height = 0;

								// Reset last modification time so they'll be reparsed
								lastModification = new DateTime(0);
								}
							else
								{
								width = binaryFile.ReadUInt32();
								height = binaryFile.ReadUInt32();
								}

							if (width == 0 || height == 0)
								{  file = new ImageFile(path, lastModification);  }
							else
								{  file = new ImageFile(path, lastModification, width, height);  }
							}
						else
							{
							file = new File(path, type, lastModification);
							}

						file.ID = id;
						files.Add(file);
						}
					}
				}
			catch
				{
				result = false;
				}
			finally
				{  
				binaryFile.Close();  
				}
				
			if (result == false)
				{  files.Clear();  }
				
			return result;
			}
			
			
		/* Function: Save
		 * Saves the current state into <Files.nd>.  Throws an exception if unsuccessful.  All <Files> in the structure should have
		 * their last modification time set to tick count zero before calling this function.
		 */
		public void Save (Path filename, IDObjects.Manager<File> files)
			{
			BinaryFile binaryFile = new BinaryFile();
			binaryFile.OpenForWriting(filename);
			
			try
				{
				foreach (File file in files)
					{
					// [Int32: ID]
					// [String: Path]
					// [Byte: Type]
					// [Int64: Last Modification in Ticks or 0]
					// (if image)
					//    [UInt32: Width in Pixels or 0 if unknown]
					//    [UInt32: Height in Pixels or 0 if unknown]
					
					binaryFile.WriteInt32(file.ID);
					binaryFile.WriteString(file.FileName);
					binaryFile.WriteByte((byte)file.Type);
					binaryFile.WriteInt64(file.LastModified.Ticks);

					if (file.Type == FileType.Image)
						{
						ImageFile imageFile = (ImageFile)file;

						if (imageFile.DimensionsKnown)
							{
							binaryFile.WriteUInt32(imageFile.Width);
							binaryFile.WriteUInt32(imageFile.Height);
							}
						else
							{
							binaryFile.WriteUInt32(0);
							binaryFile.WriteUInt32(0);
							}
						}
					}

				// [Int32: 0]
				binaryFile.WriteInt32(0);
				}
			finally
				{  binaryFile.Close();  }
			}

		}
	}