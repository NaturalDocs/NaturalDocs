/*
 * Class: CodeClear.NaturalDocs.Engine.Config.Manager
 * ____________________________________________________________________________
 */

// This file is part of Natural Docs, which is Copyright © 2003-2026 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using CodeClear.NaturalDocs.Engine.Errors;


namespace CodeClear.NaturalDocs.Engine.Config
	{
	public partial class Manager : Module
		{

		// Group: Initialization Functions
		// __________________________________________________________________________


		/* Function: Start_Stage1
		 *
		 * Initializes the configuration and returns whether all the settings are correct and that execution is ready to begin.
		 * If there are problems they are added as <Errors> to the errorList parameter.  This class is *not* designed to allow
		 * multiple attempts.  If this function fails scrap the entire <Engine.Instance> and start again.
		 *
		 * After <Start()> is called the properties of this class become read-only.  This function will add all the input and filter
		 * targets it has to <Files.Manager>, and all output targets to <Output.Manager>.
		 */
		public bool Start_Stage1 (ErrorList errorList, ProjectConfig commandLineConfig)
			{
			StartupIssues newStartupIssues = StartupIssues.None;
			bool success = true;


			//
			// Validate project config folder
			//

			projectConfigFolder = commandLineConfig.ProjectConfigFolder;

			if (!commandLineConfig.ProjectConfigFolderPropertyLocation.IsDefined ||
				String.IsNullOrEmpty(projectConfigFolder))
				{
				errorList.Add(
					message: Locale.Get("NaturalDocs.Engine", "Error.NoProjectConfigFolder"),
					configSource: PropertySource.CommandLine,
					property: "ProjectConfigFolder"
					);

				success = false;
				}

			else if (!System.IO.Directory.Exists(projectConfigFolder))
				{
				errorList.Add(
					message: Locale.Get("NaturalDocs.Engine", "Error.ProjectConfigFolderDoesntExist(name)", projectConfigFolder),
					propertyLocation: commandLineConfig.ProjectConfigFolderPropertyLocation,
					property: "ProjectConfigFolder"
					);

				success = false;
				}

			else if (projectConfigFolder == SystemConfigFolder)
				{
				errorList.Add(
					message: Locale.Get("NaturalDocs.Engine", "Error.ProjectConfigFolderCannotEqualSystemConfigFolder"),
					propertyLocation: commandLineConfig.ProjectConfigFolderPropertyLocation,
					property: "ProjectConfigFolder"
					);

				success = false;
				}

			if (success == false)
				{  return false;  }


			//
			// Load and merge configuration files
			//

			ProjectConfig combinedConfig = new ProjectConfig(PropertySource.Combined);
			MergeConfig(combinedConfig, commandLineConfig);

			var projectTxtParser = new ConfigFiles.TextFileParser();

			if (System.IO.File.Exists(projectConfigFolder + "/Project.txt"))
				{
				ProjectConfig projectTxtConfig;

				if (projectTxtParser.Load(projectConfigFolder + "/Project.txt", out projectTxtConfig, errorList))
					{  MergeConfig(combinedConfig, projectTxtConfig);  }
				else
					{  success = false;  }
				}
			else if (System.IO.File.Exists(ProjectConfigFolder + "/Menu.txt"))
				{
				// Try to extract information from a pre-2.0 Menu.txt instead.

				ProjectConfig menuTxtConfig;
				var menuTxtParser = new ConfigFiles.LegacyMenuFileParser();

				if (menuTxtParser.Load(ProjectConfigFolder + "/Menu.txt", out menuTxtConfig))
					{  MergeConfig(combinedConfig, menuTxtConfig);  }
				// No errors if this fails
				}
			// If neither file exists it's not an error condition.  Just treat it as if nothing was defined.

			MergeConfig(combinedConfig, systemDefaultConfig);

			if (success == false)
				{  return false;  }


			//
			// Validate the working data folder, creating one if it doesn't exist.
			//

			workingDataFolder = combinedConfig.WorkingDataFolder;

			if (!combinedConfig.WorkingDataFolderPropertyLocation.IsDefined ||
				String.IsNullOrEmpty(workingDataFolder))
				{  workingDataFolder = projectConfigFolder + "/Working Data";  }

			if (!System.IO.Directory.Exists(workingDataFolder))
				{
				try
					{  System.IO.Directory.CreateDirectory(workingDataFolder);  }
				catch (Exception e)
					{
					errorList.Add(
						message: Locale.Get("NaturalDocs.Engine", "Error.CantCreateWorkingDataFolder(name, exception)",
														workingDataFolder, e.Message),
						property: "WorkingDataFolder"
						);

					success = false;
					}
				}

			if (success == false)
				{  return false;  }


			//
			// Load the previous configuration state.  Remember that not every value in ProjectConfig is stored in Project.nd.
			//

			ProjectConfig previousConfig = null;
			var projectNDParser = new ConfigFiles.BinaryFileParser();

			if (!EngineInstance.HasIssues( StartupIssues.NeedToStartFresh ) &&
				System.IO.File.Exists(workingDataFolder + "/Project.nd"))
				{
				if (!projectNDParser.Load(workingDataFolder + "/Project.nd", out previousConfig))
					{  previousConfig = null;  }
				}


			//
			// Merge output target numbers from Project.nd into the settings.  These are not stored in Project.txt because they're
			// pretty expendible.
			//

			if (previousConfig != null)
				{
				foreach (var target in combinedConfig.OutputTargets)
					{
					foreach (var previousTarget in previousConfig.OutputTargets)
						{
						if (target.IsSameTarget(previousTarget))
							{
							target.Number = previousTarget.Number;
							break;
							}
						}
					}
				}


			//
			// Validate targets and encodings
			//

			if (combinedConfig.InputTargets.Count < 1)
				{
				errorList.Add(
					message: Locale.Get("NaturalDocs.Engine", "Error.NoInputTargets"),
					property: "InputTargets"
					);
				success = false;
				}
			if (combinedConfig.OutputTargets.Count < 1)
				{
				errorList.Add(
					message: Locale.Get("NaturalDocs.Engine", "Error.NoOutputTargets"),
					property: "OutputTargets"
					);
				success = false;
				}

			for (int i = 0; i < combinedConfig.InputTargets.Count; i++)
				{
				if (combinedConfig.InputTargets[i].Validate(errorList, i) == false)
					{  success = false;  }
				}
			for (int i = 0; i < combinedConfig.FilterTargets.Count; i++)
				{
				if (combinedConfig.FilterTargets[i].Validate(errorList, i) == false)
					{  success = false;  }
				}
			for (int i = 0; i < combinedConfig.OutputTargets.Count; i++)
				{
				if (combinedConfig.OutputTargets[i].Validate(errorList, i) == false)
					{  success = false;  }
				}

			// Input targets will validate their own encoding rules, so we only need to check the global ones.
			if (combinedConfig.InputSettings.HasCharacterEncodingRules)
				{
				foreach (var encodingRule in combinedConfig.InputSettings.CharacterEncodingRules)
					{
					if (encodingRule.Folder != null)
						{
						errorList.Add(
							Locale.Get("NaturalDocs.Engine", "Project.txt.EncodingFolderNotPartOfSourceFolder"),
							encodingRule.PropertyLocation
							);
						success = false;
						}

					else if (encodingRule.ValidateAndLookupID(errorList) == false)
						{  success = false;  }
					}
				}

			if (success == false)
				{  return false;  }


			//
			// Determine the target numbers that are already used and reset duplicates.
			//

			IDObjects.NumberSet usedSourceNumbers = new IDObjects.NumberSet();
			IDObjects.NumberSet usedImageNumbers = new IDObjects.NumberSet();

			foreach (var target in combinedConfig.InputTargets)
				{
				if (target.Number != 0)
					{
					if (target.Type == Files.InputType.Source)
						{
						if (usedSourceNumbers.Contains(target.Number))
							{
							target.Number = 0;
							target.NumberPropertyLocation = PropertySource.NotDefined;
							}
						else
							{  usedSourceNumbers.Add(target.Number);  }
						}

					else if (target.Type == Files.InputType.Image)
						{
						if (usedImageNumbers.Contains(target.Number))
							{
							target.Number = 0;
							target.NumberPropertyLocation = PropertySource.NotDefined;
							}
						else
							{  usedImageNumbers.Add(target.Number);  }
						}
					}
				}


			IDObjects.NumberSet usedOutputNumbers = new IDObjects.NumberSet();
			IDObjects.NumberSet outputNumbersToPurge = new IDObjects.NumberSet();

			foreach (var target in combinedConfig.OutputTargets)
				{
				if (target.Number != 0)
					{
					if (usedOutputNumbers.Contains(target.Number))
						{
						target.Number = 0;
						target.NumberPropertyLocation = PropertySource.NotDefined;

						// Since we don't know which of the two entries generated working data under this number, purge it to be safe.
						outputNumbersToPurge.Add(target.Number);
						}
					else
						{  usedOutputNumbers.Add(target.Number);  }
					}
				}


			//
			// Assign numbers to the targets that don't already have them and generate default input folder names.
			//

			foreach (var target in combinedConfig.InputTargets)
				{
				if (target.Type == Files.InputType.Source)
					{
					Targets.SourceFolder sourceTarget = (Targets.SourceFolder)target;

					if (target.Number == 0)
						{
						target.Number = usedSourceNumbers.LowestAvailable;
						target.NumberPropertyLocation = PropertySource.SystemGenerated;

						usedSourceNumbers.Add(target.Number);
						}

					if (sourceTarget.Name == null && combinedConfig.InputTargets.Count > 1)
						{
						sourceTarget.GenerateDefaultName();
						}
					}

				else if (target.Type == Files.InputType.Image)
					{
					if (target.Number == 0)
						{
						target.Number = usedImageNumbers.LowestAvailable;
						target.NumberPropertyLocation = PropertySource.SystemGenerated;

						usedImageNumbers.Add(target.Number);
						}
					}
				}

			foreach (var target in combinedConfig.OutputTargets)
				{
				if (target.Number == 0)
					{
					target.Number = usedOutputNumbers.LowestAvailable;
					target.NumberPropertyLocation = PropertySource.SystemGenerated;

					usedOutputNumbers.Add(target.Number);

					// If we're assigning it for the first time, purge it on the off chance that there's data left over from another
					// target.
					outputNumbersToPurge.Add(target.Number);
					}
				}


			//
			// Rebuild everything if there's an output target that didn't exist on the last run.
			//

			foreach (var target in combinedConfig.OutputTargets)
				{
				bool foundMatch = false;

				if (previousConfig != null)
					{
					foreach (var previousTarget in previousConfig.OutputTargets)
						{
						if (previousTarget.IsSameTarget(target))
							{
							foundMatch = true;
							break;
							}
						}
					}

				if (foundMatch == false)
					{
					newStartupIssues |= StartupIssues.NeedToRebuildAllOutput;
					break;
					}
				}


			//
			// Validate repository configurations and fill in implied properties
			//

			foreach (var target in combinedConfig.InputTargets)
				{
				if (target.Type == Files.InputType.Source)
					{
					Targets.SourceFolder sourceTarget = (Targets.SourceFolder)target;

					if (sourceTarget.HasRepositoryInfo)
						{
						// See if it's a known repository site.  Get it from the project URL rather than the declared name.
						var knownRepositorySite = RepositorySites.FromURL(sourceTarget.RepositoryProjectURL);

						// If it's a known repository site and the name isn't declared, add it.
						if (knownRepositorySite != null &&
							sourceTarget.RepositoryName == null)
							{
							sourceTarget.RepositoryName = knownRepositorySite.Name;
							sourceTarget.RepositoryNamePropertyLocation = PropertySource.SystemGenerated;
							}

						// If it's a known repository site, make sure the project URL is correct
						if (knownRepositorySite != null &&
							!knownRepositorySite.IsProjectURL(sourceTarget.RepositoryProjectURL))
							{
							errorList.Add(
								Locale.Get("NaturalDocs.Engine", "Project.txt.RepositoryURLMustBeToProjectPage(example)",
												knownRepositorySite.ExampleProjectURL),
								sourceTarget.RepositoryProjectURLPropertyLocation
								);
							success = false;
							}

						// If the source URL template isn't declared...
						if (sourceTarget.RepositorySourceURLTemplate == null)
							{
							// If it's not a known repository site, the source URL template must be explicitly declared
							if (knownRepositorySite == null)
								{
								errorList.Add(
									Locale.Get("NaturalDocs.Engine", "Project.txt.RepositoryRequiresLinkTemplate"),
									sourceTarget.RepositoryProjectURLPropertyLocation
									);
								success = false;
								}

							// If the site requires a branch, it must be explicitly declared
							else if (knownRepositorySite.RequiresBranch &&
									  sourceTarget.RepositoryBranch == null)
								{
								errorList.Add(
									Locale.Get("NaturalDocs.Engine", "Project.txt.RepositoryRequiresBranch"),
									sourceTarget.RepositoryProjectURLPropertyLocation
									);
								success = false;
								}

							// Otherwise we can generate the source URL template
							else
								{
								sourceTarget.RepositorySourceURLTemplate = knownRepositorySite.SourceURLTemplate(sourceTarget.RepositoryProjectURL, sourceTarget.RepositoryBranch);
								sourceTarget.RepositorySourceURLTemplatePropertyLocation = PropertySource.SystemGenerated;
								}
							}
						}
					}
				}

			if (success == false)
				{  return false;  }


			//
			// Apply global settings.
			//

			// DEPENDENCY: We assume all these settings are set in systemDefaultConfig so we don't have to worry about them being
			// undefined.
			tabWidth = combinedConfig.TabWidth;
			documentedOnly = combinedConfig.DocumentedOnly;
			autoGroup = combinedConfig.AutoGroup;
			shrinkFiles = combinedConfig.ShrinkFiles;

			if (previousConfig == null ||
				tabWidth != previousConfig.TabWidth ||
				documentedOnly != previousConfig.DocumentedOnly ||
				autoGroup != previousConfig.AutoGroup)
				{
				newStartupIssues |= StartupIssues.NeedToReparseAllFiles;
				}
			else if (previousConfig != null &&
					  shrinkFiles != previousConfig.ShrinkFiles)
				{
				newStartupIssues |= StartupIssues.NeedToRebuildAllOutput;
				}


			//
			// Resave the configuration. The text file parser will handle skipping all the default and command line properties.
			//

			projectTxtParser.Save(projectConfigFolder + "/Project.txt", combinedConfig, errorList);
			projectNDParser.Save(workingDataFolder + "/Project.nd", combinedConfig);


			//
			// Create file sources and filters for Files.Manager
			//

			foreach (var target in combinedConfig.InputTargets)
				{
				// Merge the project-wide settings into them so they have a complete configuration.  The configuration files have already
				// been saved without them.
				MergeInputSettings(target, combinedConfig.InputSettings);

				EngineInstance.Files.AddFileSource(CreateFileSource(target));
				}

			foreach (var target in combinedConfig.FilterTargets)
				{  EngineInstance.Files.AddFilter(CreateFilter(target));  }


			//
			// Create default filters
			//

			EngineInstance.Files.AddFilter( new Engine.Files.Filters.IgnoredSourceFolder(ProjectConfigFolder) );
			EngineInstance.Files.AddFilter( new Engine.Files.Filters.IgnoredSourceFolder(WorkingDataFolder) );
			EngineInstance.Files.AddFilter( new Engine.Files.Filters.IgnoredSourceFolder(SystemConfigFolder) );
			EngineInstance.Files.AddFilter( new Engine.Files.Filters.IgnoredSourceFolder(SystemStyleFolder) );

			EngineInstance.Files.AddFilter( new Engine.Files.Filters.IgnoredSourceFolderRegex(IsIgnoredSourcePathRegex()) );

			// Some people may put output folders in their source folders.  Exclude them automatically.
			foreach (var outputTarget in combinedConfig.OutputTargets)
				{
				var filter = CreateOutputFilter(outputTarget);

				if (filter != null)
					{  EngineInstance.Files.AddFilter(filter);  }
				}


			//
			// Check all source folder entries against the filters.
			//

			for (int i = 0; i < combinedConfig.InputTargets.Count; i++)
				{
				if (combinedConfig.InputTargets[i] is Targets.SourceFolder)
					{
					var sourceFolderTarget = (Targets.SourceFolder)combinedConfig.InputTargets[i];

					if (EngineInstance.Files.SourceFolderIsIgnored(sourceFolderTarget.Folder))
						{
						errorList.Add(
							message: Locale.Get("NaturalDocs.Engine", "Error.SourceFolderIsIgnored(sourceFolder)", sourceFolderTarget.Folder),
							propertyLocation: sourceFolderTarget.FolderPropertyLocation,
							property: "InputTargets[" + i + "].Folder"
							);

						success = false;
						}
					}
				}


			//
			// Create output targets for Output.Manager
			//

			foreach (var target in combinedConfig.OutputTargets)
				{
				// Merge the project-wide settings into them so they have a complete configuration.  The configuration files have already
				// been saved without them.
				MergeOutputSettings(target, combinedConfig.OutputSettings);

				EngineInstance.Output.AddTarget(CreateOutputTarget(target));
				}


			//
			// Purge stray output working data, since otherwise it will be left behind if an output entry is removed.
			//

			bool raisedPossiblyLongOperationEvent = false;

			string[] outputDataFolders = System.IO.Directory.GetDirectories(workingDataFolder, "Output*", System.IO.SearchOption.TopDirectoryOnly);
			foreach (string outputDataFolder in outputDataFolders)
				{
				System.Text.RegularExpressions.Match match = IsOutputDataPathRegex().Match(outputDataFolder);
				if (match.Success)
					{
					string numberString = match.Groups[1].ToString();
					int number;

					if (String.IsNullOrEmpty(numberString))
						{  number = 1;  }
					else
						{  number = int.Parse(numberString);  }

					if (outputNumbersToPurge.Contains(number) || !usedOutputNumbers.Contains(number))
						{
						// Since we're deleting an entire folder, mark it as a possibly long operation.  Some output formats may create many
						// files in there which could take a while to clear out.
						if (!raisedPossiblyLongOperationEvent)
							{
							EngineInstance.StartPossiblyLongOperation("PurgingOutputWorkingData");
							raisedPossiblyLongOperationEvent = true;
							}

						try
							{  System.IO.Directory.Delete(outputDataFolder, true);  }
						catch (Exception e)
							{
							if (!(e is System.IO.IOException || e is System.IO.DirectoryNotFoundException))
								{  throw;  }
							}
						}
					}
				}

			string[] outputDataFiles = System.IO.Directory.GetFiles(workingDataFolder, "Output*.nd", System.IO.SearchOption.TopDirectoryOnly);
			foreach (string outputDataFile in outputDataFiles)
				{
				System.Text.RegularExpressions.Match match = IsOutputDataPathRegex().Match(outputDataFile);
				if (match.Success)
					{
					string numberString = match.Groups[1].ToString();
					int number;

					if (String.IsNullOrEmpty(numberString))
						{  number = 1;  }
					else
						{  number = int.Parse(numberString);  }

					if (outputNumbersToPurge.Contains(number) || !usedOutputNumbers.Contains(number))
						{
						// Since this should just be a few individual files we don't have to worry about it being a possibly long operation,
						// although this will piggyback on that event if it was already raised.

						System.IO.File.Delete(outputDataFile);
						}
					}
				}

			if (raisedPossiblyLongOperationEvent)
				{  EngineInstance.EndPossiblyLongOperation();  }

			if (newStartupIssues != StartupIssues.None)
				{  EngineInstance.AddStartupIssues(newStartupIssues);  }

			started = success;
			return success;
			}


		/* Function: Start_Stage2
		 *
		 * Finishes validating the configuration, returning whether it was successful.  If there were any errors they will be added to
		 * errorList.
		 *
		 * This must be called after <Start_Stage1()> has been called, and also <Languages.Manager.Start_Stage1()>.  This
		 * finalizes any settings which also depend on <Languages.txt>.
		 *
		 * Dependencies:
		 *
		 *		- <Config.Manager.Start_Stage1()> must be started before this class can start.
		 *		- <Languages.Manager.Start_Stage1()> must be called and return true before this function can be called.
		 */
		public bool Start_Stage2 (ErrorList errorList)
			{
			bool success = true;


			//
			// Validate the home pages.  TextFileParser should have already checked whether the files exist.
			//

			foreach (var outputTarget in EngineInstance.Output.Targets)
				{
				if (outputTarget is Output.HTML.Target)
					{
					var htmlOutputTarget = (Output.HTML.Target)outputTarget;


					// If it's a source file instead of a HTML file...

					if (htmlOutputTarget.Config.HomePage != null &&
						htmlOutputTarget.Config.HomePageIsSourceFile)
						{
						AbsolutePath homePage = htmlOutputTarget.Config.HomePage;


						// Check that it appears in a file source.

						bool homePageInFileSource = false;

						foreach (var fileSource in EngineInstance.Files.FileSources)
							{
							if (fileSource.Contains(homePage))
								{
								homePageInFileSource = true;
								break;
								}
							}

						if (!homePageInFileSource)
							{
							errorList.Add(
								Locale.Get("NaturalDocs.Engine", "Error.HomePageSourceFileIsntInSourceFolders(file)", homePage),
								htmlOutputTarget.Config.HomePagePropertyLocation
								);

							success = false;
							continue;
							}


						// Check that it's not excluded by one of the filters.

						Path homePageParentFolder = homePage.ParentFolder;

						foreach (var filter in EngineInstance.Files.Filters)
							{
							if (filter.IgnoreSourceFolder(homePageParentFolder))
								{
								errorList.Add(
									Locale.Get("NaturalDocs.Engine", "Error.HomePageSourceFileIsIgnored(file)", homePage),
									htmlOutputTarget.Config.HomePagePropertyLocation
									);

								success = false;
								continue;
								}
							}


						// Check that its file extension is recognized by Languages.Manager.

						var homePageLanguage = EngineInstance.Languages.FromFileName(homePage);

						if (homePageLanguage == null)
							{
							errorList.Add(
								Locale.Get("NaturalDocs.Engine", "Error.HomePageIsntASourceFileOrHTML(file)", homePage),
								htmlOutputTarget.Config.HomePagePropertyLocation
								);

							success = false;
							continue;
							}


						// We won't be able to check whether it has content until after it's parsed, so that falls to
						// JSONMenu.BuildTabDataFile().

						}
					}
				}

			return success;
			}


		/* Function: MergeConfig
		 *
		 * Merges the settings of the secondary configuration into the primary one.  The primary configuration will only adopt the secondary
		 * settings which it does not already have set.  When merging you should start with your most important configuration and merge
		 * others into it in order of importance.
		 *
		 * If the primary config has input targets, only unset properties from matching targets in the secondary config will be copied.  Any
		 * targets not appearing in the primary config will be ignored.  If there are no input targets in the primary config, they will be copied
		 * from the secondary config.
		 *
		 * This system also applies for output and filter targets, with the exception of filter targets from a <PropertySource.SystemDefault>
		 * config.  Filters found in a <PropertySource.SystemDefault> config will always be added to the primary config.
		 */
		protected static void MergeConfig (ProjectConfig primaryConfig, ProjectConfig secondaryConfig)
			{

			// Project folders

			if (!primaryConfig.ProjectConfigFolderPropertyLocation.IsDefined)
				{
				primaryConfig.ProjectConfigFolder = secondaryConfig.ProjectConfigFolder;
				primaryConfig.ProjectConfigFolderPropertyLocation = secondaryConfig.ProjectConfigFolderPropertyLocation;
				}

			if (!primaryConfig.WorkingDataFolderPropertyLocation.IsDefined)
				{
				primaryConfig.WorkingDataFolder = secondaryConfig.WorkingDataFolder;
				primaryConfig.WorkingDataFolderPropertyLocation = secondaryConfig.WorkingDataFolderPropertyLocation;
				}


			// Project info

			MergeInputSettings(primaryConfig.InputSettings, secondaryConfig.InputSettings);
			MergeOutputSettings(primaryConfig.OutputSettings, secondaryConfig.OutputSettings);


			// Input targets

			int primarySourceFolderCount = 0;
			int primaryImageFolderCount = 0;

			foreach (var inputTarget in primaryConfig.InputTargets)
				{
				if (inputTarget is Targets.SourceFolder)
					{  primarySourceFolderCount++;  }
				else if (inputTarget is Targets.ImageFolder)
					{  primaryImageFolderCount++;  }
				}


			// Source input targets

			// Copy them from the secondary config if the primary config doesn't have any.  This allows things like having
			// input folders in Project.txt when there's none on the command line.
			if (primarySourceFolderCount == 0)
				{
				foreach (var secondaryTarget in secondaryConfig.InputTargets)
					{
					if (secondaryTarget is Targets.SourceFolder)
						{
						primaryConfig.InputTargets.Add( secondaryTarget.Duplicate() );
						}
					}
				}

			// If the primary config does have input targets, only copy secondary settings from matching targets.  This
			// allows things like having input folders specified on the command line but still being able to set the name and
			// number from Project.txt.  However, any targets in Project.txt that don't appear on the command line are ignored.
			// This allows people to change the command line and have it reflected immediately instead of having old sources
			// hang around until they're deleted from the command line AND Project.txt.
			else
				{
				foreach (var primaryTarget in primaryConfig.InputTargets)
					{
					if (primaryTarget is Targets.SourceFolder)
						{
						foreach (var secondaryTarget in secondaryConfig.InputTargets)
							{
							if (secondaryTarget.IsSameTarget(primaryTarget))
								{
								MergeInputTargets(primaryTarget, secondaryTarget);
								break;
								}
							}
						}
					}
				}


			// Image input targets

			// Copy them from the secondary config if the primary config doesn't have any.  This allows things like having
			// input folders in Project.txt when there's none on the command line.
			if (primaryImageFolderCount == 0)
				{
				foreach (var secondaryTarget in secondaryConfig.InputTargets)
					{
					if (secondaryTarget is Targets.ImageFolder)
						{
						primaryConfig.InputTargets.Add( secondaryTarget.Duplicate() );
						}
					}
				}

			// If the primary config does have input targets, only copy secondary settings from matching targets.  This
			// allows things like having input folders specified on the command line but still being able to set the name and
			// number from Project.txt.  However, any targets in Project.txt that don't appear on the command line are ignored.
			// This allows people to change the command line and have it reflected immediately instead of having old sources
			// hang around until they're deleted from the command line AND Project.txt.
			else
				{
				foreach (var primaryTarget in primaryConfig.InputTargets)
					{
					if (primaryTarget is Targets.ImageFolder)
						{
						foreach (var secondaryTarget in secondaryConfig.InputTargets)
							{
							if (secondaryTarget.IsSameTarget(primaryTarget))
								{
								MergeInputTargets(primaryTarget, secondaryTarget);
								break;
								}
							}
						}
					}
				}


			// Filter targets

			// Copy them from the secondary config if the primary config doesn't have any.  This allows things like specifying
			// filters in Project.txt when there's none on the command line.  Also copy system default filters no matter what.
			if (primaryConfig.FilterTargets.Count == 0 ||
				secondaryConfig.Source == PropertySource.SystemDefault)
				{
				foreach (var secondaryTarget in secondaryConfig.FilterTargets)
					{
					primaryConfig.FilterTargets.Add( secondaryTarget.Duplicate() );
					}
				}
			// Filter targets don't have secondary properties to merge so there's nothing to do if the primary config already had
			// some.  We don't combine them because we want changes to the command line reflected immediately instead of
			// getting tattooed in Project.txt until it's deleted there too.


			// Output targets

			// Output targets follow the same logic and behavior as input targets.
			if (primaryConfig.OutputTargets.Count == 0)
				{
				foreach (var secondaryTarget in secondaryConfig.OutputTargets)
					{
					primaryConfig.OutputTargets.Add( secondaryTarget.Duplicate() );
					}
				}
			else
				{
				foreach (var primaryTarget in primaryConfig.OutputTargets)
					{
					foreach (var secondaryTarget in secondaryConfig.OutputTargets)
						{
						if (secondaryTarget.IsSameTarget(primaryTarget))
							{
							MergeOutputTargets(primaryTarget, secondaryTarget);
							break;
							}
						}
					}
				}


			// Global settings

			if (!primaryConfig.TabWidthPropertyLocation.IsDefined)
				{
				primaryConfig.TabWidth = secondaryConfig.TabWidth;
				primaryConfig.TabWidthPropertyLocation = secondaryConfig.TabWidthPropertyLocation;
				}

			if (!primaryConfig.DocumentedOnlyPropertyLocation.IsDefined)
				{
				primaryConfig.DocumentedOnly = secondaryConfig.DocumentedOnly;
				primaryConfig.DocumentedOnlyPropertyLocation = secondaryConfig.DocumentedOnlyPropertyLocation;
				}

			if (!primaryConfig.AutoGroupPropertyLocation.IsDefined)
				{
				primaryConfig.AutoGroup = secondaryConfig.AutoGroup;
				primaryConfig.AutoGroupPropertyLocation = secondaryConfig.AutoGroupPropertyLocation;
				}

			if (!primaryConfig.ShrinkFilesPropertyLocation.IsDefined)
				{
				primaryConfig.ShrinkFiles = secondaryConfig.ShrinkFiles;
				primaryConfig.ShrinkFilesPropertyLocation = secondaryConfig.ShrinkFilesPropertyLocation;
				}

			}


		/* Function: MergeInputTargets
		 *
		 * Merges the settings of the secondary input target into the primary one.  The primary target will only adopt the secondary
		 * settings which it does not already have set.  When merging you should start with your most important target and merge
		 * others into it in order of importance.
		 *
		 * It is assumed that the two targets are of the same class and match with <Targets.InputBase.IsSameTarget()>.
		 */
		protected static void MergeInputTargets (Targets.Input primaryTarget, Targets.Input secondaryTarget)
			{
			#if DEBUG
			if (primaryTarget.GetType() != secondaryTarget.GetType())
				{  throw new Exception ("Cannot call MergeInputTargets() on two different types.");  }
			if (primaryTarget.IsSameTarget(secondaryTarget) == false)
				{  throw new Exception ("Cannot call MergeInputTargets() when they do not match with IsSameTarget().");  }
			#endif

			MergeInputSettings(primaryTarget, secondaryTarget);

			if (primaryTarget is Targets.SourceFolder)
				{
				Targets.SourceFolder primarySourceFolder = (Targets.SourceFolder)primaryTarget;
				Targets.SourceFolder secondarySourceFolder = (Targets.SourceFolder)secondaryTarget;

				if (!primarySourceFolder.FolderPropertyLocation.IsDefined)
					{
					primarySourceFolder.Folder = secondarySourceFolder.Folder;
					primarySourceFolder.FolderPropertyLocation = secondarySourceFolder.FolderPropertyLocation;
					}
				if (!primarySourceFolder.NamePropertyLocation.IsDefined)
					{
					primarySourceFolder.Name = secondarySourceFolder.Name;
					primarySourceFolder.NamePropertyLocation = secondarySourceFolder.NamePropertyLocation;
					}
				if (!primarySourceFolder.NumberPropertyLocation.IsDefined)
					{
					primarySourceFolder.Number = secondarySourceFolder.Number;
					primarySourceFolder.NumberPropertyLocation = secondarySourceFolder.NumberPropertyLocation;
					}
				}

			else if (primaryTarget is Targets.ImageFolder)
				{
				Targets.ImageFolder primaryImageFolder = (Targets.ImageFolder)primaryTarget;
				Targets.ImageFolder secondaryImageFolder = (Targets.ImageFolder)secondaryTarget;

				if (!primaryImageFolder.FolderPropertyLocation.IsDefined)
					{
					primaryImageFolder.Folder = secondaryImageFolder.Folder;
					primaryImageFolder.FolderPropertyLocation = secondaryImageFolder.FolderPropertyLocation;
					}
				if (!primaryImageFolder.NumberPropertyLocation.IsDefined)
					{
					primaryImageFolder.Number = secondaryImageFolder.Number;
					primaryImageFolder.NumberPropertyLocation = secondaryImageFolder.NumberPropertyLocation;
					}
				}

			else
				{  throw new NotImplementedException();  }
			}


		/* Function: MergeInputSettings
		 *
		 * Merges the settings of the secondary <OverridableInputSettings> into the primary one.  The primary one will only adopt
		 * the secondary settings which it does not already have set.  When merging you should start with your most important
		 * configuration and merge others into it in order of importance.
		 *
		 * Note that <Targets.Input> is derived from <OverridableInputSettings> so you can pass targets as one or both parameters.
		 */
		protected static void MergeInputSettings (OverridableInputSettings primarySettings, OverridableInputSettings secondarySettings)
			{
			if (secondarySettings.HasCharacterEncodingRules)
				{
				// We add the secondary settings to the beginning of the list instead of the end so they're overridden when evaluating
				// them all.
				primarySettings.AddCharacterEncodingRules(secondarySettings.CharacterEncodingRules, addToBeginning: true);
				}
			}


		/* Function: MergeOutputTargets
		 *
		 * Merges the settings of the secondary output target into the primary one.  The primary target will only adopt the secondary
		 * settings which it does not already have set.  When merging you should start with your most important target and merge
		 * others into it in order of importance.
		 *
		 * It is assumed that the two targets are of the same class and match with <Targets.OutputBase.IsSameTarget()>.
		 */
		protected static void MergeOutputTargets (Targets.Output primaryTarget, Targets.Output secondaryTarget)
			{
			#if DEBUG
			if (primaryTarget.GetType() != secondaryTarget.GetType())
				{  throw new Exception ("Cannot call MergeOutputTargets() on two different types.");  }
			if (primaryTarget.IsSameTarget(secondaryTarget) == false)
				{  throw new Exception ("Cannot call MergeOutputTargets() when they do not match with IsSameTarget().");  }
			#endif

			MergeOutputSettings(primaryTarget, secondaryTarget);

			if (!primaryTarget.NumberPropertyLocation.IsDefined)
			    {
			    primaryTarget.Number = secondaryTarget.Number;
			    primaryTarget.NumberPropertyLocation = secondaryTarget.NumberPropertyLocation;
			    }


			if (primaryTarget is Targets.HTMLOutputFolder)
				{
				if (!(primaryTarget as Targets.HTMLOutputFolder).FolderPropertyLocation.IsDefined)
					{
					(primaryTarget as Targets.HTMLOutputFolder).Folder = (secondaryTarget as Targets.HTMLOutputFolder).Folder;
					(primaryTarget as Targets.HTMLOutputFolder).FolderPropertyLocation = (secondaryTarget as Targets.HTMLOutputFolder).FolderPropertyLocation;
					}
				}
			else
				{  throw new NotImplementedException();  }
			}


		/* Function: MergeOutputSettings
		 *
		 * Merges the settings of the secondary <OverridableOutputSettings> into the primary one.  The primary one will only adopt
		 * the secondary settings which it does not already have set.  When merging you should start with your most important
		 * configuration and merge others into it in order of importance.
		 *
		 * Note that <Targets.Output> is derived from <OverridableOutputSettings> so you can pass targets as one or both parameters.
		 */
		protected static void MergeOutputSettings (OverridableOutputSettings primarySettings, OverridableOutputSettings secondarySettings)
			{
			if (!primarySettings.TitlePropertyLocation.IsDefined)
				{
				primarySettings.Title = secondarySettings.Title;
				primarySettings.TitlePropertyLocation = secondarySettings.TitlePropertyLocation;

				// If we're using the secondary settings' title, we want to use its subtitle as well, even if it's undefined.  Setting only the
				// title should reset the subtitle.
				if (!primarySettings.SubtitlePropertyLocation.IsDefined)
					{
					primarySettings.Subtitle = secondarySettings.Subtitle;
					primarySettings.SubtitlePropertyLocation = secondarySettings.SubtitlePropertyLocation;
					}
				}

			if (!primarySettings.CopyrightPropertyLocation.IsDefined)
				{
				primarySettings.Copyright = secondarySettings.Copyright;
				primarySettings.CopyrightPropertyLocation = secondarySettings.CopyrightPropertyLocation;
				}

			if (!primarySettings.TimestampCodePropertyLocation.IsDefined)
				{
				primarySettings.TimestampCode = secondarySettings.TimestampCode;
				primarySettings.TimestampCodePropertyLocation = secondarySettings.TimestampCodePropertyLocation;
				}

			if (!primarySettings.StyleNamePropertyLocation.IsDefined)
				{
				primarySettings.StyleName = secondarySettings.StyleName;
				primarySettings.StyleNamePropertyLocation = secondarySettings.StyleNamePropertyLocation;
				}

			if (!primarySettings.HomePagePropertyLocation.IsDefined)
				{
				primarySettings.HomePage = secondarySettings.HomePage;
				primarySettings.HomePagePropertyLocation = secondarySettings.HomePagePropertyLocation;
				}
			}


		/* Function: CreateFileSource
		 * Creates and returns a <Files.FileSource> from the passed input target config.
		 */
		protected virtual Files.FileSource CreateFileSource (Targets.Input target)
			{
			if (target is Targets.SourceFolder)
				{  return new Files.FileSources.SourceFolder(EngineInstance.Files, (Targets.SourceFolder)target);  }
			else if (target is Targets.ImageFolder)
				{  return new Files.FileSources.ImageFolder(EngineInstance.Files, (Targets.ImageFolder)target);  }
			else
				{  throw new NotImplementedException();  }
			}


		/* Function: CreateFilter
		 * Creates and returns a <Files.Filter> from the passed target config.
		 */
		protected virtual Files.Filter CreateFilter (Targets.Filter target)
			{
			if (target is Targets.IgnoredSourceFolder)
				{  return new Files.Filters.IgnoredSourceFolder((target as Targets.IgnoredSourceFolder).Folder);  }
			else if (target is Targets.IgnoredSourceFolderPattern)
				{  return new Files.Filters.IgnoredSourceFolderPattern((target as Targets.IgnoredSourceFolderPattern).Pattern);  }
			else
				{  throw new NotImplementedException();  }
			}


		/* Function: CreateOutputFilter
		 * Creates and returns a <Files.Filter> from the passed output target so that the target's files are excluded from being
		 * scanned with the source, or null if one isn't needed.
		 */
		protected virtual Files.Filter CreateOutputFilter (Targets.Output target)
			{
			if (target is Targets.HTMLOutputFolder)
				{  return new Files.Filters.IgnoredSourceFolder((target as Targets.HTMLOutputFolder).Folder);  }
			else
				{  throw new NotImplementedException();  }
			}


		/* Function: CreateOutputTarget
		 * Creates and returns an <Output.Target> from the passed target config.
		 */
		protected virtual Output.Target CreateOutputTarget (Targets.Output target)
			{
			if (target is Targets.HTMLOutputFolder)
				{  return new Output.HTML.Target(EngineInstance.Output, (Targets.HTMLOutputFolder)target);  }
			else
				{  throw new NotImplementedException();  }
			}

		}
	}
