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
 *		> linksToResolve
 *		> newTopicIDsByEndingSymbol
 * 
 *		Externally, this class is thread safe.
 *		
 *		Internally, both <linksToResolve> and <newTopicIDsByEndingSymbol> are locked independently with monitors.
 *		Currently the locks do not need to be held simultaneously for any reason so there is no locking order.  That means
 *		do not attempt to lock one while holding the other, period.
 *		
 *		> beforeFirstResolve
 *		
 *		<beforeFirstResolve> is currently only used for optimization in the parsing stage to limit the amount of work to be done
 *		in the resolving stage on a full reparse.  <WorkOnResolvingLinks()> immediately sets it to false before doing any work
 *		and no code in that or later stages references it.  As such it is not governed by a lock since the only race condition it
 *		could introduce would be if a full reparse and link resolving were occuring simultaneously, which they won't.
 *		
 *		If this variable is used in any other way or this assumption is no longer correct, the locking mechanism for this variable
 *		will have to be revisited.
 * 
 */

// This file is part of Natural Docs, which is Copyright © 2003-2017 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using CodeClear.NaturalDocs.Engine.Collections;
using CodeClear.NaturalDocs.Engine.Errors;
using CodeClear.NaturalDocs.Engine.Topics;


namespace CodeClear.NaturalDocs.Engine.Links
	{
	public partial class Manager : Engine.Module, Engine.CodeDB.IChangeWatcher
		{

		public Manager (Engine.Instance engineInstance) : base (engineInstance)
			{
			linksToResolve = new IDObjects.NumberSet();
			newTopicIDsByEndingSymbol = new SafeDictionary<Symbols.EndingSymbol, IDObjects.NumberSet>();

			beforeFirstResolve = true;
			}

		public bool Start (ErrorList errorList)
			{
			// Watch CodeDB for changes
			EngineInstance.CodeDB.AddChangeWatcher(this);

			return true;
			}

		protected override void Dispose (bool strictRulesApply)
			{
			if (!strictRulesApply)
				{
				linksToResolve.Clear();
				newTopicIDsByEndingSymbol.Clear();
				}
			}


		// Group: Variables
		// __________________________________________________________________________

		/* var: linksToResolve
		 * 
		 * The IDs of all the links that need to be resolved, either because they're new or their previous target was deleted.
		 * Note that this is not the complete set of all unresolved links; some links may have previously resolved to nothing
		 * and there may have been no changes made that could affect them.
		 * 
		 * Thread Safety:
		 * 
		 *		This variable should always be locked with a monitor before using.
		 */
		protected IDObjects.NumberSet linksToResolve;

		/* var: newTopicIDsByEndingSymbol
		 * 
		 * Keeps track of all newly created <Topics>.  The keys are the <Symbols.EndingSymbols> the topics use, and the values
		 * are <IDObjects.NumberSets> of all the topic IDs associated with that ending symbol.  This is used for resolving links.
		 * 
		 * Thread Safety:
		 * 
		 *		This variable should always be locked with a monitor before using.
		 * 
		 * Rationale:
		 * 
		 *		When a new <Topic> is created, it might serve as a better definition for existing links.  We don't want to reresolve
		 *		the links as soon as the topic is created because there may be multiple topics that affect the same links and we'd 
		 *		be wasting effort.  Instead we store which topics are new and resolve the links after parsing is complete.
		 *		
		 *		We can't store the <Topic> objects themselves because when doing a non-differential run every topic will be new and 
		 *		we'd end up storing the entire documentation structure in memory.  Instead we store the topic IDs and look up the 
		 *		<Topics> again when it's time to resolve links.
		 *		
		 *		We store them by ending symbol instead of in one NumberSet so that we can reresolve links in batches.  Topics that 
		 *		have the same ending symbol will be candidates for the same group of links, so we can query those topics and links
		 *		into memory, reresolve them all at once, and then move on to the next ending symbol.  If we stored a single NumberSet
		 *		of topic IDs we'd have to handle the topics one by one and query for each topic's links separately.
		 */
		protected SafeDictionary<Symbols.EndingSymbol, IDObjects.NumberSet> newTopicIDsByEndingSymbol;

		/* var: beforeFirstResolve
		 * 
		 * Wheher we haven't called <WorkOnResolvingLinks()> yet this execution.  
		 * 
		 * Thread Safety:
		 * 
		 *		This variable is currently only used for optimization in the parsing stage to limit the amount of work to be done in the
		 *		resolving stage on a full reparse.  <WorkOnResolvingLinks()> immediately sets it to false before doing any work and
		 *		no code in that or later stages references it.  As such it is not governed by a lock since the only race condition it could
		 *		introduce would be if a full reparse and link resolving were occuring simultaneously, which they won't.
		 *		
		 *		If this variable is used in any other way or this assumption is no longer correct, the locking mechanism for this variable
		 *		will have to be revisited.
		 */
		private bool beforeFirstResolve;

		}
	}
