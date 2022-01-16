/*
 * Class: CodeClear.NaturalDocs.Engine.Links.Manager
 * ____________________________________________________________________________
 *
 * A module that manages scoring links.  Links and their targets are still stored in <CodeDB.Manager>, but this handles
 * the logic of determining how well each link and target match and generating scores for them.
 *
 *
 * Multithreading: Thread Safety Notes
 *
 *		Externally, this class is thread safe.
 *
 *		Internally, the only variable is <unprocessedChanges> which is thread safe so it doesn't need protection.
 *
 */

// This file is part of Natural Docs, which is Copyright © 2003-2022 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using CodeClear.NaturalDocs.Engine.Errors;


namespace CodeClear.NaturalDocs.Engine.Links
	{
	public partial class Manager : Engine.Module, CodeDB.IChangeWatcher, Files.IChangeWatcher
		{

		public Manager (Engine.Instance engineInstance) : base (engineInstance)
			{
			// Wait until Start() to create this object because we want to know if we're reparsing everything.
			unprocessedChanges = null;
			}

		public bool Start (ErrorList errorList)
			{
			bool reparsingEverything = EngineInstance.HasIssues( StartupIssues.NeedToStartFresh | StartupIssues.NeedToReparseAllFiles );
			unprocessedChanges = new UnprocessedChanges(reparsingEverything);

			// Watch for changes
			EngineInstance.CodeDB.AddChangeWatcher(this);
			EngineInstance.Files.AddChangeWatcher(this);

			started = true;
			return true;
			}

		protected override void Dispose (bool strictRulesApply)
			{
			}



		// Group: Processes
		// __________________________________________________________________________


		/* Function: CreateResolverProcess
		 * Creates and returns a <Resolver> process for resolving everything in <UnprocessedChanges>.
		 */
		public Links.Resolver CreateResolverProcess ()
			{
			return new Links.Resolver(EngineInstance);
			}



		// Group: Properties
		// __________________________________________________________________________


		/* var: UnprocessedChanges
		 *
		 * Returns the <Links.UnprocessedChanges> of all the unprocessed link changes that have been detected.
		 */
		public UnprocessedChanges UnprocessedChanges
			{
			get
				{  return unprocessedChanges;  }
			}



		// Group: Variables
		// __________________________________________________________________________


		/* var: unprocessedChanges
		 *
		 * All the unprocessed link changes that have been detected.
		 *
		 * Thread Safety:
		 *
		 *		This object is thread safe and can be accessed whenever.
		 */
		protected UnprocessedChanges unprocessedChanges;

		}
	}
