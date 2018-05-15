/* 
 * Class: CodeClear.NaturalDocs.Engine.Output.Builders.HTML
 * ____________________________________________________________________________
 * 
 */

// This file is part of Natural Docs, which is Copyright © 2003-2018 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using System.Text;
using CodeClear.NaturalDocs.Engine.Collections;
using CodeClear.NaturalDocs.Engine.Languages;
using CodeClear.NaturalDocs.Engine.Links;
using CodeClear.NaturalDocs.Engine.Output.Components;
using CodeClear.NaturalDocs.Engine.Symbols;
using CodeClear.NaturalDocs.Engine.Topics;
using CodeClear.NaturalDocs.Engine.CommentTypes;


namespace CodeClear.NaturalDocs.Engine.Output.Builders
	{
	public partial class HTML
		{

		// Group: Functions
		// __________________________________________________________________________


		/* Function: BuildClassFile
		 * Builds an output file based on a class.  The accessor should NOT hold a lock on the database.  This will also
		 * build the metadata files.
		 */
		protected void BuildClassFile (int classID, CodeDB.Accessor accessor, CancelDelegate cancelDelegate)
			{
			#if DEBUG
			if (accessor.LockHeld != CodeDB.Accessor.LockType.None)
				{  throw new Exception ("Shouldn't call BuildClassFile() when the accessor already holds a database lock.");  }
			#endif

			Components.HTMLTopicPages.Class page = new Components.HTMLTopicPages.Class(this, classID);

			bool hasTopics = page.Build(accessor, cancelDelegate);

			if (cancelDelegate())
				{  return;  }


			if (hasTopics)
				{
				lock (accessLock)
					{
					if (buildState.ClassFilesWithContent.Add(classID) == true)
						{  buildState.NeedToBuildMenu = true;  }
					}
				}
			else
				{
				DeleteOutputFileIfExists(page.OutputFile);
				DeleteOutputFileIfExists(page.ToolTipsFile);
				DeleteOutputFileIfExists(page.SummaryFile);
				DeleteOutputFileIfExists(page.SummaryToolTipsFile);

				lock (accessLock)
				   {
				   if (buildState.ClassFilesWithContent.Remove(classID) == true)
				      {  buildState.NeedToBuildMenu = true;  }
				   }
				}
			}



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
				string pathString = partialSymbol.FormatWithSeparator('.');
				result.Append(SanitizePath(pathString));
				result.Append('.');
				}

			return result.ToString();
			}

		/* Function: Database_OutputFolder
		 * 
		 * Returns the output folder for database files, optionally for the partial symbol within it.
		 * 
		 * - If the partial symbol isn't specified, it returns the output folder for all database files.
		 * - If partial symbol is specified, it returns the output folder for that symbol.
		 */
		public Path Database_OutputFolder (SymbolString partialSymbol = default(SymbolString))
			{
			StringBuilder result = new StringBuilder(OutputFolder);
			result.Append("/database");  

			if (partialSymbol != null)
				{
				result.Append('/');
				string pathString = partialSymbol.FormatWithSeparator('/');
				result.Append(SanitizePath(pathString));
				}

			return result.ToString();
			}


		/* Function: Database_OutputFolderHashPath
		 * Returns the hash path of the output folder for database files, optionally for the partial symbol within.  The hash path will 
		 * always include a trailing symbol so that the file name can simply be concatenated.
		 * 
		 * - If the partial symbol isn't specified, it returns the prefix for all database members.
		 * - If the partial symbol is specified, it returns the hash path for that symbol.
		 */
		public string Database_OutputFolderHashPath (SymbolString partialSymbol = default(SymbolString))
			{
			if (partialSymbol == null)
				{  return "Database:";  }
			else
				{
				StringBuilder result = new StringBuilder("Database:");

				if (partialSymbol != null)
					{
					string pathString = partialSymbol.FormatWithSeparator('.');
					result.Append(SanitizePath(pathString));
					result.Append('.');
					}

				return result.ToString();
				}
			}

		}
	}

