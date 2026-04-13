/*
 * Class: CodeClear.NaturalDocs.Engine.Repositories.Templates.AzureRepos
 * ____________________________________________________________________________
 *
 * A repository site template for Azure Repos.
 *
 *
 * Topic: URL Format
 *
 *		Project Page:
 *
 *			> https://dev.azure.com/{user}/{project}
 *			> https://{user}.visualstudio.com/{project}
 *
 *			For example, https://dev.azure.com/dbma-dev/AzureDevOpsAngular and https://dbma-dev.visualstudio.com/AzureDevOpsAngular
 *
 *
 *		Source Page, With Branch:
 *
 *			> https://dev.azure.com/{user}/_git/{project}?path=/{filepath}&version=GB{branch}&line={linenumber}&lineEnd={linenumber+1}&lineStartColumn=1&lineEndColumn=1&lineStyle=plain&_a=contents
 *
 *			For example, https://dev.azure.com/dbma-dev/_git/AzureDevOpsAngular?path=/karma.conf.js&version=GBmaster&line=12&lineEnd=13&lineStartColumn=1&lineEndColumn=1&lineStyle=plain&_a=contents
 *
 *
 *		Source Page, Without Branch:
 *
 *			> https://dev.azure.com/{user}/_git/{project}?path=/{filepath}&line={linenumber}&lineEnd={linenumber+1}&lineStartColumn=1&lineEndColumn=1&lineStyle=plain&_a=contents
 *
 *			You can omit the branch section of the URL and it will forward to the trunk.
 *
 *			For example, https://dev.azure.com/dbma-dev/_git/AzureDevOpsAngular?path=/karma.conf.js&line=12&lineEnd=13&lineStartColumn=1&lineEndColumn=1&lineStyle=plain&_a=contents
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
	public partial class AzureRepos : Template
		{

		// Group: Functions
		// __________________________________________________________________________

		public AzureRepos () : base ("Azure Repos", "https://dev.azure.com/UserName/ProjectName")
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
			int projectURLLength = projectURL.Length;

			// Exclude the trailing slash if there is one
			if (projectURL[projectURLLength - 1] == '/')
				{  projectURLLength--;  }

			// Find the insertion point for "_git", which is at the slash before the project name.  This should be the last slash in the project
			// URL since we've already excluded the trailing slash if there was one.
			int gitInsertionPoint = projectURL.LastIndexOf('/', projectURLLength - 1);

			// Just to avoid a crash if they manually specified a bad URL template with no slashes.
			if (gitInsertionPoint == -1)
				{  gitInsertionPoint = projectURLLength;  }

			stringBuilder.Append(projectURL, 0, gitInsertionPoint);
			stringBuilder.Append("/_git");
			stringBuilder.Append(projectURL, gitInsertionPoint, projectURLLength - gitInsertionPoint);
			stringBuilder.Append("?path=/");
			stringBuilder.Append( path.ToURL() );

			if (repository.Branch != null)
				{
				stringBuilder.Append("&version=");

				if (repository.Branch.StartsWith("GB") == false)
					{  stringBuilder.Append("GB");  }

				stringBuilder.Append(repository.Branch);
				}

			stringBuilder.Append("&line=");
			stringBuilder.Append(lineNumber);
			stringBuilder.Append("&lineEnd=");
			stringBuilder.Append(lineNumber + 1);
			stringBuilder.Append("&lineStartColumn=1&lineEndColumn=1&lineStyle=plain&_a=contents");

			return stringBuilder.ToString();
			}


		// Group: Regular Expressions
		// __________________________________________________________________________

		/* Regex: IsNameOrAliasRegex
		 * Will match if the string is "Azure Repos" or an acceptable variation.
		 */
		[GeneratedRegex("""^(?:Azure(?:[ \-]?(?:Repos|Dev[ \-]?Ops))?|Visual Studio Team.*|VSTS)$""",
								  RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
		static internal partial Regex IsNameOrAliasRegex();

		/* Regex: IsSiteURLRegex
		 * Will match if the string is an Azure Repos URL of any kind.
		 */
		[GeneratedRegex("""^https?://(?:dev\.azure\.com|[a-z0-9\-]+\.visualstudio\.com)(?:/|$)""",
								  RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
		static internal partial Regex IsSiteURLRegex();

		/* Regex: IsProjectURLRegex
		 * Will match if the string is an Azure Repos project URL.
		 */
		[GeneratedRegex("""^https?://(?:dev\.azure\.com/[^/?#]+|[a-z0-9\-]+\.visualstudio\.com)/[^/?#]+/?$""",
								  RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
		static internal partial Regex IsProjectURLRegex();

		}
	}
