/* 
 * Class: CodeClear.NaturalDocs.Engine.BinaryFile
 * ____________________________________________________________________________
 * 
 * A class to handle reading standard Natural Docs binary configuration files.  This class does NOT support binary files prior
 * to version 2.0.  Supporting those files isn't really necessary due to the vast changes appearing with 2.0.
 * 
 * Rationale over using BinaryReader/Writer:
 * 
 *		- Handles the header and version string.
 *		- Allows null strings to be written.  Never returns empty strings, only nulls.
 * 
 * 
 * Topic: File Format
 * 
 *		Standard Header:
 *		
 *			All binary files from version 2.0 onwards start with the following sequence of bytes
 *			
 *			> C9 4E 44 CD 62 69 6E BC
 *			
 *			The 4x and 6x bytes spell out "ND" and "bin".
 *			
 *			Next is the file format version as a string.  After that comes the binary data in the format particular to that file.
 *			
 *		Types and Encoding:
 *		
 *			All data types are stored in the standard encodings provided by C#'s BinaryReader and BinaryWriter.  This means
 *			multibyte integers are stored in little endian and strings are stored in UTF-8 and are preceded by a character count
 *			in .NET's 7-bit encoding scheme.
 *			
 */

// This file is part of Natural Docs, which is Copyright © 2003-2021 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.IO;


namespace CodeClear.NaturalDocs.Engine
	{
	public class BinaryFile : IDisposable
		{

		// Group: Constants
		// __________________________________________________________________________


		/* Constant: BinaryHeader
		 * The first bytes that must appear in a binary file for it to be seen as being in Natural Docs' format.
		 */
		private const ulong BinaryHeader = 0xBC6E6962CD444EC9UL;


		
		// Group: Functions
		// __________________________________________________________________________
		
		
		/* Constructor: BinaryFile
		 * Creates the object.  Does not open a file.
		 */
		public BinaryFile ()
			{
			fileStream = null;
			fileReader = null;
			fileWriter = null;

			fileName = null;
			version = null;
			}
			
		
		/* Function: OpenForReading
		 * Attempts to open the passed binary file for reading and returns whether it was successful.  Since these files are
		 * considered disposable (i.e. they can be regenerated as necessary) it doesn't take an error list.  The calling code is
		 * assumed to just handle the condition silently.
		 */
		public bool OpenForReading (Path newFileName)
			{
			if (IsOpen)
				{  throw new Engine.Exceptions.FileAlreadyOpen(newFileName, fileName);  }

			try
				{  
				fileStream = new FileStream(newFileName, FileMode.Open);
				}
			catch (Exception e)
				{
				if (e is FileNotFoundException || e is DirectoryNotFoundException)
					{  return false;  }
				else
					{  throw;  }
				}
				
			fileName = newFileName;
			fileReader = new BinaryReader(fileStream, System.Text.Encoding.UTF8);
			
			try
				{
				ulong header = ReadUInt64();
				string versionString = ReadString();

				if (header != BinaryHeader ||
					Version.TryParse(versionString, out version) == false)
					{
					Close();
					return false;
					}
				}
			catch
				{
				Close();
				return false;
				}
							
			return true;
			}
			
			
		/* Function: OpenForWriting
		 * 
		 * Attempts to open the passed binary file for writing.  Throws exceptions on failure.
		 */
		public void OpenForWriting (Path newFileName)
			{
			if (IsOpen)
				{  throw new Engine.Exceptions.FileAlreadyOpen(newFileName, fileName);  }

			try
				{  
				fileStream = new FileStream(newFileName, FileMode.Create);
				}
			catch (Exception e)
				{
				throw new Engine.Exceptions.UserFriendly ( 
					Locale.Get("NaturalDocs.Engine", "Error.CouldNotWriteToDataFile(name, exception)", newFileName, e.Message)
					);
				}
				
			fileName = newFileName;
			fileWriter = new BinaryWriter(fileStream, System.Text.Encoding.UTF8);
			
			WriteUInt64(BinaryHeader);
			WriteString(Engine.Instance.VersionString);
			}
			
			
			
		/* Function: Close
		 * Closes the file if one was open.
		 */
		public void Close ()
			{
			if (IsOpen)
				{
				Dispose();
					
				fileStream = null;
				fileReader = null;
				fileWriter = null;

				fileName = null;
				version = null; 
				}
			}
			

		/* Functions: Reading Functions
		 * Returns the specified type from a binary file open for reading.
		 * 
		 * ReadInt8 - Reads a signed 8-bit integer.
		 * ReadUInt8 - Reads an unsigned 8-bit integer.
		 * ReadByte - Reads an unsigned 8-bit integer.
		 * ReadInt16 - Reads a signed 16-bit integer.
		 * ReadUInt16 - Reads an unsigned 16-bit integer.
		 * ReadInt32 - Reads a signed 32-bit integer.
		 * ReadUInt32 - Reads an unsigned 32-bit integer.
		 * ReadInt64 - Reads a signed 64-bit integer.
		 * ReadUInt64 - Reads an unsigned 64-bit integer.
		 * ReadString - Reads a string.  Returns null for empty strings.
		 */
		public sbyte ReadInt8()
			{  return fileReader.ReadSByte();  }
		public byte ReadUInt8()
			{  return fileReader.ReadByte();  }
		public byte ReadByte()
			{  return fileReader.ReadByte();  }
		public short ReadInt16()
			{  return fileReader.ReadInt16();  }
		public ushort ReadUInt16()
			{  return fileReader.ReadUInt16();  }
		public int ReadInt32()
			{  return fileReader.ReadInt32();  }
		public uint ReadUInt32()
			{  return fileReader.ReadUInt32();  }
		public long ReadInt64()
			{  return fileReader.ReadInt64();  }
		public ulong ReadUInt64()
			{  return fileReader.ReadUInt64();  }

		public string ReadString()
			{
			// Zero length strings are returned as empty strings, but we want them to be nulls instead.
			string result = fileReader.ReadString();  
			
			if (result == "")
				{  return null;  }
				
			return result;
			}

		/* Functions: Skip
		 * Skips ahead the passed number of bytes without reading them.
		 */
		public void Skip (int bytes)
			{
			fileStream.Seek(bytes, SeekOrigin.Current);
			}

		/* Functions: Writing Functions
		 * Writes the specified type to a binary file open for writing.
		 * 
		 * WriteInt8 - Writes a signed 8-bit integer.
		 * WriteUInt8 - Writes an unsigned 8-bit integer.
		 * WriteByte - Writes an unsigned 8-bit integer.
		 * WriteInt16 - Writes a signed 16-bit integer.
		 * WriteUInt16 - Writes an unsigned 16-bit integer.
		 * WriteInt32 - Writes a signed 32-bit integer.
		 * WriteUInt32 - Writes an unsigned 32-bit integer.
		 * WriteInt64 - Writes a signed 64-bit integer.
		 * WriteUInt64 - Writes an unsigned 64-bit integer.
		 * WriteString - Writes a string.  Empty strings are encoded as nulls.
		 */
		public void WriteInt8 (sbyte value)
			{  fileWriter.Write(value);  }
		public void WriteUInt8 (byte value)
			{  fileWriter.Write(value);  }
		public void WriteByte (byte value)
			{  fileWriter.Write(value);  }
		public void WriteInt16 (short value)
			{  fileWriter.Write(value);  }
		public void WriteUInt16 (ushort value)
			{  fileWriter.Write(value);  }
		public void WriteInt32 (int value)
			{  fileWriter.Write(value);  }
		public void WriteUInt32 (uint value)
			{  fileWriter.Write(value);  }
		public void WriteInt64 (long value)
			{  fileWriter.Write(value);  }
		public void WriteUInt64 (ulong value)
			{  fileWriter.Write(value);  }

		public void WriteString (string value)
			{
			// It throws an exception if you pass null, so encode an empty string instead.
			if (value == null)
				{  value = "";  }
				
			fileWriter.Write(value);  
			}



		// Group: IDisposable Functions
		// __________________________________________________________________________
		
			
		/* Function: Dispose
		 */
		public void Dispose ()
			{
			if (fileReader != null)
				{
				fileReader.Close();
				fileReader = null;
				}
			if (fileWriter != null)
				{
				fileWriter.Close();
				fileWriter = null;
				}
			if (fileStream != null)
				{
				fileStream.Close();
				fileStream = null;
				}
			}
			
			
			
		// Group: Properties
		// __________________________________________________________________________
		
		
		/* Property: IsOpen
		 * Whether the class has a file open.
		 */
		public bool IsOpen
			{
			get
				{  return (fileName != null);  }
			}
			
			
		/* Property: Version
		 * The <Engine.Version> of the file if one is open, null otherwise.
		 */
		public Engine.Version Version
			{
			get
				{  return version;  }
			}
			
			
			
		
		// Group: Variables
		// __________________________________________________________________________
		
		
		/* var: fileStream
		 * The FileStream used if a file is open, or null if not.
		 */
		protected FileStream fileStream;
		
		
		/* var: fileReader
		 * The BinaryReader used if there is a file open for reading, or null if not.
		 */
		protected BinaryReader fileReader;
		
		
		/* var: fileWriter
		 * The BinaryWriter used if there is a file open for writing, or null if not.
		 */
		protected BinaryWriter fileWriter;
		
		
		/* var: fileName
		 * The <Path> of the file currently being parsed, or null if none.
		 */
		protected Path fileName;
		
		
		/* var: version
		 * The version of the file if one is open, null otherwise.
		 */
		protected Engine.Version version;
								
		}
	}