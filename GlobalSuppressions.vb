' Global analyzer suppressions for CLEVIR_INCA_7_5.
'
' Suppressions here are deliberate and reviewed. Prefer a narrowly scoped entry in
' this file over disabling a rule project-wide, so the analyzer keeps working on
' code where it is genuinely useful.

Imports System.Diagnostics.CodeAnalysis

' ─────────────────────────────────────────────────────────────────────────────
' CA1063: All IDisposable types should implement the Dispose pattern correctly.
'
' GmResidentClient does not introduce IDisposable - it inherits it from
' System.Windows.Forms.Form, which already implements the full pattern (a public
' non-virtual Dispose() that calls Dispose(True) and GC.SuppressFinalize(Me)).
' The override in GM_ResidentClient.designer.vb is the sanctioned extension point:
' it guards on 'disposing' before touching managed objects and calls
' MyBase.Dispose(disposing) unconditionally.
'
' Satisfying CA1063 literally (adding a second public Dispose() and another
' GC.SuppressFinalize call) would duplicate the base implementation and can
' interfere with WinForms control lifetime handling. The rule remains active for
' hand-written types that genuinely implement IDisposable, such as
' OxtsNcomInterface and LidarDevice.
' ─────────────────────────────────────────────────────────────────────────────
<Assembly: SuppressMessage("Design", "CA1063:Implement IDisposable Correctly",
                           Justification:="Form-derived type; System.Windows.Forms.Form already implements the Dispose pattern. Only Dispose(Boolean) is overridden.",
                           Scope:="type", Target:="~T:CLEVIR_INCA_7_5.GmResidentClient")>
