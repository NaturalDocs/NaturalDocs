/*
 * Class: CodeClear.NaturalDocs.Engine.Repositories.Manager
 * ____________________________________________________________________________
 *
 * A module to handle repository sites within Natural Docs.
 *
 */

// This file is part of Natural Docs, which is Copyright © 2003-2026 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Text.RegularExpressions;


namespace CodeClear.NaturalDocs.Engine.Repositories
	{
	public partial class Manager : Module
		{

		// Group: Functions
		// __________________________________________________________________________


		public Manager (Engine.Instance engineInstance) : base (engineInstance)
			{
			knownSites = null;
			}


		/* Function: Start
		 * Starts the module, returning whether it was successful.  If there were any errors they will be added to errorList.
		 */
		public bool Start (Errors.ErrorList errorList)
			{
			var gitHub = new KnownSite (
				name: "GitHub",
				isSiteURLRegex: IsGitHubURLRegex(),
				isProjectURLRegex: IsGitHubProjectURLRegex(),
				sourceURLTemplate_WithBranch: URLSubstitutions.ProjectURL +
																"/blob/" + URLSubstitutions.Branch + "/" +
																URLSubstitutions.FilePath +
																"#L" + URLSubstitutions.LineNumber,
				sourceURLTemplate_WithoutBranch: URLSubstitutions.ProjectURL +
																	"/blob/master/" + // Will redirect to the default branch even if it isn't called "master"
																	URLSubstitutions.FilePath +
																	"#L" + URLSubstitutions.LineNumber,
				exampleProjectURL: "https://github.com/UserName/ProjectName"
				);

			var gitLab = new KnownSite (
				name: "GitLab",
				isSiteURLRegex: IsGitLabURLRegex(),
				isProjectURLRegex: IsGitLabProjectURLRegex(),
				sourceURLTemplate_WithBranch: URLSubstitutions.ProjectURL +
																"/-/blob/" + URLSubstitutions.Branch + "/" +
																URLSubstitutions.FilePath +
																"#L" + URLSubstitutions.LineNumber,
				sourceURLTemplate_WithoutBranch: null, // Not possible to omit the branch or redirect to the default branch
				exampleProjectURL: "https://gitlab.com/UserName/ProjectName"
				);

			var codeberg = new KnownSite (
				name: "Codeberg",
				isSiteURLRegex: IsCodebergURLRegex(),
				isProjectURLRegex: IsCodebergProjectURLRegex(),
				sourceURLTemplate_WithBranch: URLSubstitutions.ProjectURL +
																"/src/branch/" + URLSubstitutions.Branch + "/" +
																URLSubstitutions.FilePath +
																"#L" + URLSubstitutions.LineNumber,
				sourceURLTemplate_WithoutBranch: URLSubstitutions.ProjectURL +
																	"/src/" +
																	URLSubstitutions.FilePath +
																	"#L" + URLSubstitutions.LineNumber,
				exampleProjectURL: "https://codeberg.org/UserName/ProjectName"
				);

			var gitea = new KnownSite (
				name: "Gitea",
				isSiteURLRegex: IsGiteaURLRegex(),
				isProjectURLRegex: IsGiteaProjectURLRegex(),
				sourceURLTemplate_WithBranch: URLSubstitutions.ProjectURL +
																"/src/branch/" + URLSubstitutions.Branch + "/" +
																URLSubstitutions.FilePath +
																"#L" + URLSubstitutions.LineNumber,
				sourceURLTemplate_WithoutBranch: URLSubstitutions.ProjectURL +
																	"/src/" +
																	URLSubstitutions.FilePath +
																	"#L" + URLSubstitutions.LineNumber,
				exampleProjectURL: "https://gitea.com/UserName/ProjectName"
				);

			knownSites = new KnownSite[] {  gitHub, gitLab, codeberg, gitea  };
			return true;
			}


		protected override void Dispose (bool strictRulesApply)
			{
			}


		/* Function: FromName
		 * Returns a <KnownSite> from the passed name, or null if it is unrecognized.
		 */
		public KnownSite FromName (string name)
			{
			foreach (var knownSite in knownSites)
				{
				if (string.Compare(name, knownSite.Name, StringComparison.InvariantCultureIgnoreCase) == 0)
					{  return knownSite;  }
				}

			return null;
			}


		/* Function: FromURL
		 * Returns a <KnownSite> associated with the passed URL, or null if it is unrecognized.
		 */
		public KnownSite FromURL (string url)
			{
			foreach (var knownSite in knownSites)
				{
				if (knownSite.IsSiteURL(url))
					{  return knownSite;  }
				}

			return null;
			}



		// Group: Variables
		// __________________________________________________________________________

		protected KnownSite[] knownSites;



		// Group: Regular Expressions
		// __________________________________________________________________________


		/* Regex: IsGitHubURLRegex
		 * Will match if the string is a GitHub URL of any kind.
		 */
		[GeneratedRegex("""^https?://(?:www\.)?github\.com(?:/|$)""",
								  RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
		static internal partial Regex IsGitHubURLRegex();

		/* Regex: IsGitHubProjectURLRegex
		 * Will match if the string is a GitHub project URL.
		 */
		[GeneratedRegex("""^https?://(?:www\.)?github\.com/[^/?#]+/[^/?#]+/?$""",
								  RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
		static internal partial Regex IsGitHubProjectURLRegex();

		/* Regex: IsGitLabURLRegex
		 * Will match if the string is a GitLab URL of any kind.
		 */
		[GeneratedRegex("""^https?://(?:www\.)?gitlab\.com(?:/|$)""",
								  RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
		static internal partial Regex IsGitLabURLRegex();

		/* Regex: IsGitLabProjectURLRegex
		 * Will match if the string is a GitLab project URL.
		 */
		[GeneratedRegex("""^https?://(?:www\.)?gitlab\.com/[^/?#]+/[^/?#]+/?$""",
								  RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
		static internal partial Regex IsGitLabProjectURLRegex();

		/* Regex: IsCodebergURLRegex
		 * Will match if the string is a Codeberg URL of any kind.
		 */
		[GeneratedRegex("""^https?://(?:www\.)?codeberg\.org(?:/|$)""",
								  RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
		static internal partial Regex IsCodebergURLRegex();

		/* Regex: IsCodebergProjectURLRegex
		 * Will match if the string is a Codeberg project URL.
		 */
		[GeneratedRegex("""^https?://(?:www\.)?codeberg\.org/[^/?#]+/[^/?#]+/?$""",
								  RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
		static internal partial Regex IsCodebergProjectURLRegex();

		/* Regex: IsGiteaURLRegex
		 * Will match if the string is a Gitea URL of any kind.
		 */
		[GeneratedRegex("""^https?://(?:www\.)?gitea\.com(?:/|$)""",
								  RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
		static internal partial Regex IsGiteaURLRegex();

		/* Regex: IsGiteaProjectURLRegex
		 * Will match if the string is a Gitea project URL.
		 */
		[GeneratedRegex("""^https?://(?:www\.)?gitea\.com/[^/?#]+/[^/?#]+/?$""",
								  RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
		static internal partial Regex IsGiteaProjectURLRegex();

		}
	}
