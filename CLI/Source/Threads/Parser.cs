/* 
 * Class: GregValure.NaturalDocs.CLI.Threads.Parser
 * ____________________________________________________________________________
 * 
 * A thread that implements <Engine.Files.Manager.WorkOnProcessingChanges()>.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2012 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;


namespace GregValure.NaturalDocs.CLI.Threads
	{
	public class Parser : Engine.Thread
		{
		
		public Parser (int number) : base ("Parser Thread " + number, System.Threading.ThreadPriority.BelowNormal, true)
			{
			}
			

		protected override void Run ()
			{
			Engine.Instance.Files.WorkOnProcessingChanges(Engine.Delegates.NeverCancel);
			}			
			
		}
	}