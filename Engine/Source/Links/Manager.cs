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
 *		> newTopicIDsByEndingSymbol -> linksToResolve
 * 
 *		Externally, this class is thread safe.
 *		
 *		Internally, both <newTopicIDsByEndingSymbol> and <linksToResolve> are locked independently.  You may attempt to
 *		acquire the lock for <linksToResolve> while holding <newTopicIDsByEndingSymbol> but not vice versa.
 *		
 *		> newTopicIDsByEndingSymbol = beforeFirstResolve
 *		
 *		<beforeFirstResolve> uses <newTopicIDsByEndingSymbol>'s lock, so locking it locks both.  Kind of ugly, but the
 *		current implementation doesn't warrant requiring a separate lock.
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
		 *		This variable should always be locked with a monitor before using.  If you need to lock <newTopicIDsByEndingSymbol>
		 *		at the same time you must lock that one *before* this one.
		 */
		protected IDObjects.NumberSet linksToResolve;

		/* var: newTopicIDsByEndingSymbol
		 * 
		 * Keeps track of all newly created <Topics>.  The keys are the <Symbols.EndingSymbols> the topics use, and the values
		 * are <IDObjects.NumberSets> of all the topic IDs associated with that ending symbol.  This is used for resolving links.
		 * 
		 * Thread Safety:
		 * 
		 *		This variable should always be locked with a monitor before using.  You may attempt to lock <newTopicIDsByEndingSymbol>
		 *		while holding this lock.
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
		 *		This variable shares a monitor with <newTopicIDsByEndingSymbol> since that's the only variable that's affected.
		 *		Ideally it should have its own monitor but it's not necessary with the current implementation.
		 */
		protected bool beforeFirstResolve;

		}
	}
