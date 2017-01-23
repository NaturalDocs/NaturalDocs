/* 
 * Class: CodeClear.NaturalDocs.Engine.Config.Menu_txt
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
 * 
 * File: Menu.txt
 * 
 *		The file used to generate the menu in pre-2.0 versions of Natural Docs.
 *
 *		Project Information:
 * 
 *			> Title: [title]
 *			> Subtitle: [subtitle]
 *			> Footer: [footer]
 *			> Timestamp: [timestamp code]
 *
 *			These directly correspond to the equivalent entries in <Project.txt>.
 *		
 *
 *		Ignored Settings:
 *		
 *			> File: [title] ([file name])
 *			> File: [title] (auto-title, [file name])
 *			> File: [title] (no auto-title, [file name])
 *			>
 *			> Group: [title]
 *			> Group: [title] { ... }
 *			>
 *			> Text: [text]
 *			> Link: [URL]
 *			> Link: [title] ([URL])
 *			>
 *			> Index: [name]
 *			> [comment type name] Index: [name]
 *			>
 *			> Don't Index: [comment type name]
 *			> Don't Index: [comment type name], [comment type name], ...
 *			>
 *			> Data: [number]([obscured data])
 *			> Data: 1([obscured: [directory name]///[input directory]])
 *			> Data: 2([obscured: [directory name])
 *			
 *			This is kept for historical reasons but all of this data is ignored.  View the pre-2.0 documentation if you want more
 *			information on these items or to see the file format history.
 *			
 *			The only thing to keep in mind when parsing Menu.txt is that groups can have braces, which can appear anywhere on
 *			a line and violate the one-command-per-line rule.  They always have implicit line breaks around them.  For example, 
 *			this is valid:
 *			
 *			> Group: Classes { File: ClassA (ClassA.cs)
 *			> File: ClassB (ClassB.cs) } File: Functions (Functions.cs)
 *			
 */

// This file is part of Natural Docs, which is Copyright © 2003-2016 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Text;
using CodeClear.NaturalDocs.Engine.Config;
using CodeClear.NaturalDocs.Engine.Errors;


namespace CodeClear.NaturalDocs.Engine.Config
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
			projectConfig = new ProjectConfig(Source.OldMenuFile);

			using (var configFile = new ConfigFile())
				{
				ErrorList ignored = new Errors.ErrorList();
				bool openResult = configFile.Open(path, 
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
					var propertyLocation = new PropertyLocation(Source.OldMenuFile, path, configFile.LineNumber);

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