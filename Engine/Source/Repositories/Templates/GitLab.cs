/*
 * Class: CodeClear.NaturalDocs.Engine.Repositories.Templates.GitLab
 * ____________________________________________________________________________
 *
 * A repository site template for GitLab.
 *
 *
 * Topic: URL Format
 *
 *		Project Page:
 *
 *			> https://gitlab.com/{user}/{project}
 *
 *			For example, https://gitlab.com/inkscape/inkscape/
 *
 *
 *		Source Page, With Branch:
 *
 *			> https://gitlab.com/{user}/{project}/-/blob/{branch}/{filepath}#L{linenumber}
 *
 *			For example, https://gitlab.com/inkscape/inkscape/-/blob/master/packaging/appimage/generate.sh#L52
 *
 *
 *		Source Page, Without Branch:
 *
 *			The branch section of the URL must be included and you must know the branch name.  There is no forwarding
 *			that I'm aware of to work around this.
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
	public partial class GitLab : Template
		{

		// Group: Functions
		// __________________________________________________________________________

		public GitLab () : base ("GitLab", "https://gitlab.com/UserName/ProjectName", Flags.RequiresBranch)
			{
			// RequiresBranch because it's not possible to omit the branch portion of the URLs nor will it redirect between
			// "main" and "master".
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

			stringBuilder.Append("-/blob/");
			stringBuilder.Append(repository.Branch);
			stringBuilder.Append('/');
			stringBuilder.Append( path.ToURL() );
			stringBuilder.Append("#L");
			stringBuilder.Append(lineNumber);

			return stringBuilder.ToString();
			}


		// Group: Regular Expressions
		// __________________________________________________________________________

		/* Regex: IsNameOrAliasRegex
		 * Will match if the string is "GitLab" or an acceptable variation.
		 */
		[GeneratedRegex("""^Git[ \-]?Lab(?:\.com)?$""",
								  RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
		static internal partial Regex IsNameOrAliasRegex();

		/* Regex: IsSiteURLRegex
		 * Will match if the string is a GitLab URL of any kind.
		 */
		[GeneratedRegex("""^https?://(?:www\.)?gitlab\.com(?:/|$)""",
								  RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
		static internal partial Regex IsSiteURLRegex();

		/* Regex: IsProjectURLRegex
		 * Will match if the string is a GitLab project URL.
		 */
		[GeneratedRegex("""^https?://(?:www\.)?gitlab\.com/[^/?#]+/[^/?#]+/?$""",
								  RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
		static internal partial Regex IsProjectURLRegex();

		}
	}
