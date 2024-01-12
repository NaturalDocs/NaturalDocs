/*
 * Class: CodeClear.NaturalDocs.Engine.Config.CharacterEncodingRule
 * ____________________________________________________________________________
 *
 * A class representing a single rule to determine a file's character encoding.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2024 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using CodeClear.NaturalDocs.Engine.Errors;


namespace CodeClear.NaturalDocs.Engine.Config
	{
	public class CharacterEncodingRule
		{

		// Group: Functions
		// __________________________________________________________________________

		public CharacterEncodingRule (int characterEncodingID, string characterEncodingName, AbsolutePath folder, string fileExtension,
													 PropertyLocation propertyLocation)
			{
			this.characterEncodingID = characterEncodingID;
			this.characterEncodingName = characterEncodingName;

			this.folder = folder;
			this.fileExtension = fileExtension;

			this.propertyLocation = propertyLocation;
			}


		/* Function: ValidateAndLookupID
		 * Checks if there are any problems with the encoding rule, such as the name being invalid or the folder not existing.
		 * If there are problems it will add errors to the list and return false.  If <CharacterEncodingName> is set but
		 * <CharacterEncodingID> is not it will also look it up and set it.
		 */
		public bool ValidateAndLookupID (ErrorList errorList)
			{
			bool valid = true;


			// Folder

			if (folder != null &&
				System.IO.Directory.Exists(folder) == false)
				{
				errorList.Add( Locale.Get("NaturalDocs.Engine", "Project.txt.EncodingFolderDoesNotExist(folder)", folder),
								     propertyLocation );
				valid = false;
				}


			// Character encoding ID

			if (characterEncodingID != 0)
				{
				bool validEncodingID = true;

				try
					{
					// This should throw an exception on failure instead of returning null, but test it anyway for defensive programming.
					if (System.Text.Encoding.GetEncoding(characterEncodingID) == null)
						{  validEncodingID = false;  }
					}
				catch (Exception e)
					{
					if (e is System.ArgumentException || e is System.NotSupportedException)
						{  validEncodingID = false;  }
					else
						{  throw;  }
					}

				if (!validEncodingID)
					{
					errorList.Add( Locale.Get("NaturalDocs.Engine", "Project.txt.EncodingCodePageDoesNotExist(codePage)", characterEncodingID),
										 propertyLocation );
					valid = false;
					}
				}


			// Character encoding name and ID lookup

			if (characterEncodingName == null)
				{  /* do nothing */  }
			else if (characterEncodingName.Equals("Unicode", StringComparison.OrdinalIgnoreCase))
				{  characterEncodingID = 0;  }
			else
				{
				bool validEncodingName = true;

				try
					{
					var encoding = System.Text.Encoding.GetEncoding(characterEncodingName);

					// GetEncoding should throw an exception on failure instead of returning null, but test it anyway for defensive programming.
					if (encoding == null)
						{  validEncodingName = false;  }
					else
						{  characterEncodingID = encoding.CodePage;  }
					}
				catch (Exception e)
					{
					if (e is System.ArgumentException || e is System.NotSupportedException)
						{  validEncodingName = false;  }
					else
						{  throw;  }
					}

				if (!validEncodingName)
					{
					errorList.Add( Locale.Get("NaturalDocs.Engine", "Project.txt.EncodingNameDoesNotExist(name)", characterEncodingName),
										 propertyLocation );
					valid = false;
					}
				}

			return valid;
			}


		/* Function: Score
		 * Returns a numeric score representing how well the file matches the rule, or zero if it doesn't match at all.  Higher scores
		 * are better, since multiple rules may apply to a file.
		 */
		public int Score (Path file)
			{
			if (folder != null &&
				!folder.Contains(file))
				{  return 0;  }

			if (fileExtension != null &&
				!fileExtension.Equals(file.Extension, StringComparison.OrdinalIgnoreCase))
				{  return 0;  }

			// Format: 0LLLLLLL LLLLLLLL LLLLLLLL LLLLLLE1
			// 0 - First bit zero so it's always positive.
			// L - Length of the folder path.  Longer is better since it represents a deeper folder.  29 bits gets us up to 512M.
			// E - Whether it matches the extension.
			// 1 - Last bit one so matches are always non-zero.

			// Add the 1 bit.
			int score = 0x00000001;

			// Add the folder length if folder is defined.  If it is we already know it contains the file path.
			if (folder != null)
				{  score |= Math.Min(folder.Length, 0x1FFFFFFF) << 2;  }

			// Add the file extension bit if extension is defined.  If it is we already know it matches.
			if (fileExtension != null)
				{  score |= 0x00000002;  }

			return score;
			}



		// Group: Properties
		// __________________________________________________________________________


		/* Property: CharacterEncodingID
		 * The numeric ID of the character encoding used for this rule.  Zero means use Unicode auto-detect, which covers
		 * all forms of UTF-8, UTF-16, and UTF-32.  Other values correspond to the code page number used by .NET and can
		 * be passed directly to System.Text.Encoding.GetEncoding(int32).
		 */
		public int CharacterEncodingID
			{
			get
				{  return characterEncodingID;  }
			}

		/* Property: CharacterEncodingName
		 * The text name of <CharacterEncodingID> if it's set, or null if not.
		 */
		public string CharacterEncodingName
			{
			get
				{  return characterEncodingName;  }
			}

		/* Property: Folder
		 *	The source folder the rule applies to, or null if it applies to all folders.
		 */
		public AbsolutePath Folder
			{
			get
				{  return folder;  }
			}

		/* Property: FileExtension
		 * The file extension this rule applies to, or null if it applies to all extensions.
		 */
		public string FileExtension
			{
			get
				{  return fileExtension;  }
			}


		/* Property: PropertyLocation
		 * Where the character encoding rule is defined.
		 */
		public PropertyLocation PropertyLocation
			{
			get
				{  return propertyLocation;  }
			}



		// Group: Variables
		// __________________________________________________________________________

		protected int characterEncodingID;
		protected string characterEncodingName;

		protected AbsolutePath folder;
		protected string fileExtension;

		protected PropertyLocation propertyLocation;

		}
	}
