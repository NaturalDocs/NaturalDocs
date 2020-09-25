/* 
 * Class: CodeClear.NaturalDocs.Engine.Config.Manager
 * ____________________________________________________________________________
 * 
 * A class to manage the engine's configuration.
 * 
 * 
 * Topic: Usage
 * 
 *		- Create a <ProjectInfo> object with the command line configuration.  At minimum the project config folder must be set.
 *		
 *		- Call <Engine.Instance.Start()>, which will start this module.
 *		  
 *		- After the engine has been called all the properties are read-only.
 *		
 *		- All modules *MUST* check <ReparseEverything> before loading their own working data files and not bother if it's
 *		  set.
 *		
 */

// This file is part of Natural Docs, which is Copyright © 2003-2020 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using CodeClear.NaturalDocs.Engine.Errors;


namespace CodeClear.NaturalDocs.Engine.Config
	{
	public class Manager : Module
		{
		
		// Group: Functions
		// __________________________________________________________________________
		
		
		/* Constructor: Manager
		 */
		static Manager ()
			{
			systemDefaultConfig = new ProjectConfig(PropertySource.SystemDefault);

			// DEPENDENCY: Start() assumes these properties are set.

			systemDefaultConfig.TabWidth = DefaultTabWidth;
			systemDefaultConfig.TabWidthPropertyLocation = PropertySource.SystemDefault;

			systemDefaultConfig.DocumentedOnly = false;
			systemDefaultConfig.DocumentedOnlyPropertyLocation = PropertySource.SystemDefault;

			systemDefaultConfig.AutoGroup = true;
			systemDefaultConfig.AutoGroupPropertyLocation = PropertySource.SystemDefault;

			systemDefaultConfig.ShrinkFiles = true;
			systemDefaultConfig.ShrinkFilesPropertyLocation = PropertySource.SystemDefault;

			systemDefaultConfig.ProjectInfo.StyleName = "Default";
			systemDefaultConfig.ProjectInfo.StyleNamePropertyLocation = PropertySource.SystemDefault;
			}


		/* Constructor: Manager
		 */
		public Manager (Engine.Instance engineInstance) : base (engineInstance)
			{  
			projectConfigFolder = null;
			workingDataFolder = null;
			
			tabWidth = DefaultTabWidth;
			documentedOnly = false;
			autoGroup = true;
			shrinkFiles = true;

			reparseEverything_old = false;
			rebuildAllOutput_old = false;

			userWantsEverythingRebuilt = false;
			userWantsOutputRebuilt = false;
			}


		protected override void Dispose (bool strictRulesApply)
			{
			}


		/* Function: Start
		 * 
		 * Initializes the configuration and returns whether all the settings are correct and that execution is ready to begin.  
		 * If there are problems they are added as <Errors> to the errorList parameter.  This class is *not* designed to allow 
		 * multiple attempts.  If this function fails scrap the entire <Engine.Instance> and start again.
		 * 
		 * After <Start()> is called the properties of this class become read-only.  This function will add all the input and filter
		 * targets it has to <Files.Manager>, and all output targets to <Output.Manager>.
		 */
		public bool Start (ErrorList errorList, ProjectConfig commandLineConfig)
			{
			bool success = true;
			
			
			// Validate project config folder

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
				

			// Load and merge configuration files
			
			ProjectConfig combinedConfig = new ProjectConfig(PropertySource.Combined);
			MergeConfig(combinedConfig, commandLineConfig);

			var projectTxtParser = new Project_txt();

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
				var menuTxtParser = new Menu_txt();

				if (menuTxtParser.Load(ProjectConfigFolder + "/Menu.txt", out menuTxtConfig))
					{  MergeConfig(combinedConfig, menuTxtConfig);  }
				// No errors if this fails
				}
			// If neither file exists it's not an error condition.  Just treat it as if nothing was defined.

			MergeConfig(combinedConfig, systemDefaultConfig);

			if (success == false)
				{  return false;  }


			// Validate the working data folder, creating one if it doesn't exist.

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
				
				
			// Load the previous configuration state.  Remember that not every value in ProjectConfig is stored in Project.nd.
				
			ProjectConfig previousConfig = null;
			var projectNDParser = new Project_nd();

			if (!EngineInstance.HasIssues( StartupIssues.NeedToStartFresh ) && 
				System.IO.File.Exists(workingDataFolder + "/Project.nd"))
				{  
				if (!projectNDParser.Load(workingDataFolder + "/Project.nd", out previousConfig))
					{  previousConfig = null;  }
				}
				

			// Merge output target numbers from Project.nd into the settings.  These are not stored in Project.txt because they're
			// pretty expendible.

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

				
			// Target validation
						
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

			if (success == false)
				{  return false;  }


			// Determine the target numbers that are already used and reset duplicates.
			
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
				

			// Assign numbers to the entries that don't already have them and generate default input folder names.

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


			// Rebuild everything if there's an output target that didn't exist on the last run.

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
					RebuildAllOutput_old = true;
					break;
					}
				}

				
			// Apply global settings.

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
				ReparseEverything_old = true;
				}
			else if (previousConfig != null &&
					  shrinkFiles != previousConfig.ShrinkFiles)
				{
				RebuildAllOutput_old = true;
				}


			// Resave the configuration. Project_txt will handle skipping all the default and command line properties.

			projectTxtParser.Save(projectConfigFolder + "/Project.txt", combinedConfig, errorList);
			projectNDParser.Save(workingDataFolder + "/Project.nd", combinedConfig);
			
			
			// Create file sources and filters for Files.Manager
	
			foreach (var target in combinedConfig.InputTargets)
				{  EngineInstance.Files.AddFileSource(CreateFileSource(target));  }

			foreach (var target in combinedConfig.FilterTargets)
				{  EngineInstance.Files.AddFilter(CreateFilter(target));  }

			// Some people may put the output folder in their source folder.  Exclude it automatically.
			foreach (var target in combinedConfig.OutputTargets)
				{  
				var filter = CreateOutputFilter(target);

				if (filter != null)
					{  EngineInstance.Files.AddFilter(filter);  }
				}
				
				
			// Create more default filters

			EngineInstance.Files.AddFilter( new Engine.Files.Filters.IgnoredSourceFolder(ProjectConfigFolder) );
			EngineInstance.Files.AddFilter( new Engine.Files.Filters.IgnoredSourceFolder(WorkingDataFolder) );
			EngineInstance.Files.AddFilter( new Engine.Files.Filters.IgnoredSourceFolder(SystemConfigFolder) );
			EngineInstance.Files.AddFilter( new Engine.Files.Filters.IgnoredSourceFolder(SystemStyleFolder) );
			
			EngineInstance.Files.AddFilter( new Engine.Files.Filters.IgnoredSourceFolderRegex(new Regex.Config.DefaultIgnoredSourceFolderRegex()) );
			
			
			// Check all source folder entries against the filters.
			
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
			

			// Create targets for Output.Manager
			
			foreach (var target in combinedConfig.OutputTargets)
				{
				// Merge the global project info so it has a complete configuration.  The configuration files have already been saved without it.
				MergeProjectInfo(target.ProjectInfo, combinedConfig.ProjectInfo);

				EngineInstance.Output.AddTarget(CreateOutputTarget(target));  
				}


			// Purge stray output working data, since otherwise it will be left behind if an output entry is removed.

			Regex.Config.OutputPathNumber outputPathNumberRegex = new Regex.Config.OutputPathNumber();
			bool raisedPossiblyLongOperationEvent = false;

			string[] outputDataFolders = System.IO.Directory.GetDirectories(workingDataFolder, "Output*", System.IO.SearchOption.TopDirectoryOnly);
			foreach (string outputDataFolder in outputDataFolders)
				{  
				System.Text.RegularExpressions.Match match = outputPathNumberRegex.Match(outputDataFolder);
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
				System.Text.RegularExpressions.Match match = outputPathNumberRegex.Match(outputDataFile);
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
		public static void MergeConfig (ProjectConfig primaryConfig, ProjectConfig secondaryConfig)
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


			// Global project info

			MergeProjectInfo(primaryConfig.ProjectInfo, secondaryConfig.ProjectInfo);


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


		/* Function: MergeProjectInfo
		 * 
		 * Merges the settings of the secondary <ProjectInfo> into the primary one.  The primary <ProjectInfo> will only adopt the secondary 
		 * settings which it does not already have set.  When merging you should start with your most important configuration and merge
		 * others into it in order of importance.
		 */
		protected static void MergeProjectInfo (ProjectInfo primaryProjectInfo, ProjectInfo secondaryProjectInfo)
			{
			if (!primaryProjectInfo.TitlePropertyLocation.IsDefined)
				{
				primaryProjectInfo.Title = secondaryProjectInfo.Title;
				primaryProjectInfo.TitlePropertyLocation = secondaryProjectInfo.TitlePropertyLocation;

				// If we're using the secondary project info's title, we want to use its subtitle as well, even if it's undefined.  Setting only the
				// title should reset the subtitle.
				if (!primaryProjectInfo.SubtitlePropertyLocation.IsDefined)
					{
					primaryProjectInfo.Subtitle = secondaryProjectInfo.Subtitle;
					primaryProjectInfo.SubtitlePropertyLocation = secondaryProjectInfo.SubtitlePropertyLocation;
					}
				}

			if (!primaryProjectInfo.CopyrightPropertyLocation.IsDefined)
				{
				primaryProjectInfo.Copyright = secondaryProjectInfo.Copyright;
				primaryProjectInfo.CopyrightPropertyLocation = secondaryProjectInfo.CopyrightPropertyLocation;
				}

			if (!primaryProjectInfo.TimestampCodePropertyLocation.IsDefined)
				{
				primaryProjectInfo.TimestampCode = secondaryProjectInfo.TimestampCode;
				primaryProjectInfo.TimestampCodePropertyLocation = secondaryProjectInfo.TimestampCodePropertyLocation;
				}

			if (!primaryProjectInfo.StyleNamePropertyLocation.IsDefined)
				{
				primaryProjectInfo.StyleName = secondaryProjectInfo.StyleName;
				primaryProjectInfo.StyleNamePropertyLocation = secondaryProjectInfo.StyleNamePropertyLocation;
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

			MergeProjectInfo(primaryTarget.ProjectInfo, secondaryTarget.ProjectInfo);

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



		// Group: Support Functions
		// __________________________________________________________________________


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


	
		// Group: Properties
		// __________________________________________________________________________
		
		
		/* Property: ProjectConfigFolder
		 * 
		 * The project configuration folder's absolute <Path>, formerly called the project folder.  This must be 
		 * defined before <Start()> will succeed.  If it is set to a relative path, it will be converted to absolute 
		 * with the current working folder.  Once <Start()> is called it cannot be changed.
		 */
		public Path ProjectConfigFolder
			{
			get
				{  return projectConfigFolder;  }
			}
			
		
		/* Property: SystemConfigFolder
		 * The system configuration folder's absolute <Path>.
		 */
		public Path SystemConfigFolder
			{
			get
				{  
				return Path.FromAssembly( System.Reflection.Assembly.GetExecutingAssembly() ).ParentFolder + "/Config";  
				}
			}
			
			
		/* Property: SystemStyleFolder
		 * The system style folder's absolute <Path>.  All styles will be subfolders of this one.
		 */
		public Path SystemStyleFolder
			{
			get
				{  
				return Path.FromAssembly( System.Reflection.Assembly.GetExecutingAssembly() ).ParentFolder + "/Styles";  
				}
			}
			
			
		/* Property: WorkingDataFolder
		 * The working data folder's absolute <Path>.  Optional to set.  Once <Start()> is called it cannot
		 * be changed.
		 */
		public Path WorkingDataFolder
			{
			get
				{  return workingDataFolder;  }
			}
			

		/* Property: ReparseEverything_old
		 * 
		 * If set, all source files are going to be reparsed.  Modules *MUST* check this and rebuild their data files from scratch
		 * if it's set.  This is important because this gets set if certain data files are corrupted (such as <Languages.nd>) and thus
		 * various numeric IDs are not guaranteed to mean the same thing they did on the last run.
		 * 
		 * It is only possible to change this property to true.  You cannot turn it off once it's on.
		 */
		public bool ReparseEverything_old
			{
			get
				{  return reparseEverything_old;  }
			set
				{
				if (value == true)
					{
					reparseEverything_old = true;

					// xxx temporary shim between old and new systems
					EngineInstance.AddStartupIssues(StartupIssues.NeedToReparseAllFiles);
					}
				else
					{  throw new InvalidOperationException();  }
				}
			}
			
			
		/* Property: RebuildAllOutput_old
		 * 
		 * If set, all output is going to be regenerated.
		 * 
		 * It is only possible to change this property to true.  You cannot turn it off once it's on.
		 */
		public bool RebuildAllOutput_old
			{
			get
				{  return rebuildAllOutput_old;  }
			set
				{
				if (value == true)
					{  
					rebuildAllOutput_old = true;  

					// xxx temporary shim between old and new systems
					EngineInstance.AddStartupIssues(StartupIssues.NeedToRebuildAllOutput);
					}
				else
					{  throw new InvalidOperationException();  }
				}
			}
			
			
		/* Property: UserWantsEverythingRebuilt
		 * 
		 * If set, the user has indicated that everything from the previous run should be ignored and Natural Docs should start fresh.  It is
		 * only possible to set this property to true.  You cannot turn it off once it's on.
		 * 
		 * The property is given this name because it specifically represents whether the *user* requested everything to be rebuilt, such as 
		 * with -r on the  command line.  It should not be used by <Modules> to indicate that an internal issue requires everything to be 
		 * rebuilt.  <Modules> should use <Engine.Instance.AddStartupIssues()> and <Engine.Instance.HasIssues()> instead.
		 */
		public bool UserWantsEverythingRebuilt
			{
			get
				{  return userWantsEverythingRebuilt;  }
			set
				{
				if (value == true)
					{
					userWantsEverythingRebuilt = true;
					EngineInstance.AddStartupIssues(StartupIssues.NeedToStartFresh |
																	 StartupIssues.NeedToReparseAllFiles |
																	 StartupIssues.NeedToRebuildAllOutput);
					}
				else
					{  throw new InvalidOperationException();  }
				}
			}
			
			
		/* Property: UserWantsOutputRebuilt
		 * 
		 * If set, the user has indicated that all the output should be rebuilt.  It is only possible to set this property to true.  You cannot turn 
		 * it off once it's on.
		 * 
		 * The property is given this name because it specifically represents whether the *user* requested the output to be rebuilt, such as 
		 * with -ro on the command line.  It should not be used by <Modules> to indicate that an internal issue requires the output to be 
		 * rebuilt.  <Modules> should use <Engine.Instance.AddStartupIssues()> and <Engine.Instance.HasIssues()> instead.
		 */
		public bool UserWantsOutputRebuilt
			{
			get
				{  return userWantsOutputRebuilt;  }
			set
				{
				if (value == true)
					{  
					userWantsOutputRebuilt = true;  
					EngineInstance.AddStartupIssues(StartupIssues.NeedToRebuildAllOutput);
					}
				else
					{  throw new InvalidOperationException();  }
				}
			}
			
			
		/* Property: TabWidth
		 * The number of spaces a tab character should be expanded to.
		 */
		public int TabWidth
			{
			get
				{  return tabWidth;  }
			}


		/* Property: DocumentedOnly
		 * Whether only documented code elements should appear in the output.
		 */
		public bool DocumentedOnly
			{
			get
				{  return documentedOnly;  }
			}


		/* Property: AutoGroup
		 * Whether automatic grouping should be applied.
		 */
		public bool AutoGroup
			{
			get
				{  return autoGroup;  }
			}


		/* Property: ShrinkFiles
		 * Whether whitespace and comments should be removed from CSS and JavaScript files in the output.
		 */
		public bool ShrinkFiles
			{
			get
				{  return shrinkFiles;  }
			}


		/* Function: OutputWorkingDataFileOf
		 * Returns the working data file path for the passed output entry number.  It's up to the output entry whether
		 * it wants to actually create and use a file at this path, this just makes sure it has its own unique path.
		 */
		public Path OutputWorkingDataFileOf (int number)
			{
			return workingDataFolder + "/Output" + (number == 1 ? "" : number.ToString()) + ".nd";
			}
			
			
		/* Function: OutputWorkingDataFolderOf
		 * Returns the working data folder path for the passed output entry number.  It's up to the output entry whether
		 * it wants to actually create and use files in this folder, this just makes sure it has its own unique path.
		 */
		public Path OutputWorkingDataFolderOf (int number)
			{
			return workingDataFolder + "/Output" + (number == 1 ? "" : number.ToString());
			}
			
			

		// Group: Static Properties
		// __________________________________________________________________________
		
		
		/* Property: SystemDefaultConfig
		 * The system defaults that should be combined with user <ProjectConfigs>.
		 */
		static public ProjectConfig SystemDefaultConfig
			{
			get
				{  return systemDefaultConfig;  }
			}


		/* Property: KeySettingsForPaths
		 * The <Collections.KeySettings> that should be used when using paths as a key.
		 */
		static public Collections.KeySettings KeySettingsForPaths
			{
			get
				{
				// Natural Docs should treat paths as case-insensitive regardless of platform, as that makes its
				// behavior more consistent.  Otherwise you could have things like image links that work on one 
				// platform but not another.  However, code should always try to accommodate case-sensitive file
				// systems whenever possible.  If someone writes "(see symboltable.jpg)" it should still resolve to 
				// SymbolTable.jpg and the HTML output should  put SymbolTable.jpg in the image src attribute 
				// instead of symboltable.jpg.
				return Collections.KeySettings.IgnoreCase;
				}
			}


			
		// Group: Variables
		// __________________________________________________________________________
		
		
		/* String: projectConfigFolder
		 * The project configuration folder <Path>.  It will always be absolute.
		 */
		protected Path projectConfigFolder;
		
		/* String: workingDataFolder
		 * The working data folder <Path>, formerly always a subfolder of the project folder.  It will always
		 * be absolute.
		 */
		protected Path workingDataFolder;
		
		/* var: tabWidth
		 * The number of spaces tabs should be expanded to.
		 */
		protected int tabWidth;

		/* var: documentedOnly
		 * Whether only documented code elements should appear in the output.
		 */
		protected bool documentedOnly;

		/* var: autoGroup
		 * Whether automatic grouping should be applied.
		 */
		protected bool autoGroup;

		/* var: shrinkFiles
		 * Whether whitespace and comments should be removed from JavaScript and CSS files in the output.
		 */
		protected bool shrinkFiles;

		/* bool: reparseEverything_old
		 * Whether all source files should be reparsed.
		 */
		protected bool reparseEverything_old;
		
		/* bool: rebuildAllOutput_old
		 * Whether all output should be recreated from scatch.
		 */
		protected bool rebuildAllOutput_old;
		
		/* bool: userWantsEverythingRebuilt
		 * Whether the user wants Natural Docs to ignore everything from the previous run and start fresh.
		 */
		protected bool userWantsEverythingRebuilt;
		
		/* bool: userWantsOutputRebuilt
		 * Whether the user wants all output to be recreated from scatch.
		 */
		protected bool userWantsOutputRebuilt;
		


		// Group: Static Variables
		// __________________________________________________________________________

		static protected ProjectConfig systemDefaultConfig;


		// Group: Constants
		// __________________________________________________________________________

		public const int DefaultTabWidth = 4;
		
		}
	}