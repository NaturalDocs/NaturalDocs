/* 
 * Class: GregValure.NaturalDocs.CLI.Threads.Finalizer
 * ____________________________________________________________________________
 * 
 * A thread that implements <Engine.Output.Manager.WorkOnFinalizingOutput()>.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2012 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;


namespace GregValure.NaturalDocs.CLI.Threads
	{
	public class Finalizer : Engine.Thread
		{
		
		public Finalizer (int number) : base ("Finalizer Thread " + number, System.Threading.ThreadPriority.BelowNormal, true)
			{
			}
			

		protected override void Run ()
			{
			Engine.Instance.Output.WorkOnFinalizingOutput(Engine.Delegates.NeverCancel);
			}			
			
		}
	}