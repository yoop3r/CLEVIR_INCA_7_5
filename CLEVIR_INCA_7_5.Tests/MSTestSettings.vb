Imports Microsoft.VisualStudio.TestTools.UnitTesting

' Characterization tests set CultureInfo.CurrentCulture explicitly; keep execution sequential so
' culture changes cannot interleave between tests.
<Assembly: DoNotParallelize>
