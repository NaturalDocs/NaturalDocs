using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Text.RegularExpressions;

namespace CodeClear.NaturalDocs.Engine.Regex.Config;

public class DocumentPrivates : System.Text.RegularExpressions.Regex {
	public DocumentPrivates() {
		//Error decoding local variables: Signature type sequence must have at least one element.
		pattern = "^(?:document[ -]privates|privates[ -]document)$";
		roptions = RegexOptions.Singleline | RegexOptions.CultureInvariant;
		factory = new DocumentPrivatesFactory411();
		capsize = 1;
		InitializeReferences();
	}
}

internal class DocumentPrivatesFactory411 : RegexRunnerFactory {
	protected override RegexRunner CreateInstance() {
		//Error decoding local variables: Signature type sequence must have at least one element.
		return new DocumentPrivatesRunner411();
	}
}

internal class DocumentPrivatesRunner411 : RegexRunner {
	protected override void Go() {
		string text = runtext;
		int num = runtextstart;
		int num2 = runtextbeg;
		int num3 = runtextend;
		int num4 = runtextpos;
		int[] array = runtrack;
		int num5 = runtrackpos;
		int[] array2 = runstack;
		int num6 = runstackpos;
		array[--num5] = num4;
		array[--num5] = 0;
		array2[--num6] = num4;
		array[--num5] = 1;
		if(num4 <= num2) {
			array[--num5] = num4;
			array[--num5] = 2;
			if(8 <= num3 - num4 && text[num4] == 'd' && text[num4 + 1] == 'o' && text[num4 + 2] == 'c' && text[num4 + 3] == 'u' && text[num4 + 4] == 'm' && text[num4 + 5] == 'e' && text[num4 + 6] == 'n' && text[num4 + 7] == 't' 
				/*&& text[num4 + 8] == 'e' && text[num4 + 9] == 'd'*/) {
				num4 += 8;
				if(num4 < num3 && RegexRunner.CharInClass(text[num4++], "\0\u0004\0 !-.") && 8 <= num3 - num4 && text[num4] == 'p' && text[num4 + 1] == 'r' && text[num4 + 2] == 'i' && text[num4 + 3] == 'v' && text[num4 + 4] == 'a' && text[num4 + 5] == 't' && text[num4 + 6] == 'e' && text[num4 + 7] == 's') {
					num4 += 8;
					goto IL_0340;
				}
			}
		}

		goto IL_039b;
IL_0392:
		runtextpos = num4;
		return;
IL_0339:
		num4 += 8;
		goto IL_0340;
IL_0340:
		if(num4 >= num3 - 1 && (num4 >= num3 || text[num4] == '\n')) {
			int num7 = array2[num6++];
			Capture(0, num7, num4);
			array[--num5] = num7;
			array[--num5] = 3;
			goto IL_0392;
		}

		goto IL_039b;
IL_039b:
		while(true) {
			runtrackpos = num5;
			runstackpos = num6;
			EnsureStorage();
			num5 = runtrackpos;
			num6 = runstackpos;
			array = runtrack;
			array2 = runstack;
			switch(array[num5++]) {
				case 1:
					num6++;
					continue;
				case 2:
					goto IL_040d;
				case 3:
					array2[--num6] = array[num5++];
					Uncapture();
					continue;
			}

			break;
IL_040d:
			num4 = array[num5++];
			if(8 > num3 - num4 || text[num4] != 'p' || text[num4 + 1] != 'r' || text[num4 + 2] != 'i' || text[num4 + 3] != 'v'|| text[num4 + 4] != 'a'|| text[num4 + 5] != 't'|| text[num4 + 6] != 'e'|| text[num4 + 7] != 's') {
				continue;
			}

			num4 += 8;
			if(num4 >= num3 || !RegexRunner.CharInClass(text[num4++], "\0\u0004\0 !-.") || 8>num3-num4 || text[num4]!='d' || text[num4 + 1]!='o' || text[num4 + 2]!='c' || text[num4 + 3]!='u' || text[num4 + 4]!='m' || text[num4 + 5]!='e' || text[num4 + 6]!='n' || text[num4 + 7]!='t') {
				continue;
			}

			goto IL_0339;
		}

		num4 = array[num5++];
		goto IL_0392;
	}

	protected override bool FindFirstChar() {
		if(runtextpos > runtextbeg) {
			runtextpos = runtextend;
			return false;
		}

		return true;
	}

	protected override void InitTrackCount() {
		//Error decoding local variables: Signature type sequence must have at least one element.
		runtrackcount = 5;
	}
}
