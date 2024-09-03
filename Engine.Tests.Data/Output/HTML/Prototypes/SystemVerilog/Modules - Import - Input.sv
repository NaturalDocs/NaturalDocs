
// Module: ImportPackage
module ImportPackage
	import PackageName::*;
	();
endmodule

// Module: ImportMember
module ImportMember
	import PackageName::MemberName;
	();
endmodule

// Module: ImportMultiple1
module ImportMultiple1
	import PackageA::*, PackageB::MemberName;
	();
endmodule

// Module: ImportMultiple2
module ImportMultiple2
	import PackageA::*;
	import PackageB::MemberName;
	();
endmodule

// Module: ImportMultiple3
module ImportMultiple3
	import PackageA::*, PackageB::MemberName;
	import PackageC::*;
	();
endmodule
