/*
 * Class: CodeClear.NaturalDocs.Engine.Files.File
 * ____________________________________________________________________________
 *
 * A class containing information about a file.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2022 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;


namespace CodeClear.NaturalDocs.Engine.Files
	{
	public class File : IDObjects.IDObject
		{

		// Group: Functions
		// __________________________________________________________________________


		/* Function: File
		 */
		public File (AbsolutePath fileName, FileType type, DateTime lastModified, int characterEncodingID = 0) : base()
			{
			this.fileName = fileName;
			this.type = type;
			this.lastModified = lastModified;
			this.characterEncodingID = characterEncodingID;
			this.deleted = false;
			}


		/* Function: CreateSnapshotOfProperties
		 * Creates a duplicate File object that contains all the file's properties at the time this function was called.
		 * The duplicate will not change so it can be used to compare to the original File object later to see if any
		 * of the properties have changed.
		 */
		virtual public File CreateSnapshotOfProperties ()
			{
			File duplicate = new File (fileName, type, lastModified, characterEncodingID);
			duplicate.ID = ID;
			duplicate.deleted = deleted;

			return duplicate;
			}



		// Group: Properties
		// __________________________________________________________________________


		/* Property: FileName
		 * The file name as an <AbsolutePath>.
		 */
		public AbsolutePath FileName
			{
			get
				{  return fileName;  }
			}

		/* Property: Type
		 * The <FileType>.
		 */
		public FileType Type
			{
			get
				{  return type;  }
			}

		/* Property: LastModified
		 * The timestamp of when the file was last modified.
		 */
		public DateTime LastModified
			{
			get
				{  return lastModified;  }
			set
				{  lastModified = value;  }
			}

		/* Property: AutoDetectUnicodeEncoding
		 * Whether you should auto-detect this file's Unicode encoding instead of using a specific one.  This should handle all
		 * forms of UTF-8, UTF-16, and UTF-32.  If this is false you need to pass <CharacterEncodingID> to
		 * System.Text.Encoding.GetEncoding(int32) to read the file.
		 */
		public bool AutoDetectUnicodeEncoding
			{
			get
				{  return (characterEncodingID == 0);  }
			}

		/* Property: CharacterEncodingID
		 *
		 * The ID of the character encoding used for this file if it's a text file.  Zero means auto-detect the Unicode encoding,
		 * which should handle all forms of UTF-8, UTF-16, and UTF-32, though you should use <AutoDetectUnicodeEncoding>
		 * instead as it's clearer in the calling code.  Zero could also mean this property isn't relevant because it's not a text file.
		 * Other values correspond to the code page identifier and can be passed directly to
		 * System.Text.Encoding.GetEncoding(int32).
		 *
		 * <Code Page Reference: https://docs.microsoft.com/en-us/dotnet/api/system.text.encoding#list-of-encodings>
		 */
		public int CharacterEncodingID
			{
			get
				{  return characterEncodingID;  }
			set
				{  characterEncodingID = value;  }
			}

		/* Property: Deleted
		 * Whether this file is deleted.  The File object will continue to exist until the deletion is fully processed.
		 */
		public bool Deleted
			{
			get
				{  return deleted;  }
			set
				{  deleted = value;  }
			}

		/* Property: Name
		 * The name of the object, which is the same as <FileName>.
		 */
		public override string Name
			{
			get
				{  return fileName.ToString();  }
			}



		// Group: Variables
		// __________________________________________________________________________


		/* var: fileName
		 * The <AbsolutePath> to the current file.
		 */
		protected AbsolutePath fileName;

		/* var: type
		 * The <FileType>.
		 */
		protected FileType type;

		/* var: lastModified
		 * The timestamp of when the file was last modified.
		 */
		protected DateTime lastModified;

		/* var: characterEncodingID
		 *
		 * The ID of the character encoding used for this file if it's a text file.  Zero means use Unicode auto-detect, which
		 * covers all forms of UTF-8, UTF-16, and UTF-32, or that it's not relevant because it's not a text file.  Other values
		 * correspond to the code page identifier used by .NET and can be passed directly to
		 * System.Text.Encoding.GetEncoding(int32).
		 *
		 * <Code Page Reference: https://docs.microsoft.com/en-us/dotnet/api/system.text.encoding#list-of-encodings>
		 */
		protected int characterEncodingID;

		/* var: deleted
		 * Whether this file was deleted, since the File object will persist until it's fully processed.
		 */
		protected bool deleted;

		}
	}
