/*
 * Class: CodeClear.NaturalDocs.Engine.Repositories.Templates.GitHub
 * ____________________________________________________________________________
 *
 * A repository site template for GitHub.
 *
 *
 * Topic: URL Format
 *
 *		Project Page:
 *
 *			> https://github.com/{user}/{project}
 *
 *			For example, https://github.com/NaturalDocs/NaturalDocs/
 *
 *
 *		Source Page, With Branch:
 *
 *			> https://github.com/{user}/{project}/blob/{branch}/{filepath}#L{linenumber}
 *
 *			For example, https://github.com/NaturalDocs/NaturalDocs/blob/main/Engine/Source/CodeDB/Accessor.cs#L38
 *
 *
 *		Source Page, Without Branch:
 *
 *			> https://github.com/{user}/{project}/blob/master/{filepath}#L{linenumber}
 *
 *			The branch section of the URL must be included, but you can use "master" and it will redirect to the trunk even if
 *			it's called "main".
 *
 *			For example, https://github.com/NaturalDocs/NaturalDocs/blob/master/Engine/Source/CodeDB/Accessor.cs#L38
 *
 */

// This file is part of Natural Docs, which is Copyright © 2003-2026 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Text;
using System.Text.RegularExpressions;


namespace CodeClear.NaturalDocs.Engine.Repositories.Templates
	{
	public partial class GitHub : Template
		{

		// Group: Functions
		// __________________________________________________________________________

		public GitHub () : base ("GitHub", "https://github.com/UserName/ProjectName")
			{
			}

		override public bool IsNameOrAlias (string name)
			{
			return IsNameOrAliasRegex().IsMatch(name);
			}

		override public bool IsSiteURL (string url)
			{
			return IsSiteURLRegex().IsMatch(url);
			}

		override public bool IsProjectURL (string url)
			{
			return IsProjectURLRegex().IsMatch(url);
			}

		override public string SourceURL (RelativePath path, int lineNumber, Repository repository)
			{
			#if DEBUG
			if (repository.SiteTemplate != this)
				{  throw new Exception ("This isn't the template assigned to the passed repository.");  }
			#endif

			StringBuilder stringBuilder = new StringBuilder(128);

			string projectURL = repository.ProjectURL;
			stringBuilder.Append(projectURL);

			if (projectURL[projectURL.Length - 1] != '/')
				{  stringBuilder.Append('/');  }

			stringBuilder.Append("blob/");

			if (repository.Branch != null)
				{  stringBuilder.Append(repository.Branch);  }
			else
				{  stringBuilder.Append("master");  }

			stringBuilder.Append('/');
			stringBuilder.Append( path.ToURL() );
			stringBuilder.Append("#L");
			stringBuilder.Append(lineNumber);

			return stringBuilder.ToString();
			}


		// Group: Regular Expressions
		// __________________________________________________________________________

		/* Regex: IsNameOrAliasRegex
		 * Will match if the string is "GitHub" or an acceptable variation.
		 */
		[GeneratedRegex("""^Git[ \-]?Hub(?:\.com)?$""",
								  RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
		static internal partial Regex IsNameOrAliasRegex();

		/* Regex: IsSiteURLRegex
		 * Will match if the string is a GitHub URL of any kind.
		 */
		[GeneratedRegex("""^https?://(?:www\.)?github\.com(?:/|$)""",
								  RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
		static internal partial Regex IsSiteURLRegex();

		/* Regex: IsProjectURLRegex
		 * Will match if the string is a GitHub project URL.
		 */
		[GeneratedRegex("""^https?://(?:www\.)?github\.com/[^/?#]+/[^/?#]+/?$""",
								  RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
		static internal partial Regex IsProjectURLRegex();

		}
	}
