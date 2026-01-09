/*
 * Class: CodeClear.NaturalDocs.Engine.Config.RepositorySite
 * ____________________________________________________________________________
 *
 * A class storing information about a known repository site.
 *
 */

// This file is part of Natural Docs, which is Copyright © 2003-2026 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Text.RegularExpressions;


namespace CodeClear.NaturalDocs.Engine.Config
	{
	public class RepositorySite
		{

		// Group: Functions
		// __________________________________________________________________________


		/* Function: RepositorySite
		 */
		public RepositorySite (string name, Regex isSiteURLRegex, Regex isProjectURLRegex, string sourceURLTemplate_WithBranch,
										string sourceURLTemplate_WithoutBranch, string exampleProjectURL)
			{
			this.name = name;
			this.isSiteURLRegex = isSiteURLRegex;
			this.isProjectURLRegex = isProjectURLRegex;
			this.sourceURLTemplate_WithBranch = sourceURLTemplate_WithBranch;
			this.sourceURLTemplate_WithoutBranch = sourceURLTemplate_WithoutBranch;
			this.exampleProjectURL = exampleProjectURL;
			}


		/* Function: IsSiteURL
		 * Returns whether the URL is from anywhere on this repository site.
		 */
		public bool IsSiteURL (string url)
			{
			return isSiteURLRegex.IsMatch(url);
			}


		/* Function: IsProjectURL
		 * Returns whether the URL is to a project page on this repository site.
		 */
		public bool IsProjectURL (string url)
			{
			return isProjectURLRegex.IsMatch(url);
			}


		/* Function: SourceURLTemplate
		 * Creates a source URL template with the passed project URL and optionally the branch name as well.  The resulting
		 * URL template will only require relative path and line number substitutions.
		 */
		public string SourceURLTemplate (string projectURL, string branchName = null)
			{

			// Determine which template to use

			string sourceURLTemplate;

			if (branchName != null && sourceURLTemplate_WithBranch != null)
				{  sourceURLTemplate = sourceURLTemplate_WithBranch;  }
			else
				{  sourceURLTemplate = sourceURLTemplate_WithoutBranch;  }

			#if DEBUG
			if (sourceURLTemplate == null)
				{  throw new Exception ("Couldn't create a source URL template.");  }
			#endif


			// If the project URL ends with / and that's also the first character after the substitution point, remove it from the
			// project URL.  This allows project URLs like "https://github.com/NaturalDocs/NaturalDocs/" and templates like
			// "{ProjectURL}/blob/..." to work without creating a double slash.

			if (projectURL[projectURL.Length - 1] == '/')
				{
				#if DEBUG
				if (RepositorySubstitutions.ProjectURL.Length != 1)
					{  throw new Exception ("Assumed RepositorySubstitutions.ProjectURL was a single character.");  }
				#endif

				int projectURLSubstitutionIndex = sourceURLTemplate.IndexOf(RepositorySubstitutions.ProjectURL[0]);

				if (projectURLSubstitutionIndex != -1 &&
					projectURLSubstitutionIndex + 1 < sourceURLTemplate.Length &&
					sourceURLTemplate[projectURLSubstitutionIndex + 1] == '/')
					{
					projectURL = projectURL.Substring(0, projectURL.Length - 1);
					}
				}


			// Perform the substitutions

			sourceURLTemplate = sourceURLTemplate.Replace(RepositorySubstitutions.ProjectURL, projectURL);

			if (branchName != null)
				{  sourceURLTemplate = sourceURLTemplate.Replace(RepositorySubstitutions.Branch, branchName);  }

			return sourceURLTemplate;
			}



		// Group: Properties
		// __________________________________________________________________________


		/* Property: Name
		 * The name of the repository site, such as "GitHub".
		 */
		public string Name
			{
			get
				{  return name;  }
			}


		/* Property: RequiresBranch
		 * Whether <SourceURLTemplate()> can be called without a branch name.
		 */
		public bool RequiresBranch
			{
			get
				{  return (sourceURLTemplate_WithoutBranch == null);  }
			}


		/* Property: ExampleProjectURL
		 * A string to use as an example project URL in error messages, such as "https://github.com/username/projectname/"
		 */
		public string ExampleProjectURL
			{
			get
				{  return exampleProjectURL;  }
			}



		// Group: Variables
		// __________________________________________________________________________


		/* var: name
		 * The name of the repository site, such as "GitHub".
		 */
		protected string name;

		/* var: isSiteURLRegex
		 * A regular expression that matches whether an URL is from anywhere on the repository site.
		 */
		protected Regex isSiteURLRegex;

		/* var: isProjectURLRegex
		 * A regular expression that matches whether an URL is to a project page on the repository site.
		 */
		protected Regex isProjectURLRegex;

		/* var: sourceURLTemplate_WithBranch
		 * A repository source URL template string used when a branch name is defined, or null if branch names don't appear
		 * in URLs.
		 */
		protected string sourceURLTemplate_WithBranch;

		/* var: sourceURLTemplate_WithoutBranch
		 * A repository source URL template string used when a branch name is not defined, or null if branches must appear in
		 * URLs.
		 */
		protected string sourceURLTemplate_WithoutBranch;

		/* var: exampleProjectURL
		 * A string to use as an example project URL in error messages, such as "https://github.com/username/projectname/".
		 */
		protected string exampleProjectURL;

		}
	}
