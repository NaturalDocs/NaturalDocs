/*
 * Class: CodeClear.NaturalDocs.Engine.Config.Manager
 * ____________________________________________________________________________
 *
 * A class to manage the engine's configuration.
 *
 *
 * Topic: Usage
 *
 *		- Create a <ProjectConfig> object with the command line configuration.  At minimum the project config folder must be set.
 *
 *		- Call <Engine.Instance.Start()>, which will start this module.
 *
 *		- After the engine has been called all the properties are read-only.
 *
 *		- All modules *MUST* check <ReparseEverything> before loading their own working data files and not bother if it's
 *		  set.
 *
 */

// This file is part of Natural Docs, which is Copyright © 2003-2026 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Text.RegularExpressions;


namespace CodeClear.NaturalDocs.Engine.Config
	{
	public partial class Manager : Module
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

			systemDefaultConfig.OutputSettings.StyleName = "Default";
			systemDefaultConfig.OutputSettings.StyleNamePropertyLocation = PropertySource.SystemDefault;
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

			userWantsEverythingRebuilt = false;
			userWantsOutputRebuilt = false;
			}


		protected override void Dispose (bool strictRulesApply)
			{
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
				return System.AppContext.BaseDirectory + "/Config";
				}
			}


		/* Property: SystemStyleFolder
		 * The system style folder's absolute <Path>.  All styles will be subfolders of this one.
		 */
		public Path SystemStyleFolder
			{
			get
				{
				return System.AppContext.BaseDirectory + "/Styles";
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



		// Group: Regular Expressions
		// __________________________________________________________________________


		/* Regex: IsIgnoredSourcePathRegex
		 * Will match source paths that should be excluded from scanning by default, such as ".git" and ".vs".  This regex
		 * can be matched against the entire path and not just the folder segment.
		 */
		[GeneratedRegex("""(?:^|[/\\])\.(?:cvs|svn|git|hg|vs)(?:$|[/\\])""",
								  RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
		static private partial Regex IsIgnoredSourcePathRegex();


		/* Regex: IsOutputDataPathRegex
		 * Will match file and folder paths that represent stray Natural Docs working data.  This regex can be matched
		 * against the entire path and not just the folder segment.
		 */
		[GeneratedRegex("""(?:^|[/\\])Output([0-9]{0,9})(?:\.nd)?$""",
								  RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
		static private partial Regex IsOutputDataPathRegex();

		}
	}
