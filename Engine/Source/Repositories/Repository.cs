/*
 * Class: CodeClear.NaturalDocs.Engine.Repositories.Repository
 * ____________________________________________________________________________
 *
 * A class representing a single repository.  Not a repository site like GitHub, but a single repository that would be on that
 * or another site.
 *
 */

// This file is part of Natural Docs, which is Copyright © 2003-2026 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;


namespace CodeClear.NaturalDocs.Engine.Repositories
	{
	public class Repository
		{

		// Group: Functions
		// __________________________________________________________________________


		/* Constructor: Repository
		 */
		public Repository (string projectURL, string siteName = null, Template siteTemplate = null, string branch = null, string sourceURLTemplate = null)
			{
			this.siteTemplate = siteTemplate;

			this.siteName = siteName;
			this.projectURL = projectURL;
			this.branch = branch;
			this.sourceURLTemplate = sourceURLTemplate;

			if (sourceURLTemplate != null)
				{
				// Use LastIndexOf instead of IndexOf for efficiency since it's likely to be at the end of the URL
				this.supportsLineNumbers = (sourceURLTemplate.LastIndexOf(URLSubstitutions.LineNumber) != -1);
				}
			else
				{  this.supportsLineNumbers = false;  }

			#if DEBUG
			if (sourceURLTemplate == null && siteTemplate == null)
				{  throw new Exception ("sourceURLTemplate and siteTemplate cannot both be null");  }

			if (siteTemplate != null)
				{
				if (siteTemplate.RequiresSourceURLTemplate && sourceURLTemplate == null)
					{  throw new Exception ("Site template " + siteTemplate.Name + " requires a source URL template.");  }
				else if (siteTemplate.RequiresBranch && branch == null)
					{  throw new Exception ("Site template " + siteTemplate.Name + " requires a branch name.");  }
				}
			#endif
			}


		/* Function: SourceURL
		 * Creates an URL to the passed source file and line number.
		 */
		public string SourceURL (RelativePath path, int lineNumber)
			{
			// If sourceURLTemplate is defined it overrides the site template function
			if (sourceURLTemplate != null)
				{
				string url = sourceURLTemplate;

				url = url.Replace( URLSubstitutions.FilePathString, path.ToURL() );
				url = url.Replace( URLSubstitutions.LineNumberString, lineNumber.ToString() );

				return url;
				}

			else if (siteTemplate != null)
				{
				return siteTemplate.SourceURL(path, lineNumber, this);
				}
			else
				{  throw new InvalidOperationException();  }
			}



		// Group: Properties
		// __________________________________________________________________________


		/* Property: SiteTemplate
		 * The <Template> to use, or null if none.
		 */
		public Template SiteTemplate
			{
			get
				{  return siteTemplate;  }
			}


		/* Property: SiteName
		 * The name of the repository site, such as "GitHub", or null if not known.
		 */
		public string SiteName
			{
			get
				{
				if (siteName != null)
					{  return siteName;  }
				else if (siteTemplate != null)
					{  return siteTemplate.Name;  }
				else
					{  return null;  }
				}
			}


		/* Property: ProjectURL
		 * The URL of the project's page, such as "https://github.com/User/MyProject", or null if not set.
		 */
		public string ProjectURL
			{
			get
				{  return projectURL;  }
			}


		/* Property: Branch
		 * The name of the repository branch to use in file URLs, such as "main", or null if not set.
		 */
		public string Branch
			{
			get
				{  return branch;  }
			}


		/* Property: SupportsLineNumbers
		 * Whether the site supports links to individual line numbers in source file URLs.
		 */
		public bool SupportsLineNumbers
			{
			get
				{
				// If sourceURLTemplate is defined it overrides the site template
				if (sourceURLTemplate != null)
					{  return supportsLineNumbers;  }
				else if (siteTemplate != null)
					{  return siteTemplate.SupportsLineNumbers;  }
				else
					{  return false;  }
				}
			}



		// Group: Variables
		// __________________________________________________________________________


		/* var: siteTemplate
		 * The site <Template> to use, or null if none.
		 */
		protected Template siteTemplate;

		/* var: siteName
		 * The name of the repository site, such as "GitHub", or null if not set.  If set this will override the name in <siteTemplate>.
		 */
		protected string siteName;

		/* var: projectURL
		 * The URL of the project's page, such as "https://github.com/User/MyProject", or null if not set.
		 */
		protected string projectURL;

		/* var: branch
		 * The name of the repository branch to use in file URLs, such as "main", or null if not set.
		 */
		protected string branch;

		/* var: sourceURLTemplate
		 * The URL of a source file with <URLSubstitutions> present, or null if not set.
		 */
		protected string sourceURLTemplate;

		/* var: supportsLineNumbers
		 * If <sourceURLTemplate> is set, whether it contains <URLSubstitutions.LineNumber>.
		 */
		protected bool supportsLineNumbers;

		}
	}
