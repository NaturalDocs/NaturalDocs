/* 
 * Class: CodeClear.NaturalDocs.Engine.Config.ConfigFiles.TextFileParser
 * ____________________________________________________________________________
 * 
 * A class to handle loading and saving <Project.txt>.
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
	public class TextFileParser
		{
		
		// Group: Functions
		// __________________________________________________________________________
		
		
		/* Constructor: TextFileParser
		 */
		public TextFileParser ()
			{
			errorList = null;
			projectConfig = null;

			yesRegex = new Regex.Config.Yes();
			noRegex = new Regex.Config.No();
							
			subtitleRegex = new Regex.Config.Subtitle();
			timestampRegex = new Regex.Config.Timestamp();
			homePageRegex = new Regex.Config.HomePage();
			tabWidthRegex = new Regex.Config.TabWidth();
			documentedOnlyRegex = new Regex.Config.DocumentedOnly();
			autoGroupRegex = new Regex.Config.AutoGroup();

			sourceFolderRegex = new Regex.Config.SourceFolder();
			imageFolderRegex = new Regex.Config.ImageFolder();
			htmlOutputFolderRegex = new Regex.Config.HTMLOutputFolder();
			ignoredSourceFolderRegex = new Regex.Config.IgnoredSourceFolder();
			ignoredSourceFolderPatternRegex = new Regex.Config.IgnoredSourceFolderPattern();
			}



		// Group: Loading Functions
		// __________________________________________________________________________


		/* Function: Load
		 * Attempts to parse <Project.txt> and return it as a <ProjectConfig>.  Any syntax errors found will be added to the
		 * <ErrorList>.  The <ProjectConfig> object will always exist, even if all its properties are empty.
		 */
		public bool Load (Path path, out ProjectConfig projectConfig, ErrorList errorList)
			{
			projectConfig = new ProjectConfig(PropertySource.ProjectFile);

			this.errorList = errorList;
			this.projectConfig = projectConfig;

			int originalErrorCount = errorList.Count;

			using (var configFile = new ConfigFile())
				{
				// We don't condense value whitespace because some things like title, subtitle, and copyright may want multiple spaces.
				bool openResult = configFile.Open(path, 
																  PropertySource.ProjectFile,
																  ConfigFile.FileFormatFlags.CondenseIdentifierWhitespace |
																  ConfigFile.FileFormatFlags.MakeIdentifiersLowercase,
																  errorList);
														 
				if (openResult == false)
					{  return false;  }

				string lcIdentifier, value;
				Target currentTarget =  null;

				while (configFile.Get(out lcIdentifier, out value))
					{
					var propertyLocation = new PropertyLocation(PropertySource.ProjectFile, configFile.FileName, configFile.LineNumber);

					if (GetTargetHeader(lcIdentifier, value, propertyLocation, out var target, errorList))
						{  currentTarget = target;  }
					else if (GetProjectInfoProperty(lcIdentifier, value, propertyLocation,
																(currentTarget != null && currentTarget is Targets.Output ? 
																	 (currentTarget as Targets.Output).ProjectInfo : projectConfig.ProjectInfo),
																errorList))
						{  }
					else if (currentTarget != null && currentTarget is Targets.Input &&
							   GetInputTargetProperty(lcIdentifier, value, propertyLocation, currentTarget as Targets.Input, errorList))
						{  }
					else if (GetGlobalProperty(lcIdentifier, value, propertyLocation, errorList))
						{  currentTarget = null;  }
					else
						{
						errorList.Add (
							message: Locale.Get("NaturalDocs.Engine", "ConfigFile.NotAValidIdentifier(identifier)", lcIdentifier),
							propertyLocation: propertyLocation
							);
						}
					}
				
				configFile.Close();
				}
				
			return (errorList.Count == originalErrorCount);
			}


		/* Function: GetTargetHeader
		 * If the passed identifier starts a target like "Source Folder", creates a new target for it and returns true.  If it's a recognized 
		 * identifier but there is a syntax error in the value it will add an error to <errorList> and still return true.  It only returns 
		 * false for unrecognized identifiers.
		 */
		protected bool GetTargetHeader (string lcIdentifier, string value, PropertyLocation propertyLocation, out Target newTarget, 
														ErrorList errorList)
			{

			// Source folder

			System.Text.RegularExpressions.Match match = sourceFolderRegex.Match(lcIdentifier);

			if (match.Success)
				{
				var target = new Targets.SourceFolder(propertyLocation);
				Path path = value;

				if (path.IsRelative)
					{  path = propertyLocation.FileName.ParentFolder + "/" + path;  }

				target.Folder = (AbsolutePath)path;
				target.FolderPropertyLocation = propertyLocation;

				int number = 0;

				if (int.TryParse(match.Groups[1].Value, out number))
					{  
					target.Number = number;
					target.NumberPropertyLocation = propertyLocation;
					}

				projectConfig.InputTargets.Add(target);
				newTarget = target;
				return true;
				}
				

			// Image folder

			match = imageFolderRegex.Match(lcIdentifier);

			if (match.Success)
				{  
				var target = new Targets.ImageFolder(propertyLocation);
				Path path = value;

				if (path.IsRelative)
					{  path = propertyLocation.FileName.ParentFolder + "/" + path;  }

				target.Folder = (AbsolutePath)path;
				target.FolderPropertyLocation = propertyLocation;

				int number = 0;

				if (int.TryParse(match.Groups[1].Value, out number))
					{  
					target.Number = number;
					target.NumberPropertyLocation = propertyLocation;
					}

				projectConfig.InputTargets.Add(target);
				newTarget = target;
				return true;
				}


			// HTML output folder
				
			else if (htmlOutputFolderRegex.IsMatch(lcIdentifier))
				{  
				var target = new Targets.HTMLOutputFolder(propertyLocation);
				Path path = value;

				if (path.IsRelative)
					{  path = propertyLocation.FileName.ParentFolder + "/" + path;  }

				target.Folder = (AbsolutePath)path;
				target.FolderPropertyLocation = propertyLocation;

				projectConfig.OutputTargets.Add(target);
				newTarget = target;
				return true;
				}


			// Ignored source folder
				
			else if (ignoredSourceFolderRegex.IsMatch(lcIdentifier))
				{  
				var target = new Targets.IgnoredSourceFolder(propertyLocation);
				Path path = value;

				if (path.IsRelative)
					{  path = propertyLocation.FileName.ParentFolder + "/" + path;  }

				target.Folder = (AbsolutePath)path;
				target.FolderPropertyLocation = propertyLocation;

				projectConfig.FilterTargets.Add(target);
				newTarget = target;
				return true;
				}
				

			// Ignored source folder pattern
				
			else if (ignoredSourceFolderPatternRegex.IsMatch(lcIdentifier))
				{  
				var target = new Targets.IgnoredSourceFolderPattern(propertyLocation);

				target.Pattern = value;
				target.PatternPropertyLocation = propertyLocation;

				projectConfig.FilterTargets.Add(target);
				newTarget = target;
				return true;
				}

			else
				{  
				newTarget = null;
				return false;  
				}
		    }


		/* Function: GetProjectInfoProperty
		 * 
		 * If the passed identifier is a property like Style that's part of <ProjectInfo>, adds it to the passed <ProjectInfo> object
		 * and returns true.  This can be the global one in <projectConfig> or one attached to an output target.
		 * 
		 * If it's a recognized identifier but there's a syntax error in the value it will add an error to <errorList> and still return true.
		 * It only returns false for unrecognized identifiers.
		 */
		protected bool GetProjectInfoProperty (string lcIdentifier, string value, PropertyLocation propertyLocation, ProjectInfo projectInfo,
																ErrorList errorList)
			{

			// Title

			if (lcIdentifier == "title")
				{
				projectInfo.Title = value.ConvertCopyrightAndTrademark();
				projectInfo.TitlePropertyLocation = propertyLocation;
				return true;
				}


			// Subtitle

			else if (subtitleRegex.IsMatch(lcIdentifier))
				{
				projectInfo.Subtitle = value.ConvertCopyrightAndTrademark();
				projectInfo.SubtitlePropertyLocation = propertyLocation;
				return true;
				}


			// Copyright

			else if (lcIdentifier == "copyright")
				{
				projectInfo.Copyright = value.ConvertCopyrightAndTrademark();
				projectInfo.CopyrightPropertyLocation = propertyLocation;
				return true;
				}


			// Timestamp

			else if (timestampRegex.IsMatch(lcIdentifier))
				{
				projectInfo.TimestampCode = value;
				projectInfo.TimestampCodePropertyLocation = propertyLocation;
				return true;
				}


			// Style

			else if (lcIdentifier == "style")
				{
				projectInfo.StyleName = value;
				projectInfo.StyleNamePropertyLocation = propertyLocation;
				return true;
				}


			// Home page

			else if (homePageRegex.IsMatch(lcIdentifier))
				{
				Path path = value;

				if (path.IsRelative)
					{  path = propertyLocation.FileName.ParentFolder + "/" + path;  }

				if (!System.IO.File.Exists(path))
					{  
					errorList.Add(
						Locale.Get("NaturalDocs.Engine", "Project.txt.CantFindHomePageFile(name)", path), 
						propertyLocation);  
					}

				projectInfo.HomePage = (AbsolutePath)path;
				projectInfo.HomePagePropertyLocation = propertyLocation;
				return true;
				}

			else
				{
				return false;
				}
			}


		/* Function: GetInputTargetProperty
		 * If the passed identifier is a valid property for an input target, applies the property and returns true.  If the value is 
		 * invalid it will add an error to <errorList> and still return true.  It will only return false if the identifier is unrecognized.
		 */
		protected bool GetInputTargetProperty (string lcIdentifier, string value, PropertyLocation propertyLocation, 
																 Targets.Input inputTarget, ErrorList errorList)
			{
			if (lcIdentifier == "name")
				{
				 if (inputTarget is Targets.SourceFolder && 
					(inputTarget as Targets.SourceFolder).Type == Files.InputType.Source)
					{
					(inputTarget as Targets.SourceFolder).Name = value;
					(inputTarget as Targets.SourceFolder).NamePropertyLocation = propertyLocation;
					}
				else
					{
					errorList.Add( Locale.Get("NaturalDocs.Engine", "Project.txt.NameOnlyAppliesToSourceFolders"),
									   propertyLocation.FileName, propertyLocation.LineNumber );
					}

				return true;
				}
				
			else
				{  return false;  }
			}


		/* Function: GetGlobalProperty
		 * If the passed identifier is a global property like Tab Width, adds it to <projectConfig> and returns true.  If it is a recognized
		 * global property but has a syntax error in the value, it will add an error to <errorList> and still return true.  It only returns
		 * false on unrecognized identifiers.
		 */
		protected bool GetGlobalProperty (string lcIdentifier, string value, PropertyLocation propertyLocation, ErrorList errorList)
			{

			// Tab width

			if (tabWidthRegex.IsMatch(lcIdentifier))
				{
				int tabWidth = 0;
						
				if (Int32.TryParse(value, out tabWidth) == true)
					{  
					projectConfig.TabWidth = tabWidth;
					projectConfig.TabWidthPropertyLocation = propertyLocation;
					}
				else
					{
					errorList.Add( Locale.Get("NaturalDocs.Engine", "Error.TabWidthMustBeANumber"), 
									   propertyLocation.FileName, propertyLocation.LineNumber );
					}

				return true;
				}


			// Documented only

			else if (documentedOnlyRegex.IsMatch(lcIdentifier))
				{
				if (yesRegex.IsMatch(value))
					{  
					projectConfig.DocumentedOnly = true;  
					projectConfig.DocumentedOnlyPropertyLocation = propertyLocation;
					}
				else if (noRegex.IsMatch(value))
					{  
					projectConfig.DocumentedOnly = false;
					projectConfig.DocumentedOnlyPropertyLocation = propertyLocation;
					}
				else
					{
					errorList.Add( Locale.Get("NaturalDocs.Engine", "Project.txt.UnrecognizedValue(keyword, value)", "Documented Only", value),
									   propertyLocation.FileName, propertyLocation.LineNumber );
					}

				return true;
				}


			// Auto-group

			else if (autoGroupRegex.IsMatch(lcIdentifier))
				{
				if (yesRegex.IsMatch(value))
					{  
					projectConfig.AutoGroup = true;  
					projectConfig.AutoGroupPropertyLocation = propertyLocation;
					}
				else if (noRegex.IsMatch(value))
					{  
					projectConfig.AutoGroup = false;  
					projectConfig.AutoGroupPropertyLocation = propertyLocation;
					}
				else
					{
					errorList.Add( Locale.Get("NaturalDocs.Engine", "Project.txt.UnrecognizedValue(keyword, value)", "Auto Group", value),
									   propertyLocation.FileName, propertyLocation.LineNumber );
					}

				return true;
				}

			else
				{
				return false;
				}
			}



		// Group: Saving Functions
		// __________________________________________________________________________


		/* Function: Save
		 * 
		 * Saves the passed <ConfigData> into <Project.txt>, returning whether it was successful.  It will automatically skip
		 * properties that shouldn't be saved into the file.
		 * 
		 * Property Source:
		 * 
		 *		<PropertySource.SystemDefault> - Will never be saved into <Project.txt>.
		 *		
		 *		<PropertySource.SystemGenerated> - Will always be saved into <Project.txt>.  This allows things like source folder
		 *			names and numbers to be written into <Project.txt> so that they remain consistent between runs and so it's easy 
		 *			for the user to edit them.
		 *												 
		 *		<PropertySource.CommandLine> - Global properties will not be saved into <Project.txt> so that settings don't get 
		 *			tattooed into place.  If someone specifies Documented Only on the command line it should turn off when they take
		 *			it off the command line.  They shouldn't have to edit <Project.txt> as well.
		 *										   
		 *			Targets and their associated properties will be saved into <Project.txt>.  This allows a <Project.txt> file to be 
		 *			generated from the command line so we can store secondary target settings.  Since <Project.txt> targets are 
		 *			ignored except for secondary settings when they're specified on the command line, tattooing is less of an issue.
		 */
		public bool Save (Path path, ProjectConfig projectConfig, Errors.ErrorList errorList)
			{
			this.projectConfig = projectConfig;
			this.errorList = errorList;

			StringBuilder output = new StringBuilder(1024);
			Path projectFolder = path.ParentFolder;
			
			output.AppendLine("Format: " + Engine.Instance.VersionString);
			output.AppendLine();

			AppendFileHeader(output);			
			
			AppendGlobalProjectInfo(output);
			AppendSourceTargets(output, projectFolder);
			AppendFilterTargets(output, projectFolder);
			AppendImageTargets(output, projectFolder);
			AppendOutputTargets(output, projectFolder);
			AppendGlobalSettings(output);
			
			return ConfigFile.SaveIfDifferent(path, output.ToString(), false, errorList);
			}


		/* Function: AppendFileHeader
		 * Appends the general file header to the passed string.
		 */
		protected void AppendFileHeader (StringBuilder output)
			{
			output.Append( Locale.Get("NaturalDocs.Engine", "Project.txt.FileHeader.multiline") );
			output.AppendLine();
			output.AppendLine();
			}


		/* Function: AppendGlobalProjectInfo
		 * Appends the global <ProjectInfo> in <projectConfig> to the passed string.
		 */
		protected void AppendGlobalProjectInfo (StringBuilder output)
			{

			// Header

			output.Append( Locale.Get("NaturalDocs.Engine", "Project.txt.ProjectInfoHeader.multiline") );
			output.AppendLine();


			// Defined values

			bool hasTitle = (projectConfig.ProjectInfo.TitlePropertyLocation.IsDefined &&
									projectConfig.ProjectInfo.TitlePropertyLocation.Source != PropertySource.SystemDefault &&
									projectConfig.ProjectInfo.TitlePropertyLocation.Source != PropertySource.CommandLine);
			bool hasSubtitle = (projectConfig.ProjectInfo.SubtitlePropertyLocation.IsDefined &&
										projectConfig.ProjectInfo.SubtitlePropertyLocation.Source != PropertySource.SystemDefault &&
										projectConfig.ProjectInfo.SubtitlePropertyLocation.Source != PropertySource.CommandLine);
			bool hasCopyright = (projectConfig.ProjectInfo.CopyrightPropertyLocation.IsDefined &&
											projectConfig.ProjectInfo.CopyrightPropertyLocation.Source != PropertySource.SystemDefault &&
											projectConfig.ProjectInfo.CopyrightPropertyLocation.Source != PropertySource.CommandLine);
			bool hasTimestampCode = (projectConfig.ProjectInfo.TimestampCodePropertyLocation.IsDefined &&
													projectConfig.ProjectInfo.TimestampCodePropertyLocation.Source != PropertySource.SystemDefault &&
													projectConfig.ProjectInfo.TimestampCodePropertyLocation.Source != PropertySource.CommandLine);
			bool hasStyleName = (projectConfig.ProjectInfo.StyleNamePropertyLocation.IsDefined &&
											 projectConfig.ProjectInfo.StyleNamePropertyLocation.Source != PropertySource.SystemDefault &&
											 projectConfig.ProjectInfo.StyleNamePropertyLocation.Source != PropertySource.CommandLine);
			bool hasHomePage = (projectConfig.ProjectInfo.HomePagePropertyLocation.IsDefined &&
											projectConfig.ProjectInfo.HomePagePropertyLocation.Source != PropertySource.SystemDefault &&
											projectConfig.ProjectInfo.HomePagePropertyLocation.Source != PropertySource.CommandLine);

			if (hasTitle)
				{  
				output.AppendLine("Title: " + projectConfig.ProjectInfo.Title);

				if (!hasSubtitle)
					{  output.AppendLine();  }
				}
					
			if (hasSubtitle)
				{
				output.AppendLine("Subtitle: " + projectConfig.ProjectInfo.Subtitle);
				output.AppendLine();
				}
					
			if (hasCopyright)
				{
				output.AppendLine("Copyright: " + projectConfig.ProjectInfo.Copyright);
				output.AppendLine();
				}
			
			if (hasTimestampCode)
				{  
				output.AppendLine("Timestamp: " + projectConfig.ProjectInfo.TimestampCode);
				output.Append( Locale.Get("NaturalDocs.Engine", "Project.txt.TimestampSubstitutions.multiline") );
				output.AppendLine();
				}

			if (hasStyleName)
				{
				output.AppendLine("Style: " + projectConfig.ProjectInfo.StyleName);
				output.AppendLine();
				}

			if (hasHomePage)
				{
				Path relativePath = projectConfig.ProjectInfo.HomePage.MakeRelativeTo(projectConfig.ProjectConfigFolder);
				
				output.AppendLine("Home Page: " + (relativePath != null ? relativePath : projectConfig.ProjectInfo.HomePage));
				output.AppendLine();
				}

			if (hasTitle || hasSubtitle || hasCopyright || hasTimestampCode || hasStyleName || hasHomePage)
				{  output.AppendLine();  }


			// Syntax reference

			output.Append( Locale.Get("NaturalDocs.Engine", "Project.txt.ProjectInfoHeaderText.multiline") );

			if (!hasTitle)
				{
				output.AppendLine("#");
				output.Append( Locale.Get("NaturalDocs.Engine", "Project.txt.TitleSyntax.multiline") );
				}
					
			if (!hasSubtitle)
				{  
				output.AppendLine("#");
				output.Append( Locale.Get("NaturalDocs.Engine", "Project.txt.SubtitleSyntax.multiline") );
				}
					
			if (!hasCopyright)
				{  
				output.AppendLine("#");
				output.Append( Locale.Get("NaturalDocs.Engine", "Project.txt.CopyrightSyntax.multiline") );
				}
			
			if (!hasTimestampCode)
				{  
				output.AppendLine("#");
				output.Append( Locale.Get("NaturalDocs.Engine", "Project.txt.TimestampSyntax.multiline") );
				output.Append( Locale.Get("NaturalDocs.Engine", "Project.txt.TimestampSubstitutions.multiline") );
				}

			if (!hasStyleName)
				{
				output.AppendLine("#");
				output.Append( Locale.Get("NaturalDocs.Engine", "Project.txt.StyleSyntax.multiline") );
				}

			if (!hasHomePage)
				{
				output.AppendLine("#");
				output.Append( Locale.Get("NaturalDocs.Engine", "Project.txt.HomePageSyntax.multiline") );
				}

			output.AppendLine();
			output.AppendLine();
			}


		/* Function: AppendOverriddenProjectInfo
		 * Appends the overridden <ProjectInfo> to the passed string.
		 */
		protected void AppendOverriddenProjectInfo (ProjectInfo projectInfo, StringBuilder output)
			{
			if (projectInfo.TitlePropertyLocation.IsDefined &&
				projectInfo.TitlePropertyLocation.Source != PropertySource.SystemDefault)
				{  output.AppendLine("   Title: " + projectInfo.Title);  }
					
			if (projectInfo.SubtitlePropertyLocation.IsDefined &&
				projectInfo.SubtitlePropertyLocation.Source != PropertySource.SystemDefault)
				{  output.AppendLine("   Subtitle: " + projectInfo.Subtitle);  }
					
			if (projectInfo.CopyrightPropertyLocation.IsDefined &&
				projectInfo.CopyrightPropertyLocation.Source != PropertySource.SystemDefault)
				{  output.AppendLine("   Copyright: " + projectInfo.Copyright);  }
			
			if (projectInfo.TimestampCodePropertyLocation.IsDefined &&
				projectInfo.TimestampCodePropertyLocation.Source != PropertySource.SystemDefault)
				{  output.AppendLine("   Timestamp: " + projectInfo.TimestampCode);  }

			if (projectInfo.StyleNamePropertyLocation.IsDefined &&
				projectInfo.StyleNamePropertyLocation.Source != PropertySource.SystemDefault)
				{  output.AppendLine("   Style: " + projectInfo.StyleName);   }

			if (projectInfo.HomePagePropertyLocation.IsDefined &&
				projectInfo.HomePagePropertyLocation.Source != PropertySource.SystemDefault)
				{  
				Path relativePath = projectInfo.HomePage.MakeRelativeTo(projectConfig.ProjectConfigFolder);
				output.AppendLine("   Home Page: " + (relativePath != null ? relativePath : projectInfo.HomePage));
				}
			}


		/* Function: AppendSourceTargets
		 * Appends all source targets in <projectConfig> to the passed StringBuilder.
		 */
		protected void AppendSourceTargets (StringBuilder output, Path projectFolder)
			{
			output.Append( Locale.Get("NaturalDocs.Engine", "Project.txt.SourceHeader.multiline") );
			output.AppendLine();

			int appended = 0;

			foreach (var target in projectConfig.InputTargets)
				{
				// We save input targets even if they're specified on the command line so we can still use Project.txt for secondary
				// settings.
				if (target is Targets.SourceFolder &&
					target.PropertyLocation.Source != PropertySource.SystemDefault)
					{  
					AppendSourceFolder((Targets.SourceFolder)target, output, projectFolder);

					output.AppendLine();
					appended++;
					}
				}

			if (appended > 0)
				{  output.AppendLine();  }

			output.Append( Locale.Get("NaturalDocs.Engine", "Project.txt.SourceHeaderText.multiline") );
			output.AppendLine("#");
			output.Append( Locale.Get("NaturalDocs.Engine", "Project.txt.SourceFolderSyntax.multiline") );
			output.AppendLine();
			output.AppendLine();
			}


		/* Function: AppendSourceFolder
		 * Appends a source folder target and all its settings to the StringBuilder.
		 */
		protected void AppendSourceFolder (Targets.SourceFolder target, StringBuilder output, Path projectFolder)
			{
			if (target.PropertyLocation.Source == PropertySource.SystemDefault)
				{  return;  }

			output.Append("Source Folder");

			if (target.NumberPropertyLocation.IsDefined &&
				target.NumberPropertyLocation.Source != PropertySource.SystemDefault &&
				target.Number != 1)
				{  output.Append(" " + target.Number);  }

			output.Append(": ");

			Path relativePath = target.Folder.MakeRelativeTo(projectFolder);
			output.AppendLine( (relativePath != null ? relativePath : target.Folder) );

			if (target.NamePropertyLocation.IsDefined &&
				target.NamePropertyLocation.Source != PropertySource.SystemDefault)
				{  output.AppendLine("   Name: " + target.Name);  }
			}


		/* Function: AppendFilterTargets
		 * Appends all filter targets in <projectConfig> to the passed StringBuilder.
		 */
		protected void AppendFilterTargets (StringBuilder output, Path projectFolder)
			{
			output.Append( Locale.Get("NaturalDocs.Engine", "Project.txt.FilterHeader.multiline") );
			output.AppendLine();

			int appended = 0;

			foreach (var target in projectConfig.FilterTargets)
				{
				// We save filter targets even if they're specified on the command line so we can still use Project.txt for secondary
				// settings.
				if (target.PropertyLocation.Source != PropertySource.SystemDefault)
					{
					if (target is Targets.IgnoredSourceFolder)
						{  AppendIgnoredSourceFolder((Targets.IgnoredSourceFolder)target, output, projectFolder);  }
					else if (target is Targets.IgnoredSourceFolderPattern)
						{  AppendIgnoredSourceFolderPattern((Targets.IgnoredSourceFolderPattern)target, output);  }
					else
						{  throw new NotImplementedException();  }

					output.AppendLine();
					appended++;
					}
				}

			if (appended > 0)
				{  output.AppendLine();  }

			output.Append( Locale.Get("NaturalDocs.Engine", "Project.txt.FilterHeaderText.multiline") );
			output.AppendLine("#");
			output.Append( Locale.Get("NaturalDocs.Engine", "Project.txt.IgnoreSourceFolderSyntax.multiline") );
			output.AppendLine("#");
			output.Append( Locale.Get("NaturalDocs.Engine", "Project.txt.IgnoreSourceFolderPatternSyntax.multiline") );
			output.AppendLine();
			output.AppendLine();
			}


		/* Function: AppendIgnoredSourceFolder
		 * Appends an ignored source folder target and all its settings to the passed StringBuilder.
		 */
		protected void AppendIgnoredSourceFolder (Targets.IgnoredSourceFolder target, StringBuilder output, Path projectFolder)
			{
			if (target.PropertyLocation.Source == PropertySource.SystemDefault)
				{  return;  }

			output.Append("Ignore Source Folder: ");

			Path relativePath = target.Folder.MakeRelativeTo(projectFolder);
			output.AppendLine( (relativePath != null ? relativePath : target.Folder) );
			}


		/* Function: AppendIgnoredSourceFolderPattern
		 * Appends an ignored source folder pattern target and all its settings to the passed StringBuilder.
		 */
		protected void AppendIgnoredSourceFolderPattern (Targets.IgnoredSourceFolderPattern target, StringBuilder output)
			{
			if (target.PropertyLocation.Source == PropertySource.SystemDefault)
				{  return;  }

			output.AppendLine("Ignore Source Folder Pattern: " + target.Pattern);
			}


		/* Function: AppendImageTargets
		 * Appends all image targets in <projectConfig> to the passed StringBuilder.
		 */
		protected void AppendImageTargets (StringBuilder output, Path projectFolder)
			{
			output.Append( Locale.Get("NaturalDocs.Engine", "Project.txt.ImageHeader.multiline") );
			output.AppendLine();

			int appended = 0;

			foreach (var target in projectConfig.InputTargets)
				{
				if (target is Targets.ImageFolder &&
					target.PropertyLocation.Source != PropertySource.SystemDefault)
					{
					AppendImageFolder((Targets.ImageFolder)target, output, projectFolder);

					output.AppendLine();
					appended++;
					}
				}

			if (appended > 0)
				{  output.AppendLine();  }

			output.Append( Locale.Get("NaturalDocs.Engine", "Project.txt.ImageHeaderText.multiline") );
			output.AppendLine("#");
			output.Append(Locale.Get("NaturalDocs.Engine", "Project.txt.ImageFolderSyntax.multiline"));
			output.AppendLine();
			output.AppendLine();
			}


		/* Function: AppendImageFolder
		 * Appends an image folder target and all its settings to the StringBuilder.
		 */
		protected void AppendImageFolder (Targets.ImageFolder target, StringBuilder output, Path projectFolder)
			{
			if (target.PropertyLocation.Source == PropertySource.SystemDefault)
				{  return;  }

			output.Append("Image Folder");

			if (target.NumberPropertyLocation.IsDefined &&
				target.NumberPropertyLocation.Source != PropertySource.SystemDefault &&
				target.Number != 1)
				{  output.Append(" " + target.Number);  }

			output.Append(": ");

			Path relativePath = target.Folder.MakeRelativeTo(projectFolder);
			output.AppendLine( (relativePath != null ? relativePath : target.Folder) );
			}


		/* Function: AppendOutputTargets
		 * Appends all output targets in <projectConfig> to the passed StringBuilder.
		 */
		protected void AppendOutputTargets (StringBuilder output, Path projectFolder)
			{
			output.Append( Locale.Get("NaturalDocs.Engine", "Project.txt.OutputHeader.multiline") );
			output.AppendLine();

			int appended = 0;

			foreach (var target in projectConfig.OutputTargets)
				{
				// We save output targets even if they're specified on the command line so we can still use Project.txt for secondary
				// settings.
				if (target.PropertyLocation.Source != PropertySource.SystemDefault)
					{
					if (target is Targets.HTMLOutputFolder)
						{  AppendHTMLOutputFolder((Targets.HTMLOutputFolder)target, output, projectFolder);  }
					else
						{  throw new NotImplementedException();  }

					output.AppendLine();
					appended++;
					}
				}

			if (appended > 0)
				{  output.AppendLine();  }

			output.Append( Locale.Get("NaturalDocs.Engine", "Project.txt.OutputHeaderText.multiline") );
			output.AppendLine("#");
			output.Append( Locale.Get("NaturalDocs.Engine", "Project.txt.HTMLOutputFoldersSyntax.multiline") );
			output.AppendLine();
			output.AppendLine();
			}


		protected void AppendHTMLOutputFolder (Targets.HTMLOutputFolder target, StringBuilder output, Path projectFolder)
			{
			if (target.PropertyLocation.Source == PropertySource.SystemDefault)
				{  return;  }

			output.Append("HTML Output Folder: ");

			Path relativePath = target.Folder.MakeRelativeTo(projectFolder);
			output.AppendLine( (relativePath != null ? relativePath : target.Folder) );

			AppendOverriddenProjectInfo(target.ProjectInfo, output);
			}


		/* Function: AppendGlobalSettings
		 * Appends the global properties in <projectConfig> to the passed string.
		 */
		protected void AppendGlobalSettings (StringBuilder output)
			{

			// Header

			output.Append( Locale.Get("NaturalDocs.Engine", "Project.txt.GlobalSettingsHeader.multiline") );
			output.AppendLine();


			// Defined values

			bool hasTabWidth = (projectConfig.TabWidthPropertyLocation.IsDefined &&
										projectConfig.TabWidthPropertyLocation.Source != PropertySource.SystemDefault &&
										projectConfig.TabWidthPropertyLocation.Source != PropertySource.CommandLine);
			bool hasDocumentedOnly = (projectConfig.DocumentedOnlyPropertyLocation.IsDefined &&
													projectConfig.DocumentedOnlyPropertyLocation.Source != PropertySource.SystemDefault &&
													projectConfig.DocumentedOnlyPropertyLocation.Source != PropertySource.CommandLine);
			bool hasAutoGroup = (projectConfig.AutoGroupPropertyLocation.IsDefined &&
										  projectConfig.AutoGroupPropertyLocation.Source != PropertySource.SystemDefault &&
										  projectConfig.AutoGroupPropertyLocation.Source != PropertySource.CommandLine);
				
			if (hasTabWidth)
				{
				output.AppendLine("Tab Width: " + projectConfig.TabWidth);
				output.AppendLine();
				}

			if (hasDocumentedOnly)
				{
				output.Append("Documented Only: " + (projectConfig.DocumentedOnly ? "Yes" : "No"));
				output.AppendLine();
				}

			if (hasAutoGroup)
				{
				output.Append("Auto Group: " + (projectConfig.AutoGroup ? "Yes" : "No"));
				output.AppendLine();
				}

			if (hasTabWidth || hasDocumentedOnly || hasAutoGroup)
				{  output.AppendLine();  }


			// Syntax reference

			output.Append( Locale.Get("NaturalDocs.Engine", "Project.txt.GlobalSettingsHeaderText.multiline") );

			if (!hasTabWidth)
				{
				output.AppendLine("#");
				output.Append( Locale.Get("NaturalDocs.Engine", "Project.txt.TabWidthSyntax.multiline") );
				}
			if (!hasDocumentedOnly)
				{
				output.AppendLine("#");
				output.Append( Locale.Get("NaturalDocs.Engine", "Project.txt.DocumentedOnlySyntax.multiline") );
				}
			if (!hasAutoGroup)
				{
				output.AppendLine("#");
				output.Append( Locale.Get("NaturalDocs.Engine", "Project.txt.AutoGroupSyntax.multiline") );
				}

			output.AppendLine();
			output.AppendLine();
			}



		// Group: Variables
		// __________________________________________________________________________

		protected ErrorList errorList;
		protected ProjectConfig projectConfig;


		// Group: Regular Expressions
		// __________________________________________________________________________

		protected Regex.Config.Yes yesRegex;
		protected Regex.Config.No noRegex;

		protected Regex.Config.Subtitle subtitleRegex;
		protected Regex.Config.Timestamp timestampRegex;
		protected Regex.Config.HomePage homePageRegex;
		protected Regex.Config.TabWidth tabWidthRegex;
		protected Regex.Config.DocumentedOnly documentedOnlyRegex;
		protected Regex.Config.AutoGroup autoGroupRegex;

		protected Regex.Config.SourceFolder sourceFolderRegex;
		protected Regex.Config.ImageFolder imageFolderRegex;
		protected Regex.Config.HTMLOutputFolder htmlOutputFolderRegex;
		protected Regex.Config.IgnoredSourceFolder ignoredSourceFolderRegex;
		protected Regex.Config.IgnoredSourceFolderPattern ignoredSourceFolderPatternRegex;
		
		}
	}