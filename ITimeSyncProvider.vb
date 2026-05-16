Option Strict On

Public Interface ITimeSyncProvider
    ReadOnly Property ProviderName As String
    ReadOnly Property LastUpdateUtc As DateTime?

    Sub Start()
    Sub [Stop]()

    Function GetSynchronizedTimestamp() As DateTime
    Function IsSynchronized() As Boolean

    Function GetPtpStatusText() As String
    Function IsPtpSynchronized() As Boolean
    Function GetNtpStatusText() As String
End Interface
