/*
 * Class: CodeClear.NaturalDocs.Engine.Output.HTML.RepositoryLinks
 * ____________________________________________________________________________
 *
 * A static class to centralize generation of repository links.
 *
 */

// This file is part of Natural Docs, which is Copyright © 2003-2026 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using CodeClear.NaturalDocs.Engine.Topics;


namespace CodeClear.NaturalDocs.Engine.Output.HTML
	{
	static public class RepositoryLinks
		{

		/* Function: EffectiveLineNumber
		 * Chooses either the code line number or the comment line number to use in a repository link.
		 */
		static public int EffectiveLineNumber (int commentLineNumber, int codeLineNumber)
			{
			return (codeLineNumber > 0 ? codeLineNumber : commentLineNumber);
			}

		/* Function: EffectiveLineNumber
		 * Chooses either the code line number or the comment line number to use in a repository link.
		 */
		static public int EffectiveLineNumber (Topic topic)
			{
			return EffectiveLineNumber (commentLineNumber: topic.CommentLineNumber, codeLineNumber: topic.CodeLineNumber);
			}

		/* Function: ToSourceFile
		 * Generates a repository link to the passed source file and line number.
		 */
		static public string ToSourceFile (string sourceFileURLTemplate, RelativePath sourceFilePath, int lineNumber)
			{
			string url = sourceFileURLTemplate;

			url = url.Replace( Config.RepositorySubstitutions.FilePath, sourceFilePath.ToURL() );
			url = url.Replace( Config.RepositorySubstitutions.LineNumber, lineNumber.ToString() );

			return url;
			}

		/* Function: ToSourceFile
		 * Generates a repository link to the passed source file and line number.  This is a shortcut function that chooses
		 * the appropriate line number for you.
		 */
		static public string ToSourceFile (string sourceFileURLTemplate, RelativePath sourceFilePath, int commentLineNumber, int codeLineNumber)
			{
			return ToSourceFile(sourceFileURLTemplate, sourceFilePath, EffectiveLineNumber(commentLineNumber, codeLineNumber));
			}

		/* Function: ToSourceFile
		 * Generates a repository link to the passed source file and line number.  This is a shortcut function that chooses
		 * the appropriate line number for you.
		 */
		static public string ToSourceFile (string sourceFileURLTemplate, RelativePath sourceFilePath, Topic topic)
			{
			return ToSourceFile(sourceFileURLTemplate, sourceFilePath, EffectiveLineNumber(topic));
			}

		}
	}
