/*
 * Class: CodeClear.NaturalDocs.Engine.Files.ConfigFiles.BinaryFileParser
 * ____________________________________________________________________________
 *
 * A class to handle loading and saving <Files.nd>.
 *
 *
 * Threading: Not Thread Safe
 *
 *		The parser object may be reused, but multiple threads cannot use it at the same time.
 *
 */

// This file is part of Natural Docs, which is Copyright © 2003-2025 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;


namespace CodeClear.NaturalDocs.Engine.Files.ConfigFiles
	{
	public class BinaryFileParser
		{

		// Group: Functions
		// __________________________________________________________________________


		/* Constructor: BinaryFileParser
		 */
		public BinaryFileParser ()
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
				if (binaryFile.OpenForReading(filename) == false)
					{
					result = false;
					}
				// We'll continue to handle 2.0 files in later versions since it's easy enough
				else if (binaryFile.Version.IsAtLeastRelease("2.0") == false &&
						   binaryFile.Version.IsSamePreRelease(Engine.Instance.Version) == false)
					{
					binaryFile.Close();
					result = false;
					}
				else
					{
					bool forceReparse = (binaryFile.Version < "2.3.1");
					bool didntStoreImageDimensions = (binaryFile.Version < "2.0.2");
					bool didntStoreEncodingID = (binaryFile.Version < "2.2");

					int id;
					AbsolutePath path;
					FileType type;
					DateTime lastModification;
					DateTime lastModification_ForceReparse = new DateTime(0);
					int characterEncodingID;
					File file;
					uint width, height;

					for (;;)
						{
						// [Int32: ID]
						//    (properties)
						// ...
						// [Int32: 0]

						id = binaryFile.ReadInt32();

						if (id == 0)
							{  break;  }

						// [String: Absolute Path]
						// [Byte: Type]

						path = binaryFile.ReadString();
						type = (FileType)binaryFile.ReadByte();

						// [Int64: Last Modification in Ticks or 0]

						if (forceReparse)
							{
							lastModification = lastModification_ForceReparse;
							binaryFile.Skip(8);
							}
						else
							{
							lastModification = new DateTime(binaryFile.ReadInt64());
							}

						// [Int32: Character Encoding ID or 0]

						if (didntStoreEncodingID)
							{  characterEncodingID = 0;  }
						else
							{  characterEncodingID = binaryFile.ReadInt32();  }

						// (if image)
						//    [UInt32: Width in Pixels or 0 if unknown]
						//    [UInt32: Height in Pixels or 0 if unknown]

						if (type == FileType.Image)
							{
							if (didntStoreImageDimensions)
								{
								width = 0;
								height = 0;
								}
							else
								{
								width = binaryFile.ReadUInt32();
								height = binaryFile.ReadUInt32();
								}

							if (width == 0 || height == 0)
								{
								// If this file is from a different version of Natural Docs, no matter which one, reset the last modification
								// time so they'll be reparsed and take another stab at getting the dimensions
								if (binaryFile.Version != Engine.Instance.Version)
									{  lastModification = lastModification_ForceReparse;  }

								file = new ImageFile(path, lastModification);
								}
							else
								{  file = new ImageFile(path, lastModification, width, height);  }
							}
						else
							{
							file = new File(path, type, lastModification, characterEncodingID);
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
				if (binaryFile.IsOpen)
					{  binaryFile.Close();  }
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
					// [String: Absolute Path]
					// [Byte: Type]
					// [Int64: Last Modification in Ticks or 0]
					// [Int32: Character Encoding ID or 0]

					binaryFile.WriteInt32(file.ID);
					binaryFile.WriteString(file.FileName);
					binaryFile.WriteByte((byte)file.Type);
					binaryFile.WriteInt64(file.LastModified.Ticks);
					binaryFile.WriteInt32(file.CharacterEncodingID);

					// (if image)
					//    [UInt32: Width in Pixels or 0 if unknown]
					//    [UInt32: Height in Pixels or 0 if unknown]

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
