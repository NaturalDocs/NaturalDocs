/* 
 * Class: GregValure.NaturalDocs.CLI.Threads.Builder
 * ____________________________________________________________________________
 * 
 * A thread that implements <Engine.Output.Manager.WorkOnUpdatingOutput()>.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2012 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;


namespace GregValure.NaturalDocs.CLI.Threads
	{
	public class Builder : Engine.Thread
		{
		
		public Builder (int number) : base ("Builder Thread " + number, System.Threading.ThreadPriority.BelowNormal, true)
			{
			}
			

		protected override void Run ()
			{
			Engine.Instance.Output.WorkOnUpdatingOutput(Engine.Delegates.NeverCancel);
			}			
			
		}
	}