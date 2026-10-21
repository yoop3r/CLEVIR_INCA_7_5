Option Strict Off
Option Explicit On

Imports System.Xml

''' <summary>
''' Typed config.xml node readers extracted from GmResidentClient.Read{Boolean,String,Integer}Config.
''' Every read reports either "{name} set to: {value}" or "{name} not found, using default: {value}"
''' through <c>log</c>. Malformed values throw (CBool/CInt semantics) exactly as before.
''' </summary>
Friend NotInheritable Class ConfigValueReader

    Private Sub New()
    End Sub

    Friend Shared Function ReadBoolean(parentNode As XmlNode,
                                       nodeName As String,
                                       defaultValue As Boolean,
                                       log As Action(Of String)) As Boolean
        Dim node = parentNode.SelectSingleNode(nodeName)
        If node IsNot Nothing Then
            Dim value As Boolean = CBool(node.InnerText)
            log($"{nodeName} set to: {value}")
            Return value
        End If

        log($"{nodeName} not found, using default: {defaultValue}")
        Return defaultValue
    End Function

    Friend Shared Function ReadString(parentNode As XmlNode,
                                      nodeName As String,
                                      defaultValue As String,
                                      log As Action(Of String)) As String
        Dim node = parentNode.SelectSingleNode(nodeName)
        If node IsNot Nothing Then
            Dim value As String = node.InnerText.Trim()
            log($"{nodeName} set to: {value}")
            Return value
        End If

        log($"{nodeName} not found, using default: {defaultValue}")
        Return defaultValue
    End Function

    Friend Shared Function ReadInteger(parentNode As XmlNode,
                                       nodeName As String,
                                       defaultValue As Integer,
                                       log As Action(Of String)) As Integer
        Dim node = parentNode.SelectSingleNode(nodeName)
        If node IsNot Nothing Then
            Dim value As Integer = CInt(node.InnerText)
            log($"{nodeName} set to: {value}")
            Return value
        End If

        log($"{nodeName} not found, using default: {defaultValue}")
        Return defaultValue
    End Function
End Class
