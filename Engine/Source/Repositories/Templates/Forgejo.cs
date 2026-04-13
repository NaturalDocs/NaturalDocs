/*
 * Class: CodeClear.NaturalDocs.Engine.Repositories.Templates.Forgejo
 * ____________________________________________________________________________
 *
 * A repository site template for Forgejo.
 *
 *
 * Topic: URL Format
 *
 *		Project Page:
 *
 *			> https://{host}/{user}/{project}
 *
 *			Forgejo is only self-hosted.
 *
 *
 *		Source Page, With Branch:
 *
 *			> https://{host}/{user}/{project}/src/branch/{branch}/{filepath}#L{linenumber}
 *
 *
 *		Source Page, Without Branch:
 *
 *			> https://{host}/{user}/{project}/src/{filepath}#L{linenumber}
 *
 *			You can omit the branch section of the URL and presumably it will forward to the trunk like Codeberg and Gitea.
 *
 */

// This file is part of Natural Docs, which is Copyright © 2003-2026 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Text;


namespace CodeClear.NaturalDocs.Engine.Repositories.Templates
	{
	public partial class Forgejo : Template
		{

		// Group: Functions
		// __________________________________________________________________________

		public Forgejo () : base ("Forgejo", "https://forgejo.yoursite.com/UserName/ProjectName")
			{
			}

		override public bool IsSiteURL (string url)
			{
			return false;
			}

		override public bool IsProjectURL (string url)
			{
			return false;
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

		}
	}
