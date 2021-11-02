/* 
 * Class: CodeClear.NaturalDocs.Engine.Config.CharacterEncodingRule
 * ____________________________________________________________________________
 * 
 * A class representing a single rule to determine a file's character encoding.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2021 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;


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


		/* Function: Score
		 * Returns a numeric score representing how well the file matches the rule, or zero if it doesn't match at all.  Higher scores
		 * are better, since multiple rules may apply to a file.
		 */
		public int Score (Path file)
			{
			if (!folder.Contains(file))
				{  return 0;  }

			if (fileExtension != null &&
				!fileExtension.Equals(file.Extension, StringComparison.InvariantCultureIgnoreCase))
				{  return 0;  }

			// Format: 0LLL LLLE
			// 0 - First bit zero so it's always positive.
			// L - Length of the folder path.  Longer is better since it represents a deeper folder.
			// E - Whether it matches the extension.

			// 24 bits of length should be fine since it gets us to 16M and Windows' path limits are usually just under 255.
			int score = Math.Min(folder.Length, 0x00FFFFFF) << 1;

			// We already know it matches the extension if there is one.
			if (fileExtension != null)
				{  score++;  }

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