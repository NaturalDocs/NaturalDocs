/*
 * Class: CodeClear.NaturalDocs.Engine.Repositories.Templates.Codeberg
 * ____________________________________________________________________________
 *
 * A repository site template for Codeberg.
 *
 *
 * Topic: URL Format
 *
 *		Project Page:
 *
 *			> https://codeberg.org/{user}/{project}
 *
 *			For example, https://codeberg.org/FreeBSD/freebsd-ports/
 *
 *
 *		Source Page, With Branch:
 *
 *			> https://codeberg.org/{user}/{project}/src/branch/{branch}/{filepath}#L{linenumber}
 *
 *			For example, https://codeberg.org/FreeBSD/freebsd-ports/src/branch/main/accessibility/orca/Makefile#L33
 *
 *
 *		Source Page, Without Branch:
 *
 *			> https://codeberg.org/{user}/{project}/src/{filepath}#L{linenumber}
 *
 *			You can omit the branch section of the URL and it will forward to the trunk.
 *
 *			For example, https://codeberg.org/FreeBSD/freebsd-ports/src/accessibility/orca/Makefile#L33
 *
 *
 *		Other Notes:
 *
 *			- As of March 2026, Codeberg does not own Codeberg.com.  You must use .org.
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
	public partial class Codeberg : Template
		{

		// Group: Functions
		// __________________________________________________________________________

		public Codeberg () : base ("Codeberg", "https://codeberg.org/UserName/ProjectName")
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

			stringBuilder.Append("src/");

			if (repository.Branch != null)
				{
				stringBuilder.Append("branch/");
				stringBuilder.Append(repository.Branch);
				stringBuilder.Append('/');
				}

			stringBuilder.Append( path.ToURL() );
			stringBuilder.Append("#L");
			stringBuilder.Append(lineNumber);

			return stringBuilder.ToString();
			}


		// Group: Regular Expressions
		// __________________________________________________________________________

		/* Regex: IsNameOrAliasRegex
		 * Will match if the string is "Codeberg" or an acceptable variation.
		 */
		[GeneratedRegex("""^Codeb[eu]rg(?:\.org)?$""",
								  RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
		static internal partial Regex IsNameOrAliasRegex();

		/* Regex: IsSiteURLRegex
		 * Will match if the string is a Codeberg URL of any kind.
		 */
		[GeneratedRegex("""^https?://(?:www\.)?codeberg\.org(?:/|$)""",
								  RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
		static internal partial Regex IsSiteURLRegex();

		/* Regex: IsProjectURLRegex
		 * Will match if the string is a Codeberg project URL.
		 */
		[GeneratedRegex("""^https?://(?:www\.)?codeberg\.org/[^/?#]+/[^/?#]+/?$""",
								  RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
		static internal partial Regex IsProjectURLRegex();

		}
	}
