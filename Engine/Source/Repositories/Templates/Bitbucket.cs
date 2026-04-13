/*
 * Class: CodeClear.NaturalDocs.Engine.Repositories.Templates.Bitbucket
 * ____________________________________________________________________________
 *
 * A repository site template for Bitbucket.
 *
 *
 * Topic: URL Format
 *
 *		Project Page:
 *
 *			> https://bitbucket.org/{user}/{project}
 *
 *			For example, https://bitbucket.org/sonarsource/sample-maven-project
 *
 *
 *		Source Page, With Branch:
 *
 *			> https://bitbucket.org/{user}/{project}/src/{branch}/{path}#lines-{linenumber}
 *
 *			For example, https://bitbucket.org/sonarsource/sample-maven-project/src/master/src/main/java/com/sonarsource/App.java#lines-14
 *
 *
 *		Source Page, Without Branch:
 *
 *			The branch section of the URL must be included and you must know the branch name.  There is no forwarding
 *			that I'm aware of to work around this.
 *
 *
 *		Other Notes:
 *
 *			- All Bitbucket.com URLs redirect to their corresponding .org address.
 *
 *			- Self-hosted Bitbucket instances have a different URL format: https://{domain}/bitbucket/projects/{project}/repos/{repo}
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
	public partial class Bitbucket : Template
		{

		// Group: Functions
		// __________________________________________________________________________

		public Bitbucket () : base ("Bitbucket", "https://bitbucket.org/UserName/ProjectName", Flags.RequiresBranch)
			{
			// RequiresBranch because it's not possible to omit the branch name from the URLs and it's not possible to redirect
			// to the trunk.
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

			stringBuilder.Append("src/");
			stringBuilder.Append(repository.Branch);
			stringBuilder.Append('/');
			stringBuilder.Append( path.ToURL() );
			stringBuilder.Append("#lines-");
			stringBuilder.Append(lineNumber);

			return stringBuilder.ToString();
			}


		// Group: Regular Expressions
		// __________________________________________________________________________

		/* Regex: IsNameOrAliasRegex
		 * Will match if the string is "Bitbucket" or an acceptable variation.
		 */
		[GeneratedRegex("""^Bit[ \-]?bucket(?:\.(?:org|com))?$""",
								  RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
		static internal partial Regex IsNameOrAliasRegex();

		/* Regex: IsSiteURLRegex
		 * Will match if the string is a Bitbucket URL of any kind.  Bitbucket.com URLs redirect to .org.
		 */
		[GeneratedRegex("""^https?://(?:www\.)?bitbucket\.(?:org|com)(?:/|$)""",
								  RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
		static internal partial Regex IsSiteURLRegex();

		/* Regex: IsProjectURLRegex
		 * Will match if the string is a Bitbucket project URL.  Bitbucket.com URLs redirect to .org.
		 */
		[GeneratedRegex("""^https?://(?:www\.)?bitbucket\.(?:org|com)/[^/?#]+/[^/?#]+/?$""",
								  RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
		static internal partial Regex IsProjectURLRegex();

		}
	}
