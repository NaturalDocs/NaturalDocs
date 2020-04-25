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

// This file is part of Natural Docs, which is Copyright © 2003-2020 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using CodeClear.NaturalDocs.Engine.CodeDB;
using CodeClear.NaturalDocs.Engine.Errors;
using CodeClear.NaturalDocs.Engine.Topics;


namespace CodeClear.NaturalDocs.Engine.Links
	{
	public partial class Manager : Engine.Module, Engine.CodeDB.IChangeWatcher
		{

		public Manager (Engine.Instance engineInstance) : base (engineInstance)
			{
			// Wait until Start() to create this object because we want to know if we're reparsing everything.
			unprocessedChanges = null;
			}

		public bool Start (ErrorList errorList)
			{
			unprocessedChanges = new UnprocessedChanges( reparsingEverything: EngineInstance.Config.ReparseEverything );

			// Watch CodeDB for changes
			EngineInstance.CodeDB.AddChangeWatcher(this);

			return true;
			}

		protected override void Dispose (bool strictRulesApply)
			{
			}



		// Group: CodeDB.IChangeWatcher Functions
		// __________________________________________________________________________


		public void OnAddTopic (Topic topic, EventAccessor eventAccessor)
			{
			unprocessedChanges.AddTopic(topic);
			}
		

		public void OnUpdateTopic (Topic oldTopic, Topic newTopic, Topic.ChangeFlags changeFlags, EventAccessor eventAccessor)
			{
			}
		

		public void OnDeleteTopic (Topic topic, IDObjects.NumberSet linksAffected, EventAccessor eventAccessor)
			{
			unprocessedChanges.DeleteTopic(topic, linksAffected);
			}

		
		public void OnAddLink (Link link, EventAccessor eventAccessor)
			{
			unprocessedChanges.AddLink(link);
			}

		
		public void OnChangeLinkTarget (Link link, int oldTargetTopicID, int oldTargetClassID, EventAccessor eventAccessor)
			{
			// We're going to be the one causing this event, not responding to it.  No other code should be changing link definitions.
			}

		
		public void OnDeleteLink (Link link, EventAccessor eventAccessor)
			{
			unprocessedChanges.DeleteLink(link);
			}


		public void OnAddImageLink (ImageLink imageLink, CodeDB.EventAccessor eventAccessor)
			{
			// xxx placeholder
			}


		public void OnChangeImageLinkTarget (ImageLink imageLink, int oldTargetFileID, CodeDB.EventAccessor eventAccessor)
			{
			// xxx placeholder
			}


		public void OnDeleteImageLink (ImageLink imageLink, CodeDB.EventAccessor eventAccessor)
			{
			// xxx placeholder
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
