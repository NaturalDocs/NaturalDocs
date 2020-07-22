/* 
 * Class: CodeClear.NaturalDocs.Engine.Output.Builders.HTML
 * ____________________________________________________________________________
 * 
 */

// This file is part of Natural Docs, which is Copyright © 2003-2020 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using System.Text;
using CodeClear.NaturalDocs.Engine.Collections;
using CodeClear.NaturalDocs.Engine.Languages;
using CodeClear.NaturalDocs.Engine.Links;
using CodeClear.NaturalDocs.Engine.Output.Components;
using CodeClear.NaturalDocs.Engine.Symbols;
using CodeClear.NaturalDocs.Engine.Tokenization;
using CodeClear.NaturalDocs.Engine.Topics;
using CodeClear.NaturalDocs.Engine.CommentTypes;


namespace CodeClear.NaturalDocs.Engine.Output.Builders
	{
	public partial class HTML
		{

		// Group: Functions
		// __________________________________________________________________________
		
		/* Function: BuildSourceFile
		 * Builds an output file based on a source file.  The accessor should NOT hold a lock on the database.  This will also
		 * build the metadata files.
		 */
		protected void BuildSourceFile (int fileID, CodeDB.Accessor accessor, CancelDelegate cancelDelegate)
			{
			#if DEBUG
			if (accessor.LockHeld != CodeDB.Accessor.LockType.None)
				{  throw new Exception ("Shouldn't call BuildSourceFile() when the accessor already holds a database lock.");  }
			#endif

			Output.HTML.Context context = new Output.HTML.Context(this, fileID);
			Components.HTMLTopicPages.File page = new Components.HTMLTopicPages.File(context);

			bool hasTopics = page.Build(accessor, cancelDelegate);

			if (cancelDelegate())
				{  return;  }


			if (hasTopics)
				{
				lock (accessLock)
					{
					if (buildState.SourceFilesWithContent.Add(fileID) == true)
						{  buildState.NeedToBuildMenu = true;;  }
					}
				}
			else
				{
				DeleteOutputFileIfExists(context.OutputFile);
				DeleteOutputFileIfExists(context.ToolTipsFile);
				DeleteOutputFileIfExists(context.SummaryFile);
				DeleteOutputFileIfExists(context.SummaryToolTipsFile);

				lock (accessLock)
					{
					if (buildState.SourceFilesWithContent.Remove(fileID) == true)
						{  buildState.NeedToBuildMenu = true;  }
					}
				}
			}


		/* Function: DeleteOutputFileIfExists
		 * If the passed file exists, deletes it and adds its parent folder to <foldersToCheckForDeletion>.  It's okay for the
		 * output path to be null.
		 */
		public void DeleteOutputFileIfExists (Path outputFile)
			{
			if (outputFile != null && System.IO.File.Exists(outputFile))
				{  
				System.IO.File.Delete(outputFile);

				lock (accessLock)
					{  buildState.FoldersToCheckForDeletion.Add(outputFile.ParentFolder);  }
				}
			}



		// Group: CodeDB.IChangeWatcher Functions
		// __________________________________________________________________________
		
		
		public void OnAddTopic (Topic topic, CodeDB.EventAccessor eventAccessor)
			{
			// If this topic defines a class, it's possible it's now the best definition and thus we have to update the class prototypes
			// of all its parents.  We don't have to worry about updating children because that will be taken care of by OnLinkChange
			// since there would be a class parent link pointing to it.

			IDObjects.NumberSet parentClassIDs = null;
			IDObjects.NumberSet parentClassFileIDs = null;

			if (topic.DefinesClass)
				{
				eventAccessor.GetInfoOnClassParents(topic.ClassID, out parentClassIDs, out parentClassFileIDs);
				}

			lock (accessLock)
				{
				buildState.SourceFilesToRebuild.Add(topic.FileID);

				if (topic.ClassID != 0)
					{  buildState.ClassFilesToRebuild.Add(topic.ClassID);  }

				if (parentClassIDs != null)
					{  buildState.ClassFilesToRebuild.Add(parentClassIDs);  }
				if (parentClassFileIDs != null)
					{  buildState.SourceFilesToRebuild.Add(parentClassFileIDs);  }
				}
			}

		public void OnUpdateTopic (Topic oldTopic, Topic newTopic, Topic.ChangeFlags changeFlags, CodeDB.EventAccessor eventAccessor)
			{
			// We don't care about line number changes.  They don't affect the output.  We also don't care about context
			// changes.  They might affect links but if they do it will be handled in OnChangeLinkTarget().
			changeFlags &= ~(Topic.ChangeFlags.CommentLineNumber | Topic.ChangeFlags.CodeLineNumber |
									   Topic.ChangeFlags.PrototypeContext | Topic.ChangeFlags.BodyContext);

			if (changeFlags == 0)
				{  return;  }

			lock (accessLock)
				{
				buildState.SourceFilesToRebuild.Add(oldTopic.FileID);

				#if DEBUG
				if (newTopic.FileID != oldTopic.FileID)
					{  throw new Exception("Called OnUpdateTopic() with topics that had different file IDs");  }
				#endif

				if (oldTopic.ClassID != 0)
					{  buildState.ClassFilesToRebuild.Add(oldTopic.ClassID);  }
				if (newTopic.ClassID != 0)
					{  buildState.ClassFilesToRebuild.Add(newTopic.ClassID);  }
				}

			// If the summary or prototype changed this means its tooltip changed.  Rebuild any file that contains links 
			// to this topic.
			if ((changeFlags & (Topic.ChangeFlags.Prototype | Topic.ChangeFlags.Summary | 
												Topic.ChangeFlags.LanguageID | Topic.ChangeFlags.CommentTypeID)) != 0)
				{
				IDObjects.NumberSet linkFileIDs, linkClassIDs;
				eventAccessor.GetInfoOnLinksThatResolveToTopicID(oldTopic.TopicID, out linkFileIDs, out linkClassIDs);

				IDObjects.NumberSet oldParentClassIDs = null;
				IDObjects.NumberSet oldParentClassFileIDs = null;
				IDObjects.NumberSet newParentClassIDs = null;
				IDObjects.NumberSet newParentClassFileIDs = null;

				if (oldTopic.DefinesClass)
					{  eventAccessor.GetInfoOnClassParents(oldTopic.ClassID, out oldParentClassIDs, out oldParentClassFileIDs);  }
				if (newTopic.DefinesClass && (newTopic.ClassID != oldTopic.ClassID || oldTopic.DefinesClass == false))
					{  eventAccessor.GetInfoOnClassParents(newTopic.ClassID, out newParentClassIDs, out newParentClassFileIDs);  }

				if (linkFileIDs != null || linkClassIDs != null || 
					oldParentClassIDs != null || oldParentClassFileIDs != null ||
					newParentClassIDs != null || newParentClassFileIDs != null)
					{  
					lock (accessLock)
						{  
						if (linkFileIDs != null)
							{  buildState.SourceFilesToRebuild.Add(linkFileIDs);  }
						if (linkClassIDs != null)
							{  buildState.ClassFilesToRebuild.Add(linkClassIDs);  }
						if (oldParentClassIDs !=  null)
							{  buildState.ClassFilesToRebuild.Add(oldParentClassIDs);  }
						if (oldParentClassFileIDs != null)
							{  buildState.SourceFilesToRebuild.Add(oldParentClassFileIDs);  }
						if (newParentClassIDs !=  null)
							{  buildState.ClassFilesToRebuild.Add(newParentClassIDs);  }
						if (newParentClassFileIDs != null)
							{  buildState.SourceFilesToRebuild.Add(newParentClassFileIDs);  }
						}
					}
				}
			}

		public void OnDeleteTopic (Topic topic, IDObjects.NumberSet linksAffected, CodeDB.EventAccessor eventAccessor)
			{
			// We'll wait for OnChangeLinkTarget to handle linksAffected

			IDObjects.NumberSet parentClassIDs = null;
			IDObjects.NumberSet parentClassFileIDs = null;

			if (topic.DefinesClass)
				{
				eventAccessor.GetInfoOnClassParents(topic.ClassID, out parentClassIDs, out parentClassFileIDs);
				}

			lock (accessLock)
				{
				buildState.SourceFilesToRebuild.Add(topic.FileID);

				if (topic.ClassID != 0)
					{  buildState.ClassFilesToRebuild.Add(topic.ClassID);  }

				if (parentClassIDs != null)
					{  buildState.ClassFilesToRebuild.Add(parentClassIDs);  }
				if (parentClassFileIDs != null)
					{  buildState.SourceFilesToRebuild.Add(parentClassFileIDs);  }
				}
			}

		public void OnAddLink (Link link, CodeDB.EventAccessor eventAccessor)
			{
			// If a class parent link was added we have to rebuild all source files that define that class as the class prototype
			// may have changed.
			IDObjects.NumberSet filesThatDefineClass = null;

			if (link.Type == LinkType.ClassParent)
				{  
				filesThatDefineClass = eventAccessor.GetFileIDsThatDefineClassID(link.ClassID);  

				// If it's resolved we have to rebuild all source files that define the target as well so its children get updated.
				if (link.TargetClassID != 0)
					{
					IDObjects.NumberSet filesThatDefineTarget = eventAccessor.GetFileIDsThatDefineClassID(link.TargetClassID);

					if (filesThatDefineClass == null)
						{  filesThatDefineClass = filesThatDefineTarget;  }
					else if (filesThatDefineTarget != null)
						{  filesThatDefineClass.Add(filesThatDefineTarget);  }
					}
				}

			lock (accessLock)
				{
				// Even if it's not a class parent link we still need to rebuild the source and class files that contain it.  We can't
				// rely on the topic events picking it up because it's possible to change links without changing topics.  How?  By
				// changing a using statement, which causes all the links to change.
				buildState.SourceFilesToRebuild.Add(link.FileID);

				if (link.ClassID != 0)
					{  buildState.ClassFilesToRebuild.Add(link.ClassID);  }

				if (link.Type == LinkType.ClassParent && link.TargetClassID != 0)
					{  buildState.ClassFilesToRebuild.Add(link.TargetClassID);  }

				if (filesThatDefineClass != null)
					{  buildState.SourceFilesToRebuild.Add(filesThatDefineClass);  }
				}
			}
		
		public void OnChangeLinkTarget (Link link, int oldTargetTopicID, int oldTargetClassID, CodeDB.EventAccessor eventAccessor)
			{
			IDObjects.NumberSet filesThatDefineClass = null;

			if (link.Type == LinkType.ClassParent)
				{
				filesThatDefineClass = eventAccessor.GetFileIDsThatDefineClassID(link.ClassID);  

				if (link.TargetClassID != 0)
					{
					IDObjects.NumberSet filesThatDefineTarget = eventAccessor.GetFileIDsThatDefineClassID(link.TargetClassID);

					if (filesThatDefineClass == null)
						{  filesThatDefineClass = filesThatDefineTarget;  }
					else if (filesThatDefineTarget != null)
						{  filesThatDefineClass.Add(filesThatDefineTarget);  }
					}

				if (oldTargetClassID != 0)
					{
					IDObjects.NumberSet filesThatDefineOldTarget = eventAccessor.GetFileIDsThatDefineClassID(oldTargetClassID);

					if (filesThatDefineClass == null)
						{  filesThatDefineClass = filesThatDefineOldTarget;  }
					else if (filesThatDefineOldTarget != null)
						{  filesThatDefineClass.Add(filesThatDefineOldTarget);  }
					}
				}

			lock (accessLock)
				{
				buildState.SourceFilesToRebuild.Add(link.FileID);

				if (link.ClassID != 0)
					{  buildState.ClassFilesToRebuild.Add(link.ClassID);  }

				if (link.Type == LinkType.ClassParent)
					{
					if (link.TargetClassID != 0)
						{  buildState.ClassFilesToRebuild.Add(link.TargetClassID);  }
					if (oldTargetClassID != 0)
						{  buildState.ClassFilesToRebuild.Add(oldTargetClassID);  }
					}

				if (filesThatDefineClass != null)
					{  buildState.SourceFilesToRebuild.Add(filesThatDefineClass);  }
				}

			// If this is a Natural Docs link, see if it appears in the summary for any topics.  This would mean that it appears in
			// these topics' tooltips, so we have to find any links to these topics and rebuild the files those links appear in.

			// Why do we have to do this if links aren't added to tooltips?  Because how it's resolved can affect it's appearance.
			// It will show up as "link" versus "<link>" if it's resolved or not, and "a at b" versus "a" depending on if it resolves to
			// the topic "a at b" or the topic "b".

			// Why don't we do this for type links?  Because unlike Natural Docs links, type links don't change in appearance
			// based on whether they're resolved or not.  Therefore the logic that we don't have to worry about it because links
			// don't appear in tooltips holds true.

			if (link.Type == LinkType.NaturalDocs)
				{
				IDObjects.NumberSet fileIDs, classIDs;
				eventAccessor.GetInfoOnLinksToTopicsWithNDLinkInSummary(link, out fileIDs, out classIDs);

				if (fileIDs != null || classIDs != null)
					{
					lock (accessLock)
						{
						if (fileIDs != null)
							{  buildState.SourceFilesToRebuild.Add(fileIDs);  }
						if (classIDs != null)
							{  buildState.ClassFilesToRebuild.Add(classIDs);  }
						}
					}
				}
			}
		
		public void OnDeleteLink (Link link, CodeDB.EventAccessor eventAccessor)
			{
			IDObjects.NumberSet filesThatDefineClass = null;

			if (link.Type == LinkType.ClassParent)
				{
				filesThatDefineClass = eventAccessor.GetFileIDsThatDefineClassID(link.ClassID);  

				if (link.TargetClassID != 0)
					{
					IDObjects.NumberSet filesThatDefineTarget = eventAccessor.GetFileIDsThatDefineClassID(link.TargetClassID);

					if (filesThatDefineClass == null)
						{  filesThatDefineClass = filesThatDefineTarget;  }
					else if (filesThatDefineTarget != null)
						{  filesThatDefineClass.Add(filesThatDefineTarget);  }
					}
				}

			lock (accessLock)
				{
				buildState.SourceFilesToRebuild.Add(link.FileID);

				if (link.ClassID != 0)
					{  buildState.ClassFilesToRebuild.Add(link.ClassID);  }

				if (link.Type == LinkType.ClassParent && link.TargetClassID != 0)
					{  buildState.ClassFilesToRebuild.Add(link.TargetClassID);  }

				if (filesThatDefineClass != null)
					{  buildState.SourceFilesToRebuild.Add(filesThatDefineClass);  }
				}
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

		}
	}

