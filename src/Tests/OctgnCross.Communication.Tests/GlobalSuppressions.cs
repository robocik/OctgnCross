﻿
// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "RCS1090:Call 'ConfigureAwait(false)'.", Justification = "It's a library. It should be up to callers to call ConfigureAwait(false)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "RCS1075:Avoid empty catch clause that catches System.Exception.", Justification = "It's a test lib")]
