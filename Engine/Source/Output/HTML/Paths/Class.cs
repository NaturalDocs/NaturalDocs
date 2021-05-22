/* 
 * Class: CodeClear.NaturalDocs.Engine.Output.HTML.Paths.Class
 * ____________________________________________________________________________
 * 
 * Path functions relating to hierarchy classes in HTML output.
 * 
 */

// This file is part of Natural Docs, which is Copyright © 2003-2021 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Text;
using CodeClear.NaturalDocs.Engine.Symbols;


namespace CodeClear.NaturalDocs.Engine.Output.HTML.Paths
	{
	static public class Class
		{

		/* Function: OutputFile
		 * Returns the output file for a class.
		 */
		static public Path OutputFile (Path targetOutputFolder, Hierarchies.Hierarchy hierarchy, Languages.Language language,
												  SymbolString classSymbol)
			{
			SymbolString qualifier = classSymbol.WithoutLastSegment;
			string endingSymbol = classSymbol.LastSegment;

			return OutputFolder(targetOutputFolder, hierarchy, language, qualifier) + '/' +
					  Utilities.Sanitize(endingSymbol, replaceDots: true) + ".html";
			}


		/* Function: OutputFolder
		 * 
		 * Returns the output folder for class files, optionally for the passed language and qualifier within it.
		 * 
		 * - If language isn't specified, it returns the output folder for all class files in that hierarchy.
		 * - If language is specified but the qualifier is not, it returns the output folder for all class files in that hierarchy and language.
		 *   If the hierarchy is language-agnostic this will be the same as if language wasn't specified.
		 * - If language and qualifier are specified, it returns the output folder for that qualifier.
		 * 
		 * Examples:
		 * 
		 *		targetOutputFolder + hierarchy - C:\Project\Documentation\classes
		 *		targetOutputFolder + hierarchy + language - C:\Project\Documentation\classes\CSharp
		 *		targetOutputFolder + hierarchy + language + qualifier - C:\Project\Documentation\classes\CSharp\Namespace1\Namespace2
		 */
		static public Path OutputFolder (Path targetOutputFolder, Hierarchies.Hierarchy hierarchy, Languages.Language language = null,
													  SymbolString qualifier = default(SymbolString))
			{
			StringBuilder result = new StringBuilder(targetOutputFolder);
			result.Append('/');
			result.Append(hierarchy.PluralSimpleIdentifier.ToLowerInvariant());  

			if (language != null && hierarchy.IsLanguageSpecific)
				{
				result.Append('/');
				result.Append(language.SimpleIdentifier);
				}
					
			if (qualifier != null)
				{
				#if DEBUG
				if (hierarchy.IsLanguageSpecific && language == null)
					{  throw new InvalidOperationException("Need to include a language for language-specific hierarchies when including a qualifier.");  }
				#endif

				result.Append('/');
				string pathString = qualifier.FormatWithSeparator('/');
				result.Append(Utilities.Sanitize(pathString));
				}

			return result.ToString();
			}


		/* Function: HashPath
		 * Returns the hash path for the class.  If the hierarchy is language-agnostic the language will be ignored, so it is okay to set
		 * it to null but it will not affect anything if it's set to a value.
		 */
		static public string HashPath (Hierarchies.Hierarchy hierarchy, Languages.Language language, SymbolString classSymbol)
			{
			SymbolString qualifier = classSymbol.WithoutLastSegment;
			string endingSymbol = classSymbol.LastSegment;

			return QualifierHashPath(hierarchy, language, qualifier) + Utilities.Sanitize(endingSymbol);
			}


		/* Function: QualifierHashPath
		 * 
		 * Returns the qualifier part of the hash path for classes.  If the qualifier symbol is specified it will include a 
		 * trailing member operator so that the last segment can simply be concatenated.
		 * 
		 * - If the hierarchy is language-specific the language must always be specified.  If it is language-agnostic the language
		 *   will be ignored, so it's okay to set it to null but it will not affect anything if it's set to a value.
		 * - If the qualifier is not specified, it returns the prefix for all class paths of that hierarchy and language.
		 * - If the qualifier is specified, it returns the prefix plus the hash path for that qualifier, including a trailing separator.
		 * 
		 * Examples:
		 * 
		 *		hierarchy - Database:
		 *		hierarchy + language - CSharpClass:
		 *		hierarchy + language + qualifier - CSharpClass:Namespace1.Namespace2.
		 */
		static public string QualifierHashPath (Hierarchies.Hierarchy hierarchy, Languages.Language language = null,
																SymbolString qualifier = default(SymbolString))
			{
			#if DEBUG
			if (hierarchy.IsLanguageSpecific && language == null)
				{  throw new InvalidOperationException("Need to include a language for language-specific hierarchies.");  }
			#endif

			StringBuilder result = new StringBuilder();

			if (hierarchy.IsLanguageSpecific)
				{  result.Append(language.SimpleIdentifier);  }

			result.Append(hierarchy.SimpleIdentifier);
			result.Append(':');

			if (qualifier != null)
				{
				string pathString = qualifier.FormatWithSeparator('.');
				result.Append(Utilities.Sanitize(pathString));
				result.Append('.');
				}

			return result.ToString();
			}

		}
	}
