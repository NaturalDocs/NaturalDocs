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


namespace CodeClear.NaturalDocs.Engine.Repositories
	{
	public class Manager : Module
		{

		// Group: Functions
		// __________________________________________________________________________


		public Manager (Engine.Instance engineInstance) : base (engineInstance)
			{
			templates = null;
			}


		/* Function: Start
		 * Starts the module, returning whether it was successful.  If there were any errors they will be added to errorList.
		 */
		public bool Start (Errors.ErrorList errorList)
			{
			templates = new Template[] {
				new Templates.GitHub(),
				new Templates.GitLab(),
				new Templates.Codeberg(),
				new Templates.Gitea(),
				new Templates.Forgejo(),
				new Templates.Bitbucket(),
				new Templates.AzureRepos()
				};

			return true;
			}


		protected override void Dispose (bool strictRulesApply)
			{
			}


		/* Function: TemplateFromName
		 * Returns a repository <Template> from the passed name or alias, or null if it is unrecognized.
		 */
		public Template TemplateFromName (string name)
			{
			foreach (var template in templates)
				{
				if (template.IsNameOrAlias(name))
					{  return template;  }
				}

			return null;
			}


		/* Function: TemplateFromURL
		 * Returns a repository <Template> associated with the passed URL, or null if it is unrecognized.
		 */
		public Template TemplateFromURL (string url)
			{
			foreach (var template in templates)
				{
				if (template.IsSiteURL(url))
					{  return template;  }
				}

			return null;
			}



		// Group: Variables
		// __________________________________________________________________________

		protected Template[] templates;

		}
	}
