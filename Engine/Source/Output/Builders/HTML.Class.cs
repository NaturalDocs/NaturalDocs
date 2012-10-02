/* 
 * Class: GregValure.NaturalDocs.Engine.Output.Builders.HTML
 * ____________________________________________________________________________
 * 
 */

// This file is part of Natural Docs, which is Copyright © 2003-2012 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Text;
using GregValure.NaturalDocs.Engine.Languages;
using GregValure.NaturalDocs.Engine.Symbols;


namespace GregValure.NaturalDocs.Engine.Output.Builders
	{
	public partial class HTML
		{

		// Group: Path Functions
		// __________________________________________________________________________


		/* Function: Class_OutputFolder
		 * 
		 * Returns the output folder for class files, optionally for the passed language and partial symbol within it.
		 * 
		 * - If language isn't specified, it returns the output folder for all class files.
		 * - If language is specified but the symbol is not, it returns the output folder for all class files of that language.
		 * - If language and partial symbol are specified, it returns the output folder for that symbol.
		 */
		public Path Class_OutputFolder (Language language = null, SymbolString partialSymbol = default(SymbolString))
			{
			StringBuilder result = new StringBuilder(OutputFolder);
			result.Append("/classes");  

			if (language != null)
				{
				result.Append('/');
				result.Append(language.SimpleIdentifier);
					
				if (partialSymbol != null)
					{
					result.Append('/');
					string pathString = partialSymbol.FormatWithSeparator('/');
					result.Append(SanitizePath(pathString));
					}
				}

			return result.ToString();
			}


		/* Function: Class_OutputFolderHashPath
		 * Returns the hash path of the output folder for class files, optionally for the passed language and partial symbol 
		 * within.  The hash path will always include a trailing symbol so that the file name can simply be concatenated.
		 * 
		 * - If language isn't specified, it returns null since there is no common prefix for all class paths.
		 * - If language is specified but the symbol is not, it returns the prefix for all class paths of that language.
		 * - If language and partial symbol are specified, it returns the hash path for that symbol.
		 */
		public string Class_OutputFolderHashPath (Language language = null, SymbolString partialSymbol = default(SymbolString))
			{
			if (language == null)
				{  return null;  }

			StringBuilder result = new StringBuilder();

			result.Append(language.SimpleIdentifier);
			result.Append("Class:");

			if (partialSymbol != null)
				{
				string memberOperator = language.MemberOperator;

				// We only support :: and . in hash paths.  Default to . for anything else.
				if (memberOperator != "::")
					{  memberOperator = ".";  }

				string pathString = partialSymbol.FormatWithSeparator(memberOperator);
				result.Append(SanitizePath(pathString));
				result.Append(memberOperator);
				}

			return result.ToString();
			}


		/* Function: Class_OutputFile
		 * Returns the path of the output file generated for the passed class.
		 */
		public Path Class_OutputFile (ClassString classString)
			{
			var language = Engine.Instance.Languages.FromID(classString.LanguageID);

			return Class_OutputFolder(language, classString.Symbol.WithoutLastSegment) + '/' + 
						Class_OutputFileNameOnly(classString);
			}


		/* Function: Class_OutputFileHashPath
		 * Returns the hash path of the passed class.
		 */
		public string Class_OutputFileHashPath (ClassString classString)
			{
			var language = Engine.Instance.Languages.FromID(classString.LanguageID);

			// OutputFolderHashPath already includes the trailing separator so we can just concatenate them.
			return Class_OutputFolderHashPath(language, classString.Symbol.WithoutLastSegment) +
						Class_OutputFileNameOnlyHashPath(classString);
			}


		/* Function: Class_OutputFileNameOnly
		 * Returns the output file name of the passed class.  Any scope attached to it will be ignored and not included in 
		 * the result.
		 */
		public Path Class_OutputFileNameOnly (ClassString classString)
			{
			string nameString = classString.Symbol.LastSegment;
			return SanitizePath(nameString, true) + ".html";
			}


		/* Function: Class_OutputFileNameOnlyHashPath
		 * Returns the hash path of the passed class.  Any scope attached to it will be ignored and not included in the result.
		 */
		public string Class_OutputFileNameOnlyHashPath (ClassString classString)
			{
			string nameString = classString.Symbol.LastSegment;
			return SanitizePath(nameString);
			}

		}
	}

