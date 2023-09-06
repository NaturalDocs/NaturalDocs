/*
 * Class: CodeClear.NaturalDocs.Engine.Languages.Manager
 * ____________________________________________________________________________
 *
 * A module to handle <Languages.txt> and all the language parsers within Natural Docs.
 *
 *
 * Topic: Usage
 *
 *		- Call <Engine.Instance.Start()> which will start this module.
 *
 */

// This file is part of Natural Docs, which is Copyright © 2003-2023 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;


namespace CodeClear.NaturalDocs.Engine.Languages
	{
	public partial class Manager : Module
		{

		// Group: Functions
		// __________________________________________________________________________


		/* Constructor: Manager
		 */
		public Manager (Engine.Instance engineInstance) : base (engineInstance)
			{
			config = null;

			systemTextConfig = null;
			projectTextConfig = null;
			mergedTextConfig = null;
			lastRunConfig = null;


			// Predefined languages

			Language textFile = new Language("Text File");
				textFile.Type = Language.LanguageType.TextFile;

			Language shebangScript = new Language("Shebang Script");
				shebangScript.Type = Language.LanguageType.Container;
				shebangScript.Parser = new Parsers.ShebangScript(engineInstance, shebangScript);

			Language cSharp = new Language("C#");
				cSharp.Type = Language.LanguageType.FullSupport;
				cSharp.Parser = new Parsers.CSharp(engineInstance, cSharp);

				cSharp.LineCommentSymbols = new string[] { "//" };
				cSharp.BlockCommentSymbols = new BlockCommentSymbols[] { new BlockCommentSymbols("/*", "*/") };
				cSharp.JavadocBlockCommentSymbols = new BlockCommentSymbols[] { new BlockCommentSymbols("/**", "*/") };
				cSharp.XMLLineCommentSymbols = new string[] { "///" };
				cSharp.MemberOperator = ".";
				cSharp.EnumValue = Language.EnumValues.UnderType;
				cSharp.CaseSensitive = true;

			Language systemVerilog = new Language("SystemVerilog");
				//systemVerilog.Type = Language.LanguageType.FullSupport;
				systemVerilog.Parser = new Parsers.SystemVerilog(engineInstance, systemVerilog);

				//systemVerilog.LineCommentSymbols = new string[] { "//" };
				//systemVerilog.BlockCommentSymbols = new BlockCommentSymbols[] { new BlockCommentSymbols("/*", "*/") };
				//systemVerilog.JavadocBlockCommentSymbols = new BlockCommentSymbols[] { new BlockCommentSymbols("/**", "*/") };
				//systemVerilog.XMLLineCommentSymbols = new string[] { "///" };
				//systemVerilog.MemberOperator = ".";
				//systemVerilog.EnumValue = Language.EnumValues.Global;
				//systemVerilog.CaseSensitive = true;

			Language perl = new Language("Perl");
				perl.Parser = new Parsers.Perl(engineInstance, perl);

			Language python = new Language("Python");
				python.Parser = new Parsers.Python(engineInstance, python);

			Language ruby = new Language("Ruby");
				ruby.Parser = new Parsers.Ruby(engineInstance, ruby);

			Language sql = new Language("SQL");
				sql.Parser = new Parsers.SQL(engineInstance, sql);

			Language java = new Language("Java");
				java.Parser = new Parsers.Java(engineInstance, java);

			Language lua = new Language("Lua");
				lua.Parser = new Parsers.Lua(engineInstance, lua);

			Language php = new Language("PHP");
				php.Parser = new Parsers.PHP(engineInstance, php);

			Language powershell = new Language("PowerShell");
				powershell.Parser = new Parsers.PowerShell(engineInstance, powershell);

			predefinedLanguages = new Language[] { textFile, shebangScript, cSharp, systemVerilog, perl, python, ruby, sql, java, lua, php, powershell };
			}


		protected override void Dispose (bool strictRulesApply)
			{
			}


		/* Function: FromFileName
		 *
		 * Returns the <Language> associated with the passed file name, or null if none.
		 *
		 * Note that this will *not* open files to search for things like shebang strings.  If the file name indicates a container
		 * language like Shebang Script, it will return that container's language information.
		 *
		 * If the file name has no extension, it will return the language information for Shebang Script if it is defined, or null
		 * if it is not.
		 */
		public Language FromFileName (Path filename)
			{
			return config.LanguageFromFileExtension(filename.Extension);
			}

		/* Function: FromFileExtension
		 *
		 * Returns the <Language> associated with the passed extension, or null if none.
		 *
		 * If you pass null or an empty string, it will return the language information for Shebang Script if it is defined, or null
		 * if it is not.
		 */
		public Language FromFileExtension (string fileExtension)
			{
			return config.LanguageFromFileExtension(fileExtension);
			}

		/* Function: FromShebangLine
		 * Returns the <Language> associated with the passed shebang line, or null if none.  Pass the entire line, this function
		 * will handle picking out the substrings.
		 */
		public Language FromShebangLine (string shebangLine)
			{
			return config.LanguageFromShebangLine(shebangLine);
			}

		/* Function: FromName
		 * Returns the <Language> associated with the passed name or alias, or null if none.
		 */
		public Language FromName (string languageName)
			{
			return config.LanguageFromName(languageName);
			}

		/* Function: FromID
		 * Returns the <Language> associated with the passed ID, or null if none.
		 */
		public Language FromID (int languageID)
			{
			return config.LanguageFromID(languageID);
			}



		// Group: Variables
		// __________________________________________________________________________


		/* var: config
		 * The final configuration to use.
		 */
		protected Config config;

		/* var: predefinedLanguages
		 * An array of <Language>s predefined by the engine because they require special settings or objects.  These
		 * languages will not appear in the <languages> structure if they are not also defined in <Languages.txt>.
		 */
		protected Language[] predefinedLanguages;



		// Group: Temporary Initialization Variables
		// __________________________________________________________________________
		//
		// These variables are used to store data between <Start_Stage1()> and <Start_Stage2()>.  They are not used
		// afterwards.
		//


		/* var: systemTextConfig
		 * The <ConfigFiles.TextFile> representing the system <Languages.txt>.  This is only stored between <Start_Stage1()>
		 * and <Start_Stage2()>.  It will be null afterwards.
		 */
		protected ConfigFiles.TextFile systemTextConfig;

		/* var: projectTextConfig
		 * The <ConfigFiles.TextFile> representing the project <Languages.txt>.  This is only stored between <Start_Stage1()>
		 * and <Start_Stage2()>.  It will be null afterwards.
		 */
		protected ConfigFiles.TextFile projectTextConfig;

		/* var: mergedTextConfig
		 * A <ConfigFiles.TextFile> representing the merger of <systemTextConfig> and <projectTextConfig>, sans file extensions,
		 * aliases, and shebang strings.  This is only stored between <Start_Stage1()> and <Start_Stage2()>.  It will be null
		 * afterwards.
		 */
		protected ConfigFiles.TextFile mergedTextConfig;

		/* var: lastRunConfig
		 * The <Config> representing the contents of <Languages.nd>.  This is only stored between <Start_Stage1()> and
		 * <Start_Stage2()>.  It will be null afterwards.
		 */
		protected Config lastRunConfig;

		}
	}
