﻿/*
 * Class: CodeClear.NaturalDocs.Engine.Files.FileSources.SourceFolder
 * ____________________________________________________________________________
 *
 * A file source representing a specific source folder on disk.
 *
 */

// This file is part of Natural Docs, which is Copyright © 2003-2025 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Globalization;


namespace CodeClear.NaturalDocs.Engine.Files.FileSources
	{
	public class SourceFolder : FileSources.Folder
		{

		// Group: Functions
		// __________________________________________________________________________


		/* Function: SourceFolder
		 */
		public SourceFolder (Files.Manager manager, Config.Targets.SourceFolder config) : base (manager)
			{
			this.config = config;
			}


		/* Function: CharacterEncodingID
		 * Returns the character encoding ID of the passed file.  Zero means it's not a text file or use Unicode auto-detection,
		 * which will handle all forms of UTF-8, UTF-16, and UTF-32.  It's assumed that the file belongs to this file source.
		 */
		override public int CharacterEncodingID (Path file)
			{
			#if DEBUG
			if (!Contains(file))
				{  throw new Exception("Tried to call FileSource.CharacterEncodingID with a file that didn't belong to it.");  }
			#endif

			if (config.HasCharacterEncodingRules)
				{
				int bestEncodingID = 0;  // Unicode auto-detect
				int bestScore = 0;

				foreach (var characterEncodingRule in config.CharacterEncodingRules)
					{
					int score = characterEncodingRule.Score(file);

					// We compare to bestScore with >= instead of > because later rules that match the same way should take
					// precedence.  For example, a source folder rule for the same extension as a global rule.
					if (score != 0 && score >= bestScore)
						{
						bestEncodingID = characterEncodingRule.CharacterEncodingID;
						bestScore = score;
						}
					}

				return bestEncodingID;
				}
			else // doesn't have character encoding rules
				{  return 0;  }
			}



		// Group: Processes
		// __________________________________________________________________________


		/* Function: CreateAdderProcess
		 * Returns a <FileSourceAdder> that can be used with this FileSource.
		 */
		override public FileSourceAdder CreateAdderProcess()
			{
			return new SourceFolderAdder(this, EngineInstance);
			}



		// Group: Properties
		// __________________________________________________________________________

		/* Property: UniqueIDString
		 * A string that uniquely identifies this FileSource among all others of its <Type>, including FileSources based on other
		 * classes.
		 */
		override public string UniqueIDString
			{
			get
				{
				if (SystemInfo.IgnoreCaseInPaths)
					{  return "Folder:" + config.Folder.ToString().ToLower(CultureInfo.InvariantCulture);  }
				else
					{  return "Folder:" + config.Folder;  }
				}
			}

		/* Property: Path
		 * The path to the FileSource's folder.
		 */
		override public Path Path
			{
			get
				{  return config.Folder;  }
			}

		/* Property: Type
		 * The type of files this FileSource provides.
		 */
		override public InputType Type
			{
			get
				{  return InputType.Source;  }
			}

		/* Property: Number
		 * The number assigned to this FileSource.
		 */
		override public int Number
			{
			get
				{  return config.Number;  }
			}

		/* Property: Name
		 * The name assigned to this FileSource, or null if one hasn't been set.
		 */
		override public string Name
			{
			get
				{  return config.Name;  }
			}


		/* Property: Url
		 * The url of the FileSource's folder.
		 */
		public string Url
			{
			get
				{  return config.Url;  }
			}


		// Group: Variables
		// __________________________________________________________________________

		protected Config.Targets.SourceFolder config;

		}
	}
