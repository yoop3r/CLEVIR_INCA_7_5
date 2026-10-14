Option Strict On

Imports System.IO
Imports System.Xml
Imports System.Text.RegularExpressions

Public NotInheritable Class ProcessorIdentity
    Public ReadOnly Property DeviceName As String
    Public ReadOnly Property VariantName As String
    Public ReadOnly Property Controller As String

    Private Sub New(deviceName As String, variantName As String, controller As String)
        Me.DeviceName = deviceName
        Me.VariantName = variantName
        Me.Controller = controller
    End Sub

    Public Shared Function ParseList(tokens As IEnumerable(Of String), vehicleNumber As String) As ProcessorIdentity()
        Dim processors As New List(Of ProcessorIdentity)
        Dim names As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
        If tokens IsNot Nothing Then
            For Each token As String In tokens
                Dim name As String = If(token, String.Empty).Trim()
                If String.Equals(name, "NA", StringComparison.OrdinalIgnoreCase) Then Continue For
                If Not Regex.IsMatch(name, "\A[A-Za-z0-9][A-Za-z0-9-]*_[A-Za-z0-9][A-Za-z0-9-]*\z") Then
                    Throw New FormatException($"Vehicle '{vehicleNumber}': invalid config.xml <Processors> entry '{name}'. Use Variant_Controller (for example FPM_MCU), or NA for unused slots.")
                End If
                If Not names.Add(name) Then
                    Throw New FormatException($"Vehicle '{vehicleNumber}': duplicate config.xml <Processors> entry '{name}'.")
                End If
                Dim parts As String() = name.Split("_"c)
                processors.Add(New ProcessorIdentity(name, parts(0).ToUpperInvariant(), parts(1).ToUpperInvariant()))
            Next
        End If
        If processors.Count = 0 Then
            Throw New FormatException($"Vehicle '{vehicleNumber}': config.xml <Processors> must contain at least one active Variant_Controller.")
        End If
        Return processors.ToArray()
    End Function

    Public Shared Function ContainsDevice(names As IEnumerable(Of String), deviceName As String) As Boolean
        Return names IsNot Nothing AndAlso names.Any(Function(name) String.Equals(name, deviceName, StringComparison.OrdinalIgnoreCase))
    End Function
End Class

Public NotInheritable Class IncaConfiguration
    Public ReadOnly Property Database As String
    Public ReadOnly Property Workspace As String
    Public ReadOnly Property Experiment As String
    Public ReadOnly Property ExperimentTemplate As String
    Public ReadOnly Property VariableFile As String
    Public ReadOnly Property EtasUserPath As String
    Public ReadOnly Property SelectedVehicle As String
    Public ReadOnly Property Processors As ProcessorIdentity()
    Public ReadOnly Property DataDictionary As String
    Public ReadOnly Property VehicleSpySetup As String
    Public ReadOnly Property AlternateRecordConfig As String

    Public Sub New(document As XmlDocument, baseDirectory As String)
        Dim root As XmlNode = document.SelectSingleNode("/Configuration")
        If root Is Nothing Then Throw New FormatException("Missing config.xml <Configuration> root.")
        Database = RequiredValue(root, "INCADatabase")
        Workspace = RequiredValue(root, "INCAWorkspace")
        Experiment = RequiredValue(root, "INCAExperiment")
        ExperimentTemplate = If(root.SelectSingleNode("INCAExperimentTemplate")?.InnerText, String.Empty).Trim()
        VariableFile = RequiredValue(root, "INCAVariableFile")
        If Not Path.IsPathRooted(VariableFile) Then
            VariableFile = Path.Combine(If(String.IsNullOrEmpty(Path.GetDirectoryName(VariableFile)), Path.Combine(baseDirectory, "SignalLists"), baseDirectory), VariableFile)
        End If
        EtasUserPath = If(root.SelectSingleNode("ETAS_USER_PATH")?.InnerText, String.Empty).Trim()
        DataDictionary = ResolveFile(baseDirectory, RequiredValue(root, "DataDictionary"))
        AlternateRecordConfig = RequiredValue(root, "AlternateRecordConfig")
        VehicleSpySetup = If(root.SelectSingleNode("VehicleSpySetup")?.InnerText, String.Empty).Trim()
        If String.Equals(AlternateRecordConfig, "VehicleSpy", StringComparison.OrdinalIgnoreCase) Then
            VehicleSpySetup = ResolveFile(baseDirectory, RequiredValue(root, "VehicleSpySetup"))
        ElseIf VehicleSpySetup.Length > 0 Then
            VehicleSpySetup = ResolveFile(baseDirectory, VehicleSpySetup)
        End If
        SelectedVehicle = If(root.SelectSingleNode("SelectedVehicleNumber")?.InnerText, "UNDEFINED").Trim()
        If SelectedVehicle.Length = 0 Then SelectedVehicle = "UNDEFINED"
        Dim matches As New List(Of XmlNode)
        If root.SelectNodes("Vehicles/Vehicle").Count = 0 Then
            Throw New FormatException("Missing config.xml <Vehicles>/<Vehicle> entries.")
        End If
        For Each vehicle As XmlNode In root.SelectNodes("Vehicles/Vehicle")
            If String.Equals(vehicle.SelectSingleNode("VehicleNumber")?.InnerText?.Trim(), SelectedVehicle, StringComparison.OrdinalIgnoreCase) Then
                matches.Add(vehicle)
            End If
        Next
        If matches.Count > 1 Then Throw New FormatException($"Duplicate config.xml <VehicleNumber> '{SelectedVehicle}'.")
        Processors = Array.Empty(Of ProcessorIdentity)()
        If matches.Count = 1 AndAlso Not String.Equals(SelectedVehicle, "UNDEFINED", StringComparison.OrdinalIgnoreCase) Then
            Dim text As String = If(matches(0).SelectSingleNode("Processors")?.InnerText, String.Empty)
            Processors = ProcessorIdentity.ParseList(text.Split(","c), SelectedVehicle)
        End If
    End Sub

    Public Function HasSameSessionSettings(other As IncaConfiguration) As Boolean
        If other Is Nothing Then Return False
        Dim values As String() = {Database, Workspace, Experiment, ExperimentTemplate, VariableFile, EtasUserPath, SelectedVehicle, DataDictionary, VehicleSpySetup, AlternateRecordConfig}
        Dim otherValues As String() = {other.Database, other.Workspace, other.Experiment, other.ExperimentTemplate, other.VariableFile, other.EtasUserPath, other.SelectedVehicle, other.DataDictionary, other.VehicleSpySetup, other.AlternateRecordConfig}
        Return values.SequenceEqual(otherValues, StringComparer.OrdinalIgnoreCase) AndAlso
            Processors.Select(Function(p) p.DeviceName).SequenceEqual(other.Processors.Select(Function(p) p.DeviceName), StringComparer.OrdinalIgnoreCase)
    End Function

    Private Shared Function RequiredValue(root As XmlNode, name As String) As String
        Dim value As String = root.SelectSingleNode(name)?.InnerText?.Trim()
        If String.IsNullOrWhiteSpace(value) Then
            Throw New FormatException($"Missing config.xml <{name}>. Set the value in the Configuration Editor and reload settings.")
        End If
        Return value
    End Function

    Private Shared Function ResolveFile(baseDirectory As String, value As String) As String
        Return Path.GetFullPath(If(Path.IsPathRooted(value), value, Path.Combine(baseDirectory, value)))
    End Function

    Public Shared Sub MergeRuntimeSettings(target As XmlDocument, updates As XmlDocument, master As XmlDocument)
        Dim identityNames As New HashSet(Of String)(StringComparer.Ordinal) From {
            "INCADatabase", "INCAWorkspace", "INCAExperiment", "INCAExperimentTemplate", "INCAVariableFile", "ETAS_USER_PATH",
            "Vehicles", "SelectedVehicleNumber", "DataDictionary", "VehicleSpySetup", "AlternateRecordConfig"
        }
        For Each update As XmlElement In updates.DocumentElement.SelectNodes("*")
            If Not identityNames.Contains(update.Name) Then MergeElement(target.DocumentElement, update)
        Next
        For Each name As String In identityNames
            Dim current As XmlNode = target.DocumentElement.SelectSingleNode(name)
            Dim source As XmlNode = master.DocumentElement.SelectSingleNode(name)
            If source Is Nothing Then
                If current IsNot Nothing Then target.DocumentElement.RemoveChild(current)
            ElseIf current Is Nothing Then
                target.DocumentElement.AppendChild(target.ImportNode(source, True))
            Else
                target.DocumentElement.ReplaceChild(target.ImportNode(source, True), current)
            End If
        Next
    End Sub

    Private Shared Sub MergeElement(parent As XmlElement, update As XmlElement)
        Dim current As XmlElement = Nothing
        For Each candidate As XmlElement In parent.SelectNodes(update.Name)
            Dim sameKey As Boolean = True
            For Each key As String In {"id", "position"}
                If update.HasAttribute(key) AndAlso candidate.GetAttribute(key) <> update.GetAttribute(key) Then sameKey = False
            Next
            If sameKey Then
                current = candidate
                Exit For
            End If
        Next
        If current Is Nothing Then
            parent.AppendChild(parent.OwnerDocument.ImportNode(update, True))
            Return
        End If
        For Each attribute As XmlAttribute In update.Attributes
            current.SetAttribute(attribute.Name, attribute.Value)
        Next
        If update.SelectNodes("*").Count > 0 Then
            For Each child As XmlElement In update.SelectNodes("*")
                MergeElement(current, child)
            Next
        ElseIf current.SelectNodes("*").Count = 0 Then
            current.InnerText = update.InnerText
        End If
    End Sub
End Class
