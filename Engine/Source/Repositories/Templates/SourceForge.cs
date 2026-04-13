/*
 * Class: CodeClear.NaturalDocs.Engine.Repositories.Templates.SourceForge
 * ____________________________________________________________________________
 *
 * A repository site template for SourceForge.
 *
 *
 *		Project Page:
 *
 *			> https://sourceforge.net/projects/{project}/
 *
 *			For example, https://sourceforge.net/projects/naturaldocs/
 *
 *
 *		Source Page:
 *
 *			SourceForge projects can use Git, Subversion, or other version control systems which makes it impossible to support
 *			with a single template.  For example, Natural Docs 1.52 is still hosted on SourceForge using Subversion and its URLs
 *			look like this:
 *
 *			- https://sourceforge.net/p/naturaldocs/code/HEAD/tree/trunk/NaturalDocs/Modules/NaturalDocs/SymbolTable.pm#l46
 *
 *			Other projects may be using Git which has URLs that look like this:
 *
 *			- https://sourceforge.net/p/infrarecorder/code/ci/master/tree/src/app/core/core.cc#l21
 *
 *			But then some which use Git have multiple repositories and the URLs have an extra level that looks like this:
 *
 *			- https://sourceforge.net/p/mingw/msys-runtime/ci/master/tree/winsup/cygwin/cygrun.c#l31
 *
 *			Since we won't be able to assume one or the other we require the source file template to be manually specified.
 *
 *
 *		Other Notes:
 *
 *			- All Sourceforge.com URLs redirect to their corresponding .net address.
 *
 */

// This file is part of Natural Docs, which is Copyright © 2003-2026 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Text.RegularExpressions;


namespace CodeClear.NaturalDocs.Engine.Repositories.Templates
	{
	public partial class SourceForge : Template
		{

		// Group: Functions
		// __________________________________________________________________________

		public SourceForge () : base ("SourceForge", "https://sourceforge.net/projects/ProjectName", Flags.RequiresSourceURLTemplate)
			{
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

			throw new NotImplementedException();
			}


		// Group: Regular Expressions
		// __________________________________________________________________________

		/* Regex: IsSiteURLRegex
		 * Will match if the string is a SourceForge URL of any kind.  SourceForge.com URLs redirect to .net.
		 */
		[GeneratedRegex("""^https?://(?:www\.)?sourceforge\.(?:net|com)(?:/|$)""",
								  RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
		static internal partial Regex IsSiteURLRegex();

		/* Regex: IsProjectURLRegex
		 * Will match if the string is a SourceForge project URL.  SourceForge.com URLs redirect to .net.
		 */
		[GeneratedRegex("""^https?://(?:www\.)?sourceforge\.(?:net|com)/projects/[^/?#]+/?$""",
								  RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
		static internal partial Regex IsProjectURLRegex();

		}
	}
