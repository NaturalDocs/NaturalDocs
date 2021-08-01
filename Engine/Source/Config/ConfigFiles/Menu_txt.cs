/* 
 * Class: CodeClear.NaturalDocs.Engine.Config.ConfigFiles.Menu_txt
 * ____________________________________________________________________________
 * 
 * A class to handling loading project information from a pre-2.0 version of <Menu.txt>.  It does not load the menu information
 * because there is no hand-editable menu in Natural Docs 2.0.
 * 
 * 
 * Threading: Not Thread Safe
 * 
 *		The parser object may be reused, but multiple threads cannot use it at the same time.
 *			
 */

// This file is part of Natural Docs, which is Copyright © 2003-2021 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Text;
using CodeClear.NaturalDocs.Engine.Config;
using CodeClear.NaturalDocs.Engine.Errors;


namespace CodeClear.NaturalDocs.Engine.Config.ConfigFiles
	{
	public class Menu_txt
		{
		
		// Group: Functions
		// __________________________________________________________________________
		
		
		/* Constructor: Menu_txt
		 */
		public Menu_txt ()
			{
			}



		// Group: Loading Functions
		// __________________________________________________________________________


		/* Function: Load
		 * Attempts to load any project information it can find in <Menu.txt>.  Unlike <Project_txt.Load()>, this doesn't take an
		 * <ErrorList>.  Loading <Menu.txt> is done when it can provide a benefit but it failing to load is not an error.
		 */
		public bool Load (Path path, out ProjectConfig projectConfig)
			{
			projectConfig = new ProjectConfig(PropertySource.OldMenuFile);

			using (var configFile = new ConfigFile())
				{
				ErrorList ignored = new Errors.ErrorList();
				bool openResult = configFile.Open(path, 
																  PropertySource.OldMenuFile,
																  ConfigFile.FileFormatFlags.CondenseIdentifierWhitespace |
																  ConfigFile.FileFormatFlags.SupportsBraces |
																  ConfigFile.FileFormatFlags.MakeIdentifiersLowercase,
																  ignored);
														 
				if (openResult == false)
					{  return false;  }
					
				Regex.Config.Subtitle subtitleRegex = new Regex.Config.Subtitle();
				Regex.Config.Timestamp timestampRegex = new Regex.Config.Timestamp();
					
				string lcIdentifier, value;
				
				while (configFile.Get(out lcIdentifier, out value))
					{
					var propertyLocation = new PropertyLocation(PropertySource.OldMenuFile, path, configFile.LineNumber);

					if (lcIdentifier == "title")
						{  
						projectConfig.ProjectInfo.Title = value.ConvertCopyrightAndTrademark();
						projectConfig.ProjectInfo.TitlePropertyLocation = propertyLocation;
						}
					else if (subtitleRegex.IsMatch(lcIdentifier))
						{  
						projectConfig.ProjectInfo.Subtitle = value.ConvertCopyrightAndTrademark();  
						projectConfig.ProjectInfo.SubtitlePropertyLocation = propertyLocation;
						}
					else if (lcIdentifier == "footer" || lcIdentifier == "copyright")
						{  
						projectConfig.ProjectInfo.Copyright = value.ConvertCopyrightAndTrademark();
						projectConfig.ProjectInfo.CopyrightPropertyLocation = propertyLocation;
						}
					else if (timestampRegex.IsMatch(lcIdentifier))
						{  
						projectConfig.ProjectInfo.TimestampCode = value;
						projectConfig.ProjectInfo.TimestampCodePropertyLocation = propertyLocation;
						}
					// Otherwise just ignore the entry.
					}
				
				configFile.Close();				
				}

			return true;				
			}
		
		}
	}