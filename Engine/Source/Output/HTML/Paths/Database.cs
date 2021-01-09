/* 
 * Class: CodeClear.NaturalDocs.Engine.Output.HTML.Paths.Database
 * ____________________________________________________________________________
 * 
 * Path functions relating to databases in HTML output.
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
	static public class Database
		{

		/* Function: OutputFile
		 * Returns the output file for a class.
		 */
		static public Path OutputFile(Path targetOutputFolder, SymbolString databaseSymbol)
			{
			SymbolString qualifier = databaseSymbol.WithoutLastSegment;
			string endingSymbol = databaseSymbol.LastSegment;

			return OutputFolder(targetOutputFolder, qualifier) + '/' +
					  Utilities.Sanitize(endingSymbol, replaceDots: true) + ".html";
			}


		/* Function: OutputFolder
		 * 
		 * Returns the output folder for database files, optionally for the passed qualifier within it.
		 * 
		 * - If the qualifier isn't specified, it returns the root output folder for all database files.
		 * - If the qualifier is specified, it returns the output folder for that qualifier.
		 * 
		 * Examples:
		 * 
		 *		targetOutputFolder - C:\Project\Documentation\database
		 *		targetOutputFolder + qualifier - C:\Project\Documentation\database\Qualifier1\Qualifier2
		 */
		static public Path OutputFolder (Path targetOutputFolder, SymbolString qualifier = default(SymbolString))
			{
			StringBuilder result = new StringBuilder(targetOutputFolder);
			result.Append("/database");  

			if (qualifier != null)
				{
				result.Append('/');
				string pathString = qualifier.FormatWithSeparator('/');
				result.Append(Utilities.Sanitize(pathString));
				}

			return result.ToString();
			}


		/* Function: HashPath
		 * Returns the hash path for the class.
		 */
		static public string HashPath(SymbolString databaseSymbol)
			{
			SymbolString qualifier = databaseSymbol.WithoutLastSegment;
			string endingSymbol = databaseSymbol.LastSegment;

			return QualifierHashPath(qualifier) + Utilities.Sanitize(endingSymbol);
			}


		/* Function: QualifierHashPath
		 * 
		 * Returns the qualifier part of the hash path for databases.  If the qualifier symbol is specified it will include a 
		 * trailing member operator so that the last segment can simply be concatenated.  If no qualifier is specified, it
		 * returns the prefix for all database hash paths.
		 */
		static public string QualifierHashPath (SymbolString qualifier = default(SymbolString))
			{
			if (qualifier == null)
				{  return "Database:";  }
			else
				{
				StringBuilder result = new StringBuilder("Database:");

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
	}
