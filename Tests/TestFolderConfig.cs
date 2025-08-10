/*
 * Class: CodeClear.NaturalDocs.Tests.TestFolderConfig
 * ____________________________________________________________________________
 *
 * A class to handle a test folder configuration.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2025 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using CodeClear.NaturalDocs.Engine;
using CodeClear.NaturalDocs.Engine.Errors;


namespace CodeClear.NaturalDocs.Tests
	{
	public partial class TestFolderConfig
		{

		// Group: Static Functions
		// __________________________________________________________________________


		/* Function: Load
		 * Attempts to load and parse a configuration file.  If successful it will return true and its contents as a <TestFolderConfig> object.
		 * Otherwise it will return false and add error messages to the <ErrorList>.
		 */
		static public bool Load (AbsolutePath configFilePath, out TestFolderConfig testFolderConfig, ErrorList errorList)
			{
			int initialErrorCount = errorList.Count;

			using (var configFile = new ConfigFile())
				{
				bool openResult = configFile.Open(configFilePath,
																 Engine.Config.PropertySource.TestFolderConfigurationFile,
																 ConfigFile.FileFormatFlags.CondenseIdentifierWhitespace |
																 ConfigFile.FileFormatFlags.MakeIdentifiersLowercase,
																 errorList);

				if (openResult == false)
					{
					testFolderConfig = null;
					return false;
					}

				testFolderConfig = new TestFolderConfig();
				string lcIdentifier, value;

				while (configFile.Get(out lcIdentifier, out value))
					{
					if (lcIdentifier == "test type")
						{  testFolderConfig.testType = value;  }

					else if (lcIdentifier == "config")
						{
						Path pathAsEntered = value;
						AbsolutePath absolutePath;

						if (pathAsEntered.IsAbsolute)
							{  absolutePath = (AbsolutePath)pathAsEntered;  }
						else
							{  absolutePath = (AbsolutePath)(configFilePath.ParentFolder + "/" + pathAsEntered);  }

						if (System.IO.Directory.Exists(absolutePath))
							{  testFolderConfig.engineConfigFolder = absolutePath;  }
						else
							{  configFile.AddError("Config folder \"" + absolutePath + "\" does not exist.", lcIdentifier);  }
						}

					else if (lcIdentifier == "keep html")
						{
						if (ConfigFile.IsYes(value))
							{  testFolderConfig.keepHTML = true;  }
						else if (ConfigFile.IsNo(value))
							{  testFolderConfig.keepHTML = false;  }
						else
							{  configFile.AddError("Unrecognized value \"" + value + "\" for Keep HTML.", lcIdentifier);  }
						}

					else if (lcIdentifier == "title")
						{  testFolderConfig.htmlTitle = value;  }
					else if (lcIdentifier == "subtitle")
						{  testFolderConfig.htmlSubtitle = value;  }
					else if (lcIdentifier == "style")
						{  testFolderConfig.htmlStyle = value;  }

					else if (lcIdentifier == "auto-group")
						{
						if (ConfigFile.IsYes(value))
							{  testFolderConfig.autoGroup = true;  }
						else if (ConfigFile.IsNo(value))
							{  testFolderConfig.autoGroup = false;  }
						else
							{  configFile.AddError("Unrecognized value \"" + value + "\" for Auto-Group.", lcIdentifier);  }
						}

					else
						{  configFile.AddError("Unrecognized identifier \"" + lcIdentifier + "\".", lcIdentifier);  }
					}

				if (testFolderConfig.testType == null)
					{  configFile.AddError("Test Type must be defined in \"" + configFilePath + "\".");  }

				configFile.Close();
				}

			return (errorList.Count == initialErrorCount);
			}



		// Group: Private Functions
		// __________________________________________________________________________


		/* Constructor: TestFolderConfig
		 */
		private TestFolderConfig ()
			{
			testType = null;
			engineConfigFolder = null;

			keepHTML = false;
			htmlTitle = null;
			htmlSubtitle = null;
			htmlStyle = null;

			// Defaults to false to prevent unnecessary topics from appearing in the output
			autoGroup = false;
			}



		// Group: Properties
		// __________________________________________________________________________


		/* Property: TestType
		 * The name of the test to be run on all files in this folder.
		 */
		public string TestType
			{
			get
				{  return testType;  }
			}

		/* Property: EngineConfigFolder
		 * The configuration folder the Natural Docs engine should use, or null to leave it as the default.  Useful to add custom
		 * <Languages.txt> and <Comments.txt> settings.
		 */
		public AbsolutePath EngineConfigFolder
			{
			get
				{  return engineConfigFolder;  }
			}

		/* Property: KeepHTML
		 * If HTML output is built as part of the test, whether it should be kept so that it can be manually opened and inspected
		 * afterwards.  If not it will be deleted with the other temporary data generated.
		 */
		public bool KeepHTML
			{
			get
				{  return keepHTML;  }
			}

		/* Property: HTMLTitle
		 * If <KeepHTML> is true, a title to use for the generated HTML output, or null if it should be left as the default.
		 */
		public string HTMLTitle
			{
			get
				{  return htmlTitle;  }
			}

		/* Property: HTMLSubtitle
		 * If <KeepHTML> is true, a subtitle to use for the generated HTML output, or null if it should be left as the default.
		 */
		public string HTMLSubtitle
			{
			get
				{  return htmlSubtitle;  }
			}

		/* Property: HTMLStyle
		 * If <KeepHTML> is true, the style to use for the generated HTML output, or null if it should be left as the default.
		 */
		public string HTMLStyle
			{
			get
				{  return htmlStyle;  }
			}

		/* Property: AutoGroup
		 * Whether automatic grouping is turned on or off.
		 */
		public bool AutoGroup
			{
			get
				{  return autoGroup;  }
			}



		// Group: Variables
		// __________________________________________________________________________


		/* var: testType
		 * The name of the test to be run on all files in this folder.
		 */
		protected string testType;

		/* var: engineConfigFolder
		 * The configuration folder the Natural Docs engine should use, or null to leave it as the default.  Useful to add custom
		 * <Languages.txt> and <Comments.txt> settings.
		 */
		protected AbsolutePath engineConfigFolder;

		/* var: keepHTML
		 * If HTML output is built as part of the test, whether it should be kept so that it can be manually opened and inspected
		 * afterwards.  If not it will be deleted with the other temporary data generated.
		 */
		protected bool keepHTML;

		/* var: htmlTitle
		 * If <keepHTML> is true, a title to use for the generated HTML output, or null to leave it as the default.
		 */
		protected string htmlTitle;

		/* var: htmlSubtitle
		 * If <keepHTML> is true, a subtitle to use for the generated HTML output, or null to leave it as the default.
		 */
		protected string htmlSubtitle;

		/* var: htmlStyle
		 * If <keepHTML> is true, the style to use for the generated HTML output, or null to leave it as the default.
		 */
		protected string htmlStyle;

		/* var: autoGroup
		 * Whether automatic grouping is turned on or off.
		 */
		protected bool autoGroup;

		}
	}
