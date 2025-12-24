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

// This file is part of Natural Docs, which is Copyright © 2003-2025 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using CodeClear.NaturalDocs.Engine.Errors;


namespace CodeClear.NaturalDocs.Engine.Config.ConfigFiles
	{
	public partial class TextFileParser
		{

		// Group: Functions
		// __________________________________________________________________________


		/* Constructor: TextFileParser
		 */
		public TextFileParser ()
			{
			errorList = null;
			projectConfig = null;
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
				Targets.Input currentInputTarget = null;
				Targets.Output currentOutputTarget =  null;

				while (configFile.Get(out lcIdentifier, out value))
					{
					var propertyLocation = new PropertyLocation(PropertySource.ProjectFile, configFile.FileName, configFile.LineNumber);

					if (GetInputTargetHeader(lcIdentifier, value, propertyLocation, out var inputTarget, errorList))
						{
						currentInputTarget = inputTarget;
						currentOutputTarget = null;
						}
					else if (GetFilterTargetHeader(lcIdentifier, value, propertyLocation, out var filterTarget, errorList))
						{
						currentInputTarget = null;
						currentOutputTarget = null;
						}
					else if (GetOutputTargetHeader(lcIdentifier, value, propertyLocation, out var outputTarget, errorList))
						{
						currentInputTarget = null;
						currentOutputTarget = outputTarget;
						}
					else if (GetInputProperty(lcIdentifier, value, propertyLocation, currentInputTarget, errorList) ||
							   GetOutputProperty(lcIdentifier, value, propertyLocation, currentOutputTarget, errorList))
						{  }
					else if (GetGlobalProperty(lcIdentifier, value, propertyLocation, errorList))
						{
						currentInputTarget = null;
						currentOutputTarget = null;
						}
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


		/* Function: GetInputTargetHeader
		 * If the passed identifier starts an input target like "Source Folder", creates a new target object for it and returns true.  If it's
		 * a recognized identifier but there is a syntax error in the value it will add an error to <errorList> and still return true.  It only
		 * returns false for unrecognized identifiers.
		 */
		protected bool GetInputTargetHeader (string lcIdentifier, string value, PropertyLocation propertyLocation,
															   out Targets.Input newTarget, ErrorList errorList)
			{

			// Source folder

			Match match = IsSourceFolderRegexLC().Match(lcIdentifier);

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

			match = IsImageFolderRegexLC().Match(lcIdentifier);

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

			else
				{
				newTarget = null;
				return false;
				}
		    }


		/* Function: GetInputProperty
		 *
		 * If the passed identifier is an input property like Name or Encoding, adds it to the relevant object and returns true.  If
		 * inputTarget is specified it will be added to that.  If it's null it will be added to <ProjectConfig.InputSettings>.
		 *
		 * If it's a recognized identifier but there's a syntax error in the value it will add an error to <errorList> and still return true.
		 * It only returns false for unrecognized identifiers.
		 */
		protected bool GetInputProperty (string lcIdentifier, string value, PropertyLocation propertyLocation,
														Targets.Input inputTarget, ErrorList errorList)
			{

			// Name

			if (lcIdentifier == "name")
				{
				if (inputTarget != null &&
					inputTarget is Targets.SourceFolder &&
					(inputTarget as Targets.SourceFolder).Type == Files.InputType.Source)
					{
					(inputTarget as Targets.SourceFolder).Name = value;
					(inputTarget as Targets.SourceFolder).NamePropertyLocation = propertyLocation;
					}
				else
					{
					errorList.Add( Locale.Get("NaturalDocs.Engine", "Project.txt.PropertyOnlyAppliesToSourceFolders(property)", "Name"),
										 propertyLocation.FileName, propertyLocation.LineNumber );
					}

				return true;
				}


			// Encoding

			else if (IsEncodingRegexLC().IsMatch(lcIdentifier))
				{
				if (inputTarget == null ||
					(inputTarget is Targets.SourceFolder &&
					 (inputTarget as Targets.SourceFolder).Type == Files.InputType.Source))
					{
					var inputSettings = inputTarget ?? projectConfig.InputSettings;

					var encodingRule = GetEncodingRule (value, propertyLocation, inputTarget);
					inputSettings.AddCharacterEncodingRule(encodingRule);
					}
				else
					{
					errorList.Add( Locale.Get("NaturalDocs.Engine", "Project.txt.PropertyOnlyAppliesToSourceFolders(property)", "Encoding"),
										 propertyLocation.FileName, propertyLocation.LineNumber );
					}

				return true;
				}

			else
				{  return false;  }
			}


		/* Function: GetEncodingRule
		 * Converts the value of an Encoding line into a <CharacterEncodingRule>.  It does not validate whether the encoding name,
		 * number, or folder are valid.  If it contains a relative path it will be relative to the inputTarget if it's set, or the project folder
		 * if it's null.
		 */
		protected CharacterEncodingRule GetEncodingRule (string value, PropertyLocation propertyLocation, Targets.Input inputTarget)
			{
			// Possible formats:
			//
			//     Encoding: iso-8859-1
			//     Encoding: iso-8859-1 *.*
			//     Encoding: iso-8859-1 *.txt
			//
			//     Encoding: iso-8859-1 C:\My Project\Source
			//     Encoding: iso-8859-1 C:\My Project\Source\*.*
			//     Encoding: iso-8859-1 C:\My Project\Source\*.txt
			//
			//     Encoding: iso-8859-1 Source
			//     Encoding: iso-8859-1 Source\*.*
			//     Encoding: iso-8859-1 Source\*.txt
			//
			//     Encoding: 28591
			//     Encoding: 28591 *.*
			//     ...
			//     and all other permutations above with an integer in place of the encoding name


			// Split the path and the encoding

			value = value.CondenseWhitespace();  // the edges should already be trimmed

			string encoding;
			Path path;

			int spaceIndex = value.IndexOf(' ');

			if (spaceIndex == -1)
				{
				encoding = value;
				path = null;
				}
			else
				{
				encoding = value.Substring(0, spaceIndex);
				path = value.Substring(spaceIndex + 1);
				}


			// Split the folder and extension

			Path folder;
			string extension;

			if (path == null)
				{
				folder = null;
				extension = null;
				}
			else
				{
				string filename = path.NameWithoutPath;

				if (filename == "*.*")
					{
					folder = path.ParentFolder;
					extension = null;
					}
				else if (filename.StartsWith("*."))
					{
					folder = path.ParentFolder;
					extension = filename.Substring(2);
					}
				else
					{
					folder = path;
					extension = null;
					}

				// Calling ParentFolder on "*.*" or "*.ext" will return ".", so we want to replace that with null.
				if (folder == ".")
					{  folder = null;  }
				}


			// Make the path absolute

			AbsolutePath absoluteFolder;

			if (folder == null)
				{
				if (inputTarget != null && inputTarget is Targets.SourceFolder)
					{  absoluteFolder = (inputTarget as Targets.SourceFolder).Folder;  }
				else
					{  absoluteFolder = null;  }
				}
			else if (folder.IsAbsolute)
				{
				absoluteFolder = (AbsolutePath)folder;
				}
			else // folder.IsRelative
				{
				if (inputTarget != null && inputTarget is Targets.SourceFolder)
					{  absoluteFolder = (inputTarget as Targets.SourceFolder).Folder + '/' + folder;  }
				else
					{  absoluteFolder = (AbsolutePath)propertyLocation.FileName.ParentFolder + '/' + folder;  }
				}


			// Check whether it's an encoding name or code page number

			int codePage;
			string encodingName;

			// None of the valid encoding names start with a number, but use TryParse just in case
			if (encoding[0] >= '0' && encoding[0] <= '9' &&
				Int32.TryParse(encoding, out codePage))
				{
				// codePage set by TryParse
				encodingName = null;
				}
			else
				{
				codePage = 0;
				encodingName = encoding;
				}


			// Create and return the rule

			return new CharacterEncodingRule(codePage, encodingName, absoluteFolder, extension, propertyLocation);
			}


		/* Function: GetFilterTargetHeader
		 * If the passed identifier starts a filter target like "Ignore Source Folder", creates a new target for it and returns true.  If it's
		 * a recognized identifier but there is a syntax error in the value it will add an error to <errorList> and still return true.  It
		 * only returns false for unrecognized identifiers.
		 */
		protected bool GetFilterTargetHeader (string lcIdentifier, string value, PropertyLocation propertyLocation,
															   out Targets.Filter newTarget, ErrorList errorList)
			{

			// Ignored source folder

			if (IsIgnoreSourceFolderRegexLC().IsMatch(lcIdentifier))
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

			else if (IsIgnoreSourceFolderPatternRegexLC().IsMatch(lcIdentifier))
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


		/* Function: GetOutputTargetHeader
		 * If the passed identifier starts an output target like "HTML Output Folder", creates a new target for it and returns true.  If
		 * it's a recognized identifier but there is a syntax error in the value it will add an error to <errorList> and still return true.
		 * It only returns false for unrecognized identifiers.
		 */
		protected bool GetOutputTargetHeader (string lcIdentifier, string value, PropertyLocation propertyLocation,
																 out Targets.Output newTarget, ErrorList errorList)
			{

			// HTML output folder

			if (IsHTMLOutputFolderRegexLC().IsMatch(lcIdentifier))
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

			else
				{
				newTarget = null;
				return false;
				}
		    }


		/* Function: GetOutputProperty
		 *
		 * If the passed identifier is an output property like Style, adds it to the relevant object and returns true.  If outputTarget
		 * is specified it will be added to that.  If it's null it will be added to <ProjectConfig.OutputSettings>.
		 *
		 * If it's a recognized identifier but there's a syntax error in the value it will add an error to <errorList> and still return true.
		 * It only returns false for unrecognized identifiers.
		 */
		protected bool GetOutputProperty (string lcIdentifier, string value, PropertyLocation propertyLocation,
														  Targets.Output outputTarget, ErrorList errorList)
			{

			// Title

			if (lcIdentifier == "title")
				{
				var outputSettings = outputTarget ?? projectConfig.OutputSettings;

				outputSettings.Title = value.ConvertCopyrightAndTrademark();
				outputSettings.TitlePropertyLocation = propertyLocation;
				return true;
				}


			// Subtitle

			else if (IsSubtitleRegexLC().IsMatch(lcIdentifier))
				{
				var outputSettings = outputTarget ?? projectConfig.OutputSettings;

				outputSettings.Subtitle = value.ConvertCopyrightAndTrademark();
				outputSettings.SubtitlePropertyLocation = propertyLocation;
				return true;
				}


			// Copyright

			else if (lcIdentifier == "copyright")
				{
				var outputSettings = outputTarget ?? projectConfig.OutputSettings;

				outputSettings.Copyright = value.ConvertCopyrightAndTrademark();
				outputSettings.CopyrightPropertyLocation = propertyLocation;
				return true;
				}


			// Timestamp

			else if (IsTimestampRegexLC().IsMatch(lcIdentifier))
				{
				var outputSettings = outputTarget ?? projectConfig.OutputSettings;

				outputSettings.TimestampCode = value;
				outputSettings.TimestampCodePropertyLocation = propertyLocation;
				return true;
				}


			// Style

			else if (lcIdentifier == "style")
				{
				var outputSettings = outputTarget ?? projectConfig.OutputSettings;

				outputSettings.StyleName = value;
				outputSettings.StyleNamePropertyLocation = propertyLocation;
				return true;
				}


			// Home page

			else if (IsHomePageRegexLC().IsMatch(lcIdentifier))
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

				var outputSettings = outputTarget ?? projectConfig.OutputSettings;

				outputSettings.HomePage = (AbsolutePath)path;
				outputSettings.HomePagePropertyLocation = propertyLocation;
				return true;
				}


			else
				{
				return false;
				}
			}


		/* Function: GetGlobalProperty
		 * If the passed identifier is a global property like Tab Width, adds it to <projectConfig> and returns true.  If it is a recognized
		 * global property but has a syntax error in the value, it will add an error to <errorList> and still return true.  It only returns
		 * false on unrecognized identifiers.
		 */
		protected bool GetGlobalProperty (string lcIdentifier, string value, PropertyLocation propertyLocation, ErrorList errorList)
			{

			// Tab width

			if (IsTabWidthRegexLC().IsMatch(lcIdentifier))
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

			else if (IsDocumentedOnlyRegexLC().IsMatch(lcIdentifier))
				{
				if (ConfigFile.IsYes(value))
					{
					projectConfig.DocumentedOnly = true;
					projectConfig.DocumentedOnlyPropertyLocation = propertyLocation;
					}
				else if (ConfigFile.IsNo(value))
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

			else if (IsAutoGroupRegexLC().IsMatch(lcIdentifier))
				{
				if (ConfigFile.IsYes(value))
					{
					projectConfig.AutoGroup = true;
					projectConfig.AutoGroupPropertyLocation = propertyLocation;
					}
				else if (ConfigFile.IsNo(value))
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

			AppendProjectInfo(output, projectFolder);
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


		/* Function: AppendProjectInfo
		 * Appends the project information to the passed string, which includes the global <OverridableInputSettings> and
		 * <OverridableOutputSettings>.
		 */
		protected void AppendProjectInfo (StringBuilder output, Path projectFolder)
			{

			// Header

			output.Append( Locale.Get("NaturalDocs.Engine", "Project.txt.ProjectInfoHeader.multiline") );
			output.AppendLine();


			// Defined values

			bool hasTitle = (projectConfig.OutputSettings.TitlePropertyLocation.IsDefined &&
									projectConfig.OutputSettings.TitlePropertyLocation.Source != PropertySource.SystemDefault &&
									projectConfig.OutputSettings.TitlePropertyLocation.Source != PropertySource.CommandLine);
			bool hasSubtitle = (projectConfig.OutputSettings.SubtitlePropertyLocation.IsDefined &&
										projectConfig.OutputSettings.SubtitlePropertyLocation.Source != PropertySource.SystemDefault &&
										projectConfig.OutputSettings.SubtitlePropertyLocation.Source != PropertySource.CommandLine);
			bool hasCopyright = (projectConfig.OutputSettings.CopyrightPropertyLocation.IsDefined &&
											projectConfig.OutputSettings.CopyrightPropertyLocation.Source != PropertySource.SystemDefault &&
											projectConfig.OutputSettings.CopyrightPropertyLocation.Source != PropertySource.CommandLine);
			bool hasTimestampCode = (projectConfig.OutputSettings.TimestampCodePropertyLocation.IsDefined &&
													projectConfig.OutputSettings.TimestampCodePropertyLocation.Source != PropertySource.SystemDefault &&
													projectConfig.OutputSettings.TimestampCodePropertyLocation.Source != PropertySource.CommandLine);
			bool hasStyleName = (projectConfig.OutputSettings.StyleNamePropertyLocation.IsDefined &&
											 projectConfig.OutputSettings.StyleNamePropertyLocation.Source != PropertySource.SystemDefault &&
											 projectConfig.OutputSettings.StyleNamePropertyLocation.Source != PropertySource.CommandLine);
			bool hasHomePage = (projectConfig.OutputSettings.HomePagePropertyLocation.IsDefined &&
											projectConfig.OutputSettings.HomePagePropertyLocation.Source != PropertySource.SystemDefault &&
											projectConfig.OutputSettings.HomePagePropertyLocation.Source != PropertySource.CommandLine);

			bool hasEncodingRules = false;
			if (projectConfig.InputSettings.HasCharacterEncodingRules)
				{
				foreach (var encodingRule in projectConfig.InputSettings.CharacterEncodingRules)
					{
					if (encodingRule.PropertyLocation.Source != PropertySource.SystemDefault &&
						encodingRule.PropertyLocation.Source != PropertySource.CommandLine)
						{
						hasEncodingRules = true;
						break;
						}
					}
				}

			if (hasTitle)
				{
				output.AppendLine("Title: " + projectConfig.OutputSettings.Title);

				if (!hasSubtitle)
					{  output.AppendLine();  }
				}

			if (hasSubtitle)
				{
				output.AppendLine("Subtitle: " + projectConfig.OutputSettings.Subtitle);
				output.AppendLine();
				}

			if (hasCopyright)
				{
				output.AppendLine("Copyright: " + projectConfig.OutputSettings.Copyright);
				output.AppendLine();
				}

			if (hasTimestampCode)
				{
				output.AppendLine("Timestamp: " + projectConfig.OutputSettings.TimestampCode);
				output.Append( Locale.Get("NaturalDocs.Engine", "Project.txt.TimestampSubstitutions.multiline") );
				output.AppendLine();
				}

			if (hasStyleName)
				{
				output.AppendLine("Style: " + projectConfig.OutputSettings.StyleName);

				if (!hasHomePage)
					{  output.AppendLine();  }
				}

			if (hasHomePage)
				{
				Path relativePath = projectConfig.OutputSettings.HomePage.MakeRelativeTo(projectConfig.ProjectConfigFolder);

				output.AppendLine("Home Page: " + (relativePath != null ? relativePath : projectConfig.OutputSettings.HomePage));
				output.AppendLine();
				}

			if (hasEncodingRules)
				{
				AppendCharacterEncodingRules(projectConfig.InputSettings.CharacterEncodingRules, output, indent: false);
				output.AppendLine();
				}

			if (hasTitle || hasSubtitle || hasCopyright || hasTimestampCode || hasStyleName || hasHomePage || hasEncodingRules)
				{  output.AppendLine();  }


			// Syntax reference

			output.Append( Locale.Get("NaturalDocs.Engine", "Project.txt.ProjectInfoHeaderText.multiline") );
			output.AppendLine("#");
			output.Append( Locale.Get("NaturalDocs.Engine", "Project.txt.TitleSyntax.multiline") );
			output.AppendLine("#");
			output.Append( Locale.Get("NaturalDocs.Engine", "Project.txt.SubtitleSyntax.multiline") );
			output.AppendLine("#");
			output.Append( Locale.Get("NaturalDocs.Engine", "Project.txt.CopyrightSyntax.multiline") );
			output.AppendLine("#");
			output.Append( Locale.Get("NaturalDocs.Engine", "Project.txt.TimestampSyntax.multiline") );
			output.AppendLine("#");
			output.Append( Locale.Get("NaturalDocs.Engine", "Project.txt.TimestampSubstitutions.multiline") );
			output.AppendLine("#");
			output.Append( Locale.Get("NaturalDocs.Engine", "Project.txt.StyleSyntax.multiline") );
			output.AppendLine("#");
			output.Append( Locale.Get("NaturalDocs.Engine", "Project.txt.HomePageSyntax.multiline") );
			output.AppendLine("#");
			output.Append( Locale.Get("NaturalDocs.Engine", "Project.txt.EncodingSyntax.multiline") );
			output.AppendLine();
			output.AppendLine();
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

			bool hasName = (target.NamePropertyLocation.IsDefined &&
									 target.NamePropertyLocation.Source != PropertySource.SystemDefault);

			int encodingRules = 0;
			if (target.HasCharacterEncodingRules)
				{
				foreach (var encodingRule in target.CharacterEncodingRules)
					{
					if (encodingRule.PropertyLocation.Source != PropertySource.SystemDefault &&
						encodingRule.PropertyLocation.Source != PropertySource.CommandLine)
						{
						encodingRules++;
						}
					}
				}

			output.Append("Source Folder");

			if (target.NumberPropertyLocation.IsDefined &&
				target.NumberPropertyLocation.Source != PropertySource.SystemDefault &&
				target.Number != 1)
				{  output.Append(" " + target.Number);  }

			output.Append(": ");

			Path relativePath = target.Folder.MakeRelativeTo(projectFolder);
			output.AppendLine( (relativePath != null ? relativePath : target.Folder) );

			if (hasName)
				{  output.AppendLine("   Name: " + target.Name);  }

			if (encodingRules > 1)
				{
				if (hasName)
					{  output.AppendLine();  }

				AppendCharacterEncodingRules(target.CharacterEncodingRules, output, indent: true, foldersRelativeTo: target.Folder);
				}
			}


		/* Function: AppendCharacterEncodingRules
		 * Appends any <CharacterEncodingRules> to the passed string.
		 */
		protected void AppendCharacterEncodingRules (IList<CharacterEncodingRule> encodingRules, StringBuilder output, bool indent,
																			 AbsolutePath foldersRelativeTo = default)
			{
			foreach (var encodingRule in encodingRules)
				{
				if (encodingRule.PropertyLocation.Source != PropertySource.SystemDefault &&
					encodingRule.PropertyLocation.Source != PropertySource.CommandLine)
					{
					if (indent)
						{  output.Append("   ");  }

					output.Append("Encoding: ");

					if (encodingRule.CharacterEncodingName != null)
						{  output.Append(encodingRule.CharacterEncodingName);  }
					else
						{  output.Append(encodingRule.CharacterEncodingID);  }

					Path encodingFolder = encodingRule.Folder;

					// Make a relative version of the encoding folder
					if (encodingFolder != null && foldersRelativeTo != null)
						{
						Path relativeFolder = encodingRule.Folder.MakeRelativeTo(foldersRelativeTo);

						// If the folder isn't relative to the passed one, leave it as is
						if (relativeFolder == null)
							{  }

						// If it's the same as the passed folder it will reduce to ".", so omit it entirely
						else if (relativeFolder == ".")
							{  encodingFolder = null;  }

						else
							{  encodingFolder = relativeFolder;  }
						}

					if (encodingFolder != null)
						{
						output.Append(' ');
						output.Append(encodingFolder);
						}

					if (encodingRule.FileExtension != null)
						{
						if (encodingFolder != null)
							{  output.Append(SystemInfo.PathSeparatorCharacter);  }
						else
							{  output.Append(' ');  }

						output.Append("*.");
						output.Append(encodingRule.FileExtension);
						}

					output.AppendLine();
					}
				}
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

			AppendOverriddenOutputSettings(target, output);
			}


		/* Function: AppendOverriddenOutputSettings
		 * Appends any <OverridableOutputSettings> defined in the output target to the passed string.
		 */
		protected void AppendOverriddenOutputSettings (Targets.Output outputTarget, StringBuilder output)
			{
			if (outputTarget.TitlePropertyLocation.IsDefined &&
				outputTarget.TitlePropertyLocation.Source != PropertySource.SystemDefault)
				{  output.AppendLine("   Title: " + outputTarget.Title);  }

			if (outputTarget.SubtitlePropertyLocation.IsDefined &&
				outputTarget.SubtitlePropertyLocation.Source != PropertySource.SystemDefault)
				{  output.AppendLine("   Subtitle: " + outputTarget.Subtitle);  }

			if (outputTarget.CopyrightPropertyLocation.IsDefined &&
				outputTarget.CopyrightPropertyLocation.Source != PropertySource.SystemDefault)
				{  output.AppendLine("   Copyright: " + outputTarget.Copyright);  }

			if (outputTarget.TimestampCodePropertyLocation.IsDefined &&
				outputTarget.TimestampCodePropertyLocation.Source != PropertySource.SystemDefault)
				{  output.AppendLine("   Timestamp: " + outputTarget.TimestampCode);  }

			if (outputTarget.StyleNamePropertyLocation.IsDefined &&
				outputTarget.StyleNamePropertyLocation.Source != PropertySource.SystemDefault)
				{  output.AppendLine("   Style: " + outputTarget.StyleName);   }

			if (outputTarget.HomePagePropertyLocation.IsDefined &&
				outputTarget.HomePagePropertyLocation.Source != PropertySource.SystemDefault)
				{
				Path relativePath = outputTarget.HomePage.MakeRelativeTo(projectConfig.ProjectConfigFolder);
				output.AppendLine("   Home Page: " + (relativePath != null ? relativePath : outputTarget.HomePage));
				}
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
		//
		// These are internal rather than private because they may also be used by <LegacyMenuFileParser>.


		/* Regex: IsSubtitleRegexLC
		 * Will match if the entire string is the property name "subtitle" or one of its acceptable variants.  Assumes the
		 * input string is already in lowercase.
		 */
		[GeneratedRegex("""^sub[ -]?title$""",
								  RegexOptions.Singleline | RegexOptions.CultureInvariant)]
		static internal partial Regex IsSubtitleRegexLC();


		/* Regex: IsTimestampRegexLC
		 * Will match if the entire string is the property name "timestamp" or one of its acceptable variants.  Assumes the
		 * input string is already in lowercase.
		 */
		[GeneratedRegex("""^time[ -]?stamp$""",
								  RegexOptions.Singleline | RegexOptions.CultureInvariant)]
		static internal partial Regex IsTimestampRegexLC();


		/* Regex: IsHomePageRegexLC
		 * Will match if the entire string is the property name "home page" or one of its acceptable variants.  Assumes the
		 * input string is already in lowercase.
		 */
		[GeneratedRegex("""^home[ -]?page$""",
								  RegexOptions.Singleline | RegexOptions.CultureInvariant)]
		static internal partial Regex IsHomePageRegexLC();


		/* Regex: IsTabWidthRegexLC
		 * Will match if the entire string is the property name "tab width" or one of its acceptable variants.  Assumes the
		 * input string is already in lowercase.
		 */
		[GeneratedRegex("""^tabs?(?:[ -]?(?:width|length|len))?$""",
								  RegexOptions.Singleline | RegexOptions.CultureInvariant)]
		static internal partial Regex IsTabWidthRegexLC();


		/* Regex: IsDocumentedOnlyRegexLC
		 * Will match if the entire string is the property name "documented only" or one of its acceptable variants.
		 * Assumes the input string is already in lowercase.
		 */
		[GeneratedRegex("""^(?:documented[ -]only|only[ -]documented)$""",
								  RegexOptions.Singleline | RegexOptions.CultureInvariant)]
		static internal partial Regex IsDocumentedOnlyRegexLC();


		/* Regex: IsAutoGroupRegexLC
		 * Will match if the entire string is the property name "auto group" or one of its acceptable variants.  Assumes the
		 * input string is already in lowercase.
		 */
		[GeneratedRegex("""^auto(?:matic)?[ -]?group(?:ing)?$""",
								  RegexOptions.Singleline | RegexOptions.CultureInvariant)]
		static internal partial Regex IsAutoGroupRegexLC();


		/* Regex: IsSourceFolderRegexLC
		 *
		 * Will match if the entire string is the property name "source folder" or one of its acceptable variants.  Assumes
		 * the input string is already in lowercase.
		 *
		 * Capture Groups:
		 *
		 *		1 - The source folder number, if it exists, such as "2" in "Source Folder 2".
		 */
		[GeneratedRegex("""^(?:source|input) (?:folder|dir|directory) ?([0-9]*)$""",
								  RegexOptions.Singleline | RegexOptions.CultureInvariant)]
		static internal partial Regex IsSourceFolderRegexLC();


		/* Regex: IsImageFolderRegexLC
		 *
		 * Will match if the entire string is the property name "image folder" or one of its acceptable variants.  Assumes
		 * the input string is already in lowercase.
		 *
		 * Capture Groups:
		 *
		 *		1 - The source folder number, if it exists, such as "2" in "Image Folder 2".
		 */
		[GeneratedRegex("""^image (?:folder|dir|directory) ?([0-9]*)$""",
								  RegexOptions.Singleline | RegexOptions.CultureInvariant)]
		static internal partial Regex IsImageFolderRegexLC();


		/* Regex: IsHTMLOutputFolderRegexLC
		 * Will match if the entire string is the property name "html output folder" or one of its acceptable variants.
		 * Assumes the input string is already in lowercase.
		 */
		[GeneratedRegex("""^(?:framed ?)?html (?:output )?(?:folder|dir|directory) ?[0-9]*$""",
								  RegexOptions.Singleline | RegexOptions.CultureInvariant)]
		static internal partial Regex IsHTMLOutputFolderRegexLC();


		/* Regex: IsIgnoreSourceFolderRegexLC
		 * Will match if the entire string is the property name "ignore source folder" or one of its acceptable variants.
		 * Assumes the input string is already in lowercase.
		 */
		[GeneratedRegex("""^ignored? (?:(?:source|input) )?(?:folder|dir|directory) ?[0-9]*$""",
								  RegexOptions.Singleline | RegexOptions.CultureInvariant)]
		static internal partial Regex IsIgnoreSourceFolderRegexLC();


		/* Regex: IsIgnoreSourceFolderPatternRegexLC
		 * Will match if the entire string is the property name "ignore source folder pattern" or one of its acceptable
		 * variants.  Assumes the input string is already in lowercase.
		 */
		[GeneratedRegex("""^ignored? (?:(?:source|input) )?(?:(?:folder|dir|directory) )?pattern ?[0-9]*$""",
								  RegexOptions.Singleline | RegexOptions.CultureInvariant)]
		static internal partial Regex IsIgnoreSourceFolderPatternRegexLC();


		/* Regex: IsEncodingRegexLC
		 * Will match if the entire string is the property name "encoding" or one of its acceptable variants.  Assumes the
		 * input string is already in lowercase.
		 */
		[GeneratedRegex("""^(?:char|character)?[ \-]?(?:encoding|set)$""",
								  RegexOptions.Singleline | RegexOptions.CultureInvariant)]
		static internal partial Regex IsEncodingRegexLC();

		}
	}
