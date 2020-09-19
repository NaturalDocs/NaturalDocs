/* 
 * Class: CodeClear.NaturalDocs.Engine.Files.ImageFileProcessor
 * ____________________________________________________________________________
 * 
 * A class containing functions to determine information about an image file.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2020 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.IO;


namespace CodeClear.NaturalDocs.Engine.Files
	{
	public static class ImageFileProcessor
		{

		// Group: Types
		// __________________________________________________________________________

		/* enum: Result
		 * 
		 * Success - The operation was successful.
		 *	FileDoesntExist - The file couldn't be opened because it doesn't exist.
		 *	CantAccessFile - The file exists but couldn't be opened, such as if the program doesn't have permission to access
		 *							the file.
		 *	IncorrectFormat - The operation couldn't complete because it was in an incorrect format.
		 */
		public enum Result : byte
			{
			Success, FileDoesntExist, CantAccessFile, IncorrectFormat
			}

		
		// Group: Functions
		// __________________________________________________________________________


		/* Function: GetDimensions
		 * Attempts to retrieve the dimensions of the passed image file, automatically choosing the correct function to use
		 * based on the file extension.  Returns whether it was successful.
		 */
		public static Result GetDimensions (Path imageFile, out uint width, out uint height)
			{
			string extension = imageFile.Extension.ToLower();

			if (extension == "gif")
				{  return GetGIFDimensions(imageFile, out width, out height);  }
			else if (extension == "png")
				{  return GetPNGDimensions(imageFile, out width, out height);  }
			else if (extension == "jpg" || extension == "jpeg")
				{  return GetJPGDimensions(imageFile, out width, out height);  }
			else if (extension == "svg")
				{  return GetSVGDimensions(imageFile, out width, out height);  }
			else if (extension == "bmp")
				{  return GetBMPDimensions(imageFile, out width, out height);  }
			else
				{
				width = 0;
				height = 0;
				return Result.IncorrectFormat;
				}
			}


		/* Function: GetGIFDimensions
		 * Attempts to retrieve the dimensions of the passed GIF file.  Returns whether it was successful.
		 */
		public static Result GetGIFDimensions (Path imageFile, out uint width, out uint height)
			{
			FileStream fileStream = null;
			BinaryReader binaryReader = null;

			width = 0;
			height = 0;

			// GIF Header:
			// 6 bytes - file signature: "GIF87a" or "GIF89a": 47 49 46 38 (37/39) 61
			// 2 bytes - width in pixels, little-endian
			// 2 bytes - height in pixels, little-endian

			try
				{
				// We only need 10 bytes
				fileStream = new FileStream(imageFile, FileMode.Open, FileAccess.Read, FileShare.Read, 10);
				}
			catch (FileNotFoundException)
				{  return Result.FileDoesntExist;  }
			catch (DirectoryNotFoundException)
				{  return Result.FileDoesntExist;  }
			catch
				{  return Result.CantAccessFile;  }

			try
				{
				binaryReader = new BinaryReader(fileStream);
				
				uint signature = binaryReader.ReadUInt32();

				// BinaryReader always uses little-endian
				if (signature != 0x38464947)
					{  return Result.IncorrectFormat;  }

				ushort version = binaryReader.ReadUInt16();

				if (version != 0x6139 && version != 0x6137)
					{  return Result.IncorrectFormat;  }

				// Can read the dimensions directly since GIF is little-endian
				width = binaryReader.ReadUInt16();
				height = binaryReader.ReadUInt16();

				if (width == 0 || height == 0)
					{
					width = 0;
					height = 0;
					return Result.IncorrectFormat;
					}

				return Result.Success;
				}
			catch
				{
				width = 0;
				height = 0;
				return Result.IncorrectFormat;
				}
			finally
				{
				if (binaryReader != null)
					{  binaryReader.Close();  }
				if (fileStream != null)
					{  
					fileStream.Close();
					fileStream.Dispose();
					}
				}
			}


		/* Function: GetPNGDimensions
		 * Attempts to retrieve the dimensions of the passed PNG file.  Returns whether it was successful.
		 */
		public static Result GetPNGDimensions (Path imageFile, out uint width, out uint height)
			{
			FileStream fileStream = null;
			BinaryReader binaryReader = null;

			width = 0;
			height = 0;

			// PNG Header:
			// 8 bytes - file signature: 89 50 4E 47 0D 0A 1A 0A
			// 4 bytes - length of first chunk's data, big-endian
			// 4 bytes - type of first chunk, which must be "IHDR" (49 48 44 52)
			// 4 bytes - width in pixels, big-endian
			// 4 bytes - height in pixels, big-endian

			try
				{
				// So we only need 24 bytes
				fileStream = new FileStream(imageFile, FileMode.Open, FileAccess.Read, FileShare.Read, 24);
				}
			catch (FileNotFoundException)
				{  return Result.FileDoesntExist;  }
			catch (DirectoryNotFoundException)
				{  return Result.FileDoesntExist;  }
			catch
				{  return Result.CantAccessFile;  }

			try
				{
				binaryReader = new BinaryReader(fileStream);
				
				ulong signature = binaryReader.ReadUInt64();

				// BinaryReader always uses little-endian
				if (signature != 0x0A1A0A0D474E5089)
					{  return Result.IncorrectFormat;  }

				// Skip the chunk length
				binaryReader.BaseStream.Seek(4, SeekOrigin.Current);

				uint chunkType = binaryReader.ReadUInt32();

				if (chunkType != 0x52444849)
					{  return Result.IncorrectFormat;  }

				// PNG is big-endian so we need to convert them manually
				byte[] dimensions = binaryReader.ReadBytes(8);
				
				width = (uint)(dimensions[0] << 24);
				width |= (uint)(dimensions[1] << 16);
				width |= (uint)(dimensions[2] << 8);
				width |= (uint)(dimensions[3]);

				height = (uint)(dimensions[4] << 24);
				height |= (uint)(dimensions[5] << 16);
				height |= (uint)(dimensions[6] << 8);
				height |= (uint)(dimensions[7]);

				if (width == 0 || height == 0)
					{
					width = 0;
					height = 0;
					return Result.IncorrectFormat;
					}

				return Result.Success;
				}
			catch
				{
				width = 0;
				height = 0;
				return Result.IncorrectFormat;
				}
			finally
				{
				if (binaryReader != null)
					{  binaryReader.Close();  }
				if (fileStream != null)
					{  
					fileStream.Close();
					fileStream.Dispose();
					}
				}
			}


		/* Function: GetBMPDimensions
		 * Attempts to retrieve the dimensions of the passed BMP file.  Returns whether it was successful.
		 */
		public static Result GetBMPDimensions (Path imageFile, out uint width, out uint height)
			{
			FileStream fileStream = null;
			BinaryReader binaryReader = null;

			width = 0;
			height = 0;

			// BMP Header:
			// 2 bytes - file signature: "BM": 42 4D
			// 4 bytes - size of file in bytes, little-endian
			// 2 bytes - reserved
			// 2 bytes - reserved
			// 4 bytes - offset to start of image data, little-endian
			// 4 bytes - size of header structure, little-endian
			// 4 bytes - width in pixels, little-endian
			// 4 bytes - height in pixels, little-endian

			try
				{
				// So we only need 26 bytes
				fileStream = new FileStream(imageFile, FileMode.Open, FileAccess.Read, FileShare.Read, 26);
				}
			catch (FileNotFoundException)
				{  return Result.FileDoesntExist;  }
			catch (DirectoryNotFoundException)
				{  return Result.FileDoesntExist;  }
			catch
				{  return Result.CantAccessFile;  }

			try
				{
				binaryReader = new BinaryReader(fileStream);
				
				ushort signature = binaryReader.ReadUInt16();

				// BinaryReader always uses little-endian
				if (signature != 0x4D42)
					{  return Result.IncorrectFormat;  }

				// Skip to the dimensions
				binaryReader.BaseStream.Seek(16, SeekOrigin.Current);

				// Can read the dimensions directly since BMP is little-endian
				width = binaryReader.ReadUInt32();
				height = binaryReader.ReadUInt32();

				if (width == 0 || height == 0)
					{
					width = 0;
					height = 0;
					return Result.IncorrectFormat;
					}

				return Result.Success;
				}
			catch
				{
				width = 0;
				height = 0;
				return Result.IncorrectFormat;
				}
			finally
				{
				if (binaryReader != null)
					{  binaryReader.Close();  }
				if (fileStream != null)
					{  
					fileStream.Close();
					fileStream.Dispose();
					}
				}
			}


		/* Function: GetJPGDimensions
		 * Attempts to retrieve the dimensions of the passed JPG file.  Returns whether it was successful.
		 */
		public static Result GetJPGDimensions (Path imageFile, out uint width, out uint height)
			{
			FileStream fileStream = null;
			BinaryReader binaryReader = null;

			width = 0;
			height = 0;

			// JPG Header:
			//
			// 2 bytes - start of image marker: FF D8
			// (not followed by a length and payload)
			//
			// 2 bytes - JFIF block marker: FF E0
			// 2 bytes - length of block payload, big-endian (counts these two bytes but not the block marker bytes)
			// 5 bytes - "JFIF" plus null terminator: 4A 46 49 46 00
			// ...
			//
			// 2 bytes - arbitrary block marker: FF [id byte]
			// 2 bytes - length of block payload, big-endian
			// ...
			//
			// 2 bytes - start of frame block marker: FF C0
			// 2 bytes - length of block payload, big-endian
			// 1 byte - sample precision
			// 2 bytes - height in pixels, big-endian
			// 2 bytes - width in pixels, big-endian
			// ...

			try
				{
				// 1kb should be plenty since there shouldn't be too much header before hitting the SOF block
				fileStream = new FileStream(imageFile, FileMode.Open, FileAccess.Read, FileShare.Read, 1024);
				}
			catch (FileNotFoundException)
				{  return Result.FileDoesntExist;  }
			catch (DirectoryNotFoundException)
				{  return Result.FileDoesntExist;  }
			catch
				{  return Result.CantAccessFile;  }

			try
				{
				binaryReader = new BinaryReader(fileStream);
				
				uint firstTwoMarkers = binaryReader.ReadUInt32();

				// BinaryReader always uses little-endian
				if (firstTwoMarkers != 0xE0FFD8FF)
					{  return Result.IncorrectFormat;  }

				int jfifBlockLength = (binaryReader.ReadByte() << 8) | binaryReader.ReadByte();
				uint jfifSignature = binaryReader.ReadUInt32();

				if (jfifSignature != 0x4649464A)
					{  return Result.IncorrectFormat;  }

				// Skip rest of block
				binaryReader.BaseStream.Seek(jfifBlockLength - 6, SeekOrigin.Current);

				// Skip blocks until we find start of frame: FF C0
				for (;;)
					{
					if (binaryReader.ReadByte() != 0xFF)
						{  return Result.IncorrectFormat;  }
					if (binaryReader.ReadByte() != 0xC0)
						{
						int blockLength = (binaryReader.ReadByte() << 8) | binaryReader.ReadByte();
						binaryReader.BaseStream.Seek(blockLength - 2, SeekOrigin.Current);
						}
					else
						{
						// If we're here, we found the start of frame header.

						// Skip to the dimensions.  We don't care about the length of the block because we're ending after this.
						binaryReader.BaseStream.Seek(3, SeekOrigin.Current);

						byte[] dimensions = binaryReader.ReadBytes(4);
				
						height = (uint)(dimensions[0] << 8);
						height |= (uint)(dimensions[1]);

						width = (uint)(dimensions[2] << 8);
						width |= (uint)(dimensions[3]);

						break;
						}
					}

				if (width == 0 || height == 0)
					{
					width = 0;
					height = 0;
					return Result.IncorrectFormat;
					}

				return Result.Success;
				}
			catch
				{
				width = 0;
				height = 0;
				return Result.IncorrectFormat;
				}
			finally
				{
				if (binaryReader != null)
					{  binaryReader.Close();  }
				if (fileStream != null)
					{  
					fileStream.Close();
					fileStream.Dispose();
					}
				}
			}


		/* Function: GetSVGDimensions
		 * Attempts to retrieve the pixel dimensions of the passed SVG file.  Returns whether it was successful.
		 */
		public static Result GetSVGDimensions (Path imageFile, out uint width, out uint height)
			{
			FileStream fileStream = null;
			StreamReader streamReader = null;

			width = 0;
			height = 0;

			// SVG opening tag:
			// <svg ... width="123" height="456" ...>
			//
			// May have other property="value" pairs before and after.
			// May not actually have width and height properties.
			// May have whitespace or <?xml ... ?> tag ahead of it.

			try
				{
				// 1kb should be plenty to find the root svg tag and its properties
				fileStream = new FileStream(imageFile, FileMode.Open, FileAccess.Read, FileShare.Read, 1024);
				}
			catch (FileNotFoundException)
				{  return Result.FileDoesntExist;  }
			catch (DirectoryNotFoundException)
				{  return Result.FileDoesntExist;  }
			catch
				{  return Result.CantAccessFile;  }

			try
				{
				streamReader = new StreamReader(fileStream);
		
				char[] buffer = new char[1024];
				int amountRead = streamReader.Read(buffer, 0, 1024);

				streamReader.Close();
				streamReader.Dispose();
				streamReader = null;

				fileStream.Close();
				fileStream.Dispose();
				fileStream = null;

				if (amountRead == 0)
					{  return Result.IncorrectFormat;  }

				string content = new string(buffer, 0, amountRead);

				// Quick and dirty find, since it's not the end of the world if we don't get the dimensions

				int svgTagStart = content.IndexOf("<svg", StringComparison.OrdinalIgnoreCase);

				if (svgTagStart == -1)
					{  return Result.IncorrectFormat;  }

				int svgTagEnd = content.IndexOf('>', svgTagStart + 4);

				if (svgTagEnd == -1)
					{  return Result.IncorrectFormat;  }

				int propertiesStart = svgTagStart + 4;
				int propertiesLength = svgTagEnd - svgTagStart - 4;

				int widthPropertyStart = content.IndexOf(" width=\"", propertiesStart, propertiesLength, StringComparison.OrdinalIgnoreCase);
				int heightPropertyStart = content.IndexOf(" height=\"", propertiesStart, propertiesLength, StringComparison.OrdinalIgnoreCase);

				if (widthPropertyStart == -1 || heightPropertyStart == -1)
					{  return Result.IncorrectFormat;  }

				int widthPropertyEnd = content.IndexOf('"', widthPropertyStart + 8, svgTagEnd - widthPropertyStart - 8);
				int heightPropertyEnd = content.IndexOf('"', heightPropertyStart + 9, svgTagEnd - heightPropertyStart - 9);

				if (widthPropertyEnd == -1 || heightPropertyEnd == -1)
					{  return Result.IncorrectFormat;  }

				string widthString = content.Substring(widthPropertyStart + 8, widthPropertyEnd - widthPropertyStart - 8);
				string heightString = content.Substring(heightPropertyStart + 9, heightPropertyEnd - heightPropertyStart - 9);

				if (widthString.EndsWith("px", StringComparison.OrdinalIgnoreCase))
					{  widthString = widthString.Substring(0, widthString.Length - 2);  }
				if (heightString.EndsWith("px", StringComparison.OrdinalIgnoreCase))
					{  heightString = heightString.Substring(0, heightString.Length - 2);  }

				if (!uint.TryParse(widthString, out width) || 
					!uint.TryParse(heightString, out height) ||
					width == 0 || height == 0)
					{
					width = 0;
					height = 0;
					return Result.IncorrectFormat;
					}

				return Result.Success;
				}
			catch
				{
				width = 0;
				height = 0;
				return Result.IncorrectFormat;
				}
			finally
				{
				if (streamReader != null)
					{  
					streamReader.Close();
					streamReader.Dispose();
					}
				if (fileStream != null)
					{  
					fileStream.Close();
					fileStream.Dispose();
					}
				}
			}

		}
	}