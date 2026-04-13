/*
 * Class: CodeClear.NaturalDocs.Engine.Repositories.Template
 * ____________________________________________________________________________
 *
 * A template for a particular repository site.
 *
 */

// This file is part of Natural Docs, which is Copyright © 2003-2026 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;


namespace CodeClear.NaturalDocs.Engine.Repositories
	{
	abstract public class Template
		{

		// Group: Types
		// __________________________________________________________________________


		/* Enum: Flags
		 */
		[Flags]
		public enum Flags : byte
			{
			None = 0,

			RequiresSourceURLTemplate = 0x01,  /// If set, the <Repository> source URL template must be defined manually.
																   /// It cannot be generated from the project URL automatically.

			RequiresBranch = 0x02,  /// If set, the <Repository> branch name must be set.  URLs cannot be generated to the
												 /// root branch automatically.

			DoesntSupportLineNumbers = 0x04  /// If set, the URL format for source files doesn't include line numbers.
																 ///
																 /// This is a flag for "doesn't support" instead of "support" so that the default
																 /// when not specified is that it supports them.
			}



		// Group: Functions
		// __________________________________________________________________________


		/* Constructor: Template
		 */
		public Template (string name, string exampleProjectURL, Flags flags = Flags.None)
			{
			this.name = name;
			this.exampleProjectURL = exampleProjectURL;
			this.flags = flags;
			}


		/* Function: IsNameOrAlias
		 *
		 * Whether the passed string matches the site name or one of its aliases.
		 *
		 * Default Implementation:
		 *
		 *		The default implementation does a case-insensitive match against <Name>.  This function can be overridden to
		 *		also match against aliases.
		 */
		virtual public bool IsNameOrAlias (string name)
			{
			return name.Equals(this.Name, StringComparison.InvariantCultureIgnoreCase);
			}


		/* Function: IsSiteURL
		 * Whether the URL is from anywhere on this repository site.  This will only match URLs hosted on the site itself, not those
		 * on self-hosted sites.
		 */
		abstract public bool IsSiteURL (string url);


		/* Function: IsProjectURL
		 * Whether the URL is to a project page on this repository site.  This will only match URLs hosted on the site itself, not those
		 * on self-hosted sites.
		 */
		abstract public bool IsProjectURL (string url);


		/* Function: SourceURL
		 * Creates an URL to the passed source file and line number.
		 */
		abstract public string SourceURL (RelativePath path, int lineNumber, Repository repository);



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

		/* Property: ExampleProjectURL
		 * An example of a valid project URL on the repository site, such as "https://github.com/UserName/ProjectName".
		 */
		public string ExampleProjectURL
			{
			get
				{  return exampleProjectURL;  }
			}

		/* Property: RequiresSourceURLTemplate
		 * Whether this site requires <ProjectConfig.SourceURLTemplate> to be defined because it is not possible for it to be determined
		 * automatically.
		 */
		public bool RequiresSourceURLTemplate
			{
			get
				{  return ((flags & Flags.RequiresSourceURLTemplate) != 0);  }
			}

		/* Property: RequiresBranch
		 * Whether this site requires <ProjectConfig.Branch> to be defined because it is not possible to generate source URLs without it.
		 */
		public bool RequiresBranch
			{
			get
				{  return ((flags & Flags.RequiresBranch) != 0);  }
			}

		/* Property: SupportsLineNumbers
		 * Whether source URLs can include the line number.
		 */
		public bool SupportsLineNumbers
			{
			get
				{  return ((flags & Flags.DoesntSupportLineNumbers) == 0);  }
			}



		// Group: Variables
		// __________________________________________________________________________


		/* var: name
		 * The name of the site.
		 */
		protected string name;

		/* var: exampleProjectURL
		 * An example of a valid project URL on the repository site.
		 */
		protected string exampleProjectURL;

		/* var: flags
		 * The <Flags> describing the site.
		 */
		protected Flags flags;

		}
	}
