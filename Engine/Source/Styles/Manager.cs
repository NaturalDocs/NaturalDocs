/* 
 * Class: CodeClear.NaturalDocs.Engine.Styles.Manager
 * ____________________________________________________________________________
 * 
 * A class to manage all the output styles.
 * 
 * 
 * Usage:
 * 
 *		- Start the module with <Start()>.
 *		
 *		- Load styles with <LoadStyle()>.  The module has to be started before you can do this.
 *		
 *	
 *	Threading: Thread Safe
 *	
 *		This class is only changed while styles are being loaded.  After that point it should be treated as read-only.  If these
 *		rules are followed it is effectively thread safe.
 *		
 */

// This file is part of Natural Docs, which is Copyright © 2003-2021 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using CodeClear.NaturalDocs.Engine.Collections;
using CodeClear.NaturalDocs.Engine.Errors;


namespace CodeClear.NaturalDocs.Engine.Styles
	{
	public class Manager : Module
		{

		// Group: Functions
		// __________________________________________________________________________


		/* Constructor: Manager
		 */
		public Manager (Engine.Instance engineInstance) : base(engineInstance)
			{
			loadedStyles = new List<Style>();
			fileSource = null;
			}


		/* Function: Dispose
		 */
		override protected void Dispose (bool strictRulesApply)
			{
			}


		/* Function: Start
		 * Initializes the manager and returns whether all the settings are correct and that execution is ready to begin.  
		 * If there are problems they are added as <Errors> to the errorList parameter.  This class is *not* designed to allow 
		 * multiple attempts.  If this function fails scrap the entire <Engine.Instance> and start again.
		 */
		public bool Start (ErrorList errorList)
			{
			bool success = true;

			fileSource = new Styles.FileSource(this);
			EngineInstance.Files.AddFileSource(fileSource);

			started = success;
			return success;
			}


		/* Function: LoadStyle
		 * 
		 * Attempts to load a <Style> with the passed name, returning it if successful.  It will also load any inherited styles.
		 * If it couldn't find the style or there were errors in its configuration files it will add them to the error list and return 
		 * null.  Pass a <Config.PropertyLocation> so it will be able to attach where it was specified to the error message.
		 * 
		 * All loaded styles will be tracked by the <Styles.FileSource> automatically.  However, if you're adding a new style to
		 * an output target you should set <ReparseStyleFiles> so the new target will see all its files.  Otherwise if the style
		 * was previously used by a different output target it will be already loaded and the new one won't see them.
		 */
		public Style LoadStyle (string name, Errors.ErrorList errorList, Config.PropertyLocation propertyLocation)
			{
			// We have to locate the style on disk before checking the loaded styles.  This is so if there's two styles with the
			// same name it will always resolve to the correct location.

			Style style = LocateStyleOnDisk(name);

			if (style == null)
				{
				errorList.Add(Locale.Get("NaturalDocs.Engine", "Style.txt.CantFindStyle(name)", name), propertyLocation);
				return null;
				}


			// See if it's already loaded.

			Style loadedStyle = FindMatchingLoadedStyle(style);

			if (loadedStyle != null)
				{  return loadedStyle;  }


			// Load or generate the style's properties.

			int previousErrorCount = errorList.Count;

			if (style is Styles.CSSOnly)
				{
				style.AddInheritedStyle("Default", Config.PropertySource.SystemDefault);
				style.AddLinkedFile((style as Styles.CSSOnly).CSSFile,	Config.PropertySource.SystemDefault);
				}
			else if (style is Styles.Advanced)
				{
				ConfigFiles.TextFileParser styleParser = new ConfigFiles.TextFileParser();
				style = styleParser.Load((style as Styles.Advanced).ConfigFile, errorList);

				if (style == null)
					{  return null;  }
				}
			else
				{  throw new NotImplementedException();  }


			// Add it to the loaded styles list before processing inherited styles so circular dependencies don't create an 
			// infinite loop.

			loadedStyles.Add(style);


			// Now load any inherited styles as needed.

			if (style.Inherits != null)
				{
				for (int i = 0; i < style.Inherits.Count; i++)
					{
					var inheritStatement = style.Inherits[i];

					if (inheritStatement.Style == null)
						{
						Style inheritedStyle = LocateStyleOnDisk(inheritStatement.Name);

						if (inheritedStyle == null)
							{
							errorList.Add(Locale.Get("NaturalDocs.Engine", "Style.txt.CantFindInheritedStyle(name)", inheritedStyle.Name),
												inheritStatement.PropertyLocation);
							}
						else
							{
							// Have to do it this way because it's a struct.  You can't just modify style.Inherits[i].Style.
							inheritStatement.Style = LoadStyle(inheritStatement.Name, errorList, inheritStatement.PropertyLocation);
							style.Inherits[i] = inheritStatement;
							}
						}
					}
				}

			if (errorList.Count == previousErrorCount)
				{  return style;  }
			else
				{  return null;  }
			}


		public Style LoadStyle (string name, Errors.ErrorList errorList, Config.PropertySource propertySource)
			{
			return LoadStyle(name, errorList, new Config.PropertyLocation(propertySource));
			}


		/* Function: StyleExists
		 * Returns whether the passed style name resolves to any style present on disk.
		 */
		public bool StyleExists(string name)
			{
			return (LocateStyleOnDisk(name) != null);
			}



		// Group: Support Functions
		// __________________________________________________________________________


		/* Function: LocateStyleOnDisk
		 * Attempts to locate the style associated with the passed name, returning a basic <Style> object if found or null if not.  It 
		 * will not fill in the <Style>'s properties other than its name and paths.
		 */
		protected Style LocateStyleOnDisk (string name)
			{
			// StyleName folder in project folder
			Path testPath = EngineInstance.Config.ProjectConfigFolder + "/" + name + "/Style.txt";
			if (System.IO.File.Exists(testPath))
				{  return new Styles.Advanced(testPath);  }

			// StyleName.css file in project folder
			testPath = EngineInstance.Config.ProjectConfigFolder + "/" + name + ".css";
			if (System.IO.File.Exists(testPath))
				{  return new Styles.CSSOnly(testPath);  }

			// StyleName folder in system folder
			testPath = EngineInstance.Config.SystemStyleFolder + "/" + name + "/Style.txt";
			if (System.IO.File.Exists(testPath))
				{  return new Styles.Advanced(testPath);  }

			// StyleName.css file in system folder
			testPath = EngineInstance.Config.SystemStyleFolder + "/" + name + ".css";
			if (System.IO.File.Exists(testPath))
				{  return new Styles.CSSOnly(testPath);  }

			return null;
			}


		/* Function: FindMatchingLoadedStyle
		 * Determines if the passed <Style> already has a matching object in <loadedStyles>, returning it if so or null if not.
		 */
		protected Style FindMatchingLoadedStyle (Style style)
			{
			foreach (var loadedStyle in loadedStyles)
				{
				if (loadedStyle.IsSameFundamentalStyle(style))
					{  return loadedStyle;  }
				}

			return null;
			}



		// Group: Properties
		// __________________________________________________________________________


		/* Property: LoadedStyles
		 * A list of all the <Styles> that have been loaded.
		 */
		public List<Style> LoadedStyles
			{
			get
				{  return loadedStyles;  }
			}



		// Group: Static Variables
		// __________________________________________________________________________


		/* var: FileExtensions
		 * A <StringSet> of the supported extensions for files associated with styles.  This includes image extensions but not .html for
		 * the home page file.
		 */
		static public StringSet FileExtensions = new StringSet (KeySettings.IgnoreCase, new string[] { "css", "js", "json", 
																																				 "gif", "jpg", "jpeg", "png", "bmp", "svg",
																																				 "woff", "eot", "svg", "ttf" });

		/* var: LinkableFileExtensions
		 * A <StringSet> of the extensions for files that can be linked to pages in <Style.txt>.
		 */
		static public StringSet LinkableFileExtensions = new StringSet (KeySettings.IgnoreCase, new string[] { "css", "js", "json" });



		// Group: Variables
		// __________________________________________________________________________


		protected List<Style> loadedStyles;

		protected Styles.FileSource fileSource;

		}
	}