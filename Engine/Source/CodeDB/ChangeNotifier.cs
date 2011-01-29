/* 
 * Class: GregValure.NaturalDocs.Engine.CodeDB.ChangeNotifier
 * ____________________________________________________________________________
 * 
 * A simple class to show topic change events.  This class only exists if <SHOW_TOPIC_CHANGES> is defined.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2011 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;


#if SHOW_TOPIC_CHANGES

namespace GregValure.NaturalDocs.Engine.CodeDB
	{
	public class ChangeNotifier : IChangeWatcher
		{
		
		public ChangeNotifier ()
			{
			}

		public void OnAddTopic (Topic topic, EventAccessor eventAccessor)
			{
			System.Console.WriteLine("--- Added " + topic.Title + AddPath(topic.FileID));
			}

		public void OnUpdateTopic (Topic oldTopic, int newCommentLineNumber, int newCodeLineNumber, string newBody, EventAccessor eventAccessor)
			{
			System.Console.WriteLine("--- Updated " + oldTopic.Title + AddPath(oldTopic.FileID));
			}

		public void OnDeleteTopic (Topic topic, EventAccessor eventAccessor)
			{
			System.Console.WriteLine("--- Deleted " + topic.Title + AddPath(topic.FileID));
			}
			
		protected string AddPath (int fileID)
			{
			Files.File file =  Engine.Instance.Files.FromID(fileID);
			
			if (file != null)
				{  return " in " + file.FileName.NameWithoutPath;  }
			else
				{  return "";  }
			}

		}
	}
	
#endif