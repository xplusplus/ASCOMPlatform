﻿Imports System.Runtime.InteropServices
'-----------------------------------------------------------------------
' <summary>Defines the IAxisRates Interface</summary>
'-----------------------------------------------------------------------
<ComVisible(True), Guid("E39480E6-9DBB-466e-9AA4-9D1B1EA8F849")> _
    Public Interface IAxisRates ' 2B8FD76E-AF7E-4faa-9FAC-4029E96129F4
    Inherits IEnumerable
    ReadOnly Property Count() As Integer
    Sub Dispose()
    'Function GetEnumerator() As System.Collections.IEnumerator
    Default ReadOnly Property Item(ByVal index As Integer) As IRate
End Interface