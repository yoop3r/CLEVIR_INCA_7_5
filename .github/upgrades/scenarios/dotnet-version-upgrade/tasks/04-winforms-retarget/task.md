# 04-winforms-retarget: Retarget CLEVIR_INCA_7_5.vbproj to net10.0-windows and resolve WinForms/GDI+ API surface

This is the core TFM upgrade for the WinForms desktop application: retarget from `net48` to `net10.0-windows` with `UseWindowsForms` enabled. The assessment reports 53,910 API issues on this project (45,607 binary-incompatible + 8,300 source-incompatible + 3 behavioral-change), but ~98% are mechanical — Windows Forms (44,958 refs, 83.4%: `Label`, `Button`, `ListBox`, `Control` properties, etc.) and GDI+/`System.Drawing` (7,604 refs, 14.1%: `Font`, `FontStyle`, `ContentAlignment`, `GraphicsUnit`) references that resolve automatically once the project targets `net10.0-windows` with Windows Desktop support — not manual per-call fixes. The "Windows Forms Legacy Controls" bucket (1,431 issues) is a confirmed false positive (see plan Overview) and needs no control-replacement work.

Do **not** remove the class's `MarshalByRefObject` inheritance itself (line 317) as part of this task — that is a separate investigation item tracked in `07-crypto-removal`. If the build surfaces Windows-native API gaps not covered by the WinForms/GDI+ retarget alone, add the Microsoft Windows Compatibility Pack (`Microsoft.Windows.Compatibility`) per the confirmed "Windows Native APIs" option.

**Done when**: `CLEVIR_INCA_7_5.vbproj` targets `net10.0-windows` with `UseWindowsForms` enabled, the dead Remoting lease override is removed from `GM_INCA_Comm.vb`, the project builds with zero errors, and no genuine (non-false-positive) WinForms/GDI+ API incompatibilities remain.

